# Ch02 — 為 CarvedRock 建 API MCP Server 的程式實作筆記

對應課程 Module 2《Creating an MCP Server for APIs》,把前一章的 Hello World 練習搬到真實情境:
為虛構公司 **Carved Rock Fitness**(戶外運動器材電商)既有的解決方案,加上一支 MCP server,
讓 AI 能透過 tool 取用產品目錄。對象是 `CarvedRock.Mcp` 這個新專案。

> 📌 對應的原始碼專案 `src/CarvedRock.Mcp/` **尚待建立**;本篇先聚焦「怎麼組起來、資料怎麼流動」,
> 專案完成後再把下方路徑補成實際連結。
> 延續 [ch01-http-implementation.md](ch01-http-implementation.md) 的 Streamable HTTP + Aspire 基礎,這章不再重複那些共通細節。

---

## 情境:這章要做什麼

產品端提了一個功能需求:**在聊天介面用 AI 做產品推薦**——
使用者輸入想找什麼,AI 拿這段文字加上 Carved Rock 目錄裡的產品,給出簡單推薦。

我們(後端)這章要交付的第一塊:

1. 建一支 MCP server,提供一個能**回傳所有產品**的 tool。
2. (課程原本還要求自動化測試,本筆記依範圍**略過所有測試 demo**。)

> Carved Rock 起始的 app 本身沒有任何 AI/MCP,就是一個很簡單的電商:產品 CRUD API + 前端 UI +
> Aspire app host + 一堆 class library,登入走公開的 Duende IdentityServer demo。
> 這章只在它上面「加一支 MCP server」,不動原本的功能。

---

## 專案結構(這章新增的部分)

| 專案 | 角色 | 這章的動作 |
| --- | --- | --- |
| `CarvedRock.Mcp` | **新增** 的 MCP server | 用 **ASP.NET Core Empty** 範本建,掛 Streamable HTTP,提供 `get_products` tool。 |
| `CarvedRock.AppHost` | Aspire 編排入口 | 加一行把 MCP server 納入編排;另外**掛上 MCP Inspector**。 |
| `CarvedRock.Api` | 既有的產品 CRUD API | 不改,被 MCP tool 當下游呼叫(`GET /products`)。 |
| MCP Inspector | 手動互動用戶端 | 由 `Aspire MCP` 套件加進 app host,Dashboard 裡會多一個資源。 |

> 命名對照專案慣例:solution 裡用 `Ch02.` 前綴排序;實體專案預計放 `src/CarvedRock.Mcp/`。

---

## 做法重點

### 1. AppHost:把 MCP server 納入編排 + 掛 Inspector

新專案用 **ASP.NET Core Empty** 範本建立、勾選加入 Aspire 編排後,app host 會多出一行。把它整理成:

```csharp
var api = builder.AddProject<Projects.CarvedRock_Api>("api")
    .WithHttpHealthCheck("/health");

var mcp = builder.AddProject<Projects.CarvedRock_Mcp>("mcp")   // 接住回傳、簡化 Dashboard 顯示名
    .WithHttpHealthCheck("/health")
    .WithReference(api);                                        // MCP server 要呼叫 API,先給它 reference

builder.AddMcpInspector("mcp-inspector")
    .WithMcpServer(mcp, path: "");                              // Inspector 指向 mcp 的根路徑
```

- **`WithReference(api)`** 讓 MCP server 靠 **Aspire service discovery** 找得到 API(同 Ch01 的機制)。
- 順手把 `WithHttpHealthCheck("/health")` 補到其他專案,方便部署到更高環境後也能引用這些 health check。
- **MCP Inspector** 由 `Aspire MCP`(社群工具包)NuGet 套件提供 `AddMcpInspector()`,直接在 Dashboard 內起一個 Inspector 資源(不必自己另外用 npx 手動開)。
- `WithMcpServer(mcp, path: "")` 的 `path: ""` 要與下方 `MapMcp()` 的根路徑一致,Inspector 才連得到
  (Ch01 踩過這雷:路徑對不上 Inspector 會連不上)。

### 2. MCP server 的 `Program.cs`:註冊 MCP + HTTP transport + API client

先加 `ModelContextProtocol.AspNetCore`(勾 prerelease,選 **ASP.NET Core** 版)。管線大致長這樣:

```csharp
builder.AddServiceDefaults();                       // Aspire 共用設定(含 OpenTelemetry、health check)

builder.Services.AddMcpServer()
    .WithHttpTransport()                             // Streamable HTTP transport
    .WithTools<CarvedRockTools>();                   // 明確註冊 tool 類別(見下方「加 tool 的四種方式」)

builder.Services.AddHttpClient("CarvedRockApi", c =>
    c.BaseAddress = new("https://api"));            // 打 app host 裡的 api 資源(注意用 HTTPS)

var app = builder.Build();
app.MapDefaultEndpoints();                           // /health、/alive
app.MapMcp();                                        // MCP endpoint 掛在根路徑(不指定 path)
app.Run();
```

- `https://api` 不是真實 DNS,是 app host 裡 API 資源的**邏輯名稱**,靠 service discovery 解析
  ——**要用 `https://`** 對應 app host 對外開的 HTTPS endpoint,寫成 `http://` 會連不上。
