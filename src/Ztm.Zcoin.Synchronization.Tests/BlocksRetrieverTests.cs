using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using NBitcoin.RPC;
using NetMQ;
using NetMQ.Sockets;
using NSubstitute;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Rpc;

namespace Ztm.Zcoin.Synchronization.Tests
{
    public sealed class BlocksRetrieverTests : IDisposable
    {
        readonly PublisherSocket publisher;
        readonly IConfiguration config;
        readonly IZcoinRpcClient rpc;
        readonly IZcoinRpcClientFactory rpcFactory;
        readonly IBlocksRetrieverHandler handler;
        readonly BlocksRetriever subject;

        public BlocksRetrieverTests()
        {
            // Start ZeroMQ publisher.
            this.publisher = new PublisherSocket();

            try
            {
                var publisherPort = this.publisher.BindRandomPort("tcp://localhost");

                // Mock config.
                var builder = new ConfigurationBuilder();

                builder.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    {"Zcoin:Rpc:Address", "http://127.0.0.1:8888"},
                    {"Zcoin:Rpc:UserName", "root"},
                    {"Zcoin:Rpc:Password", "abc"},
                    {"Zcoin:ZeroMq:Address", "tcp://localhost:" + publisherPort}
                });

                this.config = builder.Build();

                // Mock RPC.
                this.rpc = Substitute.For<IZcoinRpcClient>();

                this.rpcFactory = Substitute.For<IZcoinRpcClientFactory>();
                this.rpcFactory.CreateRpcClientAsync(Arg.Any<CancellationToken>()).Returns(_ => this.rpc);

                // Mock handler.
                this.handler = Substitute.For<IBlocksRetrieverHandler>();

                // Create test subject.
                this.subject = new BlocksRetriever(this.config, this.rpcFactory);
            }
            catch
            {
                this.publisher.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            this.subject.Dispose();
            this.publisher.Dispose();
        }

