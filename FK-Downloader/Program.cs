
namespace FK_Downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //using (var fandomProcessor = new FandomTreeProcessor(args[0], args[1], args[2]))
            //{
            //    fandomProcessor.Login();
            //    if (fandomProcessor.GenerateFandomTree())
            //        fandomProcessor.DownloadRawContent();
            //}
            var contentProcessor = new ContentProcessor();
            contentProcessor.InitFromDirectoryTree();
            contentProcessor.ParseAll();
        }
    }
}
