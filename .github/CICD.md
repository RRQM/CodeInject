# CodeInject GitHub Actions 配置

[![Build and Publish](https://github.com/RRQM/CodeInject/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/RRQM/CodeInject/actions/workflows/nuget-publish.yml)
[![Release](https://github.com/RRQM/CodeInject/actions/workflows/release.yml/badge.svg)](https://github.com/RRQM/CodeInject/actions/workflows/release.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CodeInject)](https://www.nuget.org/packages/CodeInject/)

## 自动化 CI/CD 流程

本项目使用 GitHub Actions 实现自动化的持续集成和部署流程。

### 🔄 持续集成工作流 (nuget-publish.yml)

**触发条件:**
- 推送到 `main` 或 `master` 分支
- 对 `main` 或 `master` 分支创建 Pull Request
- 修改 `src/` 目录下的文件

**执行步骤:**
1. ✅ 检出代码
2. 🔧 设置 .NET 环境
3. 📦 还原依赖项
4. 🏗️ 构建源代码生成器
5. 🏗️ 构建主项目
6. 🧪 运行测试（如果存在）
7. 📦 打包 NuGet 包
8. 🚀 发布到 NuGet.org 和 GitHub Packages（仅在推送到主分支时）

### 🚀 发布工作流 (release.yml)

**触发条件:**
- 推送版本标签（格式：`v*`，如 `v1.0.0`）

**执行步骤:**
1. ✅ 检出代码
2. 🔧 设置 .NET 环境
3. 🏷️ 从标签获取版本号
4. 📝 更新 Directory.Build.props 中的版本
5. 📦 还原依赖项
6. 🏗️ 构建项目
7. 📦 打包 NuGet 包
8. 📋 创建 GitHub Release
9. 🚀 发布到 NuGet.org 和 GitHub Packages

## 🛠️ 设置指南

### 1. 配置 NuGet API Key

需要在 GitHub 仓库中设置以下 Secret：

| Secret 名称     | 描述               | 获取方式                                                   |
| --------------- | ------------------ | ---------------------------------------------------------- |
| `NUGET_API_KEY` | NuGet.org API 密钥 | 在 [NuGet.org](https://www.nuget.org/account/apikeys) 创建 |

**详细步骤:**
1. 登录 [NuGet.org](https://www.nuget.org/)
2. 导航到：用户名 → API Keys
3. 点击 "Create" 创建新的 API Key
4. 选择适当的权限范围
5. 复制生成的 API Key
6. 在 GitHub 仓库中添加：Settings → Secrets and variables → Actions → New repository secret

### 2. 验证项目配置

确保以下文件配置正确：

**src/Directory.Build.props:**
```xml
<PropertyGroup>
    <Version>0.0.5</Version>
    <PackageProjectUrl>https://github.com/RRQM/CodeInject</PackageProjectUrl>
    <RepositoryUrl>https://github.com/RRQM/CodeInject</RepositoryUrl>
    <!-- 其他包信息 -->
</PropertyGroup>
```

**src/CodeInject/CodeInject.csproj:**
```xml
<PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <!-- 其他项目配置 -->
</PropertyGroup>
```

## 📋 使用指南

### 开发版本发布

```bash
# 提交代码
git add .
git commit -m "feat: 添加新功能"
git push origin main
```

推送到主分支后，GitHub Actions 会自动：
- 构建项目
- 运行测试
- 发布 NuGet 包

### 正式版本发布

```bash
# 创建并推送版本标签
git tag v1.2.3
git push origin v1.2.3
```

推送标签后，GitHub Actions 会自动：
- 更新版本号
- 构建项目
- 创建 GitHub Release
- 发布 NuGet 包

### 版本号管理

- 开发版本：使用 `Directory.Build.props` 中的版本号
- 发布版本：使用 Git 标签中的版本号（自动更新）

## 🔍 监控和调试

### 查看构建状态

1. 前往 GitHub 仓库的 "Actions" 标签页
2. 查看工作流运行历史
3. 点击具体的运行记录查看详细日志

### 常见问题排查

| 问题           | 可能原因               | 解决方案                       |
| -------------- | ---------------------- | ------------------------------ |
| NuGet 发布失败 | API Key 无效或权限不足 | 重新生成 API Key 并更新 Secret |
| 构建失败       | 代码编译错误           | 检查代码语法和依赖项           |
| 版本冲突       | 相同版本已存在         | 更新版本号或使用新标签         |
| 权限错误       | GitHub Token 权限不足  | 检查仓库权限设置               |

### 日志查看

在 GitHub Actions 运行页面可以查看：
- 构建输出
- 测试结果
- 发布状态
- 错误信息

## 🔧 自定义配置

### 修改触发条件

编辑 `.github/workflows/nuget-publish.yml`：

```yaml
on:
  push:
    branches: [ main, develop ]  # 添加其他分支
    paths:
      - 'src/**'
      - 'tests/**'  # 添加其他路径
```

### 添加额外步骤

可以在工作流中添加：
- 代码质量检查
- 安全扫描
- 性能测试
- 文档生成

### 环境变量配置

在工作流文件中可配置：

```yaml
env:
  DOTNET_VERSION: '8.0.x'
  BUILD_CONFIGURATION: 'Release'
  PACKAGE_OUTPUT_PATH: './packages'
```

## 📈 最佳实践

1. **版本管理**: 使用语义化版本号（SemVer）
2. **分支策略**: main 分支保持稳定，使用 feature 分支开发
3. **测试覆盖**: 确保有足够的单元测试
4. **文档更新**: 及时更新 CHANGELOG 和 README
5. **安全考虑**: 定期更新依赖项和 API Key
