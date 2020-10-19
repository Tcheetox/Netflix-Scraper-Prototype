using MediaSkraper.Media;
using OpenQA.Selenium;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Utilities;
using Utilities.Driver;
using Utilities.Logger;
using LogEntry = Utilities.Logger.LogEntry;

namespace MediaSkraper.Scraper
{
    /// <summary>
    /// Two passes are required to scrape Netflix content
    /// (1) Collection of all provider IDs
    /// (2) Scraping of each media page
    /// </summary>
    public class NetflixScraper
    {
        private const int defaultSleepTime = 500; // Adapt this value based on your ISP bandwidth capacity

        private readonly CancellableTask scraperTask;
        private readonly string netflixBaseUrl;
        private readonly ConcurrentBag<IMedia> mediaBag;

        private Driver netflixDriver;
        private HashSet<string> fetchSet;

        public NetflixScraper(ConcurrentBag<IMedia> _mediaBag)
        {
            mediaBag = _mediaBag;
            netflixBaseUrl = ConfigurationManager.AppSettings["NetflixBaseUrl"];
            scraperTask = new CancellableTask((token) =>
                {
                    // Initialize and navigation
                    string cacheDirectory = ConfigurationManager.AppSettings["NetflixCacheDirectory"];
                    if (string.IsNullOrEmpty(cacheDirectory))
                        cacheDirectory = Path.Combine(Environment.CurrentDirectory, "NetflixCache");
                    netflixDriver = new Driver(defaultSleepTime, cacheDirectory, null, scraperTask);
                    netflixDriver.NavigateSafely(netflixBaseUrl);

                    // Login check, skip profile then explore content
                    CheckLogin();
                    SkipProfileSelection();
                    netflixDriver.ScrollToBottom(defaultSleepTime * 3);
                    netflixDriver.ScrollToTop(); // That trick ensure we will collect all the rows

                    // Fetch then scrape
                    CancellationToken cancellationToken = (CancellationToken)token;
                    if (!cancellationToken.IsCancellationRequested)
                        FetchAll(cancellationToken);
                    if (!cancellationToken.IsCancellationRequested && fetchSet.Count > 0)
                        ScrapeAll(cancellationToken);
                    netflixDriver.DisposeOrKill();
                });
            scraperTask.ExceptionRaised += ScraperTask_ExceptionRaised;
        }

        /// <summary>
        /// If login is required, authenticate through Chrome window then press 'ENTER'
        /// This step is ommitted if valid credentials are saved in the Netflix cache folder
        /// </summary>
        private void CheckLogin()
        {
            while(netflixDriver.TryFindElement((driver) => driver.FindElementByClassName("login-content"), out IWebElement output, delegate { }))
            {
                Log.Write($"You must be authenticated to start scraping! Please login then press 'ENTER'.", LogEntry.SeverityType.High);
                Console.ReadLine();
            }
        }

        /// <summary>
        /// If multiple Netflix profiles are available uses the first one by default
        /// </summary>
        private void SkipProfileSelection()
        {
            if (netflixDriver.TryFindElement((driver) => driver.FindElementByCssSelector(".avatar-wrapper > .profile-icon"), out IWebElement output, delegate { }))
                output.SafelyClickAndWait(netflixDriver);
        }

        /// <summary>
        /// Gather all provider IDs available on the page
        /// </summary>
        /// <param name="token"></param>
        private void FetchAll(CancellationToken token)
        {
            Log.Write($"Start fetching Netflix media IDs", LogEntry.SeverityType.High);
            fetchSet = new HashSet<string>();
            if (netflixDriver.TryFindElements((driver) => driver.FindElementsByClassName("rowContainer"), out ReadOnlyCollection<IWebElement> rows))
                for (int row = 0; row < rows.Count; row++)
                {
                    // Emergency exit
                    if (token.IsCancellationRequested)
                        return;
                    // Fetch row
                    Log.Write($"Fetching row ({row + 1}/{rows.Count})");
                    FetchRow(token, rows[row]);
                }
            Log.Write($"End fetching Netflix media IDs", LogEntry.SeverityType.High);
        }

        /// <summary>
        /// Gather provider IDs on a given row
        /// Each row is checked twice
        /// </summary>
        /// <param name="token"></param>
        /// <param name="row"></param>
        private void FetchRow(CancellationToken token, IWebElement row)
        {
            netflixDriver.ScrollToElement(row); // Scroll vertically to element
            IWebElement nextArrow = null;
            
            Dictionary<string, int> rowIds = new Dictionary<string, int>();
            do
            {
                foreach (IWebElement element in row.Safely((webElement) =>
                    webElement.FindElements(By.ClassName("title-card-container"))).Where(x => x.IsEntirelyDisplayed(netflixDriver)))
                {
                    if (token.IsCancellationRequested)
                        return;

                    // Extract provier ID and store it for further scraping
                    string providerId = element.Safely((webElement) => Regex.Match(webElement.FindElement(By.TagName("a")).GetAttribute("href"), "[0-9]+").Value);
                    if (!string.IsNullOrEmpty(providerId))
                    {
                        if (rowIds.TryGetValue(providerId, out int parseView))
                        {
                            rowIds.Remove(providerId);
                            rowIds.Add(providerId, parseView + 1);
                        }
                        else
                        {
                            rowIds.Add(providerId, 1);
                            if (!fetchSet.Contains(providerId))
                            {
                                fetchSet.Add(providerId);
                                Log.Write($"> Media {providerId} found ({fetchSet.Count})", LogEntry.SeverityType.Low);
                            }
                        }
                    }
                }

                // Scroll horizontally or exit method if every media has been parsed twice
                if (rowIds.Count > 0 && rowIds.Min(x => x.Value) == 2)
                    return; // Row is fully parsed -> exit method
                else // Missing title(s) -> keep scrolling horizontally
                {
                    if (nextArrow == null && row.TryFindElements((elem) => elem.FindElements(By.ClassName("handle")), out ReadOnlyCollection<IWebElement> output))
                        nextArrow = output.Last();
                    nextArrow?.SafelyClickAndWait(netflixDriver, defaultSleepTime * 2); 
                }
            }
            while (!token.IsCancellationRequested);
        }

