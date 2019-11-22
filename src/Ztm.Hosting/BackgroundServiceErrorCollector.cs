using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Hosting
{
    public sealed class BackgroundServiceErrorCollector : IBackgroundServiceErrorCollector
    {
        readonly Collection<BackgroundServiceError> errors;

        public BackgroundServiceErrorCollector()
        {
            this.errors = new Collection<BackgroundServiceError>();
        }

        public Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            lock (this.errors)
            {
                this.errors.Add(new BackgroundServiceError(service, exception));
            }

            return Task.CompletedTask;
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
