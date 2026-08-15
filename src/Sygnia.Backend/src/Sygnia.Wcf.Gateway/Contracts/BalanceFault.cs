using System.Runtime.Serialization;

namespace Sygnia.Wcf.Gateway.Contracts
{
    [DataContract(Namespace = "http://sygnia.local/balance")]
    public class BalanceFault
    {
        [DataMember]
        public string Message { get; set; }
    }
}
