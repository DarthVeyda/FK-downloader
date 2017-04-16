using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            "Драбблы", "Арт/клип/коллаж", "Мини", "Миди", "Макси", "Иллюстрации"
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
            var driverService = FirefoxDriverService.CreateDefaultService();
            driverService.FirefoxBinaryPath = @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe";
            driverService.HideCommandPromptWindow = true;
            driverService.SuppressInitialDiagnosticInformation = true;

            var driver = new FirefoxDriver(driverService, new FirefoxOptions() { Profile = firefoxProfile }, TimeSpan.FromSeconds(60));
            Driver = driver; // new FirefoxDriver(firefoxProfile);
            FandomTree = new List<FandomStructure>();
        }

        /// <summary>
        /// The sole reason to use Selenium - the FK content is only available for the registered diary.ru users, 
        /// so we somehow need to log in to the site with a genuine account created at least a year before the FK started
        /// </summary>
        public void Login()
        {
            Driver.Navigate().GoToUrl(new Uri(FKMasterURL + Config.Tags));

            Driver.FindElement(By.Id("usrlog2")).Clear();
            Driver.FindElement(By.Id("usrlog2")).SendKeys(_login);
            Driver.FindElement(By.Id("usrpass2")).Clear();
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

#if DEBUG
                Console.WriteLine("{0}: Parsing quest list:", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
#endif
                var quests = CreateTagList(Driver, questList);
#if DEBUG
                Console.WriteLine("{0}: Parsing level list:", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
#endif
                // don't need Level 1 for now (or ever - it's impossible to parse 
                // and very often has content with extremely short shell life, e.g. Flash presentations on cheap hostings)
                var levels = CreateTagList(Driver, Config.LevelText).Skip(1);
#if DEBUG
                Console.WriteLine("{0}: Parsing fandom list:", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
#endif
                FandomTree = CreateTagList(Driver, Config.FandomText).Select(fandom => new FandomStructure(fandom.Key, fandom.Value)).ToList();

                /*
                 * fandom Yuri Penguin Utena 2017
                 * Shinsekai Yori
                 * fandom Richard Armitage 2016
                 * fandom RusLitClassic 2015
                 * fandom Organizations 2015
                 * fandom Dragonriders of Pern 2017
                 * fandom K project 2015
                 * fandom IT 2016
                 */


#if DEBUG
                Console.WriteLine("{0}: Generating tag tree:", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
#endif

                var levelsFinal = CreateLevelQuestTree(levels, quests, Driver);

                foreach (var tmpFandom in FandomTree)
                {
                    tmpFandom.AddLevelList(levelsFinal);
                }

#if DEBUG
                FandomTree = FandomTree.Where(f =>
                                                f.Name.Contains("Yuri Penguin Utena")
                                                || f.Name.Contains("Shinsekai Yori")
                                                || f.Name.Contains("Richard Armitage")
                                                || f.Name.Contains("RusLitClassic")
                                                || f.Name.Contains("Organizations")
                                                || f.Name.Contains("Dragonriders of Pern")
                                                || f.Name.Contains("K project")
                                                || f.Name.Contains(" IT ")
                                                )
                                                .ToList()
                    ;
#endif
                return true;
            }
            catch (WebDriverException)
            {
                return false;
            }

        }

        public void DownloadRawContent(bool skipDropouts = false)
        {
            if (!Directory.Exists(Config.SaveFolder)) Directory.CreateDirectory(Config.SaveFolder);
            Directory.SetCurrentDirectory(Config.SaveFolder);
            foreach (var fandom in FandomTree)
            {
#if DEBUG
                Console.WriteLine("{0}: Downloading {1}: ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), fandom.Name);
#endif
                if (!Directory.Exists(fandom.Name)) Directory.CreateDirectory(fandom.Name);
                Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), fandom.Name));
                bool teamDroppedOut = false;
                foreach (var level in fandom.Levels)
                {
#if DEBUG
                    Console.WriteLine("{0}: Downloading {1}: ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), level.Name);
#endif                    
                    if (!Directory.Exists(level.Name)) Directory.CreateDirectory(level.Name);
                    Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), level.Name));

                    foreach (var quest in level.Quests)
                    {
#if DEBUG
                        Console.WriteLine("{0}: Getting posts for {1}: ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), quest.Name);
#endif
                        int skipPages = 0;
                        var posts = new List<string>();
                        var url = FKMasterURL + "?" + fandom.Tag + "&" + level.Tag + "&" + quest.Tag;
                        var tmpPosts = GetPosts(Driver, url);
                        posts.AddRange(tmpPosts);

                        if (posts.Count == Config.PostsPerPage)
                        {
                            while (tmpPosts.Any())
                            {
                                skipPages += Config.PostsPerPage;
                                tmpPosts = GetPosts(Driver, url + "&" + Config.NextPage + skipPages);
                                posts.AddRange(tmpPosts);
                            }
                        }
                        quest.AddURLList(posts);

                        teamDroppedOut = skipDropouts && !quest.PostURLs.Any();

                        if (teamDroppedOut) break;

                        if (!Directory.Exists(quest.Name) && quest.PostURLs.Any())
                        {
                            Directory.CreateDirectory(quest.Name);
                        }

#if DEBUG
                        Console.WriteLine("{0}: Posts found: {1} ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), quest.PostURLs.Count);
#endif
                        if (quest.PostURLs.Any())
                        {
#if DEBUG
                            Console.WriteLine("{0}: Downloading {1}: ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), quest.Name);
#endif
                            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), quest.Name));
                        }
                        foreach (var postUrL in quest.PostURLs)
                        {
                            try
                            {
                                Driver.Navigate().GoToUrl(postUrL + Config.OpenCut);
                                // wait until the comments form is there - it is the last one to be loaded
                                var wait = new DefaultWait<IWebDriver>(Driver) { Timeout = new TimeSpan(0, 0, 0, 60) };
                                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                                wait.Until(ExpectedConditions.ElementIsVisible(By.Id(Config.CommentBoxId)));
                            }
                            catch (WebDriverTimeoutException)
                            {
                                // unless the comments for the post were disabled (a rare occasion, but it happens)
                                var wait = new DefaultWait<IWebDriver>(Driver) { Timeout = new TimeSpan(0, 0, 0, 10) };
                                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                                wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("postLinksBackg")));

                            }

                            var filename = Path.Combine(Directory.GetCurrentDirectory(), postUrL.Substring(postUrL.LastIndexOf('/') + 1));
                            using (StreamWriter rawHTML = new StreamWriter(filename, false, Encoding.UTF8))
                            {
                                rawHTML.Write(Driver.PageSource);
                            }
                        }
                        // we won't need full URL anymore, so substituting with a filename
                        quest.AddURLList(quest.PostURLs.Select(x => Path.Combine(Directory.GetCurrentDirectory(), x.Substring(x.LastIndexOf('/') + 1))).ToList());
                        if (quest.PostURLs.Any())
                        {
                            Directory.SetCurrentDirectory("..");
                        }
                    }

                    Directory.SetCurrentDirectory("..");
                    if (teamDroppedOut) break;
                }

                teamDroppedOut = false;
                foreach (var level in fandom.LevelsNonStandard)
                {
#if DEBUG
                    Console.WriteLine("{0}: Checking non-standard for {1}: ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), level.Name);
#endif                    
                    if (!Directory.Exists(level.Name)) Directory.CreateDirectory(level.Name);
                    Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), level.Name));

                    foreach (var quest in level.Quests)
                    {
#if DEBUG
                        Console.WriteLine("{0}: Getting posts for {1}: ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), quest.Name);
#endif
                        int skipPages = 0;
                        var posts = new List<string>();
                        var url = FKMasterURL + "?" + fandom.Tag + "&" + level.Tag + "&" + quest.Tag;
                        var tmpPosts = GetPosts(Driver, url);
                        posts.AddRange(tmpPosts);

                        if (posts.Count == Config.PostsPerPage)
                        {
                            while (tmpPosts.Any())
                            {
                                skipPages += Config.PostsPerPage;
                                tmpPosts = GetPosts(Driver, url + "&" + Config.NextPage + skipPages);
                                posts.AddRange(tmpPosts);
                            }
                        }
                        quest.AddURLList(posts);

                        teamDroppedOut = !quest.PostURLs.Any();

                        if (teamDroppedOut) break;

                        if (!Directory.Exists(quest.Name) && quest.PostURLs.Any())
                        {
                            Directory.CreateDirectory(quest.Name);
                        }

#if DEBUG
                        Console.WriteLine("{0}: Posts found: {1} ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), quest.PostURLs.Count);
#endif
                        if (quest.PostURLs.Any())
                        {
#if DEBUG
                            Console.WriteLine("{0}: Downloading {1}: ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), quest.Name);
#endif
                            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), quest.Name));
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
                        quest.AddURLList(quest.PostURLs.Select(x => Path.Combine(Directory.GetCurrentDirectory(), x.Substring(x.LastIndexOf('/') + 1))).ToList());
                        if (quest.PostURLs.Any())
                        {
                            Directory.SetCurrentDirectory("..");
                        }
                    }

                    Directory.SetCurrentDirectory("..");
                    if (teamDroppedOut) break;
                }

                Directory.SetCurrentDirectory("..");
