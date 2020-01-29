using System.Threading.Channels;

namespace Ztm.Threading
{
    public interface IChannelFactory
    {
        Channel<T> Create<T>();
    }
}
