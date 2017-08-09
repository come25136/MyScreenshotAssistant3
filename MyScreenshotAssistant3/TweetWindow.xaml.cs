using System;
using System.Windows;
using System.Windows.Input;

namespace msa3
{
    /// <summary>
    /// TweetWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetWindow : Window
    {
        private App app = (App)Application.Current;

        public static string value;
        public static bool cancel_flag = true;

        public TweetWindow()
        {
            Title = $"{app.SoftwareTitle} - TweetWindow";

            InitializeComponent();

            Tweet_value_TextBox.MaxLength = MainWindow.count;

            Value_Counter_Label.Content = MainWindow.count;

            Tweet_value_TextBox.Focus();

            cancel_flag = true;

            ContentRendered += (s, e) => { Activate(); };
        }

        private void Tweet_value_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // ツイート
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Enter))
            {
                value = $"\n{Tweet_value_TextBox.Text}";
                cancel_flag = false;
                Close();
            }
            else if (Keyboard.IsKeyDown(Key.Escape))
            {
                Close();
            }
        }

        private void Tweet_value_TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Value_Counter_Label.Content = MainWindow.count - Tweet_value_TextBox.Text.Length;
        }
    }
}
