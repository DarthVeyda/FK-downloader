using System.Collections.Generic;
using System.Linq;

namespace FK_Downloader
{
    class Quest
    {
        public string Name { get; private set; }
        public string Tag { get; private set; }
        public List<string> PostURLs { get; private set; }

        public Quest(string name, string tag)
        {
            Name = name;
            Tag = tag;
            PostURLs = new List<string>();
        }

        public void AddURL(string url)
        {
            PostURLs.Add(url);
        }

        public void AddURLList(List<string> urls)
        {
            PostURLs = urls.Select(x => x).ToList();
        }
    }
}
