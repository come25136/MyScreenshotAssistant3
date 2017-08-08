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
                updatejson = JsonConvert.DeserializeObject<UpdateJson>(new StreamReader(WebRequest.Create("https://msa.momizi.work/3/update.json").GetResponse().GetResponseStream()).ReadToEnd());

                if (app.Version != updatejson.version)
                {
                    // アップデート通知ウィンドウ表示
                    new UpdateWindow().ShowDialog();
                }
            }
            catch (Exception)
            {
                Method.Message("サーバーエラー", "アップデート確認用サーバーにアクセスできませんでした");
            }
        }
    }
}