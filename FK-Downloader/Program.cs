using System;

namespace FK_Downloader
{
    class Program
    {
        static void Main(string[] args)
        {
            var contentProcessor = new ContentProcessor();

            try
            {
                using (var fandomProcessor = new FandomTreeProcessor(args[0], args[1], args[2]))
                {
                    fandomProcessor.Login();
                    if (fandomProcessor.GenerateFandomTree())
                        fandomProcessor.DownloadRawContent(true);
                    contentProcessor.AddFandomTree(fandomProcessor.FandomTree);
                    fandomProcessor.Quit();
                }
            }
            catch (Exception e)
            {

            }
            contentProcessor.InitFromDirectoryTree();
            //contentProcessor.ParseAll();
        }
    }
}
