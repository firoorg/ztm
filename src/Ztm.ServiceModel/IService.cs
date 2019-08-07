using System;
using Ztm.ObjectModel;

namespace Ztm.ServiceModel
{
    public interface IService : IDisposable
    {
        Exception Exception { get; }

        bool IsRunning { get; }

        event EventHandler<AsyncEventArgs> Stopped;

        event EventHandler<AsyncEventArgs> Started;
    }
}
