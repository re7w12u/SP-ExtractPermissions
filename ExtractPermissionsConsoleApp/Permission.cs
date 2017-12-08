using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractPermissionsConsoleApp
{

    class PermissionError
    {
        public string Url { get; set; }
        public string Date { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }

        public override string ToString()
        {
            return String.Format("------ {1} -------------------------{0}{2}{0}{3}{0}{4}",
                Environment.NewLine,
                Date,
                Url,
                Message,
                StackTrace);
        }
    }

    class Permission
    {
        public string SESA { get; set; }
        public string Url { get; set; }
        public List<string> Permissions { get; set; }
        public string Group { get; set; }

        public override string ToString()
        {
            string p = String.Join(" - ", Permissions.ToArray());
            return String.Format("{0};{1};{2};{3}", SESA, Url, Group, p);
        }
    }
}
