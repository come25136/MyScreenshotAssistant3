using System.Windows;

namespace msa3
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public string SoftwareName
        {
            get { return System.Windows.Forms.Application.ProductName; }
        }

        public string Version
        {
            get { return System.Windows.Forms.Application.ProductVersion; }
        }

        public string SoftwareTitle
        {
            get { return SoftwareName + " ver:" + Version; }
        }
    }
}
