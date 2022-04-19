# Fanbox下載工具
![img](https://github.com/jun112561/pixivFANBox/raw/master/Docs/1.png)

**需自行使用.NET Core 6.0編譯**

特色
-
* 自動化下載貼文內圖片&影片&附件&貼文內容，並建立簡易版貼文網頁
* 多帳號批次下載 (需自行新增Cookie到 "Cookie.txt" 文件內)

![img](https://github.com/jun112561/pixivFANBox/raw/master/Docs/2.png)
* 支援自訂義保存位置，並可針對單一贊助者設定位置 (預設保存在桌面的 "FanBox下載" 資料夾內，依創作者名稱區分)

![img](https://github.com/jun112561/pixivFANBox/raw/master/Docs/3.png)
* 遇到gigafile網址時自動下載檔案
* 下載完後自動建立最後下載的貼文ID清單避免重複下載
* 自動建立已贊助者清單，方便查閱該帳號贊助了哪些人
* 使用.NET Core 6.0開發，可跨平台使用 (已實測可同時用在Windows與Linux平台上，只要有.NET Runtime都可使用)

使用方式
-
1. 首次執行程式後會要求使用者輸入fanbox.cc的 "FANBOXSESSID" Cookie
2. 程式確認該Cookie有效後會自動從該Cookie取得贊助中的創作者並自動下載全部可閱讀的貼文
3. 完事
