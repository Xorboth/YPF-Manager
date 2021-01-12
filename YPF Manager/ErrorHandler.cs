using System;
using System.IO;
using System.Text;

namespace Ypf_Manager
{
    class ErrorHandler
    {

        //
        // Singleton setup
        //

        private static ErrorHandler instance = null;

        public static ErrorHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ErrorHandler();
                }

                return instance;
            }
        }


        //
        // Private variables
        //

        private StringBuilder sb;


        //
        // Constructor
        //

        public ErrorHandler()
        {
            sb = new StringBuilder();
        }


        //
        // Notify a new error
        //

        public void NotifyError(String message, int level)
        {
            sb.Append(DateTime.Now.ToString("[dd/MM/yyyy HH:mm:ss] "));
            sb.Append("[");
            sb.Append(level);
            sb.Append("] ");
            sb.Append(message);
            sb.Append('\n');

            // If level is 1 then throw an exception
            if (level == 1)
            {
                throw new Exception(message);
            }
        }


        //
        // Save the log into a file
        //

        public void SaveLog()
        {
            String outputFile = $@"{Config.ExecutableLocation()}\log.txt";

            File.AppendAllText(outputFile, sb.ToString(), new UTF8Encoding(false));

            ClearLog();
        }


        //
        // Clear the log
        //

        public void ClearLog()
        {
            sb = new StringBuilder();
        }
    }
}
