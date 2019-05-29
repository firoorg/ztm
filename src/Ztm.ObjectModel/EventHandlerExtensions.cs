using System;
using System.Threading.Tasks;

namespace Ztm.ObjectModel
{
    public static class EventHandlerExtensions
    {
        public static Task InvokeAsync<T>(this EventHandler<T> handler, object sender, T e) where T : AsyncEventArgs
        {
            if (handler == null)
            {
                return Task.CompletedTask;
            }

            handler.Invoke(sender, e);

            return Task.WhenAll(e.BackgroundTasks);
        }
    }
}
