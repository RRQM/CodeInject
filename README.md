# CodeInject - Code Region Source Generator

[![Build and Publish](https://github.com/RRQM/CodeInject/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/RRQM/CodeInject/actions/workflows/nuget-publish.yml)
[![Release](https://github.com/RRQM/CodeInject/actions/workflows/release.yml/badge.svg)](https://github.com/RRQM/CodeInject/actions/workflows/release.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CodeInject)](https://www.nuget.org/packages/CodeInject/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CodeInject)](https://www.nuget.org/packages/CodeInject/)

A powerful source generator that injects code regions from template files into partial classes at compile time.

## ✨ Features

- 📁 **Template-based code injection** - Extract code regions from template files
- 🔄 **Placeholder replacement** - Replace placeholders with custom values
- 🎯 **Multi-attribute support** - Apply multiple injections to a single class
- 🏗️ **Nested region support** - Handle nested `#region` blocks correctly
- ⚡ **Incremental generation** - Efficient compilation with minimal rebuilds

## 📦 Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package CodeInject
```

Or via Package Manager Console:

```powershell
Install-Package CodeInject
```

## 🚀 Quick Start

### 1. Create a template file

Create a template file and add it as an `AdditionalFiles` item in your project:

```xml
<ItemGroup>
  <AdditionalFiles Include="Templates/ApiTemplate.cs" />
</ItemGroup>
```

Template file content:
```csharp
#region ApiMethods
public async Task<{ReturnType}> Get{EntityName}Async(int id)
{
    // Implementation here
    return await _repository.GetByIdAsync<{ReturnType}>(id);
}

public async Task<{ReturnType}> Create{EntityName}Async({ReturnType} entity)
{
    // Implementation here
    return await _repository.CreateAsync(entity);
}
#endregion
```

### 2. Apply the attribute

```csharp
using CodeInject;

[RegionInject(FilePath = "Templates/ApiTemplate.cs", RegionName = "ApiMethods", 
    Placeholders = new[] { "ReturnType", "User", "EntityName", "User" })]
public partial class UserService
{
    private readonly IRepository _repository;
    
    public UserService(IRepository repository)
    {
        _repository = repository;
    }
    
    // Generated methods will be injected here automatically
}
```

### 3. Generated code

The source generator will automatically create:

```csharp
partial class UserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // Implementation here
        return await _repository.GetByIdAsync<User>(id);
    }

    public async Task<User> CreateUserAsync(User entity)
    {
        // Implementation here
        return await _repository.CreateAsync(entity);
    }
}
```

## 🔧 Advanced Usage

### Multiple Injections

```csharp
[RegionInject(FilePath = "Templates/CrudTemplate.cs", RegionName = "CreateMethods", 
    Placeholders = new[] { "Entity", "Product" })]
[RegionInject(FilePath = "Templates/CrudTemplate.cs", RegionName = "UpdateMethods", 
    Placeholders = new[] { "Entity", "Product" })]
[RegionInject(FilePath = "Templates/ValidationTemplate.cs", RegionName = "Validators", 
    Placeholders = new[] { "Type", "Product" })]
public partial class ProductService
{
    // Multiple code regions will be injected
}
```

### Search All Files for Region

If you don't specify the `FilePath`, the generator will search all available files for the specified region:

```csharp
[RegionInject(RegionName = "CommonMethods")]
public partial class BaseService
{
    // Generator will search all files for "CommonMethods" region
}
```

### Using Property Initializers

```csharp
[RegionInject(FilePath = "Templates/ApiTemplate.cs", RegionName = "ApiMethods", 
    Placeholders = new[] { "ReturnType", "Order", "EntityName", "Order" })]
public partial class OrderService
{
    // Generated code with Order-specific implementations
}
```

## ⚙️ Configuration

### Project Setup

Add template files to your project as `AdditionalFiles`:

```xml
<ItemGroup>
  <AdditionalFiles Include="Templates/**/*.cs" />
  <AdditionalFiles Include="CodeTemplates/**/*.txt" />
</ItemGroup>
```

### Template File Format

- Use `#region RegionName` and `#endregion` to define code blocks
- Support for nested regions
- Placeholders can be used in two formats:
  - `{PlaceholderName}` - with curly braces
  - `PlaceholderName` - without braces

## 📋 Use Cases

### 1. API Controller Templates

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

Usage:
```csharp
[RegionInject(FilePath = "Templates/ControllerTemplate.cs", RegionName = "CrudActions",
    Placeholders = new[] { "EntityType", "Product", "EntityName", "Product", "entityName", "product" })]
public partial class ProductController : ControllerBase
{
    // Generated CRUD actions will be injected here
}
```

### 2. Repository Pattern Templates

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

Usage:
```csharp
[RegionInject(FilePath = "Templates/RepositoryTemplate.cs", RegionName = "RepositoryMethods",
    Placeholders = new[] { "EntityType", "User", "EntityName", "User" })]
public partial class UserRepository
{
    // Generated repository methods will be injected here
}
```

## 🔍 Diagnostics

The source generator provides the following diagnostic information:

- **CRG001**: Template file not found
- **CRG002**: Region not found
- **CRG003**: File read error

## 💡 Best Practices

1. **Organize templates**: Keep template files in a dedicated `Templates` folder
2. **Naming conventions**: Use descriptive region names like `CrudMethods`, `ValidationRules`
3. **Placeholder naming**: Use consistent placeholder names like `EntityType`, `EntityName`
4. **Modularization**: Group related functionality into different regions
5. **Property-based syntax**: Use the new property-based initialization for better readability

## 📋 Requirements

- .NET Standard 2.0 or higher
- C# 7.3 or higher
- Visual Studio 2019 16.9+ or .NET 5.0+ SDK

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆚 Comparison with Other Solutions

| Feature                 | CodeInject | T4 Templates | Manual Coding |
| ----------------------- | ---------- | ------------ | ------------- |
| Compile-time generation | ✅          | ❌            | ❌             |
| Incremental compilation | ✅          | ❌            | ✅             |
| IDE support             | ✅          | ⚠️            | ✅             |
| Learning curve          | Low        | High         | Low           |
| Flexibility             | High       | High         | Low           |

## 📞 Support

If you encounter any issues:

1. Check the [FAQ](https://github.com/yourusername/CodeInject/wiki/FAQ)
2. Search [existing issues](https://github.com/yourusername/CodeInject/issues)
3. Create a [new issue](https://github.com/yourusername/CodeInject/issues/new)

---

⭐ If this project helps you, please give it a star!