        /// <summary>
        /// Scrape all media previously fetched and stored in fetchSet
        /// </summary>
        /// <param name="token"></param>
        private void ScrapeAll(CancellationToken token)
        {
            Log.Write($"Start scraping {fetchSet.Count} Netflix media", LogEntry.SeverityType.High);
            int i = 0, success = 0, total = fetchSet.Count;
            do
            {
                Scrape(fetchSet.ElementAt(i), out IMedia scrapedMedia);
                if (IsMediaValid(scrapedMedia))
                {
                    mediaBag.Add(scrapedMedia);
                    success++; 
                }
                fetchSet.Remove(scrapedMedia.ProviderId);
                i++;
                if (i >= fetchSet.Count) i = 0;
            }
            while (fetchSet.Count > 0 && !token.IsCancellationRequested);
            Log.Write($"End scraping Netflix media ({success}/{total})", LogEntry.SeverityType.High);
        }

        /// <summary>
        /// Scrape a given media ID
        /// </summary>
        /// <param name="providerId"></param>
        /// <param name="media"></param>
        private void Scrape(string providerId, out IMedia media)
        {
            media = null;
            netflixDriver.NavigateSafely($"https://www.netflix.com/title/{providerId}");
            if (netflixDriver.TryFindElement((driver) => driver.FindElementByClassName("detail-modal"), out IWebElement modal) 
                && modal.Safely((e) => e.FindElement(By.ClassName("duration")).Text.ToLowerInvariant()) is string duration && !string.IsNullOrEmpty(duration))
            {
                // Define media type
                if (duration.Contains("season") || duration.Contains("saison"))
                    media = new Serie { Seasons = duration }; // -> this is serie
                else
                    media = new Movie { Time = ParseDuration(duration) }; // -> this is movie

                // Collect general info
                media.ProviderId = providerId;
                media.Provider = Provider.Netflix;
                media.Name = modal.Safely((e) => e.FindElement(By.ClassName("previewModal--boxart")).GetAttribute("alt"));
                media.Thumbnail = modal.Safely((e) => e.FindElement(By.ClassName("previewModal--boxart")).GetAttribute("src"));
                media.Description = modal.Safely((e) => e.FindElement(By.ClassName("preview-modal-synopsis")).Text);
                media.Age = modal.Safely((e) => e.FindElement(By.ClassName("year")).Text);
                media.Url = $"https://www.netflix.com/watch/{providerId}";

                // Get genres & actors
                if (modal.TryFindElements((e) => e.FindElements(By.CssSelector(".previewModal--detailsMetadata-right .previewModal--tags")), out ReadOnlyCollection<IWebElement>containers))
                    foreach (IWebElement container in containers)
                    {
                        string type = container.GetAttribute("data-uia");
                        if (container.TryFindElements((e) => e.FindElements(By.TagName("a")), out ReadOnlyCollection<IWebElement> tags))
                            foreach (string tag in tags.Select(x => x.Text).Where(y => !string.IsNullOrEmpty(y) && !y.Contains("plus")))
                                if (type == "previewModal--tags-person")
                                    media.Actors.Add(tag.Replace(",", ""));
                                else if (type == "previewModal--tags-genre")
                                    media.Genres.Add(tag.Replace(",", ""));
                    }
            }
        }

        private bool IsMediaValid(IMedia media)
        {
            if (media != null && !string.IsNullOrEmpty(media.Name) && !string.IsNullOrEmpty(media.ProviderId) && !string.IsNullOrEmpty(media.Description) && !string.IsNullOrEmpty(media.Url))
            {
                Log.Write($"> Scraping {media.ProviderId} - {media.Name}" + " [Green{SUCCEEDED}]", LogEntry.SeverityType.Low);
                return true;
            }
            else if (media != null && !string.IsNullOrEmpty(media.Name))
                Log.Write($"> Scraping {media.ProviderId} - {media.Name}" + " [Red{FAILED}]", LogEntry.SeverityType.Low);
            else
                Log.Write("> Scraping unknown item [Red{FAILED}]", LogEntry.SeverityType.Low);
            return false;
        }

        private int ParseDuration(string duration)
        {
            var matches = Regex.Matches(duration, @"([0-9]+)");
            if (matches.Count == 2)
                return int.Parse(matches[0].Value) * 60 + int.Parse(matches[1].Value);
            else if (matches.Count == 1)
                return int.Parse(matches[0].Value);
            else
                throw new NotImplementedException();
        }

        private void ScraperTask_ExceptionRaised(object sender, EventArgs e)
        {
            Log.Write(((CancellableTask)sender).Exception, "Unexpected error with Netflix scraping task", LogEntry.SeverityType.High);
            netflixDriver?.DisposeOrKill();
        }

        public void Start()
        {
            scraperTask.Start();
        }

        public void Stop()
        {
            scraperTask.Stop();
        }

        public void Terminate() 
        {
            scraperTask.Terminate();
        }

        public void Wait()
        {
            scraperTask.Wait();
        }
    }
}

