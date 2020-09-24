using OpenQA.Selenium;
using Utilities.Logger;
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Utilities.Driver
{
    public partial class Driver
    {
        private object SafeCapsule<T>(T _action, EventHandler MethodExceptionRaised)
        {
            object result = null;
            try
            {
                if (_action is Func<Driver, T> funcAction)
                    result = funcAction.Invoke(this);
                else if (_action is Action<Driver> action)
                    action.Invoke(this);
                else
                    Log.Write(new NotImplementedException(), "Scenario not implemented in DriverActions.SafeCapsule", Logger.LogEntry.SeverityType.Critical);
                Sleep();
            }
            catch (WebDriverException ex)
            {
                if (MethodExceptionRaised == null && ExceptionRaised == null)
                    Log.Write(ex, "An error occured with the WebDriver (no Driver EventHandler will be raised)", Logger.LogEntry.SeverityType.High);
                else if (MethodExceptionRaised != null && !IsEmptyEventHandler(MethodExceptionRaised))
                {
                    Log.Write($"An error occured with the WebDriver (Driver's specific EventHandler will be raised)");
                    MethodExceptionRaised.Invoke(ex, EventArgs.Empty);
                }
                else if (ExceptionRaised != null && !IsEmptyEventHandler(MethodExceptionRaised))
                {
                    Log.Write($"An error occured with the WebDriver (Driver's generic EventHandler will be raised)");
                    ExceptionRaised.Invoke(ex, EventArgs.Empty);
                }
            }
            return result;
        }

        public bool TryFindElement(Func<Driver, IWebElement> action, out IWebElement output, EventHandler OnMethodExceptionRaised = null)
        {
            output = Safely(action, OnMethodExceptionRaised);
            if (output != null)
                return true;
            else return false;
        }

        public bool TryFindElements(Func<Driver, ReadOnlyCollection<IWebElement>> action, out ReadOnlyCollection<IWebElement> output, EventHandler OnMethodExceptionRaised = null)
        {
            output = Safely(action, OnMethodExceptionRaised);
            if (output != null && output.Count > 0)
                return true;
            else return false;
        }

        public IWebElement Safely(Func<Driver, IWebElement> action, EventHandler OnMethodExceptionRaised = null)
        {
            return (IWebElement)SafeCapsule((object)action, OnMethodExceptionRaised);
        }

        public string Safely(Func<Driver, string> action, EventHandler OnMethodExceptionRaised = null)
        {
            return SafeCapsule((object)action, OnMethodExceptionRaised).ToString();
        }

        public void Safely(Action<Driver> action, EventHandler OnMethodExceptionRaised = null)
        {
            SafeCapsule((object)action, OnMethodExceptionRaised);
        }

        public ReadOnlyCollection<IWebElement> Safely(Func<Driver, ReadOnlyCollection<IWebElement>> action, EventHandler OnMethodExceptionRaised = null)
        {
            return (ReadOnlyCollection<IWebElement>)SafeCapsule((object)action, OnMethodExceptionRaised);
        }

        public void NavigateSafely(string url, int cautiousWaitTime = 0, int navigationTimeOut = 0)
        {
            SafeCapsule(new Action<Driver>((driver) => {
                if (navigationTimeOut > 0)
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromMilliseconds(cautiousWaitTime);
                string lookup = Regex.Match(url, "\\.([A-Za-z]+).").Value.Replace(".", "");
                Url = url;
                driver.Navigate();
                if (!string.IsNullOrEmpty(lookup))
                    while ((string.IsNullOrEmpty(Url) || !Url.Contains(lookup) || ExecuteScript("return document.readyState").ToString() != "complete")
                    && (cancellableTask == null || !cancellableTask.IsCancelled))
                        Sleep(50);
            }), null);
        }

        public void ScrollToElement(IWebElement webElement, int cautiousWaitTime = 0)
        {
            ExecuteScript($"window.scrollTo(0,{webElement.Location.Y - Manage().Window.Size.Height / 2 + webElement.Size.Height})");
            Sleep(cautiousWaitTime);
        }

        public void ScrollInPage(int pixels, int cautiousWaitTime = 0)
        {
            ExecuteScript($"window.scrollBy(0,{pixels})");
            Sleep(cautiousWaitTime);
        }

        public void ScrollToTop(int cautiousWaitTime = 0)
        {
            ScrollInPage(-int.Parse(ExecuteScript($"return document.body.scrollHeight").ToString()), cautiousWaitTime);
        }

        public void ScrollToBottom(int cautiousWaitTime = 0)
        {
            int previousScrollHeight = 0;
            while ((cancellableTask == null) || !cancellableTask.IsCancelled)
            {
                int scrollHeight = int.Parse(ExecuteScript($"return document.body.scrollHeight").ToString());
                if (scrollHeight == previousScrollHeight)
                    break;
                else
                    previousScrollHeight = scrollHeight;
                ScrollInPage(previousScrollHeight, cautiousWaitTime);
            }
        }
    }
}
