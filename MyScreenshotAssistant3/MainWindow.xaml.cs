using CoreTweet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace msa3
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public static int count;

        private NotifyIcon notifyIcon1;
        private Icon[] Icons = new Icon[] { Properties.Resources.Progress_0, Properties.Resources.Progress_1, Properties.Resources.Progress_2, Properties.Resources.Progress_3 };

        private Tokens tokens = null;
        private List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();

        private bool IconFlag = false;

        public static DataTable AccountTable = new DataTable();
        private DataTable ApplicationTable = new DataTable();
        private DataTable DirectoryTable = new DataTable();

        private SQLiteDataAdapter AccountAdapter;
        private SQLiteDataAdapter DirectoryAdapter;
        private SQLiteDataAdapter ApplicationAdapter;

        private HttpClient hc = new HttpClient();

        private App app = (App)System.Windows.Application.Current;

        private LoginWindow LoginWindow = new LoginWindow();

        public MainWindow()
        {
            InitializeComponent();

            Title = app.SoftwareTitle;

            Value_Counter_Label.Content = Tweet_fixed_value_TextBox.MaxLength;

            count = Tweet_fixed_value_TextBox.MaxLength;

            Update.Start();

            Method.Sql_connect();

            Tasktry();

            // アカウントデータの復元
            AccountAdapter = new SQLiteDataAdapter("SELECT * FROM Account", Method.database);
            AccountAdapter.Fill(AccountTable);

            Twitter_id.ItemsSource = AccountTable.DefaultView;
            Twitter_id.DisplayMemberPath = "TwitterId";
            Twitter_id.SelectedValuePath = "TwitterId";

            DataRow New_account = AccountTable.NewRow();
            New_account["TwitterId"] = "アカウントを追加";
            AccountTable.Rows.InsertAt(New_account, 0);

            // フォルダデータの復元
            DirectoryAdapter = new SQLiteDataAdapter("SELECT * FROM DirectoryData", Method.database);
            DirectoryAdapter.Fill(DirectoryTable);

            DirectoryData_DataGrid.ItemsSource = DirectoryTable.DefaultView;

            // アプリケーションデータの復元
            ApplicationAdapter = new SQLiteDataAdapter("SELECT * FROM ApplicationData", Method.database);
            ApplicationAdapter.Fill(ApplicationTable);

            DataRow[] apptable = ApplicationTable.Select($"AppName = '{app.SoftwareName}'");
            Twitter_id.Text = apptable[0]["TwitterId"].ToString();
            Tweet_fixed_value_TextBox.Text = apptable[0]["fixed"].ToString();
            Tweet_Hashtag_value_TextBox.Text = apptable[0]["hashtag"].ToString();
            wait_time.Text = apptable[0]["wait_time"].ToString();
            ACAK.Text = apptable[0]["ACAK"].ToString();
        }

        // アカウント選択
        private void Twitter_id_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Task.Run(async () =>
            {
                Dispatcher.Invoke(() => { ProgressBar.Visibility = Visibility.Visible; });

                if (Dispatcher.Invoke(() => { return Twitter_id.SelectedValue.ToString(); }) == "アカウントを追加")
                {
                    Dispatcher.Invoke(() =>
                    {
                        LoginWindow.OAuth_url();
                        LoginWindow.ShowDialog();

                        ProgressBar.Visibility = Visibility.Hidden;

                        tokens = null;
                        Twitter_icon_Image.ImageSource = null;
                        Twitter_name.Content = "アカウントを選択してください";
                    });

                    return;
                }

                int row = Dispatcher.Invoke(() => { return Twitter_id.SelectedIndex; });

                try
                {
                    // Twitter API 認証情報
                    tokens = Tokens.Create(API_Keys.consumerKey, API_Keys.cosumerSecret, AccountTable.Rows[row]["AccessToken"].ToString(), AccountTable.Rows[row]["AccessTokenSecret"].ToString());

                    // Twitterアカウントアイコン取得
                    using (MemoryStream stream = new MemoryStream(await hc.GetByteArrayAsync(tokens.Account.VerifyCredentials().ProfileImageUrlHttps.Replace("normal", "bigger")).ConfigureAwait(false)))
                    {
                        try
                        {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.StreamSource = stream;
                            image.EndInit();
                            image.Freeze();

                            Dispatcher.Invoke(() => { Twitter_icon_Image.ImageSource = image; });
                        }
                        catch (Exception)
                        {
                            Dispatcher.Invoke(() => { Twitter_icon_Image.ImageSource = null; });
                        }
                    }

                    Dispatcher.Invoke(() => { Twitter_name.Content = tokens.Account.VerifyCredentials().Name; });
                }
                catch (TwitterException er)
                {
                    if (er.Message == "Could not authenticate you.")
                    {
                        Method.Message("Twitter認証エラー", "Twitterにログインできませんでした、連動し直してください");

                        Dispatcher.Invoke(() => { Twitter_name.Content = "Twitterにログインできません"; });
                    }
                    else
                    {
                        Method.Message("Twitter認証エラー", "Twitterへの認証回数が上限に達しました、しばらくしてから再度お試しください");

                        Dispatcher.Invoke(() => { Twitter_name.Content = "アカウント情報を取得できません"; });
                    }

                    Method.Logfile("Warning", er.Message);
                }

                Dispatcher.Invoke(() => { ProgressBar.Visibility = Visibility.Hidden; });
            });
        }

        // Start, Stopボタン
        private void Tweet_Button_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)Tweet_Button.IsChecked) { Watcher_Start(); } else { Watcher_Stop(); }
        }

        // フォルダの監視を開始 (メソッド)
        private void Watcher_Start()
        {
            if (tokens == null)
            {
                Tweet_Button.IsChecked = false;
                Method.Message("Error", "Twitterアカウントが選択されていません");
                return;
            }

            if (wait_time.Text == string.Empty)
            {
                Tweet_Button.IsChecked = false;
                Method.Message("Error", "スクリーンショット検知からツイートまでの待機時間が指定されていません");
                return;
            }

            int count = 0;

            foreach (DataRow row in DirectoryTable.Rows)
            {
                if (row["watch"].ToString() != string.Empty && (bool)row["watch"])
                {
                    try
                    {
                        Watchers.Add(new FileSystemWatcher());

                        Watchers[Watchers.Count - 1].Path = (String)row["path"];
                        Watchers[Watchers.Count - 1].NotifyFilter =
                            (NotifyFilters.LastAccess
                            | NotifyFilters.LastWrite
                            | NotifyFilters.FileName
                            | NotifyFilters.DirectoryName);
                        Watchers[Watchers.Count - 1].Filter = "*.png";

                        Watchers[Watchers.Count - 1].Created += new FileSystemEventHandler(Watcher_Changed);

                        Watchers[Watchers.Count - 1].EnableRaisingEvents = true;

                    }
                    catch (ArgumentException)
                    {
                        Tweet_Button.IsChecked = false;
                        Method.Message("Error", $"{row["path"]} は存在しません");
                        return;
                    }
                }
                else
                {
                    count++;
                }
            }

            if (DirectoryTable.Rows.Count == count)
            {
                Tweet_Button.IsChecked = false;
                Method.Message("Error", "監視対象のフォルダがありません");
                return;
            }

            notifyIcon1.Icon = Properties.Resources.done;

            Title = $"{app.SoftwareTitle} - Status start";

            Method.Logfile("Info", "Assistant start.");

            Tasktray_notify("Info", "Assistant start");
        }

        // フォルダの監視を止める (メソッド)
        private void Watcher_Stop(bool flag = true)
        {
            foreach (FileSystemWatcher watcher in Watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            if (flag)
            {
                Title = $"{app.SoftwareTitle} - Status stop";

                Method.Logfile("Info", "Assistant stop.");

                Tasktray_notify("Info", "Assistant stop");

                notifyIcon1.Icon = Properties.Resources.warning;
            }
        }

        // フォルダに変化があった時の処理
        private void Watcher_Changed(object source, FileSystemEventArgs e)
        {
            Task.Run(() =>
            {
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    Thread.Sleep(Dispatcher.Invoke(() => { return Convert.ToInt32(wait_time.Text); }));

                    try
                    {
                        FileStream file = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                        if (file.Length == 0)
                        {
                            Method.Logfile("Warning", $"There is no data in the file {e.FullPath}");

                            Tasktray_notify("Warning", "ファイルの中にデータがありません\n待機時間を調整してください");

                            return;
                        }

                        try
                        {
                            Tasktray_animation();

                            if (file.Length < 5142880 || file.Length < 83786080 && Dispatcher.Invoke(() => { return ACAK.Text; }) != string.Empty)
                            {
                                Dispatcher.Invoke(() => { new TweetWindow().ShowDialog(); });
                                if (TweetWindow.cancel_flag) return;

                                String tweet_value = TweetWindow.value;

                                // ファイルサイズの確認(5MB)
                                if (file.Length < 5142880)
                                {
                                    tokens.Statuses.Update(
                                        status: Dispatcher.Invoke(() => { return $"{Tweet_fixed_value_TextBox.Text.Replace(@"\n", "\n")}{tweet_value} {Tweet_Hashtag_value_TextBox.Text.Replace(@"\n", "\n")}"; }),
                                        media_ids: new long[] { tokens.Media.Upload(media: file).MediaId }
                                    );

                                    Method.Logfile("Info", "Success tweet.");
                                }
                                else if (file.Length < 83786080 && Dispatcher.Invoke(() => { return ACAK.Text; }) != string.Empty)
                                {
                                    MultipartFormDataContent form = new MultipartFormDataContent();

                                    StreamContent content = new StreamContent(file);
                                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "file", FileName = e.Name };

                                    form.Add(content);

                                    HttpResponseMessage response = hc.PostAsync($"https://api.ppn.pw/v2/img_uploader.php?key={Dispatcher.Invoke(() => { return ACAK.Text; })}", form).Result;
                                    Arksoft_CorkBoard_API result = JsonConvert.DeserializeObject<Arksoft_CorkBoard_API>(response.Content.ReadAsStringAsync().Result);

                                    if (result != null && result.status == "success")
                                    {
                                        tokens.Statuses.Update(
                                            status: Dispatcher.Invoke(() => { return $"{Tweet_fixed_value_TextBox.Text.Replace(@"\n", "\n")}{tweet_value} {Tweet_Hashtag_value_TextBox.Text.Replace(@"\n", "\n")}\n{result.img_url}"; })
                                        );
                                    }
                                    else
                                    {
                                        Tasktray_notify("Warning", "CorkBoardにアップロードできませんでした");
                                        Method.Logfile("Warning", $"Could not upload to CorkBoard {e.FullPath}");
                                    }
                                }
                                else
                                {
                                    Tasktray_notify("Warning", "ファイルサイズが5MBを超えています");
                                    Method.Logfile("Warning", $"File size over 5MB {e.FullPath}");
                                }
                            }
                        }
                        catch (Exception er)
                        {
                            Tasktray_notify("Warning", "ツイートに失敗しました");
                            Method.Logfile("Warning", er.Message);
                        }
                        finally
                        {
                            IconFlag = false;
                        }
                    }
                    catch (IOException er)
                    {
                        Tasktray_notify("Warning", "ファイルにアクセスできません\n待機時間を調整してください");
                        Method.Logfile("Warning", er.Message);
                    }
                }
            });
        }

        // ウィンドウを閉じた時の処理
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Hide();

            Tasktray_notify("Info", "タスクトレイに常駐しています");
        }

        // 終了処理
        private void Exit(object sender, EventArgs e)
        {
            if ((bool)Tweet_Button.IsChecked) { Watcher_Stop(); } else { Watcher_Stop(false); }

            // アカウント情報を保存
            try
            {
                AccountTable.Rows.RemoveAt(0);

                new SQLiteCommandBuilder(AccountAdapter).GetUpdateCommand();

                AccountAdapter.Update(AccountTable);
            }
            catch (Exception) { }

            // フォルダデータを保存
            try
            {
                new SQLiteCommandBuilder(DirectoryAdapter).GetUpdateCommand();

                DirectoryAdapter.Update(DirectoryTable);
            }
            catch (Exception)
            {
                Method.Message("Warning", "同じフォルダ設定が存在します");
                return;
            }

            // アプリケーションデータを保存
            try
            {
                DataRow[] apptable = ApplicationTable.Select($"AppName = '{app.SoftwareName}'");

                apptable[0]["TwitterId"] = Twitter_id.Text;
                apptable[0]["fixed"] = Tweet_fixed_value_TextBox.Text;
                apptable[0]["hashtag"] = Tweet_Hashtag_value_TextBox.Text;
                apptable[0]["wait_time"] = wait_time.Text;
                apptable[0]["ACAK"] = ACAK.Text;

                new SQLiteCommandBuilder(ApplicationAdapter).GetUpdateCommand();

                ApplicationAdapter.Update(ApplicationTable);
            }
            catch (Exception) { }

            Method.database.Close();

            Method.Logfile("Info", "disconnect database.");

            Method.Logfile("Info", "Application Exit.");

            notifyIcon1.Dispose();

            Environment.Exit(0);
        }

        // タスクトレイ登録処理
        private void Tasktry()
        {
            ContextMenuStrip menuStrip = new ContextMenuStrip();

            ToolStripMenuItem exitItem = new ToolStripMenuItem()
            {
                Text = "終了"
            };

            menuStrip.Items.Add(exitItem);
            exitItem.Click += new EventHandler(Exit);

            notifyIcon1 = new NotifyIcon()
            {
                Text = Title,
                Icon = Properties.Resources.warning,

                BalloonTipTitle = app.SoftwareTitle,

                Visible = true
            };

            notifyIcon1.DoubleClick += new EventHandler(Tasktray_click);

            notifyIcon1.ContextMenuStrip = menuStrip;
        }

        // タスクトレイクリック
        private void Tasktray_click(object sender, EventArgs e)
        {
            Show();
        }

        // タスクトレイ通知
        private void Tasktray_notify(string level, string value)
        {
            if (level == "Info")
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            }
            else if (level == "Warning")
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
            }
            else if (level == "Error")
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
            }
            else
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.None;
            }

            notifyIcon1.BalloonTipText = value;
            notifyIcon1.ShowBalloonTip(2000);
        }

        // タスクトレイ アニメーション
        private void Tasktray_animation()
        {
            Task.Run(() =>
            {
                int i;
                IconFlag = true;
                while (IconFlag)
                {
                    for (i = 0; i <= 3; i++)
                    {
                        notifyIcon1.Icon = Icons[i];
                        Thread.Sleep(200);
                    }
                }
                notifyIcon1.Icon = Properties.Resources.done;
            });
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DirectoryTable.Rows.RemoveAt(DirectoryData_DataGrid.SelectedIndex);
            }
            catch (Exception) { }
        }

        private void Tweet_fixed_value_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    count = Tweet_fixed_value_TextBox.MaxLength - Tweet_fixed_value_TextBox.Text.Length - Tweet_Hashtag_value_TextBox.Text.Length;
                    Value_Counter_Label.Content = count;
                }
                catch { }
            });
        }

        private void Tweet_Hashtag_value_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            count = Tweet_fixed_value_TextBox.MaxLength - Tweet_fixed_value_TextBox.Text.Length - Tweet_Hashtag_value_TextBox.Text.Length;
            Value_Counter_Label.Content = count;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

    }
}
