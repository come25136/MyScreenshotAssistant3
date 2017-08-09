using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace AutoUpdate
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        int blockSize = 1024;

        string roottemp = "temp";
        string temp = @"temp\";

        int count = 0;

        public MainWindow()
        {
            try
            {
                Process.GetProcessById(App.MainPid).WaitForExit(); // 終了待ち
            }
            catch { }

            InitializeComponent();

            Delete(roottemp, true);
            Directory.CreateDirectory(roottemp);

            //WebClientの作成
            WebClient downloadClient = new WebClient();

            //イベントハンドラの作成
            downloadClient.DownloadProgressChanged += DownloadClient_DownloadProgressChanged;
            downloadClient.DownloadFileCompleted += DownloadClient_DownloadFileCompleted;

            //非同期ダウンロードを開始する
            downloadClient.DownloadFileAsync(new Uri(App.dlurl), $"{temp}{App.dlname}");
        }

        private void DownloadClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            dl_ProgressBar.Value = e.ProgressPercentage;
            dl_progress_label.Content = e.ProgressPercentage + "%";
        }

        private void DownloadClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Task.Run(() =>
            {
                if (e.Error != null) Message("エラー: ファイルのダウンロードに失敗しました");

                Console.WriteLine($"{App.hash}\n{Gethash($"{temp}{App.dlname}")}");

                Console.WriteLine(string.Compare(App.hash, Gethash(temp + App.dlname), true));

                if (string.Compare(App.hash, Gethash($"{temp}{App.dlname}"), true) == 0)
                {
                    Dispatcher.Invoke(() => { dl_ProgressBar.Maximum = App.argument.Count; });

                    foreach (string cmd in App.argument)
                    {
                        try
                        {
                            File.Copy(cmd, $"{temp}{cmd}", true);

                            Progress_count();

                            if (Gethash(cmd) != Gethash($"{temp}{cmd}")) Message("エラー: ファイルのバックアップに失敗しました");
                        }
                        catch { }
                    }

                    Delete("./", false);

                    ZipFile.ExtractToDirectory($"{temp}{App.dlname}", temp);

                    CopyAndReplace($"{temp}{App.appname}", "./");

                    foreach (string cmd in App.argument)
                    {
                        try
                        {
                            File.Copy($"{temp}{cmd}", cmd, true);

                            if (Gethash(cmd) != Gethash($"{temp}{cmd}")) Message("エラー: ファイルの復元に失敗しました");
                        }
                        catch { }
                    }

                    Delete(roottemp, true);

                    Process.Start($"{App.appname}.exe");

                    Environment.Exit(0);
                }
            });
        }

        private void Message(string value)
        {
            System.Windows.Forms.MessageBox.Show(
                value,
                "AutoUpdate",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            Environment.Exit(1);
        }

        private void Progress_reset(double maxvalue)
        {
            count = 0;
            Dispatcher.Invoke(() => { dl_ProgressBar.Maximum = maxvalue; });
        }

        private void Progress_count()
        {
            count++;
            Dispatcher.Invoke(() => { dl_ProgressBar.Value = count; });
        }

        private string Gethash(string name)
        {
            // 進行状況を表示するための設定や変数
            int percent = 0;

            // ファイルをUTF-8エンコードでバイト配列化
            byte[] byteValue = File.ReadAllBytes(name);

            // SHA256のハッシュ値を取得する
            using (SHA256 crypto = new SHA256CryptoServiceProvider())
            {
                int length = byteValue.Length;
                decimal doneSize = 0;

                for (int offset = 0; offset < length; offset += blockSize)
                {
                    if (offset + blockSize < length)
                    {
                        crypto.TransformBlock(byteValue, offset, blockSize, null, 0);
                        doneSize = offset + blockSize;
                    }
                    else
                    {
                        crypto.TransformFinalBlock(byteValue, offset, length - offset);
                        doneSize = length;
                    }
                    if (percent != doneSize * 100 / length)
                    {
                        percent = (int)(doneSize * 100 / length);
                        Dispatcher.Invoke(() =>
                        {
                            dl_ProgressBar.Value = percent;
                            dl_progress_label.Content = $"{percent}%";
                        });
                    }
                }

                byte[] hashValue = crypto.Hash;

                StringBuilder hashedText = new StringBuilder();
                for (int i = 0; i < hashValue.Length; i++)
                {
                    hashedText.AppendFormat("{0:X2}", hashValue[i]);
                }

                return hashedText.ToString();
            }
        }

        /// <summary>
        /// ディレクトリを操作するクラス
        /// </summary>
        public void CopyAndReplace(string sourcePath, string copyPath)
        {
            try
            {
                String[] files = Directory.GetFiles(sourcePath);

                Progress_reset(files.Length);

                Directory.CreateDirectory(copyPath);

                //ファイルをコピー
                foreach (String file in files)
                {
                    try
                    {
                        File.Copy(file, Path.Combine(copyPath, Path.GetFileName(file)));
                    }
                    catch { }

                    Dispatcher.Invoke(() => { dl_ProgressBar.Value = count; });
                }

                //ディレクトリの中のディレクトリも再帰的にコピー
                foreach (String dir in Directory.GetDirectories(sourcePath))
                {
                    CopyAndReplace(dir, Path.Combine(copyPath, Path.GetFileName(dir)));
                }
            }
            catch { }
        }

        /// <summary>
        /// 指定したディレクトリとその中身を全て削除する
        /// </summary>
        public void Delete(string targetDirectoryPath, bool tempdel)
        {
            try
            {
                if (!Directory.Exists(targetDirectoryPath) || targetDirectoryPath == roottemp && !(tempdel))
                {
                    return;
                }

                //ディレクトリ以外の全ファイルを削除
                foreach (string filePath in Directory.GetFiles(targetDirectoryPath))
                {
                    try
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                    }
                    catch { }
                }

                //ディレクトリの中のディレクトリも再帰的に削除
                foreach (string directoryPath in Directory.GetDirectories(targetDirectoryPath))
                {
                    if (directoryPath != $"./{roottemp}")
                    {
                        try
                        {
                            Delete(directoryPath, false);
                        }
                        catch { }
                    }
                }

                //中が空になったらディレクトリ自身も削除
                Directory.Delete(targetDirectoryPath, false);
            }
            catch { }
        }
    }
}
