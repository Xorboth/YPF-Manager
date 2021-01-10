using System;
using System.IO;

namespace Ypf_Manager
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("YPF Manager v0.1");
            Console.WriteLine();
            Console.WriteLine();

            Options o = Options.Instance;
            o.Set(args);

            switch (o.Mode)
            {
                case Options.OperationMode.CreateArchive:

                    foreach (String f in o.FoldersToProcess)
                    {
                        YPFArchive.Create(f, $"{f}.ypf", o.EngineVersion);
                    }
                    break;

                case Options.OperationMode.ExtractArchive:

                    foreach (String f in o.FilesToProcess)
                    {
                        YPFArchive.Extract(f, $@"{Path.GetDirectoryName(f)}\{Path.GetFileNameWithoutExtension(f)}");
                    }
                    break;

                case Options.OperationMode.PrintArchiveInfo:

                    foreach (String f in o.FilesToProcess)
                    {
                        YPFArchive.PrintInfo(f);
                    }
                    break;

                case Options.OperationMode.Help:
                    Console.WriteLine("[DESCRIPTION]");
                    Console.WriteLine("Manage your YPF archives with this tool.");
                    Console.WriteLine();
                    Console.WriteLine("[USAGE]");
                    Console.WriteLine("Create archive:\t\t-c <folders_list> -v <version> [options]");
                    Console.WriteLine("Extract archive:\t-e <files_list> [options]");
                    Console.WriteLine("Print info:\t\t-p <files_list> [options]");
                    Console.WriteLine();

                    Console.WriteLine("[OPTIONS]");
                    Console.WriteLine("\t-c <folders_list>\tSet create archive mode");
                    Console.WriteLine("\t-e <files_list>\t\tSet extract archive mode");
                    Console.WriteLine("\t-p <files_list>\t\tSet print archive info mode");
                    Console.WriteLine("\t-v <version>\t\tSet the YU-RIS engine target version of the archive file");
                    Console.WriteLine("\t-w\t\t\tWait for user input before exit");

                    break;
            }

            if (o.WaitForUserInputBeforeExit)
            {
                Console.WriteLine();
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
        }
    }
}
