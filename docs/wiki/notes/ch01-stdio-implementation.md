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

程式用的是 .NET 的 top-level statements 寫法。Host 的組裝（前 17 行）不管加幾個 tool 都不會變，真正會長大的是後面的 tool 類別：

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
public class FirstTools(ILogger<FirstTools> logger)
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";

    [McpServerTool(Name = "fibonacci"), Description("Gets the first N Fibonacci numbers.")]
    public Dictionary<int, int> GetFibonacciNumbers(int count)
    {
        logger.LogInformation("Calculating Fibonacci numbers up to count: {Count}", count);

        var dict = new Dictionary<int, int>();
        if (count <= 0)
            return dict;

        int a = 0, b = 1;
        dict[1] = a; // 第 1 個（index 1）是 0

        if (count == 1)
            return dict;

        dict[2] = b; // 第 2 個（index 2）是 1

        for (int i = 3; i <= count; i++)
        {
            int next = a + b;
            dict[i] = next;
            a = b;
            b = next;
        }

        return dict;
    }
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

### 4. Tool 的寫法：類別 + Attribute

```csharp
[McpServerToolType]
public class FirstTools(ILogger<FirstTools> logger)
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";

    [McpServerTool(Name = "fibonacci"), Description("Gets the first N Fibonacci numbers.")]
    public Dictionary<int, int> GetFibonacciNumbers(int count) { /* ... */ }
}
```

三個 attribute 先講清楚：

- `[McpServerToolType]` 標在類別上，告訴 `WithToolsFromAssembly()`「這個類別裡有 tool」。
- `[McpServerTool]` 標在方法上，這個方法就會被暴露成一個 MCP tool。
- `[Description("...")]` 是給 **LLM** 看的說明文字，並不是給人類開發者看的註解——用戶端（如 Postman）呼叫 `tools/list`
  時，這段文字會回傳給前端顯示，也是 LLM 決定「要不要呼叫這個 tool」的依據之一。

一個類別可以放多個 tool，方法自己就是一個普通方法：參數（`message`／`count`）會自動對應成 tool 的輸入欄位，
回傳值會由 SDK 包裝進 MCP 回應。完全沒有手動處理 JSON-RPC 或協定細節。以下是把 `FirstTools` 拿來當範例後，
比起最初的 `EchoTool` 多帶出來的幾個 MCP 重點。

#### 4a. Tool 類別可以是「實例類別」，由 DI 容器建構

最初的 `EchoTool` 是 `static class`，這次改成一般 class 並用 primary constructor 注入 `ILogger<FirstTools>`：

```csharp
public class FirstTools(ILogger<FirstTools> logger)
```

關鍵在於 **`WithToolsFromAssembly()` 註冊的 tool 類別，是由 MCP server 從 DI 容器建構出實例來呼叫的**。
所以只要是 host 的 DI 容器裡有登記的服務——`ILogger<T>`、`IConfiguration`、`HttpClient`、自己註冊的 repository——
都能直接從建構子注入到 tool 裡。這是 static 類別完全看不到、卻是實務上寫「真的會做事」的 tool 時最重要的一環：
tool 不再是孤立的靜態函式，而是能吃到整個應用程式基礎建設的元件。

> 這也回收了[前面第 1 段](#1-用-generic-host-當地基)埋的伏筆——當初搭 Generic Host 換來的 DI 容器，到這裡才真正用上。

#### 4b. static 與 instance 方法可以混用

同一個類別裡，`Echo` 仍是 `static`、`GetFibonacciNumbers` 是 instance 方法，兩者都能正常註冊成 tool：

- `Echo` 不需要任何注入的服務，維持 `static`，SDK 不必先有實例就能呼叫。
- `GetFibonacciNumbers` 內部要用到注入的 `logger`，所以是 instance 方法，SDK 呼叫前會先取得類別實例。

換句話說「要不要 static」是看**這個方法本身有沒有用到實例狀態（注入的服務）**，而不是硬性規定。

#### 4c. 用 `Name =` 自訂 tool 名稱

```csharp
[McpServerTool(Name = "fibonacci"), Description("...")]
public Dictionary<int, int> GetFibonacciNumbers(int count)
```

不指定時，tool 名稱預設取自方法名（像 `Echo` 對外就叫 `Echo`）。這裡用 `Name = "fibonacci"` **顯式覆寫**，
所以用戶端在 `tools/list` 看到、`tools/call` 要呼叫的名稱是 `fibonacci`，而不是 C# 方法名 `GetFibonacciNumbers`。
好處是對外協定名稱可以跟 C# 命名慣例脫鉤——方法名照 .NET 習慣用 PascalCase，對外 tool 名可以維持精簡、穩定的識別字。

#### 4d. 回傳型別不限字串，SDK 會自動序列化

`Echo` 回傳 `string`，`GetFibonacciNumbers` 直接回傳 `Dictionary<int, int>`。SDK 會把這種結構化回傳值
序列化成 JSON 再包進 MCP 回應，呼叫端拿到的就是可解析的結構化資料，不用自己在 tool 裡手動 `JsonSerializer.Serialize`。
所以 tool 的簽章可以照商業邏輯自然地回傳 DTO／集合／字典，序列化交給 SDK。

> 費氏數列的演算法本身不是重點：它就是回傳前 `count` 個費氏數，字典的 key 是序號（從 1 起算）、value 是該位置的值，
> 並處理了 `count <= 0`、`count == 1` 兩個邊界。真正要記住的是上面 4a–4d 這幾個 MCP 面向的行為。

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
| 想加第二個 tool 卻抓不到 | 方法要 `public`、所屬類別要標 `[McpServerToolType]`；方法可 static 也可 instance（用到注入服務時就寫成 instance） |
| 注入服務拿到 null / 建不出 tool | tool 類別改成 instance 後，建構子注入的服務必須先在 DI 容器註冊（`ILogger<T>` 由 Host 內建，其餘要自己 `builder.Services.Add...`） |

---

## 延伸

- 逐步測試/連線教學（Postman + MCP Inspector）：[ch01-testing-mcp.md](ch01-testing-mcp.md)
- 專案自身的 README（含建立流程重點）：[Ch01.GettingStarted.Stdio/README.md](../../../src/Ch01.GettingStarted.Stdio/README.md)
- HTTP（Streamable HTTP）transport 的實作方式屬課程 Module 2 內容，待 Ch02 專案建立後另開筆記。