- `AddMcpServer()` 之後**一定要接 `.WithTools<...>()`**(或其他註冊方式),否則 tool 不會被登錄,
  Inspector 連得上但 List Tools 會是空的。
- `MapMcp()` 取代範本原本的 `MapGet("/", "Hello World")`,把 MCP 掛在根路徑。

### 3. `CarvedRockTools`:呼叫產品 API 的 `get_products` tool

用 **Shift+F2** 在專案節點加 `CarvedRockTools.cs`,類別上加 `[McpServerToolType]`:

```csharp
[McpServerToolType]
public class CarvedRockTools(IHttpClientFactory httpClientFactory)   // 主建構式注入工廠
{
    [McpServerTool(Name = "get_products"), Description("Gets all the products in the catalog.")]
    public async Task<List<ProductModel>> GetAllProductsAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("CarvedRockApi");
        var products = await client.GetFromJsonAsync<List<ProductModel>>(
            "/products", cancellationToken);
        return products ?? [];                                       // null 就回空清單
    }
}
```

要點對照課程逐步排錯的幾個點:

- **注入的是 `IHttpClientFactory`**(不是 `HttpClient`),方法內再 `CreateClient("CarvedRockApi")` 拿具名 client。
- 方法是 **async、帶 `CancellationToken`**,呼叫 `GET /products` **不帶任何參數**。
- 回傳型別是自訂的 `ProductModel`(見下),而不是 core library 原本的 product 類別。
- **`[McpServerTool(Name = "get_products")]`** 用 `Name` 覆蓋方法名 `GetAllProductsAsync`
  ——對外看到的 tool 名稱是 `get_products`。`Description` 需要 `using System.ComponentModel;`。

### 4. `ProductModel`:在 MCP 端「重塑」回傳型別

在 MCP 專案新建 `ProductModel.cs`,用一個**只留 AI 需要的欄位**的 record:

```csharp
// 欄位為示意 —— 課程只說「除了 image URL 之外全都要」,實際欄位以你 core library 的 product 為準
public record ProductModel(int Id, string Name, string Description, string Category, decimal Price);
// 刻意「不含」ImageUrl —— AI 用不到圖片網址,就不塞給它
```

> 這是本章第一個「tool 不是照搬 API 回傳」的例子:在 MCP 端塑形成剛好夠用的形狀。
> 背後的設計理由(為什麼要塑形、還能做什麼)整理在 [ch02-tool-design-notes.md](ch02-tool-design-notes.md)。

### 套件版本

- MCP server:`ModelContextProtocol.AspNetCore`(prerelease,ASP.NET Core 版)。
- app host:`Aspire MCP`(社群工具包,提供 MCP Inspector)。
- 課程過程中把整個 solution 的 Aspire 相關套件升到 **9.5.1**(觀測強化見 [ch02-tool-design-notes.md](ch02-tool-design-notes.md) 的 logging/tracing 段)。
- 版本集中在 [`Directory.Packages.props`](../../../Directory.Packages.props)(CPM),`csproj` 的 `PackageReference` 不帶版本。

---

## 加 tool 的四種方式(Options for Adding Tools)

> 核心提醒:**tool 給 AI 太多會讓它混亂**——叫錯 tool、或乾脆不叫。只加「該加的」很重要。

| 方式 | 怎麼運作 | 何時用 |
| --- | --- | --- |
| **`WithToolsFromAssembly()`** | 用反射掃組件,找 `[McpServerToolType]` 類別裡的 `[McpServerTool]` 方法。無參數=掃呼叫端組件,也可指定組件名。 | 想「一次全加」;Ch01 stdio 版用的就是這個。 |
| **`WithTools<T>()`** | 指定型別,找該類別裡有 `[McpServerTool]` 的方法。**類別層級的 `[McpServerToolType]` 對這個方式不是必要的**(本章範例仍加它,只為與其他註冊方式一致、也方便日後改用掃組件)。 | 只想加某個類別(本章 `CarvedRockTools` 用這個)。 |
| **`WithTools(...)` 帶清單** | 傳一組 `IEnumerable` 的 tool 方法(不給泛型型別),每個方法要有 `[McpServerTool]`。 | 想精挑細選到「方法」層級。 |
| **`ConfigureSessionOptions`** | 進階:在 session 層動態算出可用 tool 清單。 | 依授權情境決定露出哪些 tool(Module 3 安全性會用到)。 |

- 這幾種**可以組合使用**。
- 起手用 `WithTools<CarvedRockTools>()`;之後改用「授權過濾 + 掃組件」的組合是很常見的演進(下一章)。

---

## MCP 的請求／回應長怎樣(JSON-RPC 2.0)

在 Inspector 的 **History** 區可以看到每個動作其實都是一次 **HTTP POST**,底層走 **JSON-RPC 2.0**。
三個關鍵訊息:

