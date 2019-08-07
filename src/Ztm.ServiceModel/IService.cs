using System;
using Ztm.ObjectModel;

namespace Ztm.ServiceModel
{
    /// <summary>
    /// Represent a service.
    /// </summary>
    /// <remarks>
    /// Once a service instance is stopped is cannot start anymore.
    /// </remarks>
    public interface IService : IDisposable
    {
        Exception Exception { get; }

        bool IsRunning { get; }

        event EventHandler<AsyncEventArgs> Stopped;

        event EventHandler<AsyncEventArgs> Started;
    }
}
