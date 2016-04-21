using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace FK_Downloader
{
    class Program
    {        
        //TODO - everything below goes to configuration or input params
        private const string FandomText = "fandom";
        private const string LevelText = "левел";
        private const string CommentBoxId = "message";
        private const string saveFolder = @"f:\data\FK_curr";
        private static string FKMasterURL;
        private const string tags = "?tags";
        private const string openCut = "?oam";
        private const string postLink = "URL";
        private const string nextPage = "from=";
        private const int postsPerPage = 20;


        private static readonly string[] questList = //TODO in proper application this will be taken from the input. 
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
            var firefoxProfile = new FirefoxProfile { EnableNativeEvents = false };
            using (IWebDriver driver = new FirefoxDriver(firefoxProfile))
            {
                FKMasterURL = args[0];

                driver.Navigate().GoToUrl(new Uri(FKMasterURL + tags));

                driver.FindElement(By.Id("usrlog2")).SendKeys(args[1]);
                driver.FindElement(By.Id("usrpass2")).SendKeys(args[2]);
                driver.FindElement(By.ClassName("submit")).Click();

                var wait = new DefaultWait<IWebDriver>(driver) { Timeout = new TimeSpan(0, 0, 0, 10) };
                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                wait.Until(ExpectedConditions.ElementIsVisible(By.PartialLinkText(FandomText)));

                //Format for multiple tag selection
                //http://fk-2o15.diary.ru/?tag[]=26629&tag[]=5300355&from=60

                var quests = CreateTagList(driver, questList);
                var levels = CreateTagList(driver, LevelText).Skip(1); //don't need Level 1 for now
                var fandomsFinal = CreateTagList(driver, FandomText).Select(fandom => new FandomStructure(fandom.Key, fandom.Value)).ToList();

                var levelsFinal = CreateLevelQuestTree(levels, quests, driver);

                foreach (var tmpFandom in fandomsFinal)
                {
                    tmpFandom.AddLevelList(levelsFinal);
                }

                //FOR DEBUGGING//////
                fandomsFinal = new List<FandomStructure>() { fandomsFinal.First() };
                /////////////////////
                foreach (var fandom in fandomsFinal)
                {
                    if (!Directory.Exists(fandom.Name)) Directory.CreateDirectory(fandom.Name);
                    Directory.SetCurrentDirectory(fandom.Name);
                    foreach (var level in fandom.Levels)
                    {
                        if (!Directory.Exists(level.Name)) Directory.CreateDirectory(level.Name);
                        Directory.SetCurrentDirectory(level.Name);

                        foreach (var quest in level.Quests)
                        {
                            if (!Directory.Exists(quest.Name)) Directory.CreateDirectory(quest.Name);
                            int skipPages = 0;

                            var posts = new List<string>();
                            var url = FKMasterURL + "?" + fandom.Tag + "&" + level.Tag + "&" + quest.Tag;
                            var tmpPosts = GetPosts(driver, url);
                            while (tmpPosts.Any())
                            {
                                posts.AddRange(tmpPosts);
                                skipPages += postsPerPage;
                                tmpPosts = GetPosts(driver, url + "&" + nextPage + skipPages);
                            } 
                            quest.AddURLList(posts);
                            foreach (var postUrL in quest.PostURLs)
                            {
                                driver.Navigate().GoToUrl(postUrL + openCut);
                                //wait until the comments form is there - it is the last one to be loaded
                                wait = new DefaultWait<IWebDriver>(driver) { Timeout = new TimeSpan(0, 0, 0, 60) };
                                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                                wait.Until(ExpectedConditions.ElementIsVisible(By.Id(CommentBoxId)));

                                using (
                                    StreamWriter rawHTML =
                                        new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(),
                                            new Uri(postUrL).MakeRelativeUri(new Uri(FKMasterURL))
                                                .GetLeftPart(UriPartial.Path))))
                                {
                                    rawHTML.Write(driver.PageSource);
                                }

                            }
                        }                       
                        Directory.SetCurrentDirectory("..");
                    }
                    Directory.SetCurrentDirectory("..");
                }

                driver.Quit();
            }
        }

        private static List<LevelStructure> CreateLevelQuestTree(IEnumerable<KeyValuePair<string, string>> levels, Dictionary<string, string> quests, IWebDriver driver)
        {
            var levelsFinal = new List<LevelStructure>();
            //Check each tag combination to see if it returns any posts, and throw it away if it doesn't
            foreach (var level in levels)
            {
                var tmpQuests = (from quest in quests
                    where PostsExist(driver, FKMasterURL + "?" + level.Value + "&" + quest.Value)
                    select new Quest(quest.Key, quest.Value)).ToList();
                var tmpLevel = new LevelStructure(level.Key, level.Value);
                tmpLevel.AddQuestList(tmpQuests);
                levelsFinal.Add(tmpLevel);
            }
            return levelsFinal;
        }

        private static List<string> GetPosts(IWebDriver driver, string url)
        {
            return PostsExist(driver, url)
                ? driver.FindElements(By.PartialLinkText(postLink)).Select(link => link.GetAttribute("href")).ToList()
                : new List<string>();
        }

        private static bool PostsExist(IWebDriver driver, string url)
        {
            driver.Navigate().GoToUrl(new Uri(url));
            bool postsExist = false;
            try
            {
                var wait = new DefaultWait<IWebDriver>(driver) { Timeout = new TimeSpan(0, 0, 0, 30) };
                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                wait.Until(ExpectedConditions.ElementIsVisible(By.PartialLinkText(postLink)));
                postsExist = true;
            }
            catch (WebDriverTimeoutException)
            {
                // If WebDriverTimeoutException is thrown, that means there are no posts on the page, 
                // which means this is not a valid tag combination and we should ignore it altogether
            }
            catch (WebDriverException)
            {
                // in our case the page most likely takes too long to load because of 
                // embedded media and/or large images - which means there are posts on the page.
                // None of the workarounds meant to stop loading the page manually seem to work anyway.

                postsExist = true;
            }
            return postsExist;
        }
    }
}
