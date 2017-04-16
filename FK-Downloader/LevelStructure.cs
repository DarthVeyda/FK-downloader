using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace FK_Downloader
{
    [DebuggerDisplay("{Name} : {Tag}")]
    class LevelStructure
    {
        public string Name { get; private set; }
        public string Tag { get; private set; }

        public List<Quest> Quests;

        public LevelStructure(string name, string tag)
        {
            Name = name;
            Tag = tag;
            Quests = new List<Quest>();
        }

        public void AddQuest(Quest quest)
        {
            Quests.Add(quest);
        }

        public void AddQuestList(List<Quest> quests)
        {
            Quests = quests.Select(x => x).ToList();
        }
    }
}
