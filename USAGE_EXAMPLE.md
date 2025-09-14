# CodeInject 使用示例

这个示例展示了如何在项目中使用 CodeInject 源生成器。

## 项目设置

1. 安装 CodeInject NuGet 包
2. 创建模板文件并配置为 AdditionalFiles
3. 在类上使用 RegionInject 特性

## 示例项目结构

```
MyProject/
├── Templates/
│   ├── ApiTemplate.cs
│   └── EntityTemplate.cs
├── Models/
│   └── User.cs
├── Services/
│   └── UserService.cs
└── MyProject.csproj
```

## 1. 项目配置 (MyProject.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="CodeInject" Version="1.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <AdditionalFiles Include="Templates/**/*.cs" />
  </ItemGroup>
</Project>
```

## 2. 模板文件 (Templates/ApiTemplate.cs)

```csharp
#region CrudMethods
public async Task<IEnumerable<{EntityType}>> GetAll{EntityName}sAsync()
{
    return await _repository.GetAllAsync<{EntityType}>();
}

public async Task<{EntityType}> Get{EntityName}ByIdAsync(int id)
{
    return await _repository.GetByIdAsync<{EntityType}>(id);
}

public async Task<{EntityType}> Create{EntityName}Async({EntityType} entity)
{
    return await _repository.CreateAsync(entity);
}

public async Task<{EntityType}> Update{EntityName}Async({EntityType} entity)
{
    return await _repository.UpdateAsync(entity);
}

public async Task<bool> Delete{EntityName}Async(int id)
{
    return await _repository.DeleteAsync<{EntityType}>(id);
}
#endregion

#region ValidationMethods
public bool Validate{EntityName}({EntityType} entity)
{
    if (entity == null)
        return false;
    
    // {EntityName} specific validation logic
    return true;
}
#endregion
```

## 3. 服务类 (Services/UserService.cs)

```csharp
using CodeRegionSourceGenerator;

namespace MyProject.Services
{
    [RegionInject("Templates/ApiTemplate.cs", "CrudMethods", 
        "EntityType", "User", 
        "EntityName", "User")]
    [RegionInject("Templates/ApiTemplate.cs", "ValidationMethods",
        "EntityType", "User",
        "EntityName", "User")]
    public partial class UserService
    {
        private readonly IRepository _repository;
        
        public UserService(IRepository repository)
        {
            _repository = repository;
        }
        
        // 生成的方法将在这里自动注入
    }
}
```

## 4. 生成的代码

源生成器会自动生成以下代码：

```csharp
// UserService.g.cs (自动生成)
using System;

namespace MyProject.Services
{
    partial class UserService
    {
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _repository.GetAllAsync<User>();
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _repository.GetByIdAsync<User>(id);
        }

        public async Task<User> CreateUserAsync(User entity)
        {
            return await _repository.CreateAsync(entity);
        }

        public async Task<User> UpdateUserAsync(User entity)
        {
            return await _repository.UpdateAsync(entity);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _repository.DeleteAsync<User>(id);
        }

        public bool ValidateUser(User entity)
        {
            if (entity == null)
                return false;
            
            // User specific validation logic
            return true;
        }
    }
}
```

## 注意事项

1. 目标类必须声明为 `partial`
2. 模板文件必须配置为 `AdditionalFiles`
3. 占位符以成对形式提供：`"placeholder", "value", "placeholder2", "value2"`
4. 支持 `{placeholder}` 和 `placeholder` 两种格式
5. 可以对同一个类应用多个 `RegionInject` 特性