using Markdig;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace pixivFanBox
{
#pragma warning disable CS8600 // 正在將 Null 常值或可能的 Null 值轉換為不可為 Null 的型別。
#pragma warning disable CS8602 // 可能 null 參考的取值 (dereference)。
#pragma warning disable CS8603 // 可能有 Null 參考傳回。
#pragma warning disable CS8604 // 可能有 Null 參考引數。
    class Program
    {
        static Dictionary<string, string>? creatorConvertList;

        [STAThread]
        static void Main(string[] args)
        {
            Run().GetAwaiter().GetResult();
        }

        static async Task Run()
        {
            Console.Clear();
            Console.Title = $"FANBox下載工具";
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Dictionary<string, int> lastSavePostId = new Dictionary<string, int>();
            if (File.Exists("LastSavePostId.json"))
                lastSavePostId = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText("LastSavePostId.json"));

            creatorConvertList = GetCreatorConvertList();

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient httpClient = new HttpClient(handler, true);
            httpClient.BaseAddress = new Uri("https://api.fanbox.cc/");
            httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.fanbox.cc/");
            httpClient.DefaultRequestHeaders.Add("Origin", "https://www.fanbox.cc");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            if (!File.Exists("Cookie.txt"))
            {
                while (true)
                {
                    Log.Error("找不到Cookie.txt");
                    Log.Error("請將Fanbox的Cookie中的\"FANBOXSESSID\"值複製後貼到此處並按下Enter");
                    Log.Info("(例12754216_VWER3435edrsdy5234dfsu6562oQbq)> ", false);
                    string cookie = Console.ReadLine();
                    handler.CookieContainer.Add(new Uri("https://fanbox.cc"), new Cookie("FANBOXSESSID", cookie, "/", ".fanbox.cc"));

                    try
                    {
                        Console.Clear();
                        var result= await httpClient.GetStringAsync("bell.countUnread");

                        File.WriteAllText("Cookie.txt", cookie);
                        Thread.Sleep(1000);

                        Log.Info("第一次執行會下載全部贊助者的貼文，會需要一段時間");
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("400"))
                            Log.Error("缺少Cookie，請重新輸入Cookie");
                        else if (ex.Message.Contains("401"))
                            Log.Error("Cookie錯誤，請重新輸入Cookie");
                        else
                            Log.Error(ex.Message);
                    }
                }
            }

            Dictionary<string, List<string>> userSupportingDic = new Dictionary<string, List<string>>();
            foreach (var cookie in File.ReadAllLines("Cookie.txt"))
            {
                if (string.IsNullOrWhiteSpace(cookie))
                    continue;

                string cookieId = cookie.Split(new char[] { '_' })[0];
                handler.CookieContainer.Add(new Uri("https://fanbox.cc"), new Cookie("FANBOXSESSID", cookie, "/", ".fanbox.cc"));

                try
                {
                    string listSupportingJson = await httpClient.GetStringAsync("plan.listSupporting");
                    ListSupportingJson? listSupporting = JsonConvert.DeserializeObject<ListSupportingJson>(listSupportingJson);
                    Log.Info($"已贊助的人數: {listSupporting.body.Count}");

                    List<string> supportingCreatorList;
                    if (!listSupporting.body.Any()) supportingCreatorList = new List<string>();
                    else supportingCreatorList = listSupporting.body.Select((x) => $"{x.user.name} / {x.fee}").ToList();
                    userSupportingDic.Add(cookieId, supportingCreatorList);

                    foreach (var creators in listSupporting.body)
                    {
                        Console.Title = $"FANBox下載工具 - 對象: {creators.user.name} / 贊助金額: {creators.fee}";
                        Log.Green($"下載對象: {creators.user.name} (贊助金額: {creators.fee}) ", false);
                        string apiUrl = $"post.listCreator?creatorId={creators.creatorId}&limit=10";
                        int maxPostId = 0; bool isFirst = true;

                        do
                        {
                            string json = (await httpClient.GetStringAsync(apiUrl)).Substring(8);
                            json = json.Substring(0, json.Length - 1);

                            PostListCreatorJson postListCreatorJson = JsonConvert.DeserializeObject<PostListCreatorJson>(json, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full });
                            apiUrl = postListCreatorJson.nextUrl?.Replace("https://api.fanbox.cc/", "");
                            maxPostId = Math.Max(postListCreatorJson.items.Max((x) => int.Parse(x.id)), maxPostId);

                            if (lastSavePostId.ContainsKey(creators.creatorId))
                                postListCreatorJson.items = postListCreatorJson.items.Where((x) => int.Parse(x.id) > lastSavePostId[creators.creatorId]).ToList();

                            if (postListCreatorJson.items.Count == 0)
                            {
                                Log.Info("無最新貼文");
                                break;
                            }
                            else if (isFirst) { Console.WriteLine(); isFirst = false; }

                            foreach (var postListCreator in postListCreatorJson.items)
                            {
                                json = (await httpClient.GetStringAsync($"post.info?postId={postListCreator.id}")).Substring(8);
                                json = json.Substring(0, json.Length - 1);
                                var postJson = JsonConvert.DeserializeObject<PostInfoJson>(json);

                                string saveName = GetSaveFolderName(postJson.creatorId, postJson.user.name) + $"{GetEnvSlash()}[{postJson.publishedDatetime:yyyyMMdd-HHmmss}] ({postJson.id}) {MakeFileNameValid(postJson.title)}";
                                int i = 0;
                                if (Directory.Exists(saveName)) continue;

                                Log.Green($"標題: {postJson.id} - {postJson.title}");
                                Log.Info($"上傳時間: {postJson.publishedDatetime:yyyy/MM/dd HH-mm}");

                                if (postJson.body == null)
                                {
                                    if (creators.fee != postJson.feeRequired)
                                    {
                                        Log.Error($"無法取得附件資訊，請確認是否達到贊助門檻 (現在: {creators.fee}円，需要: {postJson.feeRequired}円)");
                                        continue;
                                    }
                                    else
                                    {
                                        Log.Error($"postJson.body 錯誤");
                                        Console.ReadKey();
                                        return;
                                    }
                                }

                                Directory.CreateDirectory(saveName);
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine($"# {postJson.title}\r\n");

                                JObject jobject = JObject.Parse(JsonConvert.SerializeObject(postJson))["body"].ToObject<JObject>();
                                if (postJson.body.blocks != null)
                                {
                                    json = (await httpClient.GetStringAsync($"post.info?postId={postJson.id}")).Substring(8);
                                    json = json.Substring(0, json.Length - 1);
                                    jobject = JObject.Parse(json)["body"].ToObject<JObject>();
                                    var jTokens = JObject.Parse(json)["body"].ToObject<JObject>()["blocks"].Children();

                                    var imageList = jTokens.Where((x) => x["type"].Value<string>() == "image");
                                    var fileList = jTokens.Where((x) => x["type"].Value<string>() == "file");

                                    Log.Info($"圖片數量: {imageList.Count()}");
                                    Log.Info($"檔案數量: {fileList.Count()}");

                                    using (ProgressBar progressBar = new ProgressBar())
                                    {
                                        i = 0;
                                        foreach (var item in imageList)
                                        {
                                            progressBar.Report(i++ / (double)imageList.Count());
                                            try
                                            {
                                                var imageUrl = jobject["imageMap"][item["imageId"].Value<string>()]["originalUrl"].ToString();
                                                try { await File.WriteAllBytesAsync(saveName + GetEnvSlash() + i.ToString() + Path.GetExtension(imageUrl), await httpClient.GetByteArrayAsync(imageUrl)); }
                                                catch (Exception ex) { Console.WriteLine(ex.ToString()); if (ex.Message.Contains("403")) return; }
                                            }
                                            catch (Exception)
                                            {
                                                i--;
                                            }
                                        }

                                        i = 0;
                                        foreach (var item in fileList)
                                        {
                                            progressBar.Report(i++ / (double)fileList.Count());
                                            var file = jobject["fileMap"][item["fileId"].Value<string>()];
                                            string fileName = MakeFileNameValid($"{file["name"]}.{file["extension"]}");
                                            try { await File.WriteAllBytesAsync(saveName + GetEnvSlash() + fileName, await httpClient.GetByteArrayAsync(file["url"].ToString())); }
                                            catch (Exception ex) { Console.WriteLine(ex.ToString()); if (ex.Message.Contains("403")) return; }
                                        }
                                    }

                                    i = 0;
                                    foreach (var item in postJson.body.blocks)
                                    {
                                        switch (item.type)
                                        {
                                            case "header":
                                                sb.AppendLine($"## {item.text}\r\n");
                                                break;
                                            case "p":
                                                sb.AppendLine($"{item.text}\r\n");
                                                break;
                                            case "file":
                                                var file = jobject["fileMap"][item.fileId];
                                                string fileName = MakeFileNameValid($"{file["name"]}.{file["extension"]}");
                                                sb.AppendLine($"[{fileName}]({fileName})\r\n");

                                                if (file["extension"].ToString() == "png" || file["extension"].ToString() == "jpg" || file["extension"].ToString() == "jpeg" || file["extension"].ToString() == "gif")
                                                    sb.AppendLine($"![{fileName}]({fileName})\r\n");
                                                break;
                                            case "image":
                                                try
                                                {
                                                    i++;
                                                    var image = jobject["imageMap"][item.imageId];
                                                    string imageName = $"{i}{Path.GetExtension(image["originalUrl"].ToString())}";
                                                    sb.AppendLine($"![{image["id"]}]({imageName})\r\n");
                                                }
                                                catch (Exception)
                                                {
                                                    i--;
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }

                                if (postJson.body.images != null) Log.Info($"圖片數量: {postJson.body.images.Count}");
                                if (postJson.body.files != null) Log.Info($"檔案數量: {postJson.body.files.Count}");
                                using (ProgressBar progressBar = new ProgressBar())
                                {
                                    if (postJson.body.images != null)
                                    {
                                        i = 0;
                                        foreach (var item in postJson.body.images.Select((x) => new KeyValuePair<string, string>(x.originalUrl, x.thumbnailUrl)))
                                        {
                                            progressBar.Report(i++ / (double)postJson.body.images.Count);
                                            try
                                            {
                                                string imageName = $"{i}{Path.GetExtension(item.Key)}";
                                                await File.WriteAllBytesAsync(saveName + GetEnvSlash() + imageName, await httpClient.GetByteArrayAsync(item.Key)); 
                                                sb.AppendLine($"![{imageName}]({imageName})\r\n");
                                            }
                                            catch (Exception)
                                            {
                                                try
                                                {
                                                    string imageName = $"{i}_t{Path.GetExtension(item.Value)}";
                                                    await File.WriteAllBytesAsync(saveName + GetEnvSlash() + imageName, await httpClient.GetByteArrayAsync(item.Value));
                                                    sb.AppendLine($"![{imageName}]({imageName})\r\n");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine(ex.ToString());
                                                    if (ex.Message.Contains("403")) return;
                                                }
                                            }
                                        }
                                    }

                                    if (postJson.body.files != null)
                                    {
                                        i = 0;
                                        foreach (var item in postJson.body.files)
                                        {
                                            progressBar.Report(i++ / (double)postJson.body.files.Count);
                                            try
                                            {
                                                string fileName = MakeFileNameValid($"{item.name.Replace(" ", "_")}.{item.extension}");
                                                await File.WriteAllBytesAsync(saveName + GetEnvSlash() + fileName, await httpClient.GetByteArrayAsync(item.url));
                                                sb.AppendLine($"[{fileName}]({fileName})\r\n");

                                                if (item.extension == "png" || item.extension == "jpg" || item.extension == "jpeg" || item.extension == "gif")
                                                    sb.AppendLine($"![{fileName}]({fileName})\r\n");
                                            }
                                            catch (Exception ex) { Console.WriteLine(ex.ToString()); if (ex.Message.Contains("403")) return; }
                                        }
                                    }
                                }

                                MarkdownPipelineBuilder pipelineBuilder = new MarkdownPipelineBuilder().UseAutoLinks();
                                if (postJson.body.video != null && postJson.body.video.serviceProvider == "youtube")
                                {
                                    sb.AppendLine($"![youtube.com](https://www.youtube.com/watch?v={postJson.body.video.videoId})\r\n");
                                    pipelineBuilder = pipelineBuilder.UseMediaLinks();
                                }

                                if (postJson.body.text != null)
                                    sb.AppendLine($"{postJson.body.text}");

                                string html = Markdown.ToHtml(sb.ToString(), pipelineBuilder.Build());
                                await File.WriteAllTextAsync($"{saveName}{GetEnvSlash()}Post.html", html); //可能會有非同步存取的問題

                                #region gigafile *尚未驗證是否功能正常
                                Regex regex = new Regex(@"https:\/\/\d{1,2}\.gigafile\.nu\/\d{4}-.{33}", RegexOptions.Multiline);
                                if (regex.IsMatch(html))
                                {
                                    List<string> gigafileList = new List<string>();
                                    foreach (Match item in regex.Matches(html))
                                    {
                                        gigafileList.Add(item.Value);
                                    }

                                    gigafileList = gigafileList.Distinct().ToList();
                                    Log.Info($"gigafile數量: {gigafileList.Count}");

                                    i = 0;
                                    using (ProgressBar progressBar = new ProgressBar())
                                    {
                                        foreach (var item in gigafileList)
                                        {
                                            progressBar.Report(i++ / (double)gigafileList.Count);

                                            try
                                            {
                                                string downloadUrl = item.Replace("nu/", "nu/download.php?file=");
                                                var uri = new Uri(downloadUrl);
                                                string gfCookie = await GetCookieFromWebServerAsync(httpClient, item, "gfsid");
                                                handler.CookieContainer.Add(uri, new Cookie("gfsid", gfCookie, "/", uri.Host));

                                                string fileName = MakeFileNameValid(await GetFilenameFromWebServerAsync(httpClient, downloadUrl));

                                                if (!string.IsNullOrEmpty(fileName))
                                                    await File.WriteAllBytesAsync(saveName + GetEnvSlash() + fileName, await httpClient.GetByteArrayAsync(downloadUrl));
                                                else
                                                    Log.Error($"檔案已過期: {item}");
                                            }
                                            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                                        }
                                    }
                                }
                                #endregion

                                if (html.Contains("drive.google.com"))
                                {
                                    Log.Warn("此貼文包含雲端連結，請確認是否需要自行下載附件");
                                    Log.Warn($"https://www.fanbox.cc/@{postJson.creatorId}/posts/{postJson.id}");
                                }
                            }
                        } while (!string.IsNullOrEmpty(apiUrl));

                        if (lastSavePostId.ContainsKey(creators.creatorId))
                            lastSavePostId[creators.creatorId] = maxPostId;
                        else
                            lastSavePostId.Add(creators.creatorId, maxPostId);

                        await File.WriteAllTextAsync("LastSavePostId.json", JsonConvert.SerializeObject(lastSavePostId, Formatting.Indented));
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("400"))
                        Log.Error("缺少Cookie，請重新輸入Cookie");
                    else if (ex.Message.Contains("401"))
                        Log.Error($"{cookieId}的Cookie錯誤，請重新輸入Cookie");
                    else
                        Log.Error($"{ex}");

                    Console.ReadKey();
                    return;
                }
            }

            File.WriteAllText("SupportList.json", JsonConvert.SerializeObject(userSupportingDic, Formatting.Indented));

            Console.Title = $"FANBox下載工具";
            Log.Green("全部完成，請按任意鍵繼續...");
            Console.ReadKey();
        }

        static string MakeFileNameValid(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException();

            if (filename.Length == 0)
                throw new ArgumentException();

            if (filename.Length > 245)
                throw new PathTooLongException();

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, '_');
            }

            filename = filename.Trim(new char[] { '_' });

            if (filename.EndsWith("."))
                filename = Regex.Replace(filename, @"\.+$", "");

            return filename.Trim();
        }

        static string MakePathNameValid(string pathname)
        {
            if (pathname == null)
                throw new ArgumentNullException();

            if (pathname.EndsWith("."))
                pathname = Regex.Replace(pathname, @"\.+$", "");

            if (pathname.Length == 0)
                throw new ArgumentException();

            if (pathname.Length > 245)
                throw new PathTooLongException();

            foreach (char c in Path.GetInvalidPathChars())
            {
                pathname = pathname.Replace(c, '_');
            }

            pathname = pathname.Trim(new char[] { '_' });

            return pathname.Trim();
        }


        static async Task<string> GetCookieFromWebServerAsync(HttpClient httpClient, string url, string cookieName = "") //尚未驗證是否正常
        {
            if (string.IsNullOrEmpty(cookieName))
                throw new NullReferenceException(cookieName);

            string result = "";

            try
            {
                var head = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                head.EnsureSuccessStatusCode();
                result = head.Headers.First((x) => x.Key == "Set-Cookie").Value.First((x) => x.StartsWith($"{cookieName}=")).Replace($"{cookieName}=", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return result;
        }

        static async Task<string> GetFilenameFromWebServerAsync(HttpClient httpClient, string url) //尚未驗證是否正常
        {
            string result = "";

            try
            {
                var head = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                head.EnsureSuccessStatusCode();
                result = head.Headers.First((x) => x.Key == "Content-Disposition").Value.First((x) => x.StartsWith("filename*=UTF-8''")).Replace($"filename*=UTF-8''", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return HttpUtility.UrlDecode(result, Encoding.UTF8);
        }

        private static Dictionary<string, string> GetCreatorConvertList()
        {
            try
            {
                string listSavePath = AppDomain.CurrentDomain.BaseDirectory + "SavePathList.json";
                if (File.Exists(listSavePath)) return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(listSavePath));
                else
                {
                    var dic = new Dictionary<string, string>();
                    dic.Add("savePath", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"{GetEnvSlash()}FanBox下載");
                    dic.Add("default", "2. 贊助類(圖&影)");
                    File.WriteAllText(listSavePath, JsonConvert.SerializeObject(dic, Formatting.Indented));
                    return dic;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString(), ConsoleColor.DarkRed); throw; }
        }

        static string GetSaveFolderName(string creatorId, string creatorName)
        {
            creatorName = MakePathNameValid(creatorName.Split(new char[] { '@' }).First());

            if (creatorConvertList == null)
                creatorConvertList = GetCreatorConvertList();

            string defaultFolderName;
            if (!creatorConvertList.TryGetValue("savePath", out defaultFolderName))
                defaultFolderName = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"{GetEnvSlash()}FanBox下載";

            if (creatorConvertList.TryGetValue(creatorId, out string folderName)) return defaultFolderName + GetEnvSlash() + folderName + GetEnvSlash() + creatorName;
            else if (creatorConvertList.TryGetValue("default", out string defaultPath) && !string.IsNullOrWhiteSpace(defaultPath)) return defaultFolderName + GetEnvSlash() + defaultPath + GetEnvSlash() + creatorName;
            else return defaultFolderName + GetEnvSlash() + creatorName;
        }

        static string GetEnvSlash() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";
    }
}
#pragma warning restore CS8600 // 正在將 Null 常值或可能的 Null 值轉換為不可為 Null 的型別。
#pragma warning restore CS8602 // 可能 null 參考的取值 (dereference)。
#pragma warning restore CS8603 // 可能有 Null 參考傳回。
#pragma warning restore CS8604 // 可能有 Null 參考引數。