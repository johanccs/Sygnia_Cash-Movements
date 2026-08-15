using System;
using System.ServiceModel;
using Grpc.Core;
using Sygnia.Wcf.Gateway.Contracts;

namespace Sygnia.Wcf.Gateway
{
    // The gRPC call is expressed as a delegate rather than the concrete generated client so
    // this class stays trivially testable from the net8.0 Sygnia.Tests project without a real
    // channel: Func<accountId, (accountId, balance)>.
    // ServiceHost is constructed with a singleton instance (Program.cs passes `balanceService`
    // directly, needed because it closes over the gRPC client delegate), which WCF requires to
    // be declared explicitly via InstanceContextMode.Single — otherwise ServiceHost.Open() throws.
    // ConcurrencyMode.Multiple: this instance is stateless apart from the readonly delegate
    // field, and the generated gRPC client is thread-safe, so there is no reason to serialize
    // callers behind the default ConcurrencyMode.Single — that would force one GetBalance call
    // (each blocking on a network round trip to the gRPC host) to complete before the next starts.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class BalanceService : IBalanceService
    {
        private readonly Func<string, (string AccountId, string Balance)> getBalance;

        public BalanceService(Func<string, (string AccountId, string Balance)> getBalance)
        {
            this.getBalance = getBalance ?? throw new ArgumentNullException(nameof(getBalance));
        }

        public BalanceResponse GetBalance(string accountId)
        {
            try
            {
                var (id, balance) = getBalance(accountId);
                return new BalanceResponse { AccountId = id, Balance = balance };
            }
            catch (RpcException ex)
            {
                throw new FaultException<BalanceFault>(
                    new BalanceFault { Message = ex.Status.Detail },
                    new FaultReason(ex.Status.Detail));
            }
        }
    }
}
