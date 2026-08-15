using System;
using System.ServiceModel;
using Grpc.Core;
using Sygnia.Wcf.Gateway;
using Sygnia.Wcf.Gateway.Contracts;
using Xunit;

namespace Sygnia.Wcf.Gateway.Tests
{
    public class BalanceServiceTests
    {
        [Fact]
        public void GetBalance_KnownAccount_ReturnsMappedBalance()
        {
            var service = new BalanceService(accountId =>
            {
                Assert.Equal("ACC-001", accountId);
                return ("ACC-001", "750.0000");
            });

            var result = service.GetBalance("ACC-001");

            Assert.Equal("ACC-001", result.AccountId);
            Assert.Equal("750.0000", result.Balance);
        }

        [Fact]
        public void GetBalance_RpcThrowsNotFound_ThrowsBalanceFaultWithMessage()
        {
            var service = new BalanceService(accountId =>
                throw new RpcException(new Status(StatusCode.NotFound, "Unknown account 'ACC-999'.")));

            var ex = Assert.Throws<FaultException<BalanceFault>>(() => service.GetBalance("ACC-999"));

            Assert.Equal("Unknown account 'ACC-999'.", ex.Detail.Message);
        }
    }
}
