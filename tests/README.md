# tests

各練習的單元／整合測試放這裡，命名對應目標專案：`<練習名>.Tests`（例如 `Ch02.ApiServer.Tests`）。

## 為什麼目前是空的

採「最小起步」策略——測試專案要**參照一個實際的目標專案**才有意義，因此等某個練習有值得測試的邏輯（例如 tool 的商業邏輯）時再建立，不預先鋪空殼。

## 新增一個測試專案

```powershell
# 以 xUnit 為例
dotnet new xunit -o tests/Ch02.ApiServer.Tests
dotnet sln add tests/Ch02.ApiServer.Tests
dotnet add tests/Ch02.ApiServer.Tests reference src/Ch02.ApiServer
```

> 測試相關套件（`Microsoft.NET.Test.Sdk`、`xunit`、`xunit.runner.visualstudio`）的版本一樣走 CPM，會自動寫入 `Directory.Packages.props`。
