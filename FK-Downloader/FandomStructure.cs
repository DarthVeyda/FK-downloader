using System.Collections.Generic;
using System.Linq;

namespace FK_Downloader
{
    class FandomStructure
    {
        public string Name { get; private set; }
        public string Tag { get; private set; }

        public List<LevelStructure> Levels;

        public FandomStructure(string name, string tag)
        {
            Name = name;
            Tag = tag;
            Levels = new List<LevelStructure>();
        }

        public void AddLevel(LevelStructure level)
        {
            Levels.Add(level);
        }

        public void AddLevelList(List<LevelStructure> levels)
        {
            Levels = levels.Select(x => x).ToList();
        }
    }
}
