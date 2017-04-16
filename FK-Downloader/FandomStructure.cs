using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace FK_Downloader
{
    [DebuggerDisplay("{Name} : {Tag}")]
    class FandomStructure
    {
        public string Name { get; private set; }
        public string Tag { get; private set; }

        public List<LevelStructure> Levels;
        public List<LevelStructure> LevelsNonStandard;

        public FandomStructure(string name, string tag)
        {
            Name = name;
            Tag = tag;
            Levels = LevelsNonStandard = new List<LevelStructure>();
        }

        public void AddLevel(LevelStructure level)
        {
            Levels.Add(level);
        }

        public void AddLevelList(List<LevelStructure> levels)
        {
            Levels = levels.Select(l => new LevelStructure(l.Name, l.Tag) { Quests = l.Quests.Where(q => !q.SpecificFandoms.Any()).ToList() }).ToList();
            LevelsNonStandard = levels.Select(l => new LevelStructure(l.Name, l.Tag) { Quests = l.Quests.Where(q => q.SpecificFandoms.Contains(Name)).ToList() }).ToList();
        }
    }
}
