# mcp-csharp-labs

用 C# 建立 MCP (Model Context Protocol) server 的課程練習與筆記。單一 repo + 單一 solution，每個課程模組對應一個獨立練習專案。

## 環境需求

- .NET SDK 9.0.x（見 [`global.json`](global.json)）
- （選配）[MCP Inspector](https://github.com/modelcontextprotocol/inspector)：`npx @modelcontextprotocol/inspector` — 手動測試 MCP server

## 參考資源

- [Get started with the MCP C# SDK（Microsoft Learn）](https://learn.microsoft.com/en-us/dotnet/ai/get-started-mcp) — 官方入門教學
- [modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk) — C# MCP SDK 原始碼與範例

## 練習索引

| 章節 | 專案 | Transport | 主題 |
| --- | --- | --- | --- |
| Ch01 | [`Ch01.GettingStarted.Stdio`](src/Ch01.GettingStarted.Stdio) | stdio | 最小 MCP server + Echo tool |

> 之後每完成一個模組，新增一列並附上該練習的 README 連結。

## 建置與執行

```powershell
# 建置整個 solution
dotnet build

# 執行 Ch01 stdio server（啟動後等待 stdin 為正常）
dotnet run --project src/Ch01.GettingStarted.Stdio

# 用 MCP Inspector 測試
npx @modelcontextprotocol/inspector dotnet run --project src/Ch01.GettingStarted.Stdio
```

## 專案慣例

- **命名**：`ChNN.主題`，對照課程章節，讓 Solution Explorer 依課程順序排列。
  同一模組若有多個 transport／版本，用後綴細分（如 `Ch01.GettingStarted.Stdio`、`Ch01.GettingStarted.Http`）。
- **共用設定**：TFM / Nullable / ImplicitUsings 統一在 [`Directory.Build.props`](Directory.Build.props)，各專案自動繼承。
- **套件版本**：集中式管理（CPM），版本寫在 [`Directory.Packages.props`](Directory.Packages.props)，csproj 的 `PackageReference` 不帶版本。
- 每個練習專案內都有自己的 `README.md`，記錄目標、做法、測試方式與踩雷筆記。

## 新增一個練習

用腳本一鍵完成（建專案、加入 solution、加套件、精簡 csproj、產生 README 樣板）：

```powershell
# stdio server（console 專案）
./scripts/new-lab.ps1 -Name ChNN.主題

# HTTP server（ASP.NET Core Web 專案）
./scripts/new-lab.ps1 -Name ChNN.主題 -Transport http
```

腳本跑完後仍需手動：填 `Program.cs`、更新上方的練習索引表。

<details>
<summary>手動步驟（不用腳本時）</summary>

```powershell
# stdio server（console 專案）
dotnet new console -o src/ChNN.主題
dotnet sln add src/ChNN.主題
dotnet add src/ChNN.主題 package ModelContextProtocol --prerelease
dotnet add src/ChNN.主題 package Microsoft.Extensions.Hosting

# HTTP server（ASP.NET Core Web 專案）
dotnet new web -o src/ChNN.主題
dotnet sln add src/ChNN.主題
dotnet add src/ChNN.主題 package ModelContextProtocol.AspNetCore --prerelease
```

1. 移除 csproj 中已由 `Directory.Build.props` 提供的屬性（`TargetFramework` / `Nullable` / `ImplicitUsings`）。
2. 在 CPM 下，`dotnet add package` 會自動把版本寫進 `Directory.Packages.props`。
3. 新增該練習的 `README.md`，並更新上方的練習索引表。

</details>

## 資料夾結構

```
mcp-csharp-labs/
├── .editorconfig                    # 統一 code style
├── .gitattributes                   # 換行/文字檔正規化
├── .gitignore                       # dotnet 範本
├── global.json                      # 釘住 SDK 9.0.x
├── Directory.Build.props            # 統一 TFM / Nullable / ImplicitUsings（各專案繼承）
├── Directory.Packages.props         # 集中式套件版本管理（CPM）
├── McpCsharpLabs.sln
├── README.md
│
├── src/                             # 各練習專案（ChNN.主題）
│   └── Ch01.GettingStarted.Stdio/   # 最小 MCP server + Echo tool（stdio）
│       ├── Ch01.GettingStarted.Stdio.csproj
│       ├── Program.cs
│       └── README.md                # 本練習目標 / 做法 / 測試 / 踩雷筆記
│
├── tests/                           # 測試專案（有目標可測時才建）
│   └── README.md
│
├── docs/
│   ├── notes/                       # 跨練習的課程筆記（markdown）
│   │   └── 00-index.md
│   └── diagrams/                    # 架構圖 / 流程圖
│
└── scripts/
    └── new-lab.ps1                  # 一鍵新增練習專案
```

> 灰底規則：跨練習的觀念放 `docs/notes/`，單一練習的細節放各專案自己的 `README.md`。
