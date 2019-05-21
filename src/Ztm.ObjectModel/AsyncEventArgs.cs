using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.ObjectModel
{
    public class AsyncEventArgs : EventArgs
    {
        readonly Collection<Task> backgroundTasks;

        public AsyncEventArgs(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;

            this.backgroundTasks = new Collection<Task>();
        }

        public IEnumerable<Task> BackgroundTasks => this.backgroundTasks;

        public CancellationToken CancellationToken { get; }

        public void RegisterBackgroundTask(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            this.backgroundTasks.Add(task);
        }

        public void RegisterBackgroundTask(Func<CancellationToken, Task> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            RegisterBackgroundTask(func(CancellationToken));
        }
    }
}
