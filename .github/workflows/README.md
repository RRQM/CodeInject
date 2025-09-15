# GitHub Actions CI/CD 设置说明

本项目包含两个 GitHub Actions 工作流，用于自动构建和发布 NuGet 包。

## 工作流说明

### 1. nuget-publish.yml
- **触发条件**: 推送到 main/master 分支，或创建 Pull Request
- **功能**: 
  - 构建项目和源代码生成器
  - 运行测试（如果有）
  - 打包 NuGet 包
  - 发布到 NuGet.org 和 GitHub Packages（仅在推送到主分支时）

### 2. release.yml
- **触发条件**: 推送版本标签（如 v1.0.0）
- **功能**:
  - 根据标签自动更新版本号
  - 构建和打包
  - 创建 GitHub Release
  - 发布到 NuGet.org 和 GitHub Packages

## 设置步骤

### 1. 配置 NuGet API Key

1. 前往 [NuGet.org](https://www.nuget.org/) 并登录
2. 点击用户名 → "API Keys"
3. 创建新的 API Key，选择适当的权限
4. 在 GitHub 仓库中添加 Secret：
   - 前往 GitHub 仓库 → Settings → Secrets and variables → Actions
   - 点击 "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: 您的 NuGet API Key

### 2. 配置包信息（可选）

在 `src/Directory.Build.props` 中更新包信息：

```xml
<PropertyGroup>
    <Version>0.0.5</Version>
    <PackageProjectUrl>https://github.com/RRQM/CodeInject</PackageProjectUrl>
    <RepositoryUrl>https://github.com/RRQM/CodeInject</RepositoryUrl>
    <!-- 其他包信息 -->
</PropertyGroup>
```

## 使用方法

### 开发版本发布
1. 提交代码到 main 或 master 分支
2. GitHub Actions 会自动构建并发布包

### 正式版本发布
1. 创建并推送版本标签：
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
2. GitHub Actions 会：
   - 自动更新版本号
   - 创建 GitHub Release
   - 发布到 NuGet.org 和 GitHub Packages

## 注意事项

1. 确保项目结构与工作流中的路径匹配
2. NuGet API Key 需要有推送权限
3. 版本标签必须以 'v' 开头（如 v1.0.0）
4. 工作流会跳过重复的包版本（--skip-duplicate）

## 故障排除

- 检查 GitHub Actions 日志以了解构建或发布失败的原因
- 确认 NuGet API Key 仍然有效
- 验证包版本号格式是否正确
