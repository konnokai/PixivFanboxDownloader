using FlareSolverrSharp;
using Markdig;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

#nullable disable

namespace PixivFanboxDownloader
{
    class Program
    {
        static Dictionary<string, string> creatorConvertList;
        private static readonly ProgramConfig _programConfig = new();

        [STAThread]
        static void Main(string[] args)
        {
            Run().GetAwaiter().GetResult();
        }

        static async Task Run()
        {
            Console.Clear();
            Console.Title = $"Fanbox 下載工具";
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            _programConfig.InitProgramConfig();

            Dictionary<string, int> lastSavePostId = new Dictionary<string, int>();
            if (File.Exists("LastSavePostId.json"))
                lastSavePostId = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText("LastSavePostId.json"));

            creatorConvertList = GetCreatorConvertList();

            var clientHandler = new HttpClientHandler();
            var handler = new ClearanceHandler(_programConfig.FlareSolverrApiUrl)
            {
                MaxTimeout = 60000,
                InnerHandler = clientHandler
            };

            var httpClient = new HttpClient(handler, false)
            {
                BaseAddress = new Uri("https://api.fanbox.cc/")
            };
            httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.fanbox.cc/");
            httpClient.DefaultRequestHeaders.Add("Origin", "https://www.fanbox.cc");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            if (!File.Exists("Cookie.txt"))
            {
                while (true)
                {
                    Log.Error("找不到 Cookie.txt");
                    Log.Error("請將 Fanbox 的 Cookie 中的 \"FANBOXSESSID\" 值複製後貼到此處並按下 Enter");
                    Log.Info("(例: 12754216_VWER3435edrsdy5234dfsu6562oQbq)> ", false);
                    string cookie = Console.ReadLine();
                    clientHandler.CookieContainer.Add(new Uri("https://fanbox.cc"), new Cookie("FANBOXSESSID", cookie, "/", ".fanbox.cc"));

                    try
                    {
                        Console.Clear();
                        var result = await httpClient.GetStringAsync("bell.countUnread");

                        File.WriteAllText("Cookie.txt", cookie);
                        Thread.Sleep(1000);

                        Log.Info("第一次執行會下載全部贊助者的貼文，會需要一段時間");
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("400"))
                            Log.Error("缺少 Cookie，請重新輸入 Cookie");
                        else if (ex.Message.Contains("401"))
                            Log.Error("Cookie 錯誤，請重新輸入 Cookie");
                        else
                            Log.Error(ex.Message);
                    }
                }
            }

