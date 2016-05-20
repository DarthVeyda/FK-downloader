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
                Path.GetDirectoryName(file.FileName);
                var postId = new Regex(@"(\d+)(?!.*\d)").Match(file.FileName).Value;
                var tmp = new HtmlDocument();
                tmp.Load(file.FileName, Encoding.UTF8);
                var post = tmp.DocumentNode.SelectNodes(String.Format("//div[@id = 'post{0}']/*/*/div[@class='paragraph']/div", postId));


                var comments = tmp.DocumentNode.SelectNodes("//div[starts-with(@class, 'singleComment')]");
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
                Directory.SetCurrentDirectory(Config.SaveFolder);
                
                using (StreamWriter tmpDump = new StreamWriter(Path.Combine(Path.GetDirectoryName(file.FileName), string.Format("{0}.html", postId))))
                {
                    foreach (var node in post)
                    {
                        tmpDump.WriteLine(node.InnerHtml);
#if DEBUG
                        tmpDump.WriteLine("<br/><br/>");
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
    }

    struct FileWithProperties
    {
        public string FileName;
        public string Quest;
        public string Level;
        public string Fandom;
    }
}
