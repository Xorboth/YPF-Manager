using System;
using System.IO;
using System.Linq;

namespace Ypf_Manager
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("YPF Manager v0.1");
            Console.WriteLine();
            Console.WriteLine();

            // YPF Mode

            if (args.Contains("-e"))
            {
                String[] filesToProcess = args.Where(x => x.EndsWith(".ypf") && File.Exists(x)).ToArray();

                foreach (String s in filesToProcess)
                {
                    String inputFile = Path.GetFullPath(s);
                    String outputFolder = $@"{Path.GetDirectoryName(inputFile)}\{Path.GetFileNameWithoutExtension(inputFile)}";

                    YPFArchive.Extract(inputFile, outputFolder);
                }
            }
            else if (args.Contains("-c"))
            {
                String[] filesToProcess = args.Where(x => Directory.Exists(x)).ToArray();
                Int32 version = Convert.ToInt32(args[args.ToList().IndexOf("-v") + 1]);

                foreach (String s in filesToProcess)
                {
                    String inputFolder = Path.GetFullPath(s);
                    String outputFile = $"{inputFolder}.ypf";

                    YPFArchive.Compress(inputFolder, outputFile, version);
                }
            }
            else if (args.Contains("-p"))
            {
                String[] filesToProcess = args.Where(x => x.EndsWith(".ypf") && File.Exists(x)).ToArray();

                foreach (String s in filesToProcess)
                {
                    String inputFile = Path.GetFullPath(s);

                    YPFArchive.PrintInfo(inputFile);
                }
            }
            else
            {
                Console.WriteLine("[ERROR] Missing operating mode");
            }

            Console.WriteLine();
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
