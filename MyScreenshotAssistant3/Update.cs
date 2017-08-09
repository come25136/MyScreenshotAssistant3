using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Windows;

namespace msa3
{
    class Update
    {
        public static UpdateJson updatejson;

        public static void Start()
        {
            App app = (App)Application.Current;

            try
            {
                // アップデート用jsonの取得
                using (WebResponse response = WebRequest.Create("https://msa.momizi.work/3/update.json").GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    updatejson = JsonConvert.DeserializeObject<UpdateJson>(reader.ReadToEnd());
                }

                if (app.Version != updatejson.version)
                {
                    // アップデート通知ウィンドウ表示
                    new UpdateWindow().ShowDialog();
                }
            }
            catch
            {
                Method.Message("サーバーエラー", "アップデート確認用サーバーにアクセスできませんでした");
            }
        }
    }
}