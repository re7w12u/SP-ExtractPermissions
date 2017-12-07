using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractPermissionsConsoleApp
{
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
