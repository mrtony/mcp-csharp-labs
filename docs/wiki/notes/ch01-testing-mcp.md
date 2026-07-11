# Ch01 — 測試 STDIO MCP Server(Postman + MCP Inspector)

對應課程 Module 1《Getting Started with the MCP SDK》的 *Demo: Creating an MCP Server (STDIO)*。
本篇是逐步操作手冊,示範怎麼「連上並實際呼叫」[Ch01.GettingStarted.Stdio](../../../src/Ch01.GettingStarted.Stdio/) 這個 stdio server。

> 為什麼 stdio server 不能直接 F5 就看到結果?
> 因為 stdio server 的溝通管道是行程的 **stdin/stdout**,你需要一個「MCP 用戶端」從另一端跟它對話。
> 本篇提供兩種用戶端:**Postman** 與 **MCP Inspector**。

---

## 0. 前置需求

| 項目 | 說明 |
| --- | --- |
| .NET 9 SDK | 建置與執行專案 |
| Postman | 方法 A 用。內建 MCP request(桌面版,需為支援 MCP 的版本;New 清單中要能看到 **MCP** 才行) |
| Node.js（含 npx） | 方法 B 用。`@modelcontextprotocol/inspector` 以 npx 執行(首次執行會上網下載,需連網) |
| Visual Studio（選用） | 只有方法 A-4 的中斷點除錯需要;純測試 tool 不需要 |
| 這個測試對象 | `Echo` tool:輸入 `message`(字串),回傳 `hello <message>` |

> 📌 下文所有指令與路徑都以本 repo 位於 `F:\mygithub\mcp-csharp-labs` 為例。
> 若你的 repo 在別處,請把絕對路徑換成你自己的位置(相對路徑指令則在 repo 根目錄執行即可)。

先在 repo 根目錄建置專案,產生等一下要連的執行檔:

```powershell
dotnet build src/Ch01.GettingStarted.Stdio
```

建置後的執行檔路徑(方法 A 會用到,請記下絕對路徑):

```
F:\mygithub\mcp-csharp-labs\src\Ch01.GettingStarted.Stdio\bin\Debug\net9.0\Ch01.GettingStarted.Stdio.exe
```

---

## 方法 A — 用 Postman 測試(STDIO 模式)

Postman 主要是 API 用戶端,但新版內建 **MCP** request 類型,可以直接啟動並連上本機的 stdio server。

> ⚠️ **開始前務必先建置**:方法 A 要連的是上面那支 `.exe`,它是 `dotnet build` 後才產生的。
> 若略過第 0 節的 `dotnet build`,exe 不存在,Connect 會失敗且找不到原因。

### A-1. 建立 MCP request

1. Postman 左上角 **New** → 在清單中選 **MCP**。
2. 新的 request 上方會有 transport 選項:**STDIO** 與 **HTTP**。這裡保持 **STDIO**。

### A-2. 設定要啟動的執行檔

1. 在指令欄位貼上上面那個 `.exe` 的完整路徑(含 `.exe` 副檔名):

   ```
   F:\mygithub\mcp-csharp-labs\src\Ch01.GettingStarted.Stdio\bin\Debug\net9.0\Ch01.GettingStarted.Stdio.exe
   ```

2. 點 **Connect**。
3. 出現權限詢問視窗 → 點 **OK**(允許 Postman 啟動這個本機行程)。

> 連線成功後,Postman 會實際「啟動」這支 exe 當作子行程,透過 stdin/stdout 跟它做 JSON-RPC 對話。

### A-3. 呼叫 Echo tool

1. 連上後會看到 **Tools / Prompts / Resources** 三個分頁。
   - **Tools** 分頁應看到 `Echo`,旁邊的說明文字就是程式碼裡 `[Description("Echoes the message back to the client.")]` 來的。
   - Prompts / Resources 分頁是空的(本專案沒有註冊)。
2. 點 `Echo` tool → 出現輸入欄位 `message`。
3. `message` 填 `MCP` → 按 **Run**。
4. 結果應為:

   ```
   hello MCP
   ```

5. 結果區有 **JSON** 檢視與 **Preview** 檢視兩種,Preview 較好讀。

### A-4.（進階)中斷點除錯

