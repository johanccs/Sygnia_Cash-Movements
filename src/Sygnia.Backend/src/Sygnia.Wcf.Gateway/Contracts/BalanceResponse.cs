using System.Runtime.Serialization;

namespace Sygnia.Wcf.Gateway.Contracts
{
    [DataContract(Namespace = "http://sygnia.local/balance")]
    public class BalanceResponse
    {
        [DataMember]
        public string AccountId { get; set; }

        // Decimal-as-string, matching the gRPC wire format (GetBalanceResponse.balance) —
        // never a double, which corrupts cents.
        [DataMember]
        public string Balance { get; set; }
    }
}
