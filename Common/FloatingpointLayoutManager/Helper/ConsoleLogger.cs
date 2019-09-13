using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper
{
    public static class ConsoleLogger
    {
        private static bool isDebugModeOn = false;

        public static void Write(string message)
        {
            if (isDebugModeOn)
            {
                Console.WriteLine(message);
            }
        }
    }
}
