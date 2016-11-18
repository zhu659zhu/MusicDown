using MusicDown.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MusicDown
{
    public partial class Form1 : Form
    {

        string songid = "1772446369";
        string songurl = "";
        string songname = "";
        string songsinger = "";
        string songlyric = "";

        public Form1()
        {
            InitializeComponent();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            //search("山脉", 10, 0, SearchType.Music);
            //GetCookie();
            textBox1.Text = GetPlayUrl("686361");
            return;


            //string str = "h%5xo%1F7241_3k6399748Eut2.im5118F62lFec9cbf78-ltFfa%E%87134.ay%3b6d%%%lp%im2%573768mu%51d2f555%2liF2E%2791pt3Eee26EEE3Fe.1F%572_93hDd5fe-2%-Am.c132E%415%_ada18165n";
            string HtmlData = GetHtml("http://www.xiami.com/song/playlist/id/"+songid);

            Regex pattern = new Regex(@"<location>([\w\W]+?)<\/location>");
            Match matchMode = pattern.Match(HtmlData);
            if (matchMode.Success)
            {
                songurl = Decrypt(matchMode.Groups[1].Value.Substring(1), Convert.ToInt32(matchMode.Groups[1].Value.Substring(0, 1)));
            }
            pattern = new Regex(@"<songName>([\w\W]+?)<\/songName>");
            matchMode = pattern.Match(HtmlData);
            if (matchMode.Success)
            {
                songname = matchMode.Groups[1].Value;
            }
            pattern = new Regex(@"<singers>([\w\W]+?)<\/singers>");
            matchMode = pattern.Match(HtmlData);
            if (matchMode.Success)
            {
                songsinger = matchMode.Groups[1].Value;
            }
            pattern = new Regex(@"<lyric_url>([\w\W]+?)<\/lyric_url>");
            matchMode = pattern.Match(HtmlData);
            if (matchMode.Success)
            {
                songlyric = matchMode.Groups[1].Value;
            }

            textBox1.Text += songname + "\r\n";
            textBox1.Text += songsinger + "\r\n";
            textBox1.Text += songurl + "\r\n";
            textBox1.Text += songlyric + "\r\n";
            textBox1.Text += songid + "\r\n";

        }


        #region 新版API
        private string GetPlayUrl(string id, string quality = "320000")
        {
            var text = "{\"ids\":[\"" + id + "\"],\"br\":" + quality + ",\"csrf_token\":\"\"}";
            var html = GetEncHtml("http://music.163.com/weapi/song/enhance/player/url?csrf_token=", text);
            if (string.IsNullOrEmpty(html) || html == "null")
            {
                return null;
            }
            var json = JObject.Parse(html);
            var link = json["data"].First["url"].ToString();
            return string.IsNullOrEmpty(link) || link == "null" ? "" : link;
        }

        WebClient createWeb()
        {
            WebClient wc = new WebClient();
            wc.Headers["Cookie"] = "appver=1.5.0.75771;";
            wc.Headers["Referer"] = "http://music.163.com/";
            wc.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            wc.Encoding = Encoding.UTF8;
            return wc;
        }

        string post(string url, string data)
        {
            var wc = createWeb();
            return wc.UploadString(url, "post", data);
        }
        string get(string url)
        {
            var wc = createWeb();
            return wc.DownloadString(url);
        } 

        string dict2string(Dictionary<string, string> dict)
        {
            string ret = "";
            foreach (var kv in dict)
            {
                ret += kv.Key + "=" + System.Web.HttpUtility.UrlEncode(kv.Value) + "&";
            }

            return ret.Substring(0, ret.Length - 1);
        }

        private string GetEncHtml(string url, string text)
        {
            //加密参考 https://github.com/darknessomi/musicbox
            //该处使用固定密钥，简化操作，效果与随机密钥一致
            const string secKey = "a44e542eaac91dce";
            var pad = 16 - text.Length % 16;
            Console.WriteLine(text.Length.ToString());
            //for (var i = 0; i < pad; i++)
            //{
            //    text = text + Convert.ToChar(pad);
            //}
            Console.WriteLine("***"+text+"***");
            Console.WriteLine(text.Length.ToString());
            //MessageBox.Show(pad.ToString());
            var encText = AesEncrypt(AesEncrypt(text, "0CoJUm6Qyw8W8jud"), secKey);
            const string encSecKey = "411571dca16717d9af5ef1ac97a8d21cb740329890560688b1b624de43f49fdd7702493835141b06ae45f1326e264c98c24ce87199c1a776315e5f25c11056b02dd92791fcc012bff8dd4fc86e37888d5ccc060f7837b836607dbb28bddc703308a0ba67c24c6420dd08eec2b8111067486c907b6e53c027ae1e56c188bc568e";
            var data = new Dictionary<string, string>
            {
                {"params", encText},
                {"encSecKey", encSecKey},
            };
            Console.WriteLine(encText);
            Console.WriteLine(encSecKey);
            string html = post(url, dict2string(data));
            return html;
        }


        private static string AesEncrypt(string toEncrypt, string key, string iv = "0102030405060708")
        {
            var keyArray = Encoding.UTF8.GetBytes(key);
            var ivArr = Encoding.UTF8.GetBytes(iv);
            var toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);
            using (var aesDel = Aes.Create())
            {
                aesDel.IV = ivArr;
                aesDel.Key = keyArray;
                aesDel.Mode = CipherMode.CBC;
                aesDel.Padding = PaddingMode.PKCS7;
                var cTransform = aesDel.CreateEncryptor();
                var resultArr = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return Convert.ToBase64String(resultArr, 0, resultArr.Length);
            }
        }

        public List<Model.Song> search(string key, int count, int page, SearchType type)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            dict.Add("s", key);
            dict.Add("offset", page.ToString());
            dict.Add("limit", count.ToString());
            dict.Add("type", ((int)type).ToString());
            dict.Add("hlpretag", "");
            dict.Add("hlposttag", "");
            dict.Add("total", "true");

            string data = dict2string(dict);
            //textBox1.Text = data;
            var json = post("http://music.163.com/api/search/pc", data);
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Model.MusicMeta>(json);
            textBox1.Text = json;
            if (result == null)
            {
                return new List<Model.Song>();
            }

            return result.result.songs.ToList();
        }

        #endregion


        private string GetHtml(string url)
        {
            try
            {
                string ret = string.Empty;

                HttpWebRequest request = null;
                request = (HttpWebRequest)WebRequest.Create(new Uri(url));

                request.Method = "GET";
                StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream(), Encoding.UTF8);
                ret = sr.ReadToEnd();
                return ret;
            }
            catch
            {
                return "";
            }
        }//获取网页内容


        public string Decrypt(string str,int x)
        {
            if (str == "")
                return "";
            string res = "";

            int scode = str.Length % x;

            int arrlength = str.Length / x;

            for (int j = 0; j < arrlength; j++)
            {
                for (int i = 0; i < x; i++)
                {
                    if(i<scode)
                        res += str[i * (arrlength+1) + j];
                    else
                        res += str[i * (arrlength) + scode + j];
                   
                }
            }
            for (int i = 0; i < scode;i++ )
                res += str[i * (arrlength+1) + arrlength];
            return System.Web.HttpUtility.UrlDecode(res.Replace("%5E","0"));
                //return (res);
        }

    }
}
