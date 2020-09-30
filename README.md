# Netflix Scraper Prototype

This Scraper project collects media information from Netflix using Selenium ChromeDriver.
The application has been built to easily integrate additional scrapers such as for Amazon VOD.

I built this scraper out of fun and curiosity. Use at your own risk.

# Get started
The collected information is stored in `ConcurrentBag<IMedia>()`.

## Clone
* Clone this repo to your local machine using https://github.com/Tcheetox/Netflix-Scraper-Prototype.git

## Setup
* Google Chrome **version 85.0 or higher** is required

## Included packages
* Selenium WebDriver
* Selenium WebDriver ChromeDriver

# Usage example
1. Instantiate NetflixScraper
```
NetflixScraper netflixScraper = new NetflixScraper(MediaBag);
netflixScraper.Start();
netflixScraper.Wait();
netflixScraper.Stop();
```
2. Driver wrapper class
```
netflixDriver.NavigateSafely($"https://www.netflix.com");
netflixDriver.TryFindElement((driver) => driver.FindElementByClassName("CLASSNAME"), out IWebElement element)
```
3. WebElement wrapper class
```
webElement.Safely((e) => e.FindElements(By.ClassName("CLASSNAME")))
```

[![Github All Releases](https://img.shields.io/github/downloads/atom/atom/total.svg?style=flat)]()
[![ChromeDriver Version](https://img.shields.io/npm/v/npm.svg?style=flat)]()