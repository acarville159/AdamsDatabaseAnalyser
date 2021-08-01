using System;
using System.Collections.Generic;
using System.Text;

namespace AdamsDatabaseAnalyser
{
    class ErrorManager
    {
        public static bool showErrors = false;

        public static int ErrorCount = 0;

        public static void Log(string err)
        {
            if (showErrors)
            {
                Console.WriteLine("X -> " + err);
            }
            ErrorCount++;
        }
    }
}