#if DEBUG
                Console.WriteLine("{0}: Finished downloading {1} ", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), fandom.Name);
#endif
            }
        }

        public void Quit()
        {
            Driver.Close();
            Driver.Quit();
        }

        private string TrimIllegalChars(string path)
        {
            return (new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())).Aggregate(
                path, (current, ch) => current.Replace(ch.ToString(), "_"));
        }

        /// <summary>
        /// Searches the tags page for all tags containing given entity name 
        /// (fandom or level, wouldn't work for the quests - see the overload) 
        /// </summary>
        /// <param name="driver">Current WebDriver where the page is opened</param>
        /// <param name="searchText">Entity name to search</param>
        /// <returns>Pairs of ({entity name}, {entity tag})</returns>
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
                    .OrderBy(x => x.levelName)
                    .GroupBy(x => new { levelName = x.levelName, levelURI = x.levelURI })
                    .Select(x => new { levelName = x.Key.levelName, levelURI = x.Key.levelURI })
                    .ToDictionary(val => val.levelName, val => val.levelURI);
        }

        /// <summary>
        /// Searches the tags page for the corresponding quest tags 
        /// (quest names have very little in common and may vary from year to year, 
        /// so we have to pass all names instead of one)
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="searchTextVals"></param>
        /// <returns>Pairs of ({entity name}, {entity tag})</returns>
        private Dictionary<string, string> CreateTagList(IWebDriver driver, IEnumerable<string> searchTextVals)
        {
            return
                searchTextVals.Select(searchTextVal => CreateTagList(driver, searchTextVal))
                    .SelectMany(results => results)
                    .ToDictionary(el => el.Key, el => el.Value);
        }

        private List<LevelStructure> CreateLevelQuestTree(IEnumerable<KeyValuePair<string, string>> levels, Dictionary<string, string> quests, IWebDriver driver)
        {
            var levelsTmp = levels.Select(l => new LevelStructure(l.Key, l.Value)).ToList();
            var questsTmp = quests.Select(q => new Quest(q.Key, q.Value)).ToList();


            // As of 2013 and later, these are the default quests for each level...
            var StandardQuests = new List<string>()        {
            "Драбблы", "Арт_клип_коллаж", "Мини", "Миди"
        };
            var BBQuests = new List<string>()        {
            "Макси", "Иллюстрации",
        };
            var SpecialQuests = new List<string>()        {
            ""
        };
            // ...so at least one post for each of them is guaranteed to exist, unless the fandom had missed a quest and dropped out
            // (or a person responsible for the posting forgot to add all necessary tags. Duh.)
            foreach (var level in levelsTmp.Where(l => l.Name.StartsWith("2") || l.Name.StartsWith("4")).ToList())
            {
                level.AddQuestList(
                    questsTmp.Where(q => StandardQuests.Contains(q.Name)).ToList()
                    );
            }

            foreach (var level in levelsTmp.Where(l => l.Name.StartsWith("3")).ToList())
            {
                level.AddQuestList(
                    questsTmp.Where(q => BBQuests.Contains(q.Name)).ToList()
                    );
            }

            // Level 5, unlike the previous ones, can feature any type of content, so there can be multiple (or none) quest tags.
            // The level tag is mandatory (at least, it's harder to forget adding it - if nothing else, the admins would have noticed during the checkup),
            // so it's safe to use just that (it also helps to avoid downloading a post with multiple content types more than once)
            foreach (var level in levelsTmp.Where(l => l.Name.StartsWith("5")).ToList())
            {
                level.AddQuest(
                    new Quest("Спецквест", "")
                    );
            }

            // Now we want to check for the Level 2-4 posts with tag combinations that are not valid, but they still can exist
            // because, again, somebody misplaced a tag and nobody noticed or bothered to fix it

            var allCombinations = levelsTmp.Where(l => !l.Name.StartsWith("5")).SelectMany(l => questsTmp, (l, q) => new { Level = l, Quest = q }).ToList();
            allCombinations.RemoveAll(c => levelsTmp.Any(l => l.Name == c.Level.Name && l.Quests.Any(q => q.Name == c.Quest.Name)));

            foreach (var combination in allCombinations)
            {
                if (PostsExist(driver, FKMasterURL + "?" + combination.Level.Tag + "&" + combination.Quest.Tag))
                {
                    var wait = new DefaultWait<IWebDriver>(driver) { Timeout = new TimeSpan(0, 0, 0, 30) };

                    wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                    // get fandom name - we want to know about specific fandoms here, 
                    // so we don't check for hundreds of false positives later on downloading actual posts
                    // IMPORTANT: we need the name from the fandom tag here, not the team community name
                    // - tag name is what we are going to compare to when we add the level-quest tree to each fandom
                    wait.Until(ExpectedConditions.ElementIsVisible(
                        //atTag
                        // //p[@class='atTag']
                        // //p[@class='atTag']//a[contains(text(),'match')]
                        By.XPath("//p[@class='atTag']")
                        //By.XPath("//div[@class='postContent']/div[@class='commentAuthor']/div[@class='avatar']/img")

                        ));

                    var level = levelsTmp.Where(l => l.Name == combination.Level.Name).ToList().FirstOrDefault();

                    if (!level.Quests.Any(q => q.Name == combination.Quest.Name))
                        level.AddQuest(new Quest(combination.Quest.Name, combination.Quest.Tag));

                    var quest = level.Quests.Where(q => q.Name == combination.Quest.Name).ToList().FirstOrDefault();
                    foreach (var el in driver.FindElements(By.XPath("//p[@class='atTag']//a[contains(text(),'" + Config.FandomText + "')]")))
                    {
                        quest.SpecificFandoms.Add(el.Text);
                    }
                }
            }
            return levelsTmp;
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
                try
                {
                    if (driver.FindElement(By.XPath("//*[contains(., 'Нет записей')]")) != null) return false;
                }
                catch (WebDriverException)
                {
                    // fuck people responsible for the diary.ru source code & fuck Selenium
                }
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
            Driver.Dispose();
        }
    }
}
