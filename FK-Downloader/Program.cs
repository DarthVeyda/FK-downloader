using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;

namespace FK_Downloader
{
    class Program
    {
        private static string saveFolder = @"f:\data\FK_curr";
        private static string FKMasterURL;
        private static readonly string tags = "?tags";
        private static readonly string openCut = "?oam";
        private static readonly string postLink = "URL";

        private static readonly string[] questList = //in proper application this will be taken from the input. 
        {
            "Драбблы", "Мини", "Миди", "Макси", "Иллюстрации",
            "Арт/клип/коллаж"
        };

        static string TrimIllegalChars(string path)
        {
            return (new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())).Aggregate(
                path, (current, ch) => current.Replace(ch.ToString(), ""));
        }

        static Dictionary<string, string> CreateTagList(IWebDriver driver, string searchText)
        {
            return
                driver.FindElements(By.PartialLinkText(searchText))
                    .Select(
                        levelTag =>
                            new
                            {
                                levelName = TrimIllegalChars(levelTag.GetAttribute("innerHTML")),
                                levelURI =
                                    new Uri(levelTag.GetAttribute("href")).Query.Replace("tag", "tag[]")
                                        .Replace("?", "")
                            })
                    .OrderBy(x => x.levelName).ToDictionary(val => val.levelName, val => val.levelURI);
        }

        static Dictionary<string, string> CreateTagList(IWebDriver driver, IEnumerable<string> searchTextVals)
        {
            return
                searchTextVals.Select(searchTextVal => CreateTagList(driver, searchTextVal))
                    .SelectMany(results => results)
                    .ToDictionary(el => el.Key, el => el.Value);
        }

        static void Main(string[] args)
        {
            if (!Directory.Exists(saveFolder)) Directory.CreateDirectory(saveFolder);
            Directory.SetCurrentDirectory(saveFolder);
            var firefoxProfile = new FirefoxProfile {EnableNativeEvents = false};
            using (IWebDriver driver = new FirefoxDriver(firefoxProfile))
            {
                FKMasterURL = "http://fk-2o15.diary.ru";
                
                driver.Navigate().GoToUrl("http://fk-2o15.diary.ru/?tags");

                driver.FindElement(By.Id("usrlog2")).SendKeys("littleqwerty");
                driver.FindElement(By.Id("usrpass2")).SendKeys("дшеедуйцукен");
                driver.FindElement(By.ClassName("submit")).Click();

                var wait = new DefaultWait<IWebDriver>(driver) { Timeout = new TimeSpan(0, 0, 0, 10) };
                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                wait.Until(ExpectedConditions.ElementIsVisible(By.PartialLinkText("fandom")));

                //Format for multiple tag selection
                //http://fk-2o15.diary.ru/?tag[]=4846398&tag[]=5296011

                var quests = CreateTagList(driver, questList);
                var levels = CreateTagList(driver, "левел").Skip(1); //don't need Level 1 for now
                var fandoms = CreateTagList(driver, "fandom");
                
                var levelsFinal = new List<LevelStructure>();
                //Check each tag combination to see if it returns any posts, and throw it away if it doesn't
                foreach (var level in levels)
                {
                    var tmpQuests = new List<KeyValuePair<string, string>>();
                    foreach (var quest in quests)
                    {
                        
                        driver.Navigate().GoToUrl(new Uri(FKMasterURL+"?"+level.Value+"&"+quest.Value));
                        try
                        {
                            wait = new DefaultWait<IWebDriver>(driver) {Timeout = new TimeSpan(0, 0, 0, 30)};
                            wait.IgnoreExceptionTypes(typeof (NoSuchElementException));
                            wait.IgnoreExceptionTypes(typeof(WebDriverException));
                            wait.Until(ExpectedConditions.ElementIsVisible(By.PartialLinkText(postLink)));
                            tmpQuests.Add(quest);
                        }
                        catch (WebDriverTimeoutException e)
                        {
                            // If WebDriverTimeoutException is thrown, that means there are no posts on the page, 
                            // which means this is not a valid tag combination and we can ignore it altogether
                        }
                        catch (WebDriverException)
                        {
                            // in our case the page most likely takes too long to load because of 
                            // embedded media and/or large images. We don't need any of those at this stage,
                            // so it's safe to stop loading and search again
                            Actions actions = new Actions(driver);
                            actions.SendKeys(Keys.Escape);
                            wait = new DefaultWait<IWebDriver>(driver) { Timeout = new TimeSpan(0, 0, 0, 30) };
                            wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                            wait.Until(ExpectedConditions.ElementIsVisible(By.PartialLinkText(postLink)));
                            tmpQuests.Add(quest);
                        }
                    }
                    levelsFinal.Add(new LevelStructure(level.Key,level.Value,tmpQuests));
                }

                foreach (var fandom in fandoms)
                {
                    var query = "";
                    if (!Directory.Exists(fandom.Key)) Directory.CreateDirectory(fandom.Key);
                    foreach (var level in levelsFinal)
                    {
                        
                    }

                }

                driver.Quit();
            }
        }
    }
}
