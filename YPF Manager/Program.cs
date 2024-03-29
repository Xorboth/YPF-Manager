﻿using System;
using System.IO;

namespace Ypf_Manager
{
    class Program
    {

        //
        // Function(s)
        //

        static void Main(string[] args)
        {
            // Set variables
            Log log = new Log();
            Config config = new Config();

            // Header
            Console.WriteLine("YPF Manager v0.1");
            Console.WriteLine();
            Console.WriteLine();

            try
            {
                // Scan args
                config.Set(args);

                // Process current operation mode
                switch (config.Mode)
                {
                    case Config.OperationMode.CreateArchive:

                        foreach (String f in config.FoldersToProcess)
                        {
                            YPFArchive.Create(f, $"{f}.ypf", config.EngineVersion);
                        }
                        break;

                    case Config.OperationMode.ExtractArchive:

                        foreach (String f in config.FilesToProcess)
                        {
                            YPFArchive.Extract(f, $@"{Path.GetDirectoryName(f)}\{Path.GetFileNameWithoutExtension(f)}");
                        }
                        break;

                    case Config.OperationMode.PrintArchiveInfo:

                        foreach (String f in config.FilesToProcess)
                        {
                            YPFArchive.PrintInfo(f, config.SkipDataIntegrityValidationOnPrintInfo);
                        }
                        break;

                    case Config.OperationMode.Help:

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
                        Console.WriteLine("\t-sdc\t\t\tSkip data integrity validation (Print info only)");

                        break;
                }
            }
            catch (Exception ex)
            {
                // Save error to log
                log.Add(ex.Message);
                log.Save();

                // Print error to console
                Console.WriteLine(ex.Message);
            }

            // Wait for user input if -w argument is provided
            if (config.WaitForUserInputBeforeExit)
            {
                Console.WriteLine();
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
        }

    }
}
