# Fanbox 下載工具
![img](https://github.com/konnokai/PixivFanboxDownloader/raw/master/Docs/1.png)

**需自行使用.NET Core 6.0編譯**

特色
-
* 自動化下載貼文內圖片&影片&附件&貼文內容，並建立簡易版貼文網頁
* 多帳號批次下載 (需自行新增Cookie到 "Cookie.txt" 文件內)

![img](https://github.com/konnokai/PixivFanboxDownloader/raw/master/Docs/2.png)
* 支援自訂義保存位置，並可針對單一贊助者設定位置 (預設保存在桌面的 "FanBox下載" 資料夾內，依創作者名稱區分)

![img](https://github.com/konnokai/PixivFanboxDownloader/raw/master/Docs/3.png)
* 遇到 gigafile 網址時自動下載檔案
* 下載完後自動建立最後下載的貼文 Id 清單避免重複下載
* 自動建立已贊助者清單，方便查閱該帳號贊助了哪些人
* 使用 .NET Core 6.0 開發，可跨平台使用 (已實測可同時用在 Windows 與 Linux 平台上，只要有 .NET Runtime 都可使用)

使用方式
-
1. 請先自架一台 [FlareSolverr](https://github.com/FlareSolverr/FlareSolverr) 伺服器
2. 首次執行程式後會要求使用者複製並修改 `ProgramConfigExample.json` 的內容，將其中的 `FlareSolverrApiUrl` 修改為正確的伺服器 IP
3. 重開程式後會要求使用者輸入 fanbox.cc 的 `FANBOXSESSID` Cookie
4. 程式確認該 Cookie 有效後會自動從該 Cookie 取得贊助中的創作者並自動下載全部可閱讀的貼文
5. 完事
