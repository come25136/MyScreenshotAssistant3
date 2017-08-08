using System;
using System.Diagnostics;
using System.Windows;

namespace msa3
{
    /// <summary>
    /// UpdateWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private App app = (App)Application.Current;

        public UpdateWindow()
        {
            InitializeComponent();


            New_version_Label.Content = Update.updatejson.version;
            Current_version_Label.Content = app.Version;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("AutoUpdate.exe", $"{Process.GetCurrentProcess().Id.ToString()} {Update.updatejson.dlurl} {Update.updatejson.name}.{Update.updatejson.type} {Update.updatejson.name} {Update.updatejson.hash} msa3.db msa3.log");
            Environment.Exit(0);
        }
    }
}
