using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
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
        readonly Mock<IChainInformationRpc> rpc;
        readonly Mock<IRpcFactory> rpcFactory;
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
                this.rpc = new Mock<IChainInformationRpc>();

                this.rpcFactory = new Mock<IRpcFactory>();
                this.rpcFactory.Setup(f => f.CreateChainInformationRpcAsync(It.IsAny<CancellationToken>()))
                               .ReturnsAsync(this.rpc.Object);

                // Mock handler.
                this.handler = Substitute.For<IBlocksRetrieverHandler>();

                // Create test subject.
                this.subject = new BlocksRetriever(this.config, this.rpcFactory.Object);
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
            Assert.Throws<ArgumentNullException>("config", () => new BlocksRetriever(null, this.rpcFactory.Object));
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

            this.rpc.Setup(r => r.GetBlockAsync(0, It.IsAny<CancellationToken>()))
                    .Callback<int, CancellationToken>((h, c) => c.ThrowIfCancellationRequested())
                    .ReturnsAsync(block);

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

            this.rpc.Setup(r => r.GetBlockAsync(0, It.IsAny<CancellationToken>()))
                    .Callback<int, CancellationToken>((h, c) => c.ThrowIfCancellationRequested())
                    .ReturnsAsync(block);

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

            this.rpc.Setup(r => r.GetBlockAsync(0, It.IsAny<CancellationToken>()))
                    .Callback<int, CancellationToken>((h, c) => c.ThrowIfCancellationRequested())
                    .ReturnsAsync(block);

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
            // Arrange.
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(1);

            this.rpc.SetupSequence(r => r.GetBlockAsync(1, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RPCException(
                        RPCErrorCode.RPC_INVALID_PARAMETER,
                        "Block height out of range",
                        null
                    ))
                    .ReturnsAsync(block);

            // Act.
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            await Task.Delay(500);

            this.handler.ProcessBlockAsync(block, 1, Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new OperationCanceledException()));

            this.publisher.SendMoreFrame("hashblock").SendMoreFrame(uint256.One.ToBytes()).SendFrame(BitConverter.GetBytes(0));
            await Task.WhenAny(background);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            this.rpc.Verify(
                r => r.GetBlockAsync(1, It.IsNotIn(CancellationToken.None)),
                Times.Exactly(2)
            );
            _ = this.handler.Received(1).ProcessBlockAsync(block, 1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_GetBlockHintAsyncReturnValidHeight_ShouldRetrieveThatBlock()
        {
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
            this.rpc.Setup(r => r.GetBlockAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync(block);
            this.handler.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new OperationCanceledException()));

            // Act.
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            await Task.WhenAny(background);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            this.rpc.Verify(r => r.GetBlockAsync(0, It.IsNotIn(CancellationToken.None)), Times.Once());
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

            this.rpc.Setup(r => r.GetBlockAsync(0, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(block1);
            this.rpc.SetupSequence(r => r.GetBlockAsync(1, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RPCException(
                        RPCErrorCode.RPC_INVALID_PARAMETER,
                        "Block height out of range",
                        null
                    ))
                    .ReturnsAsync(block2);

            this.handler.ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>()).Returns(1);

            // Act.
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            await Task.Delay(500);

            this.handler.ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new OperationCanceledException()));

            this.publisher.SendMoreFrame("hashblock").SendMoreFrame(block2.GetHash().ToBytes()).SendFrame(BitConverter.GetBytes(0));
            await Task.WhenAny(background);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            this.rpc.Verify(r => r.GetBlockAsync(0, It.IsNotIn(CancellationToken.None)), Times.Once());
            _ = this.handler.Received(1).ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>());
            this.rpc.Verify(r => r.GetBlockAsync(1, It.IsNotIn(CancellationToken.None)), Times.Exactly(2));
            _ = this.handler.Received(1).ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_ProcessBlockAsyncReturnValidHeight_ShouldRetrieveThatBlock()
        {
            // Arrange.
            var block1 = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block1.Header.Nonce = 1;
            block1.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            var block2 = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block2.Header.Nonce = 2;
            block2.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
            this.rpc.Setup(r => r.GetBlockAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync(block1);
            this.handler.ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>()).Returns(1);
            this.rpc.Setup(r => r.GetBlockAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(block2);
            this.handler.ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new OperationCanceledException()));

            // Act.
            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);
            await Task.WhenAny(background);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            this.rpc.Verify(r => r.GetBlockAsync(0, It.IsNotIn(CancellationToken.None)), Times.Once());
            _ = this.handler.Received(1).ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>());
            this.rpc.Verify(r => r.GetBlockAsync(1, It.IsNotIn(CancellationToken.None)), Times.Once());
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

            this.rpc.Setup(r => r.GetBlockAsync(0, It.IsAny<CancellationToken>()))
                    .Callback<int, CancellationToken>((h, c) => c.ThrowIfCancellationRequested())
                    .ReturnsAsync(block);

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
            this.rpcFactory.Verify(r => r.CreateChainInformationRpcAsync(It.IsAny<CancellationToken>()), Times.Never());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

            this.subject.IsRunning.Should().BeFalse();
            background.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_AlreadyRunning_ShouldCancelWaitForNewBlock()
        {
            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(1);

            this.rpc.Setup(r => r.GetBlockAsync(1, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RPCException(
                        RPCErrorCode.RPC_INVALID_PARAMETER,
                        "Block height out of range",
                        null
                    ));

            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            this.rpc.Verify(r => r.GetBlockAsync(1, It.IsNotIn(CancellationToken.None)), Times.Once());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

            this.subject.IsRunning.Should().BeFalse();
            background.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_AlreadyRunning_ShouldCancelCreateRpcClientAsync()
        {
            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);

            this.rpcFactory.Setup(f => f.CreateChainInformationRpcAsync(It.IsAny<CancellationToken>()))
                           .Callback<CancellationToken>(c =>
                           {
                               c.WaitHandle.WaitOne();
                               c.ThrowIfCancellationRequested();
                           });

            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            this.rpcFactory.Verify(f => f.CreateChainInformationRpcAsync(It.IsNotIn(CancellationToken.None)), Times.Once());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

            this.subject.IsRunning.Should().BeFalse();
            background.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_AlreadyRunning_ShouldCancelGetBlockAsync()
        {
            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);

            this.rpc.Setup(r => r.GetBlockAsync(0, It.IsAny<CancellationToken>()))
                    .Callback<int, CancellationToken>((h, c) =>
                    {
                        c.WaitHandle.WaitOne();
                        c.ThrowIfCancellationRequested();
                    });

            var background = await this.subject.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            this.rpc.Verify(r => r.GetBlockAsync(0, It.IsNotIn(CancellationToken.None)), Times.Once());
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
            this.rpc.Setup(r => r.GetBlockAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync(block);
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
            this.rpc.Verify(r => r.GetBlockAsync(0, It.IsNotIn(CancellationToken.None)), Times.Once());
            _ = this.handler.Received(1).ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>());

            this.subject.IsRunning.Should().BeFalse();
            background.IsCanceled.Should().BeTrue();
        }
    }
}
