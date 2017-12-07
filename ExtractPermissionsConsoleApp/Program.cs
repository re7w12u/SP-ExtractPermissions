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
    class Program : IDisposable
    {
        static Program p;
        static void Main(string[] args)
        {

            using (p = new Program())
            {
                p.init();
                p.GetCount();
                p.Run();
            }

            Console.WriteLine("DONE.\r\nType any key to exit");
            Console.ReadLine();
        }


        List<string> sesas;
        List<Permission> Permissions { get; set; }
        public string Path { get; set; }
        public StreamWriter file { get; set; }
        public Progress Progress { get; set; }
        public List<string> Exceptions { get; set; }

        void init()
        {
            sesas = new List<string>
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
            Permissions = new List<Permission>();
            Path = String.Format(@"{0}ExtractPermissions-{1}.csv", AppDomain.CurrentDomain.BaseDirectory, DateTime.Now.Ticks);
            file = new System.IO.StreamWriter(p.Path, true);
            file.AutoFlush = true;
            Exceptions = new List<string>
                {
                    "http://projects-sharing/sites/office_viewing_service_cache"
                };
        }

        void GetCount()
        {
            Progress = new Progress();
            int count = 0;
            Console.WriteLine("Initializing...");
            SPWebService svc = SPFarm.Local.Services.GetValue<SPWebService>();
            SPWebApplicationCollection webapps = svc.WebApplications;

            Action<SPFolder> countFolder = null;
            countFolder = (f) =>
            {
                count++;
                foreach (SPFolder subF in f.SubFolders)
                {
                    countFolder(subF);
                }
            };

            foreach (SPWebApplication wa in webapps)
            {
                SPSiteCollection sites = wa.Sites;

                Parallel.ForEach(sites, (s) =>
                {
                    if (!Exceptions.Contains(s.RootWeb.Url.ToLower()))
                    {
                        SPWebCollection webs = s.AllWebs;
                        Parallel.ForEach(webs, (w) =>
                        {
                            Console.WriteLine("{0}  --  {1}", Thread.CurrentThread.ManagedThreadId, w.Url);
                            foreach (SPList l in w.Lists)
                            {
                                if (l is SPDocumentLibrary && !l.Hidden)
                                {
                                    countFolder(l.RootFolder);
                                }
                            }
                            if (w != null) w.Dispose();
                        });
                    }
                    if (s != null) s.Dispose();
                });
            }
            Progress.Total = count;
            Console.WriteLine("[OK] {0} folders found. Proceeding...", count);
        }

        void Run()
        {
            Console.WriteLine("Writing to {0}", Path);

            SPWebService svc = SPFarm.Local.Services.GetValue<SPWebService>();
            SPWebApplicationCollection webapps = svc.WebApplications;

            foreach (SPWebApplication wa in webapps)
            {
                SPSiteCollection sites = wa.Sites;
                Parallel.ForEach(sites, (s) =>
                {
                    CheckWebPermissions(s.RootWeb);
                    if (s != null) s.Dispose();
                });
            }
        }

        private void CheckWebPermissions(SPWeb web)
        {
            SPListCollection lists = web.Lists;
            foreach (SPList l in lists)
            {
                if (l is SPDocumentLibrary && !l.Hidden)
                {
                    GetSecurablePermissions(l.RootFolder);
                }
            }

            SPWebCollection webs = web.Webs;
            Parallel.ForEach(webs, (w) =>
            {
                CheckWebPermissions(w);
            });

            if (web != null) web.Dispose();
        }

        private void GetSecurablePermissions(SPFolder folder)
        {
            Progress.PrintPercent();
            if (folder.Item != null)
            {
                foreach (SPRoleAssignment ra in folder.Item.RoleAssignments)
                {
                    IEnumerable<string> roleDefs = ra.RoleDefinitionBindings.Cast<SPRoleDefinition>().Where(x => x.Name != "Limited Access").Select(x => x.Name);
                    if (roleDefs.Count() > 0)
                    {
                        List<string> users = GetUsers(ra.Member);
                        if (users.Count > 0)
                        {

                            string url = String.Format("{0}/{1}", folder.ParentWeb.Url, folder.Url);
                            string group = ra.Member.Name;
                            List<string> permissions = roleDefs.ToList();

                            foreach (string u in users)
                            {
                                Permission perm = new Permission
                                {
                                    SESA = u,
                                    Url = url,
                                    Group = group,
                                    Permissions = permissions
                                };

                                WriteOuput(perm);
                                //Permissions.Add(perm);
                            }
                        }
                    }
                }
            }

            foreach (SPFolder f in folder.SubFolders)
            {
                GetSecurablePermissions(f);
            }

        }

        private void WriteOuput(Permission perm)
        {
            file.WriteLine(perm.ToString());
        }

        private List<string> GetUsers(SPPrincipal member)
        {
            List<string> result = new List<string>();



            Action<string> BelongsTo = (login) =>
            {
                if (login.Contains(@"\"))
                {
                    if (sesas.Contains(login.ToUpper().Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries)[1]))
                    {
                        result.Add(login);
                    }
                }
            };


            if (member is SPUser)
            {
                BelongsTo((member as SPUser).LoginName);
            }

            else if (member is SPGroup)
            {
                SPGroup group = (member as SPGroup);
                foreach (SPUser user in group.Users)
                {
                    BelongsTo(user.LoginName);
                }
            }

            return result;
        }

        public void Dispose()
        {
            if (file != null) file.Dispose();
        }
    }



}
