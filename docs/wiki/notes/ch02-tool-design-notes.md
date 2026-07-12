# Ch02 — Tool 設計與可觀測性筆記

對應課程 Module 2《Creating an MCP Server for APIs》裡的兩段:
*Question: What Logic Belongs in a Tool?* 與 *Demo: Logging and Tracing for our MCP Server*。

實作面(怎麼建 `CarvedRock.Mcp` 與 `get_products` tool)在
[ch02-carvedrock-mcp-implementation.md](ch02-carvedrock-mcp-implementation.md);
本篇談**觀念**:一個 MCP tool 裡「該放什麼邏輯」,以及怎麼把 logging / telemetry 弄好。

---

## 一、Tool 裡該放什麼邏輯?

會有個很自然的疑問:**既然 tool 只是打 API,為什麼不讓 agent 直接呼叫 API 就好,幹嘛多一層 MCP server?**

答案是:tool 方法是一段**你完全掌控的 .NET 程式碼**,可以做的遠不只轉呼叫。實作篇的 `get_products`
已經示範了最小的一種——回傳的 `ProductModel` 刻意**丟掉 `ImgUrl`**(塑形)。以下是這一層能塞的邏輯:

| 能做的事 | 為什麼有價值 |
| --- | --- |
| **快取(Cache)API 結果** | 若下游是較慢/較重的呼叫,而內容適合快取,就在 tool 這層擋掉重複請求。 |
| **加計算欄位(Computed fields)** | **LLM 不擅長精確計算,程式擅長**。先把該算的算好再給,結果更可靠。 |
| **塑形內容(Shape content)** | 把複雜 JSON 簡化:少幾個欄位、把巢狀拉平。`get_products` 用瘦身的 `ProductModel`(拿掉 `ImgUrl`)就是最簡版。 |
| **合併多個 API 呼叫** | 一個邏輯操作若需要打好幾支 API,在 tool 內合併,AI 只要叫**一次** tool。 |
| **給既有 API 一個更簡單的呼叫方式** | 為常見情境包一層好用的入口(課程後面會有例子)。 |
| **完全不打 API 也行** | 整個 .NET 世界都可用:自訂邏輯、甚至在 tool 裡呼叫生成式 AI 模型——這招叫 **agent as tool**。 |

> **貫穿全部的原則**:tool **回傳的內容**要「刻意設計成讓 LLM/AI 好消化」的形狀;
> **輸入參數**也一樣。這些只是範例,實際要建什麼 tool,取決於你的 AI 應用怎麼用最順。

這也呼應實作篇提到的:MCP 回應**本來就不該等於** API 回應——tool 這層存在的意義,正是這些加工。

### 塑形不是「一種形狀走天下」:讀取瘦身 vs 寫入完整

Ch02 專案剛好用**兩個型別**示範了塑形要看用途:

- **讀取路徑**(`get_products` / `get_single_product`)回傳瘦身版 **`ProductModel`**——**沒有 `ImgUrl`**,
  因為 AI 用不到圖片網址,少一個欄位就少一分雜訊。
- **寫入路徑**(`set_product_price`)卻用**含 `ImgUrl` 的完整版 `FullProductModel`**:
  它得先 `GET` 完整物件、只改價格、再整包 `PUT` 回 API,少一個欄位就會把資料寫壞。

> 同一份資源,給 AI 看的形狀(瘦身)和寫回 API 需要的形狀(完整)可以不同——塑形是**依用途**決定的。

### 順帶一提:「只加你打算讓 AI 用的」

`CarvedRockTools` 裡 `get_single_product` 上方有一句課程原註解:
**「別把 API 的每個方法都做成 tool,只加你打算讓 AI 用的」**。這正是「tool 太多會讓 LLM 混亂」的實務體現
——加 tool 的取捨細節見 [ch02-carvedrock-mcp-implementation.md](ch02-carvedrock-mcp-implementation.md) 的「加 tool 的四種方式」。

---

## 二、Logging 與 Tracing

MCP server 一樣是 ASP.NET Core app,logging/telemetry 沿用熟悉的模式即可;而且因為接了 Aspire 的
`AddServiceDefaults()`,幾乎不用額外設定就能在 Dashboard 看到 log 與 trace。

### 1. 在 tool 裡加 log:注入 `ILogger<T>`

