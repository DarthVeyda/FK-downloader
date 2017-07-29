using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace FK_Downloader
{
    class ContentProcessor
    {
        public List<FandomStructure> FandomTree { get; private set; }

        public List<Entry> Entries { get; private set; }

        public ContentProcessor()
        {
            FandomTree = new List<FandomStructure>();
            Entries = new List<Entry>();
        }

        public void AddFandomTree(List<FandomStructure> fandomTree)
        {
            FandomTree = fandomTree.Select(x => x).ToList();
        }

        public void InitFromDirectoryTree()
        {
            FandomTree = new List<FandomStructure>();
            foreach (var dir in Directory.EnumerateDirectories(Config.SaveFolder))
            {
                Directory.SetCurrentDirectory(dir);
                var tmpFandom = new FandomStructure(Path.GetFileName(dir), "");
                foreach (var dirLevel in Directory.EnumerateDirectories(dir))
                {
                    Directory.SetCurrentDirectory(dirLevel);
                    var tmpLevel = new LevelStructure(Path.GetFileName(dirLevel), "");
                    foreach (var dirQuest in Directory.EnumerateDirectories(dirLevel))
                    {
                        var tmpQuest = new Quest(Path.GetFileName(dirQuest), "");
                        foreach (var file in Directory.EnumerateFiles(dirQuest, "*.htm"))
                        {
                            tmpQuest.AddURL(file);
                        }
                        tmpLevel.AddQuest(tmpQuest);
                    }
                    Directory.SetCurrentDirectory("..");
                    tmpFandom.AddLevel(tmpLevel);
                }
                FandomTree.Add(tmpFandom);
                Directory.SetCurrentDirectory("..");
            }
        }

        public void ParseAll()
        {
            foreach (var file in GetRawHTML())
            {
                var postId = new Regex(@"(\d+)(?!.*\d)").Match(file.FileName).Value;
                var tmpHtml = new HtmlDocument();
                tmpHtml.Load(file.FileName, Encoding.UTF8);

                /* <div class="postContent"><div class="commentAuthor"><div class="avatar">
                 * <img src="http://static.diary.ru/userdir/3/1/2/1/3121599/83144722.png" title="fandom Dragonriders of Pern 2017" alt="fandom Dragonriders of Pern 2017">
                    */

                var fandomCommunityName = tmpHtml.DocumentNode.SelectNodes(string.Format($"//div[@id = 'post{postId}']/div[@class='postContent']/div[@class='commentAuthor']/div[@class='avatar']/img")).FirstOrDefault().Attributes["alt"].Value;
                var post = tmpHtml.DocumentNode.SelectNodes(string.Format($"//div[@id = 'post{postId}']/*/*/div[@class='paragraph']/div"));
                var comments = tmpHtml.DocumentNode.SelectNodes("//div[starts-with(@class, 'singleComment')]");
                
                foreach (var node in comments)
                {
                    var continuationInComments = node.SelectSingleNode(
                            "div[@class='postContent']/div[@class='commentAuthor']/div[@class='avatar']/img");
                    if (null != continuationInComments)
                    {
                        if (continuationInComments.GetAttributeValue("alt", "") == fandomCommunityName)
                            post.Last().AppendChild(node.SelectSingleNode("div[@class='postContent']/div/div/div/span"));
                        else break;
                    }
                }

                //stripping the post of everything but the text/images accompanied by headers
                foreach (var node in post)
                {
                    //removing orphaned "more" blocks in the post body (we've already expanded all mores during saving the whole page)
                    foreach (var child in node.SelectNodes(".//a[contains(@name,'more')]") ?? Enumerable.Empty<HtmlNode>())
                    {
                        child.Remove();
                    }
                    //same for the comments (extracting the span contents first - unlike the above, "more" block in comments are not expanded by "?oam" parameter)
                    foreach (var child in node.SelectNodes(".//span[contains(@id,'more')]") ?? Enumerable.Empty<HtmlNode>())
                    {
                        node.InsertBefore(child.FirstChild, child);
                        child.Remove();
                    }

                    //removing html codes for fandom banners
                    foreach (var child in node.SelectNodes(".//textarea") ?? Enumerable.Empty<HtmlNode>())
                    {
                        child.Remove();
                    }

                    //removing span blocks - they can also contain banners TODO: may be an overkill - check later for other fandoms
                    foreach (var child in node.SelectNodes(".//span[@class='postInner']") ?? Enumerable.Empty<HtmlNode>())
                    {
                        child.Remove();
                    }

                    //http://static.diary.ru/userdir/3/8/7/9/387924/81447654.png - "18+" icon
                }

                Directory.SetCurrentDirectory(Config.SaveFolder);

                var currentPath = Path.GetDirectoryName(file.FileName);

                using (StreamWriter storage = new StreamWriter(Path.Combine(currentPath, string.Format("{0}.xml", postId))))
                {
                    foreach (var node in post)
                    {
                        // Additional divs and tables are usually used for pretty formatting
                        // - we don't need them at this stage because we don't preserve fandom-specific look for posts.
                        var innerHtml = RemoveUnwantedHtmlTags(node.InnerHtml, new List<string>() { "div", "table" });
                        var innerText = new StringBuilder(innerHtml).Replace("<br>", "\n\r").Replace("<b>Название", "**DIVIDER****HEADERSTART**<b>Название").Replace("<b>Цикл:</b>", "**DIVIDER****CYCLE**<b>Цикл:</b>");
                        var r = new Regex(@"[\n\r].*<b>Для голосования\s*([^\n\r]*)");
                        foreach (Match match in r.Matches(innerText.ToString()))
                        {
                            innerText.Replace(match.Value, match.Value + "**HEADEREND**");
                        }

                        var rawEntries = innerText.ToString().Split(new[] { "**DIVIDER**" }, StringSplitOptions.RemoveEmptyEntries);

                        var strayImageURLs = new List<string>();

                        bool isCycle = false;
                        foreach (var rawEntry in rawEntries.Select(e => e.Split(new[] { "**HEADERSTART**" }, StringSplitOptions.RemoveEmptyEntries)))
                        {
                            foreach (var element in rawEntry.Select(e => e.TrimEnd(new[] { '\n', '\r' })))
                            {
                                var document = new HtmlDocument();
                                document.LoadHtml(element);

                                var currEntry = new Entry(file);

                                var tmp = element.Split(new[] { "**HEADEREND**" }, StringSplitOptions.RemoveEmptyEntries);

                                if (tmp.Length < 2) 
                                {
                                    var imgNode = document.DocumentNode.SelectSingleNode("//img");

                                    if (imgNode != null)
                                    {
                                        document.DocumentNode.RemoveChild(imgNode);
                                        var imgUrl = imgNode.GetAttributeValue("src", string.Empty);
                                        if (!string.IsNullOrWhiteSpace(imgUrl))
                                            strayImageURLs.Add(imgUrl);
                                    }
                                    if (!document.DocumentNode.ChildNodes.Any()) //meaning this is most likely a single image at the top of the post - anything else would have a header
                                        continue;
                                    //**CYCLE**
                                    if (element.Contains("**CYCLE**"))
                                    {
                                        isCycle = true;
                                    }

                                }

                                if (tmp.Length > 2)
                                {
                                    Console.WriteLine("Header parsing error in {0} - an entry cannot have two headers", file.FileName);
                                    continue;
                                }

                                var entryHeader = tmp.First();
                                var entryContent = tmp.Last();



                                currEntry.ParseHeader(entryHeader);

                                storage.WriteLine(element);
                                storage.WriteLine("-------------------------------------");
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<FileWithProperties> GetRawHTML()
        {
            return from fandom in FandomTree
                   from level in fandom.Levels
                   from quest in level.Quests
                   from postFile in quest.PostURLs
                   select new FileWithProperties
                   {
                       FileName = postFile,
                       Fandom = fandom.Name,
                       Level = level.Name,
                       Quest = quest.Name
                   };
        }

        private void ClearTrailingEnters(string sourcePath, string destinationPath)
        {
            var previousLines = new HashSet<string>();

            File.WriteAllLines(destinationPath, File.ReadAllLines(sourcePath)
                                                    .Where(line => previousLines.Add(line)));
        }

        // courtesy of SO: http://stackoverflow.com/questions/12787449/html-agility-pack-removing-unwanted-tags-without-removing-content
        private string RemoveUnwantedHtmlTags(string html, List<string> unwantedTags)
        {
            if (String.IsNullOrEmpty(html))
            {
                return html;
            }

            var document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNodeCollection tryGetNodes = document.DocumentNode.SelectNodes("./*|./text()");

            if (tryGetNodes == null || !tryGetNodes.Any())
            {
                return html;
            }

            var nodes = new Queue<HtmlNode>(tryGetNodes);

            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                var parentNode = node.ParentNode;

                var childNodes = node.SelectNodes("./*|./text()");

                if (childNodes != null)
                {
                    foreach (var child in childNodes)
                    {
                        nodes.Enqueue(child);
                    }
                }

                if (unwantedTags.Any(tag => tag == node.Name))
                {
                    if (childNodes != null)
                    {
                        foreach (var child in childNodes)
                        {
                            parentNode.InsertBefore(child, node);
                        }
                    }

                    parentNode.RemoveChild(node);

                }
            }

            return document.DocumentNode.InnerHtml;
        }
    }

    struct FileWithProperties
    {
        public string FileName;
        public string Quest;
        public string Level;
        public string Fandom;
    }
}
