/* 
 * This file is part of the MyOhmSessions distribution (https://github.com/GroovemanAndCo/MyOhmStudio).
 * Copyright (c) 2020 Fabien (https://github.com/fab672000)
 * 
 * This program is free software: you can redistribute it and/or modify  
 * it under the terms of the GNU General Public License as published by  
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NLog;

namespace OhmstudioManager.Utils
{
    public class User
    {
        public string username;
        public string password;
        public string name;
        public string Phone;
        public string Addr;
        public string Mail;

        public User(string user, string pass, string uname = "", string phone = "", string addr = "", string mail = "")
        {
            username = user;
            password = pass;
            name = uname;
            Phone = phone;
            Addr = addr;
            Mail = mail;
        }
    }
    public class JsonRpc
    {
        public JsonRpc(int uid)
        {
            @params.uid = uid;
        }
        public string jsonrpc { get; set; } = "2.0";
        public string method { get; set; } = "user/get_projects_list";

        public Params @params { get; set; } = new Params {uid=1871}; // 19964
        public string id { get; set; } = "none";
    }

    public class Params
    {
        public int uid { get; set; }
    }


    public static class HttpUtils
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static CookieContainer CookieJar;

        public static string GetProjects(int uid)
        {
            var rpc = new JsonRpc(uid);
            const string url = "http://www.ohmstudio.com/ohmrpc.php";

            var result = Post(url, rpc);
            return result;
        }

        public static void Authenticate(string username, string pass )
        {

            User user = new User(username, pass);
            var url = "http://www.ohmstudio.com/auth";
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = "application/json";
                request.Method = "POST";
                request.CookieContainer = CookieJar;
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string query = JsonConvert.SerializeObject(user);

                    streamWriter.Write(query);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var response = (HttpWebResponse)request.GetResponse();
                var responseStream = response.GetResponseStream();
                using (var streamReader = responseStream!=null ? new StreamReader(responseStream) : null)
                {
                    var responseText = streamReader?.ReadToEnd();
                }

                //Check to see if you're logged in
                url = "http://web-address/rest-server/?q=resurce/system/connect.json";
                var newRequest = (HttpWebRequest)WebRequest.Create(url);
                newRequest.CookieContainer = CookieJar;
                newRequest.ContentType = "application/json";
                newRequest.Method = "POST";


                var newResponse = (HttpWebResponse) newRequest.GetResponse();
                var respStream = newResponse.GetResponseStream();
                using (var newStreamReader = respStream!=null ? new StreamReader(respStream) : null)
                {
                    var newResponseText = newStreamReader?.ReadToEnd();
                }


            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Can't access server");
            }
        }
        public static string Get(string uri)
        {

            var request = (HttpWebRequest)WebRequest.Create(uri);

            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.CookieContainer = CookieJar;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = stream!=null ? new StreamReader(stream) : null) 
            {
                return reader?.ReadToEnd();
            }
        }

        public static string Post(string uri, object rdata, string contentType = "application/jsonrequest")
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.CookieContainer = CookieJar;
            string query = JsonConvert.SerializeObject(rdata);

            var data = Encoding.ASCII.GetBytes(query);

            request.Method = "POST";
            request.ContentType = contentType;
            request.ContentLength = data.Length;
            request.ServicePoint.Expect100Continue = false;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, query.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            var respStream = response.GetResponseStream();
            var responseString = respStream!=null ? new StreamReader(respStream).ReadToEnd() : null;
            return responseString;
        }
        public static string GetWithAuth(string uri)
        {
            var handler = new HttpClientHandler();

            Console.WriteLine($"GET: {uri}");

            // ... Use HttpClient.            
            var client = new HttpClient(handler);

            var byteArray = Encoding.ASCII.GetBytes("grooveman:@20Music17!");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var response = client.GetStringAsync(uri);
            response.Wait(52000);
            return response.Result;
        }

        static int counter;
        private static readonly object counterLock = new object();
        public static bool DownloadFile(string uri, string destPath) // string fileName, string folder)
        {
            if (string.IsNullOrWhiteSpace(destPath)) return false;
            var folder = Path.GetDirectoryName(destPath);
            var fileName = Path.GetFileName(destPath);

            try
            {

                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                if (File.Exists(destPath))
                {
                    Logger.Trace($"File {fileName} already exists in {folder}, skipping ...");
                    return true;
                }

                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                using (var wc = new WebClient())
                {
                    wc.DownloadFile(
                        // Param1 = Link of file
                        new Uri(uri),
                        // Param2 = Path to save
                        destPath
                    );
                }

            }
            catch (Exception ex)
            {

                Logger.Error(ex, $"Failed to download file with mixdownFileName {fileName} to destination directory {folder}");
                return false;
            }

            return true;
        }

        public static string DownloadString(string uri)
        {
            string ret;

            try
            {


                using (var wc = new WebClient())
                {
                    ret = wc.DownloadString(new Uri(uri));
                }

            }
            catch (Exception ex)
            {

                Logger.Error(ex, $"Failed to download file with URI {uri} ");
                ret = null;
            }

            return ret;
        }


        public static string DownloadLatestVersion(string downloadUrl)
        {
            var userDownloadFilename= Path.GetTempFileName();
            userDownloadFilename = Path.Combine(Path.GetDirectoryName(userDownloadFilename),
                "MyOhmSessionsSetup_" + Path.GetFileNameWithoutExtension(userDownloadFilename) + ".exe");
            var timeStamp = DateTime.Now.ToString(@"YYMMDD_HHmmssfff");
            var setupFile = Path.ChangeExtension(userDownloadFilename, ".exe");
            var res = DownloadFile(downloadUrl, setupFile);
            return res ? setupFile : null;
        }

    }
}
