using System;
using System.Collections.Generic;
using System.Windows;

namespace AutoUpdate
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static int MainPid = -1;
        public static string dlurl = null;
        public static string dlname = null;
        public static string appname = null;
        public static string hash = null;
        public static List<string> argument = new List<string>();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                MainPid = Convert.ToInt32(e.Args[0]);
                dlurl = e.Args[1];
                dlname = e.Args[2];
                appname = e.Args[3];
                hash = e.Args[4];

                for (int i = 5; i < e.Args.Length; ++i)
                {
                    argument.Add(e.Args[i]);
                }

            }
            catch
            {
                Environment.Exit(1);
            }
        }
    }
}
