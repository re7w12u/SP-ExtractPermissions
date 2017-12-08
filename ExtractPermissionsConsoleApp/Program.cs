using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint;
using System.IO;
using System.Threading;
using System.Drawing;

namespace ExtractPermissionsConsoleApp
{
    class Program
    {
        static Program p;
        static void Main(string[] args)
        {
            ExtractionParam param = ParamReader.Load();

            SPSecurity.RunWithElevatedPrivileges(() =>
            {
                SPWebService svc = SPFarm.Local.Services.GetValue<SPWebService>();
                SPWebApplicationCollection webapps = svc.WebApplications;

                foreach (SPWebApplication wa in webapps)
                {
                    WebAppParam waParam = param.WebApps.SingleOrDefault(x => x.Name.Trim() == wa.Name.Trim());
                    if (waParam == null)
                    {
                        Console.WriteLine("Parameter not found. Skipping {0}", wa.Name);
                    }
                    else if (waParam.Check)
                    {
                        using (PermChecker pc = new PermChecker(param.Sesas, waParam.Exceptions, wa))
                        {
                            pc.GetCount();
                            pc.Run();
                        }
                    }
                }
            });




            Console.WriteLine("DONE.\r\nType any key to exit");
            Console.ReadLine();
        }




    }



}
