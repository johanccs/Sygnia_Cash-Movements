using System.ServiceModel;

namespace Sygnia.Wcf.Gateway.Contracts
{
    [ServiceContract(Namespace = "http://sygnia.local/balance")]
    public interface IBalanceService
    {
        [OperationContract]
        [FaultContract(typeof(BalanceFault))]
        BalanceResponse GetBalance(string accountId);
    }
}
