using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NativeLibDownloader
{
    public class ConfigInfo
    {
        public string Version { get; set; }
        public string Site { get; set; }
        public string FileName { get; set; }
        public string OutputLocation { get; set; }
        public string MD5 { get; set; }

        public ConfigInfo(string version, string site, string filename, string outputLocation, string md5)
        {
            Version = version;
            Site = site;
            FileName = filename;
            OutputLocation = outputLocation;
            MD5 = md5;
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No file specified to load.");
                return 1;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File specifed does not exist.");
                return 1;
            }

            Directory.SetCurrentDirectory(Path.GetDirectoryName(args[0]));

            //Load our json
            List<ConfigInfo> configInfo = JsonConvert.DeserializeObject<List<ConfigInfo>>(File.ReadAllText(args[0]));

            //Check for pre-existing correct files.
            Task<ConfigInfo[]> checkMd5 = ForEashAsync(configInfo, CheckFileMd5);

            //Create a list of new files to download
            List<ConfigInfo> configToDownload = checkMd5.Result.Where(r => r != null).ToList();

            //If no new files to download return
            if (configToDownload.Count == 0)
            {
                Console.WriteLine("All files already downloaded. Returning");
                return 0;
            }

            
            //Download all of our files
            Task<ConfigInfo[]> ret = ForEashAsync(configToDownload, DownloadFile);

            bool success = true;

            //Check for any failures
            foreach (var info in ret.Result.Where(info => info != null))
            {
                success = false;
                Console.WriteLine($"Failed to download file: {info.OutputLocation}");
            }

            //Return 0 if successful, 1 if failure
            return success ? 0 : 1;

        }

        //Foreach loop to run all commands asyncronously and return an array of type T2
        public static Task<T2[]> ForEashAsync<T, T2>(IEnumerable<T> source, Func<T, Task<T2>> body)
        {
            return Task.WhenAll(
                from item in source
                select Task.Run(() => body(item)));
        }

        //Asyncronously check all file MD5s
        public static async Task<ConfigInfo> CheckFileMd5(ConfigInfo info)
        {
            bool ret = await Task.Run(() => CheckFileValid(info));
            //True if file correct
            //That means return null so it does not get redownloaded
            return ret ? null : info;
        }

        //Checks a specific file for MD5 equality
        public static bool CheckFileValid(ConfigInfo info)
        {
            string fileSum = Md5Sum(info.OutputLocation);

            return fileSum != null && fileSum.Equals(info.MD5);
        }

        //Grab the MD5 sum of an existing file
        private static string Md5Sum(string fileName)
        {
            byte[] fileMd5Sum = null;


            if (File.Exists(fileName))
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Open))
                {
                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    {
                        fileMd5Sum = md5.ComputeHash(stream);
                    }
                }
            }

            if (fileMd5Sum == null)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            foreach (var b in fileMd5Sum)
            {
                builder.Append(b);
            }
            return builder.ToString();
        }

        // Returns null if downloaded successfully
        //Donwload a file asyncronously
        public static async Task<ConfigInfo> DownloadFile(ConfigInfo info)
        {
            string webUrl = Path.Combine(info.Site, info.Version, info.FileName);
            string localFileName = info.OutputLocation;

            //Check for internet
            bool haveInternet = false;

            try
            {
                using (var client = new TimeoutWebClient(1000))
                {
                    using (var stream = client.OpenRead(webUrl))
                    {
                        haveInternet = true;
                    }
                }
            }
            catch
            {
                haveInternet = false;
            }

            if (!haveInternet) return info;

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    await client.DownloadFileTaskAsync(new Uri(webUrl), localFileName);
                    return null;
                }
            }
            catch (Exception)
            {
                return info;
            }
        }

        private class TimeoutWebClient : WebClient
        {
            private readonly int m_timeout;

            public TimeoutWebClient(int timeout)
            {
                this.m_timeout = timeout;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var result = base.GetWebRequest(address);
                result.Timeout = this.m_timeout;
                return result;
            }
        }
    }
}
