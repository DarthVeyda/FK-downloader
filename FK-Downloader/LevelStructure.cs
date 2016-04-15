using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FK_Downloader
{
    class LevelStructure
    {
        public string Name { get; private set; }
        public string Tag { get; private set; }
        public string Fandom { get; private set; }
        public List<KeyValuePair<string,string>> Quests;

        public LevelStructure(string name, string tag, List<KeyValuePair<string, string>> quests)
        {
            Name = name;
            Tag = tag;
            Quests = quests.Select(x => x).ToList();
        }

    }
}
