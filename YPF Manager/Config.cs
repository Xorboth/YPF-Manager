using System;
using System.Collections.Generic;
using System.IO;

namespace Ypf_Manager
{
    class Config
    {

        //
        // Enums
        //

        public enum OperationMode
        {
            CreateArchive = 0,
            ExtractArchive = 1,
            PrintArchiveInfo = 2,
            Help = 3
        }


        //
        // Variables
        //

        public static String ExecutableLocation()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        }
        public OperationMode Mode { get; set; }

        public Boolean WaitForUserInputBeforeExit { get; set; }

        public Int32 EngineVersion { get; set; }

        public List<String> FilesToProcess { get; set; }

        public List<String> FoldersToProcess { get; set; }


        //
        // Constructor
        //

        public Config()
        {
            //
            // Set initial values
            //

            Mode = OperationMode.Help;

            WaitForUserInputBeforeExit = false;

            EngineVersion = 0;

            FilesToProcess = new List<String>();
            FoldersToProcess = new List<String>();
        }


        //
        // Scan args to set options
        //

        public void Set(String[] args)
        {
            //
            // Scan arguments
            //

            for (int i = 0; i < args.Length; i++)
            {
                String currentArg = args[i];

                if (currentArg == "-c")
                {
                    // Create archive
                    Mode = OperationMode.CreateArchive;
                }
                else if (currentArg == "-e")
                {
                    // Extract archive
                    Mode = OperationMode.ExtractArchive;
                }
                else if (currentArg == "-p")
                {
                    // Print archive info
                    Mode = OperationMode.PrintArchiveInfo;
                }
                else if (currentArg == "-v")
                {
                    //
                    // Set engine version
                    //

                    // When -v is provided, the next argument is the engine version
                    i++;

                    if (i == args.Length)
                    {
                        // -v is the last argument and no version is provided
                        throw new Exception("Can't find engine version");
                    }

                    // Try parsing the provided engine version
                    if (Int32.TryParse(args[i], out int engine))
                    {
                        EngineVersion = engine;
                    }
                    else
                    {
                        throw new Exception("Failed parsing engine version");
                    }
                }
                else if (currentArg == "-w")
                {
                    // Wait for user input before exit
                    WaitForUserInputBeforeExit = true;
                }
                else if (currentArg.EndsWith(".ypf") && File.Exists(currentArg))
                {
                    // Detected file
                    FilesToProcess.Add(Path.GetFullPath(currentArg));
                }
                else if (Directory.Exists(currentArg))
                {
                    // Detected folder
                    FoldersToProcess.Add(Path.GetFullPath(currentArg));
                }
            }


            //
            // Detect edge cases
            //

            if ((Mode == OperationMode.ExtractArchive || Mode == OperationMode.PrintArchiveInfo) && FilesToProcess.Count == 0)
            {
                throw new Exception("Can't find files to process");
            }
            else if (Mode == OperationMode.CreateArchive)
            {
                if (EngineVersion <= 0)
                {
                    throw new Exception("Invalid engine version");
                }

                if (FoldersToProcess.Count == 0)
                {
                    throw new Exception("Can't find folders to process");
                }
            }
        }

    }
}