沿用 ASP.NET Core 慣例,把 `ILogger<CarvedRockTools>` 加進主建構式,方法裡就能記 log:

```csharp
[McpServerToolType]
public class CarvedRockTools(
    IHttpClientFactory httpClientFactory,
    ILogger<CarvedRockTools> logger)                 // 一起注入
{
    [McpServerTool(Name = "get_products"), Description("Get a list of all available products.")]
    public async Task<List<ProductModel>> GetAllProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("CarvedRockApi");
        var response = await client.GetFromJsonAsync<List<ProductModel>>("/product", cancellationToken);
        logger.LogInformation("Fetched {Count} products", response?.Count ?? 0);   // 例:Fetched 50 products
        return response ?? [];
    }
}
```

跑一次(Inspector → Connect → List Tools → 叫 `get_products`)後,到 Aspire Dashboard:

- **Traces** 頁:最後一筆 trace 就是這次 tool 呼叫。展開可看到完整鏈路——
  **MCP server →(HTTP)→ API →(DB 查詢)**。所有對 MCP server 的呼叫都是 **POST**(JSON-RPC,有點像早年的 SOAP)。
- **Logs** 頁:能看到我們新加的 `Fetched 50 products`。

### 2. 為什麼「免設定」就有這些?——OpenTelemetry via ServiceDefaults

log 只是我們寫的一行,但它能自動出現在 Dashboard 並**不是魔法**:關鍵在 `Program.cs` 的
**`AddServiceDefaults()`**。它定義在
[`CarvedRock-Aspire.ServiceDefaults/Extensions.cs`](../../../src/Ch02.CreatingMcpServer/CarvedRock-Aspire.ServiceDefaults/Extensions.cs),
內部的 `ConfigureOpenTelemetry` 幫你配好 OpenTelemetry、health check、service discovery、resilience 等:

- logging 段有 `IncludeScopes = true` **(include scopes)**——啟用 logging scope,下一章的 `UserScopeMiddleware` 會用到。
- **metrics**:AspNetCore / HttpClient / Runtime instrumentation。
- **tracing**:AspNetCore / HttpClient instrumentation(所以 tool 打下游 API 的 HTTP 呼叫會自動被追蹤)。

> 換句話說:只要接了 `AddServiceDefaults()`,你的 MCP server 就自動吐出 OTel 的 logs/metrics/traces,
> Aspire Dashboard 直接可視化。想深入客製 OpenTelemetry,是另一門 Aspire 課程的範圍。

### 3. 升級 Aspire 後的加分:trace 上的關聯 log 點

課程過程把 solution 的 Aspire 相關套件升到 **9.5.1**(NuGet → Updates,取消勾 prerelease,
反選掉未使用的 AutoMapper)。升級後 Traces 頁的 span 線上會多出**小圓點**:

- 每個點是某個服務在該時間點的**一筆 log**,精準對應「何時、在哪個服務」發生。
- 例如 API 那條(橘色)線上能看到「進 repository」的 log;MCP server 那條能看到「fetched 50 products」。

這種**跨服務關聯**的觀測,開發期就非常有用——全部建立在 OpenTelemetry 之上。

---

## 重點速記

- **MCP tool = 你掌控的一段程式**,不是單純 API proxy;快取、計算欄位、塑形、合併呼叫、甚至 agent as tool 都能塞。
- tool 的**輸入與輸出**都要為「LLM 好消化」而設計——這正是 MCP 回應不等於 API 回應的原因。
- **`AddServiceDefaults()`** 幫你把 OpenTelemetry(logs/metrics/traces)一次配好,MCP server 免額外設定就能在 Aspire Dashboard 觀測。
- tool 裡 log 就用一般的 **`ILogger<T>` 注入**;升上 Aspire 9.5.1 後 trace 會顯示關聯 log 點,跨服務除錯更直覺。

---

## 延伸

- 建 server 與 tool 的實作步驟、JSON-RPC 請求/回應、呼叫流程圖:[ch02-carvedrock-mcp-implementation.md](ch02-carvedrock-mcp-implementation.md)
- 更完整的 logging/telemetry(含登入使用者、`UserScopeMiddleware`、角色授權)屬 Module 3 安全性,待該章另開筆記。
- Aspire 的 `ServiceDefaults` / OpenTelemetry 觀念,在 [ch01-http-implementation.md](ch01-http-implementation.md) 的「觀測(Aspire Traces)」段也有提到。
