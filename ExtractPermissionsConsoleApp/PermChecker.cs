using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtractPermissionsConsoleApp
{
    class PermChecker : IDisposable
    {

        List<string> Sesas;
        List<Permission> Permissions;
        string OutputPath;
        string LogPath;
        StreamWriter OutputFile;
        StreamWriter LogFile;
        Progress Progress;
        string[] Exceptions;
        SPWebApplication webApp;
        ConcurrentQueue<Permission> Data;
        ConcurrentQueue<PermissionError> Errors;

        public PermChecker(List<string> sesas, string[] exceptions, SPWebApplication webApplication)
        {
            this.Sesas = sesas;
            webApp = webApplication;
            Exceptions = exceptions;
            Init();
        }

        void Init()
        {
            Permissions = new List<Permission>();
            long ticks = DateTime.Now.Ticks;
            OutputPath = String.Format(@"{0}ExtractPermissions-{1}-{2}.csv", AppDomain.CurrentDomain.BaseDirectory, webApp.Name, ticks);
            LogPath = String.Format(@"{0}ExtractPermissions-ERROR-{1}-{2}.log", AppDomain.CurrentDomain.BaseDirectory, webApp.Name, ticks);
            //OutputFile = new System.IO.StreamWriter(OutputPath, true);
            //OutputFile.AutoFlush = true;
            //LogFile = new System.IO.StreamWriter(LogPath, true);
            //LogFile.AutoFlush = true;
            Progress = new Progress();
            Data = new ConcurrentQueue<Permission>();
            Errors = new ConcurrentQueue<PermissionError>();
        }

        public void GetCount()
        {
            int count = 0;
            ConcurrentQueue<int> Count = new ConcurrentQueue<int>();
            Console.WriteLine("Initializing " + webApp.Name);

            Action<SPFolder> countFolder = null;
            countFolder = (f) =>
            {
                Count.Enqueue(1);
                foreach (SPFolder subF in f.SubFolders)
                {
                    countFolder(subF);
                }
            };

            Action<SPWeb> CountFolderInWeb = null;
            CountFolderInWeb = (w) =>
            {
                foreach (SPList l in w.Lists)
                {
                    if (l is SPDocumentLibrary && !l.Hidden)
                    {
                        countFolder(l.RootFolder);
                    }
                }

                SPWebCollection webs = w.GetSubwebsForCurrentUser();
                Parallel.ForEach(webs, (subWeb)=>
                {
                    //Console.Write("{0}  --  {1}\r", Thread.CurrentThread.ManagedThreadId, Count.Sum());
                    CountFolderInWeb(subWeb);
                });

                if (w != null) w.Dispose();
            };

            SPSiteCollection sites = webApp.Sites;

            Parallel.ForEach(sites, (s, loopState) =>
            {
                CountFolderInWeb(s.RootWeb);                
                if (s != null) s.Dispose();
            });

            Progress.Total = Count.Sum();
            Console.WriteLine();
            Console.WriteLine("[OK] {0} folders found. Proceeding...", Count.Sum());
        }

        public void Run()
        {
            Console.WriteLine("Writing to {0}", OutputPath);

            SPSiteCollection sites = webApp.Sites;
            Parallel.ForEach(sites, (s) =>
            {
                if (!Exceptions.Contains(s.Url.ToLower()))
                {
                    CheckWebPermissions(s.RootWeb);
                    if (s != null) s.Dispose();
                }
            });

            File.WriteAllLines(OutputPath, Data.Select(x => x.ToString()).ToArray());
            File.WriteAllLines(LogPath, Errors.Select(x => x.ToString()));            
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
            try
            {
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

                                    //WriteOuput(perm);
                                    Data.Enqueue(perm);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //WriteError(folder, ex);
                Errors.Enqueue(new PermissionError {
                    Url = String.Format("{0}/{1}", folder.ParentWeb.Url, folder.Url),
                    Date = DateTime.Now.ToLongTimeString(),
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }

            foreach (SPFolder f in folder.SubFolders)
            {
                GetSecurablePermissions(f);
            }

        }

        private void WriteOuput(Permission perm)
        {
            OutputFile.WriteLine(perm.ToString());
        }

        private void WriteError(SPFolder f, Exception err)
        {
            LogFile.WriteLine(String.Format("------ {0} -------------------------", DateTime.Now.ToLongTimeString()));
            LogFile.WriteLine(String.Format("{0}/{1}", f.ParentWeb.Url, f.Url));
            LogFile.WriteLine(err.Message);
            LogFile.WriteLine(err.StackTrace);
        }

        private List<string> GetUsers(SPPrincipal member)
        {
            List<string> result = new List<string>();

            Action<string> BelongsTo = (login) =>
            {
                if (login.Contains(@"\"))
                {
                    if (Sesas.Contains(login.ToUpper().Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries)[1]))
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
            if (OutputFile != null) OutputFile.Dispose();
            if (LogFile != null) LogFile.Dispose();
        }
    }
}
