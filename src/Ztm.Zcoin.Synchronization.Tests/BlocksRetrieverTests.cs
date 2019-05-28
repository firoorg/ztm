using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        readonly BlocksRetriever blocksRetriever;

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
                this.blocksRetriever = new BlocksRetriever(this.config, this.rpcFactory);
            }
            catch
            {
                this.publisher.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            this.blocksRetriever.Dispose();
            this.publisher.Dispose();
        }

        [Fact]
        public void Constructor_PassNullForConfig_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("config", () => new BlocksRetriever(config: null, rpc: this.rpcFactory));
        }

        [Fact]
        public void Constructor_PassNullForRpc_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("rpc", () => new BlocksRetriever(config: this.config, rpc: null));
        }

        [Fact]
        public async Task Dispose_WhenRunning_ShouldStop()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

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

            Assert.False(this.blocksRetriever.IsRunning);
            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.blocksRetriever.IsRunning);

            // Act.
            this.blocksRetriever.Dispose();

            // Assert.
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());

            Assert.False(this.blocksRetriever.IsRunning);
        }

        [Fact]
        public async Task Dispose_AlreadyStoppedWithCancel_ShouldSuccess()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

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

            Assert.False(this.blocksRetriever.IsRunning);
            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.blocksRetriever.IsRunning);

            await this.blocksRetriever.StopAsync(CancellationToken.None);
            Assert.False(this.blocksRetriever.IsRunning);

            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());

            // Act.
            this.blocksRetriever.Dispose();
        }

        [Fact]
        public Task StartAsync_PassNullForHandler_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "handler",
                () => this.blocksRetriever.StartAsync(handler: null, cancellationToken: CancellationToken.None)
            );
        }

        [Fact]
        public async Task StartAsync_WhenAlreadyDisposed_ShouldThrow()
        {
            this.blocksRetriever.Dispose();

            var ex = await Assert.ThrowsAsync<ObjectDisposedException>(
                () => this.blocksRetriever.StartAsync(this.handler, CancellationToken.None)
            );

            Assert.Equal(this.blocksRetriever.GetType().FullName, ex.ObjectName);
        }

        [Fact]
        public async Task StartAsync_WhenAlreadyStarted_ShouldThrow()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

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

            Assert.False(this.blocksRetriever.IsRunning);
            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.blocksRetriever.IsRunning);

            // Act.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.blocksRetriever.StartAsync(this.handler, CancellationToken.None)
            );
        }

        [Fact]
        public async Task StartAsync_BackgroundTaskError_CallToStopShouldSuccess()
        {
            var hash = uint256.One;
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);
            var error = new Exception();

            // Arrange.
            using (var stoppedEvent = new ManualResetEventSlim())
            {
                this.handler.When(h => h.GetBlockHintAsync(Arg.Any<CancellationToken>())).Do(_ => throw error);

                this.handler.StopAsync(Arg.Any<Exception>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask)
                    .AndDoes(_ => stoppedEvent.Set());

                Assert.False(this.blocksRetriever.IsRunning);
                await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
                Assert.True(this.blocksRetriever.IsRunning);

                stoppedEvent.Wait();

                _ = this.handler.Received(1).StopAsync(error, Arg.Any<CancellationToken>());
                _ = this.handler.Received(0).StopAsync(null, Arg.Any<CancellationToken>());
            }

            // Act.
            Assert.True(this.blocksRetriever.IsRunning);
            await this.blocksRetriever.StopAsync(CancellationToken.None);
            Assert.False(this.blocksRetriever.IsRunning);
        }

        [Fact]
        public async Task StartAsync_BackgroundTaskSuccess_CallToStopShouldSuccess()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            using (var stoppedEvent = new ManualResetEventSlim())
            {
                this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>())
                    .Returns(0);

                this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>())
                    .Returns(block);

                this.handler.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>())
                    .Returns(-1);

                this.handler.StopAsync(Arg.Any<Exception>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask)
                    .AndDoes(_ => stoppedEvent.Set());

                Assert.False(this.blocksRetriever.IsRunning);
                await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
                Assert.True(this.blocksRetriever.IsRunning);

                stoppedEvent.Wait();

                _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());
            }

            // Act.
            Assert.True(this.blocksRetriever.IsRunning);
            await this.blocksRetriever.StopAsync(CancellationToken.None);
            Assert.False(this.blocksRetriever.IsRunning);
        }

        [Fact]
        public async Task StartAsync_GetBlockHintAsyncReturnNegative_ShouldStopBackgroundTask()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            using (var stoppedEvent = new ManualResetEventSlim())
            {
                // Arrange.
                this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>())
                    .Returns(-1);
                this.handler.StopAsync(Arg.Any<Exception>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask)
                    .AndDoes(_ => stoppedEvent.Set());

                // Act.
                await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
                stoppedEvent.Wait();
            }

            // Assert.
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<ZcoinBlock>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_GetBlockHintAsyncReturnTooHeight_ShouldWaitForNewBlockNotification()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(1);
            this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromException<ZcoinBlock>(new RPCException(
                RPCErrorCode.RPC_INVALID_PARAMETER,
                "Block height out of range",
                null
            )));

            // Act.
            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
            await Task.Delay(2000); // 2 seconds should be sufficient for background tasks to enter waiting.

            using (var stoppedEvent = new ManualResetEventSlim())
            {
                this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(block);
                this.handler.ProcessBlockAsync(block, 1, Arg.Any<CancellationToken>()).Returns(-1);
                this.handler.StopAsync(Arg.Any<Exception>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
                {
                    stoppedEvent.Set();
                });

                this.publisher.SendMoreFrame("hashblock").SendFrame(uint256.One.ToString());

                stoppedEvent.Wait();
            }

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(2).GetBlockAsync(1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block, 1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_GetBlockHintAsyncReturnValidHeight_ShouldRetrieveThatBlock()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            using (var stoppedEvent = new ManualResetEventSlim())
            {
                // Arrange.
                this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
                this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block);
                this.handler.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>()).Returns(-1);
                this.handler.StopAsync(Arg.Any<Exception>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
                {
                    stoppedEvent.Set();
                });

                // Act.
                await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);

                stoppedEvent.Wait();
            }

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_ProcessBlockAsyncThrow_ShouldCallProcessErrorAsync()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);
            var error = new Exception();

            using (var stoppedEvent = new ManualResetEventSlim())
            {
                // Arrange.
                this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
                this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block);
                this.handler.When(h => h.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>())).Do(_ => throw error);
                this.handler.StopAsync(Arg.Any<Exception>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
                {
                    stoppedEvent.Set();
                });

                // Act.
                await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);

                stoppedEvent.Wait();
            }

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(error, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(null, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_ProcessBlockAsyncReturnTooHeight_ShouldWaitForNewBlockNotification()
        {
            // Arrange.
            var block1 = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block1.Header.Nonce = 1;
            block1.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            var block2 = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block2.Header.Nonce = 2;
            block2.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
            this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block1);
            this.handler.ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>()).Returns(1);
            this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromException<ZcoinBlock>(new RPCException(
                RPCErrorCode.RPC_INVALID_PARAMETER,
                "Block height out of range",
                null
            )));

            // Act.
            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
            await Task.Delay(2000); // 3 seconds should be sufficient for background tasks to enter waiting.

            using (var stoppedEvent = new ManualResetEventSlim())
            {
                this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(block2);
                this.handler.ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>()).Returns(-1);
                this.handler.StopAsync(Arg.Any<Exception>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
                {
                    stoppedEvent.Set();
                });

                this.publisher.SendMoreFrame("hashblock").SendFrame(block2.GetHash().ToString());

                stoppedEvent.Wait();
            }

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>());
            _ = this.rpc.Received(2).GetBlockAsync(1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(Arg.Is<Exception>(ex => ex != null), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_ProcessBlockAsyncReturnValidHeight_ShouldRetrieveThatBlock()
        {
            var block1 = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block1.Header.Nonce = 1;
            block1.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            var block2 = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block2.Header.Nonce = 2;
            block2.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            using (var stoppedEvent = new ManualResetEventSlim())
            {
                // Arrange.
                this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
                this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block1);
                this.handler.ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>()).Returns(1);
                this.rpc.GetBlockAsync(1, Arg.Any<CancellationToken>()).Returns(block2);
                this.handler.ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>()).Returns(-1);

                this.handler.StopAsync(Arg.Any<Exception>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
                {
                    stoppedEvent.Set();
                });

                // Act.
                await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);

                stoppedEvent.Wait();
            }

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block2, 1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(Arg.Is<Exception>(ex => ex != null), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_ProcessBlockAsyncReturnNegative_ShouldStopBackgroundTask()
        {
            var block1 = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);
            block1.Header.Nonce = 1;
            block1.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            using (var stoppedEvent = new ManualResetEventSlim())
            {
                // Arrange.
                this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
                this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block1);
                this.handler.ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>()).Returns(-1);
                this.handler.StopAsync(Arg.Any<Exception>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
                {
                    stoppedEvent.Set();
                });

                // Act.
                await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);

                stoppedEvent.Wait();
            }

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block1, 0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(Arg.Is<Exception>(ex => ex != null), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StopAsync_WhenSuccess_BackgroundTaskShouldStop()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

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

            Assert.False(this.blocksRetriever.IsRunning);
            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
            Assert.True(this.blocksRetriever.IsRunning);

            // Act.
            await this.blocksRetriever.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(Arg.Is<Exception>(ex => ex != null), Arg.Any<CancellationToken>());

            Assert.False(this.blocksRetriever.IsRunning);
        }

        [Fact]
        public async Task StopAsync_WhenAlreadyDisposed_ShouldThrow()
        {
            // Arrange.
            this.blocksRetriever.Dispose();

            // Act.
            var error = await Assert.ThrowsAsync<ObjectDisposedException>(
                () => this.blocksRetriever.StopAsync(CancellationToken.None)
            );

            // Assert.
            Assert.Equal(this.blocksRetriever.GetType().FullName, error.ObjectName);
        }

        [Fact]
        public Task StopAsync_WhenNotRunning_ShouldThrow()
        {
            return Assert.ThrowsAsync<InvalidOperationException>(() => this.blocksRetriever.StopAsync(CancellationToken.None));
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

            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.blocksRetriever.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpcFactory.Received(0).CreateRpcClientAsync(Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<ZcoinBlock>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(Arg.Is<Exception>(ex => ex != null), Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());

            Assert.False(this.blocksRetriever.IsRunning);
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

            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.blocksRetriever.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(1, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<ZcoinBlock>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(Arg.Is<Exception>(ex => ex != null), Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());

            Assert.False(this.blocksRetriever.IsRunning);
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

            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.blocksRetriever.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpcFactory.Received(1).CreateRpcClientAsync(Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<ZcoinBlock>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(Arg.Is<Exception>(ex => ex != null), Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());

            Assert.False(this.blocksRetriever.IsRunning);
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

            await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);

            // Act.
            await this.blocksRetriever.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).ProcessBlockAsync(Arg.Any<ZcoinBlock>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(Arg.Is<Exception>(ex => ex != null), Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());

            Assert.False(this.blocksRetriever.IsRunning);
        }

        [Fact]
        public async Task StopAsync_AlreadyRunning_ShouldCancelProcessBlockAsync()
        {
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            // Arrange.
            using (var stoppedEvent = new ManualResetEventSlim())
            {
                this.handler.GetBlockHintAsync(Arg.Any<CancellationToken>()).Returns(0);
                this.rpc.GetBlockAsync(0, Arg.Any<CancellationToken>()).Returns(block);
                this.handler.When(h => h.ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>())).Do(call =>
                {
                    var token = call.ArgAt<CancellationToken>(2);

                    token.WaitHandle.WaitOne();
                    token.ThrowIfCancellationRequested();
                });

                await this.blocksRetriever.StartAsync(this.handler, CancellationToken.None);
            }

            // Act.
            await this.blocksRetriever.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.handler.Received(1).GetBlockHintAsync(Arg.Any<CancellationToken>());
            _ = this.rpc.Received(1).GetBlockAsync(0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).ProcessBlockAsync(block, 0, Arg.Any<CancellationToken>());
            _ = this.handler.Received(0).StopAsync(Arg.Is<Exception>(ex => ex != null), Arg.Any<CancellationToken>());
            _ = this.handler.Received(1).StopAsync(null, Arg.Any<CancellationToken>());

            Assert.False(this.blocksRetriever.IsRunning);
        }
    }
}
