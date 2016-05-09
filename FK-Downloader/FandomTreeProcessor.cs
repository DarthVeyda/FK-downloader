using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace FK_Downloader
{
    class FandomTreeProcessor : IDisposable
    {
        public string FKMasterURL { get; private set; }
        public IWebDriver Driver { get; private set; }
        public List<FandomStructure> FandomTree { get; private set; }
        private string _login;
        private string _password;
        private static readonly string[] questList = //TODO in proper application this will be taken from the input. 
        {
            "Драбблы", "Мини", "Миди", "Макси", "Иллюстрации",
            "Арт/клип/коллаж"
        };
        public FandomTreeProcessor(string fkMasterURL, string login, string password)
        {
            FKMasterURL = fkMasterURL;
            _login = login;
            _password = password;
            Init();
        }

        private void Init()
        {
            var firefoxProfile = new FirefoxProfile { EnableNativeEvents = false };
            Driver = new FirefoxDriver(firefoxProfile);
            FandomTree = new List<FandomStructure>();
        }

        public void Login()
        {
            Driver.Navigate().GoToUrl(new Uri(FKMasterURL + Config.Tags));

            Driver.FindElement(By.Id("usrlog2")).SendKeys(_login);
            Driver.FindElement(By.Id("usrpass2")).SendKeys(_password);
            Driver.FindElement(By.ClassName("submit")).Click();
        }

        public bool GenerateFandomTree()
        {
            try
            {
                var wait = new DefaultWait<IWebDriver>(Driver) { Timeout = new TimeSpan(0, 0, 0, 10) };
                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                wait.Until(ExpectedConditions.ElementIsVisible(By.PartialLinkText(Config.FandomText)));

                //Format for multiple tag selection
                //http://fk-2o15.diary.ru/?tag[]=26629&tag[]=5300355&from=60

                var quests = CreateTagList(Driver, questList);
                var levels = CreateTagList(Driver, Config.LevelText).Skip(1); //don't need Level 1 for now
                FandomTree = CreateTagList(Driver, Config.FandomText).Select(fandom => new FandomStructure(fandom.Key, fandom.Value)).ToList();

                var levelsFinal = CreateLevelQuestTree(levels, quests, Driver);

                foreach (var tmpFandom in FandomTree)
                {
                    tmpFandom.AddLevelList(levelsFinal);
                }

#if DEBUG
                FandomTree = new List<FandomStructure>() { FandomTree.First() };
#endif
                return true;
            }
            catch (WebDriverException)
            {
                return false;
            }

        }

        public void DownloadRawContent()
        {
            if (!Directory.Exists(Config.SaveFolder)) Directory.CreateDirectory(Config.SaveFolder);
            Directory.SetCurrentDirectory(Config.SaveFolder);
            foreach (var fandom in FandomTree)
            {
                if (!Directory.Exists(fandom.Name)) Directory.CreateDirectory(fandom.Name);
                Directory.SetCurrentDirectory(fandom.Name);
                foreach (var level in fandom.Levels)
                {
                    if (!Directory.Exists(level.Name)) Directory.CreateDirectory(level.Name);
                    Directory.SetCurrentDirectory(level.Name);

                    foreach (var quest in level.Quests)
                    {
                        int skipPages = 0;
                        var posts = new List<string>();
                        var url = FKMasterURL + "?" + fandom.Tag + "&" + level.Tag + "&" + quest.Tag;
                        var tmpPosts = GetPosts(Driver, url);

                        while (tmpPosts.Any())
                        {
                            posts.AddRange(tmpPosts);
                            skipPages += Config.PostsPerPage;
                            tmpPosts = GetPosts(Driver, url + "&" + Config.NextPage + skipPages);
                        }
                        quest.AddURLList(posts);
                        if (!Directory.Exists(quest.Name) && quest.PostURLs.Any())
                        {
                            Directory.CreateDirectory(quest.Name);
                            Directory.SetCurrentDirectory(quest.Name);
                        }

                        foreach (var postUrL in quest.PostURLs)
                        {
                            Driver.Navigate().GoToUrl(postUrL + Config.OpenCut);
                            //wait until the comments form is there - it is the last one to be loaded
                            var wait = new DefaultWait<IWebDriver>(Driver) { Timeout = new TimeSpan(0, 0, 0, 60) };
                            wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                            wait.Until(ExpectedConditions.ElementIsVisible(By.Id(Config.CommentBoxId)));

                            var filename = Path.Combine(Directory.GetCurrentDirectory(), postUrL.Substring(postUrL.LastIndexOf('/') + 1));
                            using (StreamWriter rawHTML = new StreamWriter(filename))
                            {
                                rawHTML.Write(Driver.PageSource);
                            }
                        }
                        //we won't need full URL anymore, so leaving just filenames
                        quest.AddURLList(quest.PostURLs.Select(x => x.Substring(x.LastIndexOf('/') + 1)).ToList());
                        if (quest.PostURLs.Any())
                        {
                            Directory.SetCurrentDirectory("..");
                        }
                    }
                    Directory.SetCurrentDirectory("..");
                }
                Directory.SetCurrentDirectory("..");
            }


        }
        private string TrimIllegalChars(string path)
        {
            return (new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())).Aggregate(
                path, (current, ch) => current.Replace(ch.ToString(), "_"));
        }

        private Dictionary<string, string> CreateTagList(IWebDriver driver, string searchText)
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

        private Dictionary<string, string> CreateTagList(IWebDriver driver, IEnumerable<string> searchTextVals)
        {
            return
                searchTextVals.Select(searchTextVal => CreateTagList(driver, searchTextVal))
                    .SelectMany(results => results)
                    .ToDictionary(el => el.Key, el => el.Value);
        }
        private List<LevelStructure> CreateLevelQuestTree(IEnumerable<KeyValuePair<string, string>> levels, Dictionary<string, string> quests, IWebDriver driver)
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

        private List<string> GetPosts(IWebDriver driver, string url)
        {
            return PostsExist(driver, url)
                ? driver.FindElements(By.PartialLinkText(Config.PostLink)).Select(link => link.GetAttribute("href")).ToList()
                : new List<string>();
        }

        private bool PostsExist(IWebDriver driver, string url)
        {
            driver.Navigate().GoToUrl(new Uri(url));
            bool postsExist = false;
            try
            {
                var wait = new DefaultWait<IWebDriver>(driver) { Timeout = new TimeSpan(0, 0, 0, 30) };
                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                wait.Until(ExpectedConditions.ElementIsVisible(By.PartialLinkText(Config.PostLink)));
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

        public void Dispose()
        {
            Driver.Quit();
            Driver.Dispose();
        }
    }
}