若想在 tool 方法裡下中斷點觀察:

1. 在 Visual Studio 對 `Echo`(或你新增的方法)設中斷點。
2. **先在 Postman 按 Disconnect**,再重新建置。
   > ⚠️ **常見踩雷**:如果 Postman 還連著,exe 檔被行程鎖住,重建會失敗(「無法存取檔案」)。一定要先 **Disconnect** 才能 rebuild。
3. 重建成功後,回 Postman 按 **Connect**。
4. Visual Studio → **Debug → Attach to Process**,找到 `Ch01.GettingStarted.Stdio.exe` → **Attach**。
   > 要 attach 的是「Postman 幫你啟動的那個行程」。不要自己在 VS 按 F5 另外跑一個實例,否則會 attach 到錯的行程、停不到中斷點。
5. 回 Postman 執行 tool,就會停在中斷點。

### A-5. 看 log

- Postman 的 **Console** 區(視窗下方)可以看到 server 的 log 輸出。
  > 本專案刻意把 log 導到 **stderr**(`LogToStandardErrorThreshold = LogLevel.Trace`),所以這些 log 不會污染走 stdout 的 MCP 協定訊息。詳見 [Ch01 README](../../../src/Ch01.GettingStarted.Stdio/README.md)。

---

## 方法 B — 用 MCP Inspector 測試

[MCP Inspector](https://github.com/modelcontextprotocol/inspector) 是官方的除錯用戶端,以 npx 一行啟動、開一個網頁 UI。

### B-1. 啟動 Inspector

在 **repo 根目錄** 執行(建議先 `dotnet build`,再直接指向 exe,避免 `dotnet run` 的建置輸出污染 stdout):

```powershell
# 建議做法:指向已建置好的 exe
npx @modelcontextprotocol/inspector `
  src/Ch01.GettingStarted.Stdio/bin/Debug/net9.0/Ch01.GettingStarted.Stdio.exe
```

或用 `dotnet run`(較方便,但第一次會夾帶建置訊息):

```powershell
npx @modelcontextprotocol/inspector dotnet run --project src/Ch01.GettingStarted.Stdio
```

執行後終端機會印出一個帶 token 的本機網址(例如 `http://localhost:6274/?...`),用瀏覽器打開。

### B-2. 連線並呼叫

1. Inspector 左側 transport 選 **STDIO**(用上面指令啟動時通常已自動帶入 command/args)。
2. 按 **Connect**。
3. 切到 **Tools** 分頁 → **List Tools** → 應看到 `Echo`。
4. 點 `Echo` → `message` 填任意字(例如 `MCP`)→ **Run Tool**。
5. 回傳結果應為 `hello MCP`。

---

## 預期結果與協定長相

不論用哪種用戶端,`Echo("MCP")` 底層都是一次 JSON-RPC `tools/call`,回傳的內容大致如下(text 內容型別):

```json
{
  "content": [
    { "type": "text", "text": "hello MCP" }
  ],
  "isError": false
}
```

---

## 常見踩雷速查

| 症狀 | 原因 / 解法 |
| --- | --- |
| 重建 exe 失敗、說檔案被占用 | Postman/Inspector 還連著行程,先 **Disconnect** 再 rebuild |
| 連上但 Tools 是空的 | 確認有跑 `WithToolsFromAssembly()`,且 tool 類別有 `[McpServerToolType]`、方法有 `[McpServerTool]` |
| 用戶端收到奇怪/破損的訊息 | log 或 `Console.WriteLine` 寫到了 **stdout**,污染了協定。stdio server 的 log 一律要導到 **stderr** |
| `npx` 抓不到 inspector | 確認已安裝 Node.js,或先 `npm i -g @modelcontextprotocol/inspector` |
| Postman 找不到 MCP request 類型 | 更新到支援 MCP 的 Postman 版本(New → 清單中要有 MCP) |

---

## 延伸

- HTTP(Streamable HTTP)transport 的測試方式屬課程 Module 2 內容,待 Ch02 專案建立後另開筆記。
- 相關程式碼與說明:[Ch01.GettingStarted.Stdio/README.md](../../../src/Ch01.GettingStarted.Stdio/README.md)
