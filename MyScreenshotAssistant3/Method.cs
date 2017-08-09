using System;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace msa3
{
    class Method
    {
        private static App app = (App)System.Windows.Application.Current;

        public static SQLiteConnection database = new SQLiteConnection("Data Source=msa3.db");
        public static SQLiteCommand statement = null;

        /// <summary>MessageBox</summary>
        /// <param name="title">ダイアログタイトルを指定</param>
        /// <param name="value">ダイアログに表示する内容を指定</param>

        // MessageBoxのコード省略化
        public static void Message(string title, string value)
        {
            MessageBox.Show(value,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        /// <summary>Write log file</summary>
        /// <param name="level">エラーレベルを指定</param>
        /// <param name="value">エラー内容を指定</param>

        // ログファイルへ記入
        public static void Logfile(string level, string value)
        {
            try
            {
                File.AppendAllText("msa3.log", $"{DateTime.Now:yyyy/MM/dd HH:mm:ss} {level}: {value}\r\n");
                return;
            }
            catch { }
        }

        /// <summary>SQLite Logine</summary>

        // sqlのログイン処理
        public static void Sql_connect()
        {
            try
            {
                database.Open();
                Logfile("Info", "connect database.");

                statement = database.CreateCommand();

                Sql("create table if not exists Account (TwitterId string primary key, AccessToken string, AccessTokenSecret string)");
                Sql("create table if not exists ApplicationData (AppName string primary key, TwitterId string, fixed string, hashtag string, wait_time int, ACAK text)");
                Sql("create table if not exists DirectoryData (watch bool, name string, path string primary key)");

                Sql($"INSERT OR IGNORE INTO ApplicationData(AppName, wait_time) VALUES('{app.SoftwareName}', 300);");
            }
            catch
            {
                Logfile("Error", "Unable to connect to database.");
            }
        }

        /// <summary>SQLite Sql</summary>
        /// <param name="value">sql文を指定</param>

        // sql文の実行
        public static void Sql(string value)
        {
            try
            {
                statement.CommandText = value;
                statement.ExecuteNonQuery();
            }
            catch
            {
                Logfile("Error", "Failed to execute the sql statement.");
            }
        }

        /// <summary>SQLite sql_reader</summary>
        /// <param name="value">sql文を指定</param>
        /// <returns>sqlの実行結果を返します</returns>

        // sql文の実行
        public static SQLiteDataReader Sql_reader(string value)
        {
            try
            {
                using (statement)
                {
                    statement.CommandText = value;
                    return statement.ExecuteReader();
                }
            }
            catch
            {
                Logfile("Error", "Failed to execute the sql_reader statement.");
                return null;
            }
        }

        /// <summary>フォルダ内の最新pngファイルのファイル名を取得</summary>
        /// <param name="folderName">フォルダ名を指定</param>
        /// <returns>最新のpngファイルのファイル名を返します</returns>
        public static string GetNewestFileName(string folderName)
        {
            try
            {
                string[] files = Directory.GetFiles(folderName, "*.png", SearchOption.TopDirectoryOnly);

                string newestFileName = "";
                DateTime updateTime = DateTime.MinValue;
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.LastWriteTime > updateTime)
                    {
                        updateTime = fi.LastWriteTime;
                        newestFileName = file;
                    }
                }
                return Path.GetFileName(newestFileName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>GC強制実行</summary>
        public static void M_gc()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        } 
    }
}
