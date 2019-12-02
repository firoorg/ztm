using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Hosting
{
    public sealed class BackgroundServiceErrorCollector :
        BackgroundServiceExceptionHandler,
        IBackgroundServiceErrorCollector
    {
        readonly Collection<BackgroundServiceError> errors;

        public BackgroundServiceErrorCollector()
        {
            this.errors = new Collection<BackgroundServiceError>();
        }

        public IEnumerator<BackgroundServiceError> GetEnumerator()
        {
            lock (this.errors)
            {
                foreach (var item in this.errors)
                {
                    yield return item;
                }
            }
        }

        protected override Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken)
        {
            lock (this.errors)
            {
                this.errors.Add(new BackgroundServiceError(service, exception));
            }

            return Task.CompletedTask;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
