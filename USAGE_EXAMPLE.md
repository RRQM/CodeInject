# CodeInject ʹ��ʾ��

���ʾ��չʾ���������Ŀ��ʹ�� CodeInject Դ��������

## ��Ŀ����

1. ��װ CodeInject NuGet ��
2. ����ģ���ļ�������Ϊ AdditionalFiles
3. ������ʹ�� RegionInject ����

## ʾ����Ŀ�ṹ

```
MyProject/
������ Templates/
��   ������ ApiTemplate.cs
��   ������ EntityTemplate.cs
������ Models/
��   ������ User.cs
������ Services/
��   ������ UserService.cs
������ MyProject.csproj
```

## 1. ��Ŀ���� (MyProject.csproj)

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

## 2. ģ���ļ� (Templates/ApiTemplate.cs)

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

## 3. ������ (Services/UserService.cs)

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
        
        // ���ɵķ������������Զ�ע��
    }
}
```

## 4. ���ɵĴ���

Դ���������Զ��������´��룺

```csharp
// UserService.g.cs (�Զ�����)
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

## ע������

1. Ŀ�����������Ϊ `partial`
2. ģ���ļ���������Ϊ `AdditionalFiles`
3. ռλ���Գɶ���ʽ�ṩ��`"placeholder", "value", "placeholder2", "value2"`
4. ֧�� `{placeholder}` �� `placeholder` ���ָ�ʽ
5. ���Զ�ͬһ����Ӧ�ö�� `RegionInject` ����