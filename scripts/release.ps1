# CodeInject 发布脚本
# 使用方法: .\scripts\release.ps1 -Version "1.0.0"

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [string]$Message = "Release version $Version"
)

# 验证版本格式
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "版本格式无效。请使用语义化版本格式，如: 1.0.0"
    exit 1
}

$TagName = "v$Version"

Write-Host "🚀 准备发布 CodeInject $Version" -ForegroundColor Green

# 检查是否有未提交的更改
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Host "⚠️  检测到未提交的更改:" -ForegroundColor Yellow
    git status --short
    $continue = Read-Host "是否继续? (y/N)"
    if ($continue -ne 'y' -and $continue -ne 'Y') {
        Write-Host "❌ 发布已取消" -ForegroundColor Red
        exit 1
    }
}

# 检查标签是否已存在
$existingTag = git tag -l $TagName
if ($existingTag) {
    Write-Error "❌ 标签 $TagName 已存在"
    exit 1
}

# 更新版本号
Write-Host "📝 更新版本号到 $Version" -ForegroundColor Cyan
$buildPropsPath = "src\Directory.Build.props"
if (Test-Path $buildPropsPath) {
    $content = Get-Content $buildPropsPath -Raw
    $newContent = $content -replace '<Version>.*</Version>', "<Version>$Version</Version>"
    Set-Content $buildPropsPath $newContent -NoNewline
    Write-Host "✅ 已更新 $buildPropsPath" -ForegroundColor Green
} else {
    Write-Warning "⚠️  未找到 $buildPropsPath"
}

# 提交版本更改
Write-Host "📤 提交版本更改" -ForegroundColor Cyan
git add $buildPropsPath
git commit -m "chore: bump version to $Version"

# 创建并推送标签
Write-Host "🏷️  创建标签 $TagName" -ForegroundColor Cyan
git tag -a $TagName -m $Message

Write-Host "📤 推送到远程仓库" -ForegroundColor Cyan
git push origin main
git push origin $TagName

Write-Host "🎉 发布完成!" -ForegroundColor Green
Write-Host "📋 GitHub Actions 工作流将自动:" -ForegroundColor White
Write-Host "   • 构建项目" -ForegroundColor Gray
Write-Host "   • 创建 GitHub Release" -ForegroundColor Gray
Write-Host "   • 发布到 NuGet.org" -ForegroundColor Gray
Write-Host "   • 发布到 GitHub Packages" -ForegroundColor Gray
Write-Host ""
Write-Host "🔗 查看发布状态: https://github.com/RRQM/CodeInject/actions" -ForegroundColor Blue
