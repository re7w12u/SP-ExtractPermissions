using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractPermissionsConsoleApp
{

    public class ExtractionParam
    {


        public List<string> Sesas { get; set; }

        public List<WebAppParam> WebApps { get; set; }

    }


    public class WebAppParam
    {
        public string Name { get; set; }
        public bool Check { get; set; }
        public string[] Exceptions { get; set; }
    }

    public static class ParamReader
    {

        public static ExtractionParam Load()
        {
            string fileName = AppDomain.CurrentDomain.BaseDirectory + @"\" + ConfigurationManager.AppSettings.Get("conf");
            string value = File.ReadAllText(fileName);
            ExtractionParam param = JsonConvert.DeserializeObject<ExtractionParam>(value);
            return param;
        }

        public static void Save()
        {
            string fileName = ConfigurationManager.AppSettings.Get("conf");

            ExtractionParam param = new ExtractionParam();

            param.Sesas = new List<string>
            {
               "SESA44806",
                "SESA49546",
                "SESA51948",
                "SESA38738",
                "SESA31241",
                "SESA23683",
                "SESA150795",
                "SESA70398",
                "SESA47749",
                "SESA52920",
                "SESA171296",                
                "SESA260501",
                "SESA355685"
            };

            param.WebApps = new List<WebAppParam>();

            param.WebApps.Add(new WebAppParam
            {
                Name = "SharePoint - Archive - 80",
                Exceptions = new string[] { },
                Check = false
            });


            param.WebApps.Add(new WebAppParam
            {
                Name = "SharePoint - eknowledge",
                Exceptions = new string[] { },
                Check = true
            });

            param.WebApps.Add(new WebAppParam
            {
                Name = "SharePoint - chartreuse - 80",
                Exceptions = new string[] { },
                Check = true
            });

            param.WebApps.Add(new WebAppParam
            {
                Name = "SharePoint - rdits - mysite - 80",
                Exceptions = new string[] { },
                Check = false
            });
            
            param.WebApps.Add(new WebAppParam
            {
                Name = "SharePoint -  rdits - security",
                Exceptions = new string[] { },
                Check = false
            });

            param.WebApps.Add(new WebAppParam
            {
                Name = "SharePoint -  rdits - teamsite - 80",
                Exceptions = new string[] { },
                Check = false
            });

            param.WebApps.Add(new WebAppParam
            {
                Name = "SharePoint -  rdits - tfs - portal - 80",
                Exceptions = new string[] { },
                Check = false
            });



            string result = JsonConvert.SerializeObject(param);

            File.WriteAllText(fileName, result);

        }

    }
}
