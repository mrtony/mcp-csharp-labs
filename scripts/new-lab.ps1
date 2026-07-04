#!/usr/bin/env pwsh
<#
.SYNOPSIS
    快速建立一個新的 MCP 練習專案並加入 solution。

.DESCRIPTION
    自動完成新增練習的樣板步驟：
      1. 依 transport 建立 console(stdio) 或 web(http) 專案
      2. 加入 solution
      3. 加入對應的 MCP NuGet 套件（版本由 CPM 自動寫入 Directory.Packages.props）
      4. 移除已由 Directory.Build.props 提供的重複屬性
      5. 產生一份練習 README 樣板

    之後仍需手動：填 Program.cs、更新根 README 的練習索引表。

.PARAMETER Name
    專案名稱，建議用 ChNN.主題 格式，例如 Ch02.ApiServer。

.PARAMETER Transport
    stdio（預設，console 專案）或 http（ASP.NET Core Web 專案）。

.EXAMPLE
    ./scripts/new-lab.ps1 -Name Ch02.ApiServer -Transport http
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Name,

    [ValidateSet('stdio', 'http')]
    [string]$Transport = 'stdio'
)

$ErrorActionPreference = 'Stop'

# 定位 repo 根目錄（本腳本位於 <root>/scripts）
$root = Split-Path -Parent $PSScriptRoot
$projDir = Join-Path $root "src/$Name"

if (Test-Path $projDir) {
    throw "專案已存在：$projDir"
}

Write-Host "==> 建立 $Transport 專案：$Name" -ForegroundColor Cyan

if ($Transport -eq 'stdio') {
    dotnet new console -o $projDir
} else {
    dotnet new web -o $projDir
}

dotnet sln (Join-Path $root 'McpCsharpLabs.sln') add $projDir

Write-Host "==> 加入 MCP 套件" -ForegroundColor Cyan
if ($Transport -eq 'stdio') {
    dotnet add $projDir package ModelContextProtocol --prerelease
    dotnet add $projDir package Microsoft.Extensions.Hosting
} else {
    dotnet add $projDir package ModelContextProtocol.AspNetCore --prerelease
}

# 移除已由 Directory.Build.props 提供的重複屬性
Write-Host "==> 精簡 csproj（移除繼承自 Directory.Build.props 的屬性）" -ForegroundColor Cyan
$csproj = Join-Path $projDir "$Name.csproj"
$content = Get-Content $csproj -Raw
foreach ($prop in 'TargetFramework', 'Nullable', 'ImplicitUsings') {
    $content = $content -replace "\s*<$prop>.*?</$prop>", ''
}
Set-Content $csproj $content -NoNewline

# 產生練習 README 樣板
$readme = Join-Path $projDir 'README.md'
@"
# $Name

對應課程 Module ？？ 的《（填 demo 名稱）》。

## 目標

（這個練習要學什麼）

## 做法重點

（關鍵 API、程式片段、註冊步驟）

## 測試方式

``````powershell
dotnet run --project .
npx @modelcontextprotocol/inspector dotnet run --project src/$Name
``````

## 踩雷筆記

- （待補）
"@ | Set-Content $readme

Write-Host ""
Write-Host "完成！接下來：" -ForegroundColor Green
Write-Host "  1. 填寫 $projDir/Program.cs"
Write-Host "  2. 更新根 README.md 的練習索引表"
Write-Host "  3. dotnet build 驗證"
