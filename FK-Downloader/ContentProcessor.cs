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

        public ContentProcessor()
        {
            FandomTree = new List<FandomStructure>();
        }

        public void AddFandomTree(List<FandomStructure> fandomTree)
        {
            FandomTree = fandomTree.Select(x => x).ToList();
        }

        public void InitFromDirectoryTree()
        {
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
                        foreach (var file in Directory.EnumerateFiles(dirQuest,"*.htm"))
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
                var post = tmpHtml.DocumentNode.SelectNodes(String.Format("//div[@id = 'post{0}']/*/*/div[@class='paragraph']/div", postId));
                var comments = tmpHtml.DocumentNode.SelectNodes("//div[starts-with(@class, 'singleComment')]");
                //var comments = tmp.DocumentNode.SelectNodes("//div[following-sibling::div[./div[@class='postContent']/div[@class='commentAuthor']/div[@class='avatar']/img[@alt!='fandom 50 Shades of Grey 2015']]]");

                foreach (var node in comments)
                {
                    var continuationInComments = node.SelectSingleNode(
                            "div[@class='postContent']/div[@class='commentAuthor']/div[@class='avatar']/img");
                    if (null != continuationInComments)
                    {
                        if (continuationInComments.GetAttributeValue("alt", "") == file.Fandom) post.Add(node.SelectSingleNode("div[@class='postContent']/div/div/div/span"));
                        else break;
                    }
                }

                //stripping the post of everything but the text/images accompanied by headers
                foreach (var node in post)
                {
                    //revoving orphaned more tags (we've alreay expanded all mores during saving the whole page)
                    foreach (var child in node.SelectNodes(".//a[contains(@name,'more')]") ?? Enumerable.Empty<HtmlNode>())
                    {
                        child.Remove();
                    }
                    
                    //removing html codes for fandom banners
                    foreach (var child in node.SelectNodes(".//textarea") ?? Enumerable.Empty<HtmlNode>())
                    {
                        child.Remove();
                    }
                }

                Directory.SetCurrentDirectory(Config.SaveFolder);
                
                using (StreamWriter storage = new StreamWriter(Path.Combine(Path.GetDirectoryName(file.FileName), string.Format("{0}.xml", postId))))
                {
                    foreach (var node in post)
                    {
                        var innerHtml = RemoveUnwantedHtmlTags(node.InnerHtml, new List<string>() { "div", "table" });
                        var innerText = new StringBuilder(innerHtml).Replace("<br>", "\n\r").Replace("<b>Название", "**DIVIDER**|**HEADERSTART**<b>Название").Replace("<b>Цикл:</b>", "**DIVIDER**|**CYCLE**<b>Цикл:</b>");
                        var r = new Regex(@"[\n\r].*<b>Для голосования\s*([^\n\r]*)");
                        foreach (Match match in r.Matches(innerText.ToString()))
                        {
                            innerText.Replace(match.Value, match.Value + "**HEADEREND**");
                        }
                        
                        storage.WriteLine(innerText);
#if DEBUG
                        storage.WriteLine();
#endif
                    }
                }
                using (StreamWriter storage = new StreamWriter(Path.Combine(Path.GetDirectoryName(file.FileName), string.Format("{0}.txt", postId))))
                {
                    foreach (var node in post)
                    {
                        storage.WriteLine(node.InnerText);
#if DEBUG
                        storage.WriteLine();
#endif
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
