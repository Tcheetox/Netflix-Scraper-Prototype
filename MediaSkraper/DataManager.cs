using MediaSkraper.Media;
using MediaSkraper.Scraper;
using System;
using System.Collections.Concurrent;

namespace MediaSkraper
{
    public class DataManager : IDisposable
    {
        public ConcurrentBag<IMedia> MediaBag { get; private set; }

        private readonly NetflixScraper netflixScraper;
        private bool disposedValue;

        public DataManager()
        {
            MediaBag = new ConcurrentBag<IMedia>(); // In case we want to multi-thread and scrape from multiple provider at the same time
            netflixScraper = new NetflixScraper(MediaBag);
        }

        public void Scrape()
        {
            netflixScraper.Start();
        }

        public void WaitAll()
        {
            netflixScraper.Wait();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    netflixScraper.Terminate();
                    netflixScraper.Wait();
                }
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