        [Fact]
        public void Constructor_PassNullForConfig_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("config", () => new BlocksRetriever(null, this.rpcFactory));
        }

        [Fact]
        public void Constructor_PassNullForRpc_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("rpc", () => new BlocksRetriever(this.config, null));
        }

        [Fact]
        public async Task Dispose_WhenRunning_ShouldStop()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(0).ThrowIfCancellationRequested();
                return 0;
            });

            this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(1).ThrowIfCancellationRequested();
                return block;
            });

            this.handler.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(2).ThrowIfCancellationRequested();
                return 0;
            });

            Assert.False(this.subject.IsRunning);
            await this.subject.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.subject.IsRunning);

            // Act.
            this.subject.Dispose();

            // Assert.
            Assert.False(this.subject.IsRunning);
        }

        [Fact]
        public async Task Dispose_AlreadyStoppedWithCancel_ShouldSuccess()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(0).ThrowIfCancellationRequested();
                return 0;
            });

            this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(1).ThrowIfCancellationRequested();
                return block;
            });

            this.handler.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(2).ThrowIfCancellationRequested();
                return 0;
            });

            Assert.False(this.subject.IsRunning);
            await this.subject.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.subject.IsRunning);

            await this.subject.StopAsync(CancellationToken.None);
            Assert.False(this.subject.IsRunning);

            // Act.
            this.subject.Dispose();
        }

        [Fact]
        public Task StartAsync_PassNullForHandler_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "handler",
                () => this.subject.StartAsync(handler: null, cancellationToken: CancellationToken.None)
            );
        }

        [Fact]
        public async Task StartAsync_WhenAlreadyDisposed_ShouldThrow()
        {
            this.subject.Dispose();

            var ex = await Assert.ThrowsAsync<ObjectDisposedException>(
                () => this.subject.StartAsync(this.handler, CancellationToken.None)
            );

            Assert.Equal(this.subject.GetType().FullName, ex.ObjectName);
        }

        [Fact]
        public async Task StartAsync_WhenAlreadyStarted_ShouldThrow()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(0).ThrowIfCancellationRequested();
                return 0;
            });

            this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(1).ThrowIfCancellationRequested();
                return block;
            });

            this.handler.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(2).ThrowIfCancellationRequested();
                return 0;
            });

            Assert.False(this.subject.IsRunning);
            await this.subject.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.subject.IsRunning);

            // Act.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.StartAsync(this.handler, CancellationToken.None)
            );
        }

        [Fact]
        public async Task StartAsync_BackgroundTaskCancelled_CallToStopShouldSuccess()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.When(h => h.GetBlockHintAsync(Arg.Any<CancellationToken>())).Do(_ => throw new OperationCanceledException());

            Assert.False(this.subject.IsRunning);
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.subject.IsRunning);

            await Task.WhenAny(background);

            // Act.
            Assert.True(this.subject.IsRunning);
            await this.subject.StopAsync(CancellationToken.None);
            Assert.False(this.subject.IsRunning);

            background.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task StartAsync_BackgroundTaskError_CallToStopShouldSuccess()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);
            var error = new Exception();

            // Arrange.
            this.handler.When(h => h.GetBlockHintAsync(Arg.Any<CancellationToken>())).Do(_ => throw error);

            Assert.False(this.subject.IsRunning);
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.subject.IsRunning);

            await Task.WhenAny(background);

            // Act.
            Assert.True(this.subject.IsRunning);
            await this.subject.StopAsync(CancellationToken.None);
            Assert.False(this.subject.IsRunning);

            background.IsFaulted.Should().BeTrue();
        }

        [Fact]
        public async Task StartAsync_GetBlockHintAsyncReturnTooHeight_ShouldWaitForNewBlockNotification()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(1);
            this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromException<Block>(new RPCException(
                RPCErrorCode.RPC_INVALID_PARAMETER,
                "Block height out of range",
                null
            )));

            // Act.
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            await Task.Delay(500);

            this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(block);
            this.handler.ProcessBlockAsync(block, 1, Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new OperationCanceledException()));

            this.publisher.SendMoreFrame("hashblock").SendMoreFrame(uint256.One.ToBytes()).SendFrame(BitConverter.GetBytes(0));
            await Task.WhenAny(background);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(2).GetBlockAsync(1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block, 1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_GetBlockHintAsyncReturnValidHeight_ShouldRetrieveThatBlock()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
            this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block);
            this.handler.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new OperationCanceledException()));

            // Act.
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            await Task.WhenAny(background);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_ProcessBlockAsyncReturnTooHeight_ShouldWaitForNewBlockNotification()
        {
            // Arrange.
            var block1 = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block1.Header.Nonce = 1;
            block1.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            var block2 = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block2.Header.Nonce = 2;
            block2.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
            this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block1);
            this.handler.ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>()).Returns(1);
            this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromException<Block>(new RPCException(
                RPCErrorCode.RPC_INVALID_PARAMETER,
                "Block height out of range",
                null
            )));

            // Act.
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            await Task.Delay(500);

            this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(block2);
            this.handler.ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new OperationCanceledException()));

            this.publisher.SendMoreFrame("hashblock").SendMoreFrame(block2.GetHash().ToBytes()).SendFrame(BitConverter.GetBytes(0));
            await Task.WhenAny(background);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>());
            _ = this.rpc.Received(2).GetBlockAsync(1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_ProcessBlockAsyncReturnValidHeight_ShouldRetrieveThatBlock()
        {
            var block1 = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block1.Header.Nonce = 1;
            block1.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            var block2 = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block2.Header.Nonce = 2;
            block2.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
            this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block1);
            this.handler.ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>()).Returns(1);
            this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(block2);
            this.handler.ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new OperationCanceledException()));

            // Act.
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            await Task.WhenAny(background);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StopAsync_WhenSuccess_BackgroundTaskShouldStop()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(0).ThrowIfCancellationRequested();
                return 0;
            });

            this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(1).ThrowIfCancellationRequested();
                return block;
            });

            this.handler.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(2).ThrowIfCancellationRequested();
                return 0;
            });

            Assert.False(this.subject.IsRunning);
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.subject.IsRunning);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            this.subject.IsRunning.Should().BeFalse();
            background.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_WhenAlreadyDisposed_ShouldThrow()
        {
            // Arrange.
            this.subject.Dispose();

            // Act.
            var error = await Assert.ThrowsAsync<ObjectDisposedException>(
                () => this.subject.StopAsync(CancellationToken.None)
            );

            // Assert.
            Assert.Equal(this.subject.GetType().FullName, error.ObjectName);
        }

        [Fact]
        public Task StopAsync_WhenNotRunning_ShouldThrow()
        {
            return Assert.ThrowsAsync<InvalidOperationException>(() => this.subject.StopAsync(CancellationToken.None));
        }

        [Fact]
        public async Task StopAsync_AlreadyRunning_ShouldCancelGetBlockHintAsync()
        {
            // Arrange.
            this.handler.When(h => h.GetBlockHintAsync(Arg.Any<CancellationToken>())).Do(call =>
            {
                var token = call.ArgAt<CancellationToken>(0);

                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
            });

            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpcFactory.Received(0).CreateRpcClientAsync(Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

            this.subject.IsRunning.Should().BeFalse();
            background.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_AlreadyRunning_ShouldCancelWaitForNewBlock()
        {
            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(1);
            this.rpc.When(rpc => rpc.GetBlockAsync(1, Arg.Any<CancellationToken>())).Do(_ => throw new RPCException(
                RPCErrorCode.RPC_INVALID_PARAMETER,
                "Block height out of range",
                null
            ));

            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

            this.subject.IsRunning.Should().BeFalse();
            background.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_AlreadyRunning_ShouldCancelCreateRpcClientAsync()
        {
            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
            this.rpcFactory.When(f => f.CreateRpcClientAsync(Arg.Any<CancellationToken>())).Do(call =>
            {
                var token = call.ArgAt<CancellationToken>(0);

                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
            });

            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpcFactory.Received(1).CreateRpcClientAsync(Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

            this.subject.IsRunning.Should().BeFalse();
            background.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_AlreadyRunning_ShouldCancelGetBlockAsync()
        {
            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
            this.rpc.When(rpc => rpc.GetBlockAsync(0, Arg.Any<CancellationToken>())).Do(call =>
            {
                var token = call.ArgAt<CancellationToken>(1);

                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
            });

            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

            this.subject.IsRunning.Should().BeFalse();
            background.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_AlreadyRunning_ShouldCancelProcessBlockAsync()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
            this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block);
            this.handler.When(h => h.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>())).Do(call =>
            {
                var token = call.ArgAt<CancellationToken>(2);

                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
            });

            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>());

            this.subject.IsRunning.Should().BeFalse();
            background.IsCanceled.Should().BeTrue();
        }
    }
}
