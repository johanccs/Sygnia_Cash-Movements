using System;
using System.Configuration;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Description;
using Grpc.Net.Client;
using Sygnia.Presentation;
using Sygnia.Wcf.Gateway.Contracts;

namespace Sygnia.Wcf.Gateway
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var grpcHostAddress = ConfigurationManager.AppSettings["GrpcHostAddress"] ?? "https://localhost:7110";
            var netTcpBaseAddress = ConfigurationManager.AppSettings["NetTcpBaseAddress"] ?? "net.tcp://localhost:8090/BalanceService";

            // .NET Framework's default HttpClientHandler doesn't support HTTP/2 trailers,
            // so gRPC needs WinHttpHandler here — the same class of constraint that forces
            // gRPC-Web on the Angular frontend, solved differently on this side.
            var httpHandler = new WinHttpHandler();
            var channel = GrpcChannel.ForAddress(grpcHostAddress, new GrpcChannelOptions { HttpHandler = httpHandler });
            var grpcClient = new MovementService.MovementServiceClient(channel);

            var balanceService = new BalanceService(accountId =>
            {
                var response = grpcClient.GetBalance(new GetBalanceRequest { AccountId = accountId });
                return (response.AccountId, response.Balance);
            });

            using (var host = new ServiceHost(balanceService))
            {
                host.AddServiceEndpoint(typeof(IBalanceService), new NetTcpBinding(SecurityMode.None), netTcpBaseAddress);

                foreach (var behavior in host.Description.Behaviors)
                {
                    if (behavior is ServiceDebugBehavior debug)
                    {
                        debug.IncludeExceptionDetailInFaults = false;
                    }
                }

                host.Description.Behaviors.Add(new ErrorHandlerBehavior());
                host.Open();

                Console.WriteLine($"Sygnia.Wcf.Gateway listening on {netTcpBaseAddress}");
                Console.WriteLine($"Forwarding to gRPC host at {grpcHostAddress}");
                Console.WriteLine("Press Enter to stop.");
                Console.ReadLine();

                host.Close();
            }
        }
    }
}
