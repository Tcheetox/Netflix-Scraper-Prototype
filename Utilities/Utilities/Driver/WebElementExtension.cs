using OpenQA.Selenium;
using System;
using System.Collections.ObjectModel;
using Utilities.Logger;

namespace Utilities.Driver
{
    public static class WebElementExtension
    {
        private static object SafeCapsule(this IWebElement webElement, object _action, EventHandler OnMethodExceptionRaised)
        {
            object result = null;
            try
            {
                if (_action is Func<IWebElement, string> funcStringAction)
                    result = funcStringAction.Invoke(webElement);
                else if (_action is Func<IWebElement, IWebElement> funcIWebElementAction)
                    result = funcIWebElementAction.Invoke(webElement);
                else if (_action is Func<IWebElement, ReadOnlyCollection<IWebElement>> funcCollectionAction)
                    result = funcCollectionAction.Invoke(webElement);
                else if (_action is Action<IWebElement> action)
                    action.Invoke(webElement);
                else
                    Log.Write(new NotImplementedException(), "Scenario not implemented in WebElementExtension.SafeCapsule", Logger.LogEntry.SeverityType.Critical);
            }
            catch (WebDriverException ex)
            {
                if (OnMethodExceptionRaised != null)
                {
                    Log.Write($"An error occured within the WebElement (Element's specific EventHandler will be raised)", Logger.LogEntry.SeverityType.Low);
                    OnMethodExceptionRaised.Invoke(ex, EventArgs.Empty);
                }
                else throw;
            }
            return result;
        }

        public static bool TryFindElements(this IWebElement webElement, Func<IWebElement, ReadOnlyCollection<IWebElement>> action, out ReadOnlyCollection<IWebElement> output, EventHandler OnMethodExceptionRaised = null)
        {
            output = (ReadOnlyCollection <IWebElement>)webElement.SafeCapsule(action, OnMethodExceptionRaised);
            if (output != null && output.Count > 0)
                return true;
            else return false;
        }

        public static bool TryFindElement(this IWebElement webElement, Func<IWebElement, IWebElement> action, out IWebElement output, EventHandler OnMethodExceptionRaised = null)
        {
            output = (IWebElement)webElement.SafeCapsule(action, OnMethodExceptionRaised);
            if (output != null)
                return true;
            else return false;
        }

        public static ReadOnlyCollection<IWebElement> Safely(this IWebElement webElement, Func<IWebElement, ReadOnlyCollection<IWebElement>> action, EventHandler OnMethodExceptionRaised = null)
        {
            return (ReadOnlyCollection <IWebElement>)webElement.SafeCapsule((object)action, OnMethodExceptionRaised);
        }

        public static IWebElement Safely(this IWebElement webElement, Func<IWebElement, IWebElement> action, EventHandler OnMethodExceptionRaised = null)
        {
            return (IWebElement)webElement.SafeCapsule((object)action, OnMethodExceptionRaised);
        }

        public static string Safely(this IWebElement webElement, Func<IWebElement, string> action, EventHandler OnMethodExceptionRaised = null)
        {
            return webElement.SafeCapsule((object)action, OnMethodExceptionRaised).ToString();
        }

        public static void Safely(this IWebElement webElement, Action<IWebElement> action, EventHandler OnMethodExceptionRaised = null)
        {
            webElement.SafeCapsule((object)action, OnMethodExceptionRaised);
        }

        public static void SafelyClickAndWait(this IWebElement webElement, Driver driver, int cautiousWaitTime = 0, EventHandler OnMethodExceptionRaised = null)
        {
            webElement.SafeCapsule(new Action<IWebElement>((element) => {
                element.Click();
                driver.Sleep(cautiousWaitTime);
            }), OnMethodExceptionRaised);
        }

        public static bool IsEntirelyDisplayed(this IWebElement webElement, Driver driver, EventHandler OnMethodExceptionRaised = null)
        {
            bool result = false;
            webElement.SafeCapsule(new Action<IWebElement>((e) => {
                var driverWindow = driver.Manage().Window;
                if (e.Displayed && e.Location.X > 0 && e.Location.Y > 0
                    && e.Size.Width + e.Location.X <= driverWindow.Size.Width + int.Parse(driver.ExecuteScript("return window.pageXOffset;").ToString())
                    && e.Size.Height + e.Location.Y <= driverWindow.Size.Height + int.Parse(driver.ExecuteScript("return window.pageYOffset;").ToString()))
                    result = true;
            }), OnMethodExceptionRaised);
            return result;
        }
    }
}
