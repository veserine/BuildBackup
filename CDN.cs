using System;
using System.IO;
using System.Net.Http;

namespace BuildBackup
{
    public class CDN
    {
        public HttpClient client;
        public string cacheDir;
        public bool isEncrypted = false;
        public string decryptionKeyName = "";

        public byte[] Get(string url, bool returnstream = true, bool redownload = false)
        {
            url = url.ToLower();
            var uri = new Uri(url);

            string cleanname = uri.AbsolutePath;

            if (redownload || !File.Exists(cacheDir + cleanname))
            {
                try
                {
                    if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                    Console.Write("\nDownloading " + cleanname);
                    using (HttpResponseMessage response = client.GetAsync(uri).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (MemoryStream mstream = new MemoryStream())
                            using (HttpContent res = response.Content)
                            {
                                res.CopyToAsync(mstream);
                                if (url.StartsWith("http://188.165.192.135/1"))
                                {
                                    cleanname = cleanname.Substring(2);
                                    cleanname = "/tpr/wow/data/" + cleanname;
                                }
                                if (isEncrypted)
                                {
                                    var cleaned = Path.GetFileNameWithoutExtension(cleanname);
                                    var decrypted = BLTE.DecryptFile(cleaned, mstream.ToArray(), decryptionKeyName);

                                    File.WriteAllBytes(cacheDir + cleanname, decrypted);
                                    return decrypted;
                                }
                                else
                                {
                                    File.WriteAllBytes(cacheDir + cleanname, mstream.ToArray());
                                }
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound && !url.StartsWith("http://188.165.192.135/"))
                        {
                            Console.WriteLine("\nNot found on primary mirror, retrying on secondary mirror...");
                            return Get("http://188.165.192.135" + cleanname, returnstream, redownload);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound && url.StartsWith("http://188.165.192.135") && !url.StartsWith("http://188.165.192.135/1"))
                        {
                            Console.WriteLine("\nNot found on secondary mirror, retrying on last mirror...");
                            string ncleanname = cleanname.Substring(14);
                            return Get("http://188.165.192.135/1/" + ncleanname, returnstream, redownload);
                        }
                        else
                        {
                            throw new FileNotFoundException("\nError retrieving file: HTTP status code " + response.StatusCode + " on URL " + url);
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n!!! Error retrieving file " + url + ": " + e.Message);
                    File.AppendAllText("failedfiles.txt", url + "\n");
                }
            }

            if (returnstream)
            {
                return File.ReadAllBytes(cacheDir + cleanname);
            }
            else
            {
                return new byte[0];
            }
        }
    }
}
