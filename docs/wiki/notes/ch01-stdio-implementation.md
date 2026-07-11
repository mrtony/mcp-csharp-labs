# Ch01 — STDIO MCP Server 的程式實作筆記

對應課程 Module 1《Getting Started with the MCP SDK》的 *Demo: Creating an MCP Server (STDIO)*。
本篇聚焦「程式碼怎麼寫出來的」，逐行拆解 [Ch01.GettingStarted.Stdio](../../../src/Ch01.GettingStarted.Stdio/) 這個最小可運作 MCP server。
若要測試/連線這支 server，請看 [ch01-testing-mcp.md](ch01-testing-mcp.md)。

---

## 專案骨架

整個練習只有兩個檔案：

```
src/Ch01.GettingStarted.Stdio/
├── Ch01.GettingStarted.Stdio.csproj
└── Program.cs
```

`.csproj` 本身很單薄，因為 TFM／Nullable／ImplicitUsings 都由根目錄的
[`Directory.Build.props`](../../../Directory.Build.props)（`net9.0`、`Nullable=enable`）統一帶入，套件版本也不寫在這裡，
而是由 [`Directory.Packages.props`](../../../Directory.Packages.props) 集中管理（CPM）：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="ModelContextProtocol" />
  </ItemGroup>
</Project>
```

實際版本號記在 `Directory.Packages.props`：

| 套件 | 版本 |
| --- | --- |
| `Microsoft.Extensions.Hosting` | 10.0.9 |
| `ModelContextProtocol` | 2.0.0-preview.1（目前 SDK 還在 preview） |

---

## Program.cs 逐段拆解

完整程式只有 25 行，用的是 .NET 的 top-level statements 寫法：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);

// stdio server 的 stdout 保留給 MCP 協定，log 一律導向 stderr
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}
```

### 1. 用 Generic Host 當地基

`Host.CreateApplicationBuilder(args)` 是 ASP.NET Core / .NET 泛用的 Generic Host 建構子，MCP C# SDK 直接搭在它上面用，
帶來的好處是 DI 容器、`ILogger`、設定系統（`appsettings.json`／環境變數）這些基礎建設不用自己重寫。

### 2. 為什麼 log 一定要導到 stderr

```csharp
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
```

stdio transport 的協定訊息（JSON-RPC）就是直接寫在**行程的 stdout**。如果 log 也印到 stdout，會跟協定訊息混在一起，
讓用戶端（Postman／MCP Inspector）解析失敗或收到「破損」的回應。這行把**所有等級**（`LogLevel.Trace` 起跳，等於全部）的 log
都改導向 stderr，stdout 就乾乾淨淨只留給協定用。

> 這是 stdio transport 特有的限制；如果之後 Ch02 換成 Streamable HTTP transport，log 就不需要這樣特殊處理
>（HTTP 走的是另一條連線，不會跟 stdout 打架）。

### 3. 三行註冊出一個 MCP Server

```csharp
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
```

| 呼叫 | 作用 |
| --- | --- |
| `AddMcpServer()` | 把 MCP server 的核心服務註冊進 DI 容器，回傳一個 builder 讓後面可以鏈式設定 |
| `WithStdioServerTransport()` | 指定用 **stdio**（stdin/stdout）當傳輸層 |
| `WithToolsFromAssembly()` | 掃描目前組件（assembly），把所有標了 `[McpServerToolType]` / `[McpServerTool]` 的方法自動註冊成 tool |

最後 `await builder.Build().RunAsync()` 啟動 host，程式就會停在這裡持續監聽 stdin，直到用戶端斷線或程式被中止（Ctrl+C）。

### 4. Tool 的寫法：靜態類別 + Attribute

```csharp
[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}
```

- `[McpServerToolType]` 標在類別上，告訴 `WithToolsFromAssembly()`「這個類別裡有 tool」。
- `[McpServerTool]` 標在方法上，這個方法就會被暴露成一個 MCP tool，工具名稱預設取自方法名（`Echo`）。
- `[Description("...")]` 是給 **LLM** 看的說明文字，並不是給人類開發者看的註解——用戶端（如 Postman）呼叫 `tools/list`
  時，這段文字會回傳給前端顯示，也是 LLM 決定「要不要呼叫這個 tool」的依據之一。
- Tool 方法本身就是一個普通的靜態方法：參數（`message`）會自動對應成 tool 的輸入欄位，回傳值會包裝成 MCP 的
  `content` 陣列回傳給用戶端。這裡完全沒有手動處理 JSON-RPC 或協定細節，全部由 SDK 處理掉。

---

## 這個範例刻意省略的東西

- 沒有 Resources、沒有 Prompts，只示範最基本的 Tools 能力。
- 沒有自訂 middleware／驗證，因為 stdio transport 是行程對行程直接溝通，不像 HTTP 有網路層的認證需求（HTTP 版本會在 Ch02 出現）。
- 沒有非同步 I/O 或外部相依（例如呼叫資料庫、API），`Echo` 純粹是同步字串處理，方便先聚焦在「SDK 怎麼接起來」這件事。

---

## 常見踩雷速查

| 症狀 | 原因 / 解法 |
| --- | --- |
| 用戶端收到破損/無法解析的訊息 | 有 log 或 `Console.WriteLine` 寫到了 stdout，沒有正確導向 stderr |
| Tools 清單是空的 | 忘記加 `WithToolsFromAssembly()`，或 tool 類別/方法缺少 `[McpServerToolType]` / `[McpServerTool]` |
| 想加第二個 tool 卻抓不到 | 方法必須是 `public static`，且所屬類別要標 `[McpServerToolType]` |

---

## 延伸

- 逐步測試/連線教學（Postman + MCP Inspector）：[ch01-testing-mcp.md](ch01-testing-mcp.md)
- 專案自身的 README（含建立流程重點）：[Ch01.GettingStarted.Stdio/README.md](../../../src/Ch01.GettingStarted.Stdio/README.md)
- HTTP（Streamable HTTP）transport 的實作方式屬課程 Module 2 內容，待 Ch02 專案建立後另開筆記。
