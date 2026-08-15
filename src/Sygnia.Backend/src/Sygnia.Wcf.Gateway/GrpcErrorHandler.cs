using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Sygnia.Wcf.Gateway.Contracts;

namespace Sygnia.Wcf.Gateway
{
    // One global error handler per transport (CLAUDE.md coding standards): anything that
    // escapes BalanceService.GetBalance uncaught is logged and turned into a clean
    // FaultException<BalanceFault> here, never a raw exception on the wire.
    public class GrpcErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            Console.Error.WriteLine($"[Sygnia.Wcf.Gateway] Unhandled error: {error}");
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref System.ServiceModel.Channels.Message fault)
        {
            // A FaultException<BalanceFault> already carries the deliberate, correctly-mapped
            // detail (e.g. "Unknown account 'ACC-999'.") thrown by BalanceService itself — pass
            // it through unchanged. Only truly unexpected exceptions get genericized here, so
            // internals never leak onto the wire (CLAUDE.md: "Details go to the log, never to
            // the wire").
            var faultException = error is System.ServiceModel.FaultException<BalanceFault> knownFault
                ? knownFault
                : new System.ServiceModel.FaultException<BalanceFault>(
                    new BalanceFault { Message = "An unexpected error occurred." },
                    new System.ServiceModel.FaultReason("An unexpected error occurred."));

            var faultMessageFault = faultException.CreateMessageFault();
            fault = System.ServiceModel.Channels.Message.CreateMessage(version, faultMessageFault, faultException.Action);
        }
    }
}
