using System;
using System.Configuration;

namespace FK_Downloader
{
    internal static class Config
    {
        public static readonly string FandomText =
        ConfigurationManager.AppSettings["FandomText"] ?? string.Empty;
        public static readonly string LevelText =
ConfigurationManager.AppSettings["LevelText"] ?? string.Empty;
        public static readonly string CommentBoxId =
            ConfigurationManager.AppSettings["CommentBoxId"] ?? string.Empty;
        public static readonly string SaveFolder =
            ConfigurationManager.AppSettings["saveFolder"] ?? string.Empty;
        public static readonly string Tags =
            ConfigurationManager.AppSettings["tags"] ?? string.Empty;
        public static readonly string OpenCut =
            ConfigurationManager.AppSettings["openCut"] ?? string.Empty;
        public static readonly string PostLink =
            ConfigurationManager.AppSettings["postLink"] ?? string.Empty;
        public static readonly string NextPage =
            ConfigurationManager.AppSettings["nextPage"] ?? string.Empty;
        private static readonly string _postsPerPage =
            ConfigurationManager.AppSettings["postsPerPage"] ?? string.Empty ;
        public static readonly int PostsPerPage;

        static Config()
        {
            Int32.TryParse(_postsPerPage, out PostsPerPage);
        }
    }
}
