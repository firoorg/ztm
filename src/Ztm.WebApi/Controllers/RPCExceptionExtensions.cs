using NBitcoin.RPC;

namespace Ztm.WebApi.Controllers
{
    public static class RPCExceptionExtensions
    {
        public static bool IsInsufficientFee(this RPCException ex)
        {
            return (int?)ex.RPCResult?.Error?.Code == -212;
        }

        public static bool IsInsufficientToken(this RPCException ex)
        {
            return ex.RPCResult.Error?.Code == RPCErrorCode.RPC_TYPE_ERROR
                && ex.RPCResult.Error?.Message == "Sender has insufficient balance";
        }
    }
}