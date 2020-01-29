using System.Threading.Channels;

namespace Ztm.Threading
{
    public sealed class ChannelFactory : IChannelFactory
    {
        public Channel<T> Create<T>()
        {
            return Channel.CreateUnbounded<T>();
        }
    }
}