| 動作 | 觸發時機 | request `method` | response 重點 |
| --- | --- | --- | --- |
| `initialize` | 按 **Connect** | `initialize` | 回 `capabilities`(含 `tools` 節點,表示 tool 清單有變動)+ server 資訊 |
| `tools/list` | 按 **List Tools** | `tools/list`(`params` 空物件) | 回 `tools` 陣列,每個含 `name`／`description`／`inputSchema`;`get_products` 無參數,schema 是「沒有屬性的 object」 |
| `tools/call` | 按 **Run** | `tools/call`(`params.name = get_products`,無 arguments) | 回 `content` 陣列,第一筆 `type: text`,`text` 就是 API 來的 JSON 字串 |

**關鍵觀念:MCP 的回應「不等於」API 的回應。**
API 直接打(Swagger)回的是**乾淨的 JSON 陣列**;MCP 回的是 JSON-RPC 包裝、`content[].text` 裡塞一段
**跳脫過雙引號的 JSON 字串**。這是刻意的設計——你從 MCP tool 回什麼、怎麼包,取決於 AI 服務最好消化的形式。

```jsonc
// tools/call 的 response(簡化)
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      { "type": "text", "text": "[{\"id\":1,\"name\":\"...\",\"price\":74.99}, ...]" }
    ]
  }
}
```

> Inspector 的 History 會把 response 內容截斷,完整內容看該筆的 **Tool Result** 區;Postman 的 **Preview** 分頁也比較好讀。

---

## 呼叫流程(Sequence Diagram)

`get_products` 的路徑停在 **Aspire 應用邊界內**:MCP server 靠 service discovery 把 `api` 解析成內部服務,不出網。

```mermaid
sequenceDiagram
    autonumber
    actor Client as MCP Client (Inspector)
    participant Mcp as CarvedRock.Mcp (Streamable HTTP)
    participant Tool as CarvedRockTools
    participant SD as Aspire Service Discovery
    participant Api as CarvedRock.Api
    participant Db as PostgreSQL

    Client->>Mcp: POST / — initialize / tools-list / tools-call
    Note over Client,Mcp: 三次都是 HTTP POST(JSON-RPC 2.0)
    Client->>Mcp: tools/call "get_products"(無參數)
    Mcp->>Tool: GetAllProductsAsync(cancellationToken)
    Note over Tool: CreateClient("CarvedRockApi")<br/>BaseAddress = https://api
    Tool->>SD: 送出前解析 host "api"
    SD-->>Tool: 改寫成實際 endpoint
    Tool->>Api: GET /products
    Api->>Db: 查產品
    Db-->>Api: 產品資料列
    Api-->>Tool: 200 — Product[] (乾淨 JSON)
    Note over Tool: 反序列化成 List<ProductModel><br/>(丟掉 ImageUrl)
    Tool-->>Mcp: List<ProductModel>
    Mcp-->>Client: tools/call result(JSON-RPC:content[].text 內含 JSON 字串)
```

---

## 手動測試(Aspire Dashboard + MCP Inspector)

> 本章只做**手動**驗證;課程的自動化測試 demo 依範圍略過。

1. **啟動點選 `CarvedRock.AppHost`** 執行,開啟 Aspire Dashboard,確認資源都 **Healthy**(至少 `api`、`mcp`、`mcp-inspector`)。
2. 點 **mcp-inspector** 的連結開啟 Inspector。
   > ⚠️ Inspector 用 **Edge** 開比較穩(Ch01 踩過 Chrome 開空白的雷)。
3. Inspector:Transport 選 **Streamable HTTP** → **Connect** → **List Tools**,應看到 `get_products`。
4. 選 `get_products` → **Run**(無需輸入參數)→ 結果區應看到產品清單 JSON。
5. 到 Dashboard **Traces** 頁,可看到這次呼叫的 span:MCP server → API → DB;所有對 MCP server 的呼叫都是 **POST**。

---

## 常見踩雷速查

| 症狀 | 原因 / 解法 |
| --- | --- |
| Inspector 連得上但 **List Tools 是空的** | `AddMcpServer()` 後忘了接 `.WithTools<CarvedRockTools>()`(或其他註冊方式) |
| Inspector 連不上 mcp | Transport 要選 **Streamable HTTP**;且 Inspector 的 `path` 與 `MapMcp()` 都要在根路徑 `""` |
| tool 呼叫時連不到 API | HttpClient 的 `BaseAddress` 要用 **`https://api`**(HTTPS + app host 的邏輯名),且 app host 有 `.WithReference(api)` |
| 對外看到的 tool 名稱是 `GetAllProductsAsync` | 沒設 `[McpServerTool(Name = "get_products")]`,對外就會用方法名 |
| `Description` 編譯不過 | 少了 `using System.ComponentModel;` |
| Inspector 開起來一片空白 | 改用 **Edge** 開 |

---

## 延伸

- 為什麼 tool 要「塑形」回傳、還能塞哪些邏輯,以及 logging/tracing 怎麼設:[ch02-tool-design-notes.md](ch02-tool-design-notes.md)
- 前一章的 HTTP 版程式實作(共通的 transport / service discovery 細節):[ch01-http-implementation.md](ch01-http-implementation.md)
- 安全性(OAuth、角色授權、token 轉發)屬 Module 3,待該章專案建立後另開筆記。
