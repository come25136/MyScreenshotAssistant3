using CoreTweet;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;

namespace msa3
{
    /// <summary>
    /// LoginWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LoginWindow : Window
    {
        OAuth.OAuthSession session = CoreTweet.OAuth.Authorize(API_Keys.consumerKey, API_Keys.consumerSecret);

        string url; // OAuth_url

        public LoginWindow()
        {
            App app = (App)Application.Current;

            Title = $"{app.SoftwareTitle} - Login";

            InitializeComponent();
        }

        // 認証用urlを既定のブラウザで開く
        private void OAuth_url_open_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(url);
        }

        // 認証ボタンがクリックされた時に実行される
        private void OAuth_Button_Click(object sender, RoutedEventArgs e)
        {
            OAuth();
        }

        // PINコード入力欄でEnterキーが押された時に実行される
        private void OAuth_pin_Textbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) OAuth();
        }

        // 認証処理
        private void OAuth()
        {
            OAuth_pin_Textbox.Text = OAuth_pin_Textbox.Text.Trim();
            if (OAuth_pin_Textbox.Text == "")
            {
                //何も入力されていない場合
                Method.Message("Error", "PINコードを入力してください");
            }
            else if (IsNotDigit(OAuth_pin_Textbox.Text))
            {
                // 半角数字以外の文字列が入力されていた場合
                Method.Message("Error", "半角数字を入力してください");
            }
            else
            {
                try
                {
                    Tokens tokens = CoreTweet.OAuth.GetTokens(session, OAuth_pin_Textbox.Text);
                    Tokens info = Tokens.Create(API_Keys.consumerKey, API_Keys.consumerSecret, tokens.AccessToken, tokens.AccessTokenSecret);

                    OAuth_pin_Textbox.Text = "";

                    DataRow datarow = MainWindow.AccountTable.NewRow();

                    datarow["TwitterId"] = info.Account.VerifyCredentials().ScreenName;
                    datarow["AccessToken"] = tokens.AccessToken;
                    datarow["AccessTokenSecret"] = tokens.AccessTokenSecret;
                    MainWindow.AccountTable.Rows.Add(datarow);

                    Method.Logfile("Info", "Authentication success.");

                    Hide();
                }
                catch (TwitterException)
                {
                    // PINコードが間違っている場合
                    Method.Message("Error", "正しいPINコードを入力してください");
                }
            }
        }

        // url取得
        public void OAuth_url()
        {
            url = session.AuthorizeUri.ToString();
            OAuth_url_Textbox.Text = url;
        }

        // 半角数字検証
        public bool IsNotDigit(string context)
        {
            int length = context.Length; // 多分これが一番早いと思います
            for (int i = 0; i < length; i++)
            {
                char c = context[i];
                if ('0' > c || c > '9') return true;
            }
            return false;
        }
    }
}
