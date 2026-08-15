using System;
using System.ServiceModel;
using Grpc.Core;
using Sygnia.Wcf.Gateway;
using Sygnia.Wcf.Gateway.Contracts;
using Xunit;

namespace Sygnia.Wcf.Gateway.Tests
{
    public class BalanceServiceHostingTests : IDisposable
    {
        private readonly ServiceHost host;
        private readonly string address;

        public BalanceServiceHostingTests()
        {
            address = $"net.tcp://localhost:{GetFreePort()}/BalanceService";

            var service = new BalanceService(accountId => accountId == "ACC-001"
                ? ("ACC-001", "750.0000")
                : throw new RpcException(new Status(StatusCode.NotFound, $"Unknown account '{accountId}'.")));

            host = new ServiceHost(service);
            host.AddServiceEndpoint(typeof(IBalanceService), new NetTcpBinding(SecurityMode.None), address);
            host.Description.Behaviors.Add(new ErrorHandlerBehavior());
            host.Open();
        }

        [Fact]
        public void GetBalance_KnownAccount_ReturnsBalanceOverRealChannel()
        {
            var factory = new ChannelFactory<IBalanceService>(new NetTcpBinding(SecurityMode.None), new EndpointAddress(address));
            var proxy = factory.CreateChannel();

            var result = proxy.GetBalance("ACC-001");

            Assert.Equal("ACC-001", result.AccountId);
            Assert.Equal("750.0000", result.Balance);
        }

        [Fact]
        public void GetBalance_UnknownAccount_ThrowsBalanceFaultOverRealChannel()
        {
            var factory = new ChannelFactory<IBalanceService>(new NetTcpBinding(SecurityMode.None), new EndpointAddress(address));
            var proxy = factory.CreateChannel();

            var ex = Assert.Throws<FaultException<BalanceFault>>(() => proxy.GetBalance("ACC-999"));

            Assert.Equal("Unknown account 'ACC-999'.", ex.Detail.Message);
        }

        [Fact]
        public void GetBalance_UnexpectedException_GenericizesFaultWithoutLeakingDetail()
        {
            var sensitiveAddress = $"net.tcp://localhost:{GetFreePort()}/BalanceService";
            var sensitiveService = new BalanceService(accountId =>
                throw new InvalidOperationException("secret connection string: Server=prod-db;Password=hunter2"));
            var sensitiveHost = new ServiceHost(sensitiveService);
            sensitiveHost.AddServiceEndpoint(typeof(IBalanceService), new NetTcpBinding(SecurityMode.None), sensitiveAddress);
            sensitiveHost.Description.Behaviors.Add(new ErrorHandlerBehavior());
            sensitiveHost.Open();

            try
            {
                var factory = new ChannelFactory<IBalanceService>(new NetTcpBinding(SecurityMode.None), new EndpointAddress(sensitiveAddress));
                var proxy = factory.CreateChannel();

                var ex = Assert.Throws<FaultException<BalanceFault>>(() => proxy.GetBalance("ACC-001"));

                Assert.Equal("An unexpected error occurred.", ex.Detail.Message);
                Assert.DoesNotContain("secret", ex.Detail.Message);
                Assert.DoesNotContain("hunter2", ex.Detail.Message);
            }
            finally
            {
                sensitiveHost.Close();
            }
        }

        public void Dispose()
        {
            host.Close();
        }

        private static int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
