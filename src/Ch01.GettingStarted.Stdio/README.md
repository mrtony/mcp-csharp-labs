# Ch01 — Getting Started：STDIO MCP Server

對應課程 Module 1《Getting Started with the MCP SDK》的 *Demo: Creating an MCP Server (STDIO)*。

## 目標

用官方 C# SDK 建立一個最小可運作的 MCP server，透過 **stdio** transport 對外提供一個 `Echo` tool。

## 做法重點

- 套件：`ModelContextProtocol` + `Microsoft.Extensions.Hosting`。
- 用 `Host.CreateApplicationBuilder` 建立 host，服務註冊三步：
  - `AddMcpServer()` — 註冊 MCP server 服務
  - `WithStdioServerTransport()` — 使用 stdio transport
  - `WithToolsFromAssembly()` — 自動掃描組件中標了 `[McpServerToolType]` / `[McpServerTool]` 的 tool
- Tool 以靜態類別 + 方法定義，`[Description]` 會成為 tool/參數的說明給 LLM 看。
- **關鍵**：stdio 的 stdout 專供 MCP 協定 JSON-RPC 使用，所以 log 必須導到 **stderr**
  （`LogToStandardErrorThreshold = LogLevel.Trace`），否則會污染協定訊息。

## 測試方式

```powershell
# 直接跑，啟動後停在等待 stdin 即為正常（Ctrl+C 結束）
dotnet run --project .

# 用 MCP Inspector（於 repo 根目錄執行）
npx @modelcontextprotocol/inspector dotnet run --project src/Ch01.GettingStarted.Stdio
```

在 Inspector：Connect → Tools → 應看到 `Echo`，帶入 `message` 呼叫會回 `hello <message>`。

## 踩雷筆記

- （待補）
