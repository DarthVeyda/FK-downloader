
namespace FK_Downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            var contentProcessor = new ContentProcessor();
            contentProcessor.InitFromDirectoryTree();
            //using (var fandomProcessor = new FandomTreeProcessor(args[0], args[1], args[2]))
            //{
            //    fandomProcessor.Login();
            //    if (fandomProcessor.GenerateFandomTree())
            //        fandomProcessor.DownloadRawContent();
            //    contentProcessor.AddFandomTree(fandomProcessor.FandomTree);
            //}

            contentProcessor.ParseAll();
        }
    }
}