            var userSupportingDic = new Dictionary<string, List<string>>();
            foreach (var cookie in File.ReadAllLines("Cookie.txt"))
            {
                if (string.IsNullOrWhiteSpace(cookie))
                    continue;

                string cookieId = cookie.Split(new char[] { '_' })[0];
                clientHandler.CookieContainer.Add(new Uri("https://fanbox.cc"), new Cookie("FANBOXSESSID", cookie, "/", ".fanbox.cc"));

                try
                {
                    string listSupportingJson = await httpClient.GetStringAsync("plan.listSupporting");
                    Json.Plan.ListSupporting.ListSupporting listSupporting = JsonConvert.DeserializeObject<Json.Plan.ListSupporting.ListSupporting>(listSupportingJson);
                    Log.Info($"已贊助的人數: {listSupporting.Body.Count}");

                    List<string> supportingCreatorList;
                    if (!listSupporting.Body.Any()) supportingCreatorList = new List<string>();
                    else supportingCreatorList = listSupporting.Body.Select((x) => $"{x.User.Name} ({x.CreatorId}) / {x.Fee}").ToList();
                    userSupportingDic.Add(cookieId, supportingCreatorList);

                    foreach (var creators in listSupporting.Body)
                    {
                        Console.Title = $"Fanbox 下載工具 - 對象: {creators.User.Name} / 贊助金額: {creators.Fee}円";
                        Log.Green($"下載對象: {creators.User.Name} ({creators.CreatorId}) (贊助金額: {creators.Fee}円) ", false);

                        if (_programConfig.IgnoreCreateList.Contains(creators.CreatorId))
                        {
                            Log.Warn("Pass");
                            continue;
                        }

                        string json = await httpClient.GetStringAsync($"post.paginateCreator?creatorId={creators.CreatorId}");
                        var paginateCreator = JsonConvert.DeserializeObject<Json.Post.PaginateCreator>(json).Body;
                        Log.Info($"分頁數量: {paginateCreator.Count}", false);

                        int maxPostId = 0; bool isFirst = true;
                        foreach (var paginateUrl in paginateCreator)
                        {
                            json = await httpClient.GetStringAsync(paginateUrl.Replace("https://api.fanbox.cc/", ""));
                            var postListCreatorJson = JsonConvert.DeserializeObject<Json.Post.ListCreator.ListCreator>(json, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full })!;

                            maxPostId = Math.Max(postListCreatorJson.Body.Max((x) => int.Parse(x.Id)), maxPostId);

                            if (lastSavePostId.ContainsKey(creators.CreatorId))
                                postListCreatorJson.Body = postListCreatorJson.Body.Where((x) => int.Parse(x.Id) > lastSavePostId[creators.CreatorId]).ToList();

                            if (!postListCreatorJson.Body.Any())
                            {
                                Log.Info("無最新貼文");
                                break;
                            }
                            else if (isFirst) { Console.WriteLine(); isFirst = false; }

                            foreach (var postListCreator in postListCreatorJson.Body)
                            {
                                json = await httpClient.GetStringAsync($"post.info?postId={postListCreator.Id}");
                                var info = JsonConvert.DeserializeObject<Json.Post.Info.Info>(json).Body;

                                string saveName = GetSaveFolderName(info.CreatorId, info.User.Name) + $"{GetEnvSlash()}[{info.PublishedDatetime:yyyyMMdd-HHmmss}] ({info.Id}) {MakeFileNameValid(info.Title)}";
                                int i = 0;
                                if (Directory.Exists(saveName)) continue;

                                Log.Green($"{info.Id} - {info.Title} ({info.FeeRequired}円)");
                                Log.Info($"上傳時間: {info.PublishedDatetime:yyyy/MM/dd HH-mm}");

                                if (info.BlockBody == null)
                                {
                                    if (creators.Fee != info.FeeRequired)
                                    {
                                        Log.Error($"無法取得附件資訊，請確認是否達到贊助門檻 (現在: {creators.Fee}円，需要: {info.FeeRequired}円)");
                                        continue;
                                    }
                                    else
                                    {
                                        Log.Error($"info.Body 錯誤");
                                        Console.ReadKey();
                                        return;
                                    }
                                }

                                Directory.CreateDirectory(saveName);
                                var sb = new StringBuilder();
                                sb.AppendLine($"# {info.Title}\r\n");

                                //JObject jobject = JObject.Parse(JsonConvert.SerializeObject(json))["body"].ToObject<JObject>();
                                if (info.BlockBody.Blocks != null)
                                {
                                    var jobject = JObject.Parse(json)["body"]["body"].ToObject<JObject>();
                                    var jTokens = jobject["blocks"].Children();

                                    var imageList = jTokens.Where((x) => x["type"].Value<string>() == "image");
                                    var fileList = jTokens.Where((x) => x["type"].Value<string>() == "file");

                                    Log.Info($"圖片數量: {imageList.Count()}");
                                    Log.Info($"檔案數量: {fileList.Count()}");

                                    using (var progressBar = new ProgressBar())
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
                                    foreach (var item in info.BlockBody.Blocks)
                                    {
                                        switch (item.Type)
                                        {
                                            case "header":
                                                sb.AppendLine($"## {item.Text}\r\n");
                                                break;
                                            case "p":
                                                sb.AppendLine($"{item.Text}\r\n");
                                                break;
                                            case "file":
                                                var file = jobject["fileMap"][item.FileId];
                                                string fileName = MakeFileNameValid($"{file["name"]}.{file["extension"]}");
                                                sb.AppendLine($"[{fileName}]({fileName})\r\n");

                                                if (file["extension"].ToString() == "png" || file["extension"].ToString() == "jpg" || file["extension"].ToString() == "jpeg" || file["extension"].ToString() == "gif")
                                                    sb.AppendLine($"![{fileName}]({fileName})\r\n");
                                                break;
                                            case "image":
                                                try
                                                {
                                                    i++;
                                                    var image = jobject["imageMap"][item.ImageId];
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

                                if (info.Images != null) Log.Info($"圖片數量: {info.Images.Count}");
                                if (info.Files != null) Log.Info($"檔案數量: {info.Files.Count}");
                                using (var progressBar = new ProgressBar())
                                {
                                    if (info.Images != null) // 尚未測試
                                    {
                                        i = 0;
                                        foreach (var item in info.Images.Select((x) => new KeyValuePair<string, string>(x.OriginalUrl, x.ThumbnailUrl)))
                                        {
                                            progressBar.Report(i++ / (double)info.Images.Count);
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

                                    if (info.Files != null)
                                    {
                                        i = 0;
                                        foreach (var item in info.Files)
                                        {
                                            progressBar.Report(i++ / (double)info.Files.Count);
                                            try
                                            {
                                                string fileName = MakeFileNameValid($"{item.Name.Replace(" ", "_")}.{item.Extension}");
                                                await File.WriteAllBytesAsync(saveName + GetEnvSlash() + fileName, await httpClient.GetByteArrayAsync(item.Url));
                                                sb.AppendLine($"[{fileName}]({fileName})\r\n");

                                                if (item.Extension == "png" || item.Extension == "jpg" || item.Extension == "jpeg" || item.Extension == "gif")
                                                    sb.AppendLine($"![{fileName}]({fileName})\r\n");
                                            }
                                            catch (Exception ex) { Console.WriteLine(ex.ToString()); if (ex.Message.Contains("403")) return; }
                                        }
                                    }
                                }

                                MarkdownPipelineBuilder pipelineBuilder = new MarkdownPipelineBuilder().UseAutoLinks();
                                //if (postJson.body.video != null && postJson.body.video.serviceProvider == "youtube")
                                //{
                                //    sb.AppendLine($"![youtube.com](https://www.youtube.com/watch?v={postJson.body.video.videoId})\r\n");
                                //    pipelineBuilder = pipelineBuilder.UseMediaLinks();
                                //}

                                if (info.Text != null)
                                    sb.AppendLine(info.Text);

                                string html = Markdown.ToHtml(sb.ToString(), pipelineBuilder.Build());
                                await File.WriteAllTextAsync($"{saveName}{GetEnvSlash()}Post.html", html); //可能會有非同步存取的問題

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
                                                clientHandler.CookieContainer.Add(new Cookie("gfsid", gfCookie, "/", uri.Host));

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

                                if (html.Contains("drive.google.com"))
                                {
                                    Log.Warn("此貼文包含雲端連結，請確認是否需要自行下載附件");
                                    Log.Warn($"https://www.fanbox.cc/@{info.CreatorId}/posts/{info.Id}");
                                }
                            }
                        }

                        if (lastSavePostId.ContainsKey(creators.CreatorId))
                            lastSavePostId[creators.CreatorId] = maxPostId;
                        else
                            lastSavePostId.Add(creators.CreatorId, maxPostId);

                        await File.WriteAllTextAsync("LastSavePostId.json", JsonConvert.SerializeObject(lastSavePostId, Formatting.Indented));
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("400"))
                        Log.Error("缺少 Cookie，請重新輸入 Cookie");
                    else if (ex.Message.Contains("401"))
                        Log.Error($"{cookieId} 的 Cookie 錯誤，請重新輸入 Cookie");
                    else
                        Log.Error($"{ex}");

                    Console.ReadKey();
                    return;
                }
            }

            File.WriteAllText("SupportList.json", JsonConvert.SerializeObject(userSupportingDic, Formatting.Indented));

            Console.Title = $"Fanbox 下載工具";
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

            filename = filename.Trim();
            if (string.IsNullOrEmpty(filename))
                filename = "NA";

            return filename;
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
            pathname = pathname.Trim();
            if (string.IsNullOrEmpty(pathname))
                pathname = "No Title";

            return pathname;
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
                result = head.Headers.First((x) => x.Key == "Set-Cookie").Value.First((x) => x.StartsWith($"{cookieName}="));
                Regex regex = new Regex(cookieName + @"=(.*);");
                result = regex.Match(result).Groups[1].Value;
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

                if (string.IsNullOrEmpty(head.Content.Headers.ContentDisposition.FileNameStar))
                    return "";

                result = head.Content.Headers.ContentDisposition.FileNameStar;
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
                if (File.Exists(listSavePath))
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(listSavePath));
                }
                else
                {
                    var dic = new Dictionary<string, string>
                    {
                        { "savePath", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"{GetEnvSlash()}FanBox下載" },
                        { "default", "2. 贊助類(圖&影)" }
                    };
                    File.WriteAllText(listSavePath, JsonConvert.SerializeObject(dic, Formatting.Indented));
                    return dic;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString(), ConsoleColor.DarkRed); throw; }
        }

        static string GetSaveFolderName(string creatorId, string creatorName)
        {
            creatorName = MakeFileNameValid(creatorName.Split(new char[] { '@' }).First());

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