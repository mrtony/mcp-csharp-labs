# Project Description

這是一個 Pluralsight 課程,  使用 C# MCP SDK 建立 MCP Server.

## Tech Stack

asp.net core 9, Aspire, Docker, ModelContextProtocol, ModelContextProtocol.AspNetCore

## 專案慣例

- **命名**：`ChNN.主題`，對照課程章節，讓 Solution Explorer 依課程順序排列。
  同一模組若有多個 transport／版本，用後綴細分（如 `Ch01.GettingStarted.Stdio`、`Ch01.GettingStarted.Http`）。
- **共用設定**：TFM / Nullable / ImplicitUsings 統一在 [`Directory.Build.props`](Directory.Build.props)，各專案自動繼承。
- **套件版本**：集中式管理（CPM），版本寫在 [`Directory.Packages.props`](Directory.Packages.props)，csproj 的 `PackageReference` 不帶版本。
- 每個練習專案內都有自己的 `README.md`，記錄目標、做法、測試方式與踩雷筆記。