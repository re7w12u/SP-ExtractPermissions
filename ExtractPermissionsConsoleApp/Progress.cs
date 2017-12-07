using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractPermissionsConsoleApp
{
    public class Progress
    {
        string[] symbols = new string[4] { "-", "\\", "|", "/" };
        int current = 0;
        decimal i = 0;
        decimal total = 0;

        public void Increment()
        {
            Console.Write("{0}\r", GetSymbol());
        }

        private string GetSymbol()
        {
            string result = String.Format("{0}", symbols[current % symbols.Length]);
            current++;
            return result;
        }

        public decimal Total
        {
            get
            {
                i++;
                return total;
            }
            set
            {
                i = 0;
                total = value;
            }
        }

        public string Percent()
        {
            return String.Format("{0} {1}%\r", GetSymbol(), Math.Round(i / Total * 100, 2));
        }

        public void PrintPercent()
        {
            string percent = Percent();
            Console.Write(percent);
        }
    }
}
