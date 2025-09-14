# CodeInject - 代码区域源生成器

一个强大的源生成器，可在编译时将模板文件中的代码区域注入到部分类中。

## ✨ 特性

- 📁 **基于模板的代码注入** - 从模板文件中提取代码区域
- 🔄 **占位符替换** - 用自定义值替换占位符
- 🎯 **多特性支持** - 对单个类应用多个注入
- 🏗️ **嵌套区域支持** - 正确处理嵌套的 `#region` 块
- ⚡ **增量生成** - 高效编译，最小化重新构建

## 📦 安装

通过 NuGet 包管理器安装：

```bash
dotnet add package CodeInject
```

或通过包管理器控制台：

```powershell
Install-Package CodeInject
```

## 🚀 快速开始

### 1. 创建模板文件

创建一个模板文件并将其添加为项目中的 `AdditionalFiles` 项：

```xml
<ItemGroup>
  <AdditionalFiles Include="Templates/ApiTemplate.cs" />
</ItemGroup>
```

模板文件内容：
```csharp
#region ApiMethods
public async Task<{ReturnType}> Get{EntityName}Async(int id)
{
    // 实现代码
    return await _repository.GetByIdAsync<{ReturnType}>(id);
}

public async Task<{ReturnType}> Create{EntityName}Async({ReturnType} entity)
{
    // 实现代码
    return await _repository.CreateAsync(entity);
}
#endregion
```

### 2. 应用特性

```csharp
using CodeRegionSourceGenerator;

[RegionInject("Templates/ApiTemplate.cs", "ApiMethods", 
    "ReturnType", "User", 
    "EntityName", "User")]
public partial class UserService
{
    private readonly IRepository _repository;
    
    public UserService(IRepository repository)
    {
        _repository = repository;
    }
    
    // 生成的方法将自动注入到这里
}
```

### 3. 生成的代码

源生成器将自动创建：

```csharp
partial class UserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // 实现代码
        return await _repository.GetByIdAsync<User>(id);
    }

    public async Task<User> CreateUserAsync(User entity)
    {
        // 实现代码
        return await _repository.CreateAsync(entity);
    }
}
```

## 🔧 高级用法

### 多重注入

```csharp
[RegionInject("Templates/CrudTemplate.cs", "CreateMethods", "Entity", "Product")]
[RegionInject("Templates/CrudTemplate.cs", "UpdateMethods", "Entity", "Product")]
[RegionInject("Templates/ValidationTemplate.cs", "Validators", "Type", "Product")]
public partial class ProductService
{
    // 多个代码区域将被注入
}
```

### 使用 Placeholders 属性

```csharp
[RegionInject("Templates/ApiTemplate.cs", "ApiMethods", 
    Placeholders = new[] { "ReturnType", "Order", "EntityName", "Order" })]
public partial class OrderService
{
    // 使用 Order 特定实现的生成代码
}
```

## ⚙️ 配置

### 项目设置

将模板文件添加到项目中作为 `AdditionalFiles`：

```xml
<ItemGroup>
  <AdditionalFiles Include="Templates/**/*.cs" />
  <AdditionalFiles Include="CodeTemplates/**/*.txt" />
</ItemGroup>
```

### 模板文件格式

- 使用 `#region RegionName` 和 `#endregion` 定义代码块
- 支持嵌套区域
- 占位符可以使用两种格式：
  - `{PlaceholderName}` - 带花括号
  - `PlaceholderName` - 不带括号

## 📋 使用场景

### 1. API 控制器模板

```csharp
// Templates/ControllerTemplate.cs
#region CrudActions
[HttpGet]
public async Task<ActionResult<IEnumerable<{EntityType}>>> Get{EntityName}s()
{
    var items = await _{entityName}Service.GetAllAsync();
    return Ok(items);
}

[HttpGet("{id}")]
public async Task<ActionResult<{EntityType}>> Get{EntityName}(int id)
{
    var item = await _{entityName}Service.GetByIdAsync(id);
    return item == null ? NotFound() : Ok(item);
}

[HttpPost]
public async Task<ActionResult<{EntityType}>> Create{EntityName}({EntityType} {entityName})
{
    var created = await _{entityName}Service.CreateAsync({entityName});
    return CreatedAtAction(nameof(Get{EntityName}), new { id = created.Id }, created);
}
#endregion
```

### 2. 仓储模式模板

```csharp
// Templates/RepositoryTemplate.cs
#region RepositoryMethods
public async Task<IEnumerable<{EntityType}>> GetAll{EntityName}sAsync()
{
    return await _context.{EntityName}s.ToListAsync();
}

public async Task<{EntityType}> Get{EntityName}ByIdAsync(int id)
{
    return await _context.{EntityName}s.FindAsync(id);
}

public async Task<{EntityType}> Create{EntityName}Async({EntityType} entity)
{
    _context.{EntityName}s.Add(entity);
    await _context.SaveChangesAsync();
    return entity;
}
#endregion
```

## 🔍 诊断信息

源生成器提供以下诊断信息：

- **CRG001**: 模板文件未找到
- **CRG002**: 未找到指定区域
- **CRG003**: 文件读取错误

## 💡 最佳实践

1. **组织模板文件**: 将模板文件放在专门的 `Templates` 文件夹中
2. **命名约定**: 使用描述性的区域名称，如 `CrudMethods`、`ValidationRules`
3. **占位符命名**: 使用大写的占位符名称，如 `ENTITY_NAME`、`RETURN_TYPE`
4. **模块化**: 将相关功能分组到不同的区域中

## 📋 系统要求

- .NET Standard 2.0 或更高版本
- C# 7.3 或更高版本
- Visual Studio 2019 16.9+ 或 .NET 5.0+ SDK

## 🤝 贡献

欢迎贡献！请随时提交 Pull Request。

## 📄 许可证

本项目采用 MIT 许可证 - 详情请参阅 [LICENSE](LICENSE) 文件。

## 🆚 与其他方案对比

| 特性 | CodeInject | T4 模板 | 手动编写 |
|------|------------|---------|----------|
| 编译时生成 | ✅ | ❌ | ❌ |
| 增量编译 | ✅ | ❌ | ✅ |
| IDE 支持 | ✅ | ⚠️ | ✅ |
| 学习成本 | 低 | 高 | 低 |
| 灵活性 | 高 | 高 | 低 |

## 📞 支持

如果遇到问题，请：

1. 检查[常见问题](https://github.com/yourusername/CodeInject/wiki/FAQ)
2. 搜索[已有问题](https://github.com/yourusername/CodeInject/issues)
3. 创建[新问题](https://github.com/yourusername/CodeInject/issues/new)

---

⭐ 如果这个项目对你有帮助，请给它一个 Star！