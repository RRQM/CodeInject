// ------------------------------------------------------------------------------
// 此代码版权（除特别声明或在XREF结尾的命名空间的代码）归作者本人若汝棋茗所有
// 源代码使用协议遵循本仓库的开源协议及附加协议，若本仓库没有设置，则按MIT开源协议授权
// CSDN博客：https://blog.csdn.net/qq_40374647
// 哔哩哔哩视频：https://space.bilibili.com/94253567
// Gitee源代码仓库：https://gitee.com/RRQM_Home
// Github源代码仓库：https://github.com/RRQM
// API首页：https://touchsocket.net/
// 交流QQ群：234762506
// 感谢您的下载和使用
// ------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeInject;

[Generator]
public class CodeInjectIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 添加 attribute 源码到消费项目
        context.RegisterPostInitializationOutput((ctx) =>
        {
            var attributeSource = GetAttributeSourceFromEmbeddedResource();
            if (!string.IsNullOrEmpty(attributeSource))
            {
                ctx.AddSource("RegionInjectAttribute.g.cs", SourceText.From(attributeSource, Encoding.UTF8));
            }
        });

        // 获取所有的 AdditionalFiles
        var additionalFiles = context.AdditionalTextsProvider;

        // 获取编译信息（包含源文件）
        var compilation = context.CompilationProvider;

        // 查找所有标记了 RegionInjectAttribute 的类
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // 组合类声明、附加文件和编译信息
        var combined = classDeclarations.Collect()
            .Combine(additionalFiles.Collect())
            .Combine(compilation);

        context.RegisterSourceOutput(combined,
            static (spc, source) => Execute(source.Left.Left, source.Left.Right, source.Right, spc));
    }

    /// <summary>
    /// 获取程序集中的嵌入资源内容
    /// </summary>
    /// <param name="resourceName">资源名称，如果为null则返回所有资源名称</param>
    /// <returns>资源内容或资源名称列表</returns>
    private static string GetEmbeddedResourceContent(string resourceName = null)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        // 如果没有指定资源名称，返回所有资源名称
        if (string.IsNullOrEmpty(resourceName))
        {
            return string.Join(Environment.NewLine, resourceNames);
        }

        // 查找匹配的资源
        var targetResource = resourceNames.FirstOrDefault(name =>
            name.Equals(resourceName, StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith($".{resourceName}", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(targetResource))
        {
            return null;
        }

        try
        {
            using (var stream = assembly.GetManifestResourceStream(targetResource))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        catch
        {
            return null;
        }
    }

    private static string GetAttributeSourceFromEmbeddedResource()
    {
        // 尝试从嵌入资源中获取 RegionInjectAttribute 的源代码
        var resourceContent = GetEmbeddedResourceContent("RegionInjectAttribute.cs");

        // 如果找不到嵌入资源，使用默认的源代码
        if (string.IsNullOrEmpty(resourceContent))
        {
            return DefaultAttributeSource;
        }

        return resourceContent;
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0;

    private static ClassToGenerate GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        try
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (classSymbol is null)
            {
                return null;
            }
            
            var attributes = new List<RegionInjectData>();

            foreach (var attributeData in classSymbol.GetAttributes())
            {
                if (attributeData.AttributeClass?.Name != "RegionInjectAttribute")
                {
                    continue;
                }

                string filePath = null;
                string regionName = null;
                var placeholders = new List<string>();

                // 首先从命名参数（属性）获取值
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    switch (namedArgument.Key)
                    {
                        case "FilePath":
                            filePath = namedArgument.Value.Value?.ToString();
                            break;
                        case "RegionName":
                            regionName = namedArgument.Value.Value?.ToString();
                            break;
                        case "Placeholders":
                            var value = namedArgument.Value;
                            if (value.Kind == TypedConstantKind.Array && !value.IsNull)
                            {
                                var arrayValues = value.Values;
                                foreach (var item in arrayValues)
                                {
                                    var stringValue = item.Value?.ToString();
                                    if (!string.IsNullOrEmpty(stringValue))
                                    {
                                        placeholders.Add(stringValue);
                                    }
                                }
                            }
                            break;
                    }
                }

                // 如果没有从属性获取到值，尝试从构造函数参数获取（向后兼容）
                if (string.IsNullOrEmpty(regionName) && attributeData.ConstructorArguments.Length >= 1)
                {
                    var firstArg = attributeData.ConstructorArguments[0].Value?.ToString();
                    
                    if (attributeData.ConstructorArguments.Length == 1)
                    {
                        // 单参数构造函数: RegionInjectAttribute(regionName)
                        regionName = firstArg;
                        filePath = null; // 将搜索所有文件
                    }
                    else if (attributeData.ConstructorArguments.Length >= 2)
                    {
                        // 双参数或多参数构造函数: RegionInjectAttribute(filePath, regionName, ...)
                        filePath = firstArg;
                        regionName = attributeData.ConstructorArguments[1].Value?.ToString();
                    }

                    // 从构造函数参数获取占位符（如果属性中没有设置）
                    if (placeholders.Count == 0)
                    {
                        if (attributeData.ConstructorArguments.Length >= 3)
                        {
                            // 第3个参数可能是 params string[] placeholders
                            var placeholdersArg = attributeData.ConstructorArguments[2];
                            if (placeholdersArg.Kind == TypedConstantKind.Array && !placeholdersArg.IsNull)
                            {
                                var arrayValues = placeholdersArg.Values;
                                foreach (var item in arrayValues)
                                {
                                    var stringValue = item.Value?.ToString();
                                    if (!string.IsNullOrEmpty(stringValue))
                                    {
                                        placeholders.Add(stringValue);
                                    }
                                }
                            }
                        }
                        else if (attributeData.ConstructorArguments.Length == 2 && string.IsNullOrEmpty(filePath))
                        {
                            // 可能是 RegionInjectAttribute(regionName, params string[] placeholders)
                            var secondArg = attributeData.ConstructorArguments[1];
                            if (secondArg.Kind == TypedConstantKind.Array && !secondArg.IsNull)
                            {
                                var arrayValues = secondArg.Values;
                                foreach (var item in arrayValues)
                                {
                                    var stringValue = item.Value?.ToString();
                                    if (!string.IsNullOrEmpty(stringValue))
                                    {
                                        placeholders.Add(stringValue);
                                    }
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(regionName))
                {
                    continue;
                }

                // 获取属性的位置信息
                var attributeLocation = attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;

                attributes.Add(new RegionInjectData(filePath, regionName, placeholders.ToArray(), attributeLocation));
            }

            if (attributes.Count == 0)
            {
                return null;
            }

            return new ClassToGenerate(
                classSymbol.Name,
                GetNamespace(classDeclarationSyntax),
                attributes.ToArray());
        }
        catch
        {
            // 如果有异常，返回 null，避免源生成器崩溃
            return null;
        }
    }

    private static string GetNamespace(ClassDeclarationSyntax classDeclaration)
    {
        var namespaceDeclaration = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        if (namespaceDeclaration != null)
        {
            return namespaceDeclaration.Name.ToString();
        }

        var fileScopedNamespace = classDeclaration.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScopedNamespace != null)
        {
            return fileScopedNamespace.Name.ToString();
        }

        return string.Empty;
    }

    private static void Execute(ImmutableArray<ClassToGenerate> classes, ImmutableArray<AdditionalText> additionalFiles, Compilation compilation, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var classToGenerate in classes)
        {
            var source = GenerateClass(classToGenerate, additionalFiles, compilation, context);
            if (!string.IsNullOrEmpty(source))
            {
                context.AddSource($"{classToGenerate.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    private static string GenerateClass(ClassToGenerate classInfo, ImmutableArray<AdditionalText> additionalFiles, Compilation compilation, SourceProductionContext context)
    {
        var sb = new StringBuilder();

        // 添加必要的 using 语句
        sb.AppendLine("using System;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(classInfo.Namespace))
        {
            sb.AppendLine($"namespace {classInfo.Namespace}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"    partial class {classInfo.Name}");
        sb.AppendLine("    {");

        foreach (var attribute in classInfo.Attributes)
        {
            var injectedCode = ExtractAndProcessRegion(attribute, additionalFiles, compilation, context);
            if (!string.IsNullOrEmpty(injectedCode))
            {
                // 为注入的代码添加适当的缩进（类内部缩进 8 个空格）
                var formattedCode = FormatInjectedCode(injectedCode, "        ");
                sb.Append(formattedCode);
                // 确保注入的代码块后有一个换行符
                sb.AppendLine();
            }
        }

        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(classInfo.Namespace))
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string FormatInjectedCode(string code, string indentation)
    {
        if (string.IsNullOrEmpty(code))
        {
            return string.Empty;
        }

        var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var formattedLines = new List<string>();

        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                // 为非空行添加指定的缩进
                formattedLines.Add(indentation + line);
            }
        }

        // 移除末尾的空行，避免多余的换行符
        while (formattedLines.Count > 0 && string.IsNullOrEmpty(formattedLines[formattedLines.Count - 1]))
        {
            formattedLines.RemoveAt(formattedLines.Count - 1);
        }

        return string.Join("\r\n", formattedLines);
    }

    private static string ExtractRegion(string fileContent, string regionName)
    {
        var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var regionStart = -1;
        var regionEnd = -1;
        var nestedLevel = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("#region"))
            {
                if (regionStart == -1 && line.Contains(regionName))
                {
                    // 找到目标region的开始
                    regionStart = i + 1;
                    nestedLevel = 1;
                }
                else if (regionStart != -1)
                {
                    // 已经在目标region内，遇到嵌套region
                    nestedLevel++;
                }
            }
            else if (line.StartsWith("#endregion") && regionStart != -1)
            {
                nestedLevel--;
                if (nestedLevel == 0)
                {
                    // 找到匹配的endregion
                    regionEnd = i;
                    break;
                }
            }
        }

        if (regionStart == -1 || regionEnd == -1)
            return string.Empty;

        var regionLines = new string[regionEnd - regionStart];
        Array.Copy(lines, regionStart, regionLines, 0, regionEnd - regionStart);

        // 移除最小的公共缩进
        var nonEmptyLines = regionLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        if (nonEmptyLines.Length == 0)
            return string.Join("\r\n", regionLines);

        var minIndent = nonEmptyLines
            .Select(line => line.Length - line.TrimStart().Length)
            .Min();

        var processedLines = regionLines.Select(line =>
            string.IsNullOrWhiteSpace(line) ? string.Empty :
            line.Length > minIndent ? line.Substring(minIndent) : line.TrimStart());

        return string.Join("\r\n", processedLines);
    }

    private static string ProcessPlaceholders(string content, string[] placeholders)
    {
        if (placeholders.Length == 0)
        {
            return content;
        }

        var result = content;

        // 处理成对的占位符 (key, value, key, value, ...)
        for (var i = 0; i < placeholders.Length - 1; i += 2)
        {
            if (i + 1 < placeholders.Length)
            {
                var placeholder = placeholders[i];
                var replacement = placeholders[i + 1];

                // 替换 {placeholder} 格式
                result = result.Replace($"{{{placeholder}}}", replacement);

                // 替换 placeholder 格式（不带大括号）
                result = result.Replace(placeholder, replacement);
            }
        }

        return result;
    }

    private static string ExtractAndProcessRegion(RegionInjectData attribute, ImmutableArray<AdditionalText> additionalFiles, Compilation compilation, SourceProductionContext context)
    {
        string fileContent = null;
        string foundFilePath = null;

        // 如果指定了文件路径，优先查找指定文件
        if (!string.IsNullOrEmpty(attribute.FilePath))
        {
            var result = FindFileContent(attribute.FilePath, additionalFiles, compilation, context);
            fileContent = result.content;
            foundFilePath = result.filePath;
        }
        else
        {
            // 如果没有指定文件路径，搜索所有文件中包含指定区域的文件
            var result = SearchAllFilesForRegion(attribute.RegionName, additionalFiles, compilation, context);
            fileContent = result.content;
            foundFilePath = result.filePath;
        }

        if (fileContent == null)
        {
            var errorMessage = string.IsNullOrEmpty(attribute.FilePath) 
                ? $"Could not find region '{attribute.RegionName}' in any available files. Make sure files containing the region are included as AdditionalFiles or source files in the project."
                : $"Could not find template file: {attribute.FilePath}. Make sure the file is included as AdditionalFiles in the project that uses the source generator.";

            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CRG001",
                    "Template file or region not found",
                    errorMessage,
                    "CodeRegionGenerator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                attribute.Location));
            return string.Empty;
        }

        var regionContent = ExtractRegion(fileContent, attribute.RegionName);
        if (string.IsNullOrEmpty(regionContent))
        {
            var targetInfo = string.IsNullOrEmpty(attribute.FilePath) ? $"found file '{foundFilePath}'" : $"file '{attribute.FilePath}'";
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CRG002",
                    "Region not found",
                    $"Could not find region '{attribute.RegionName}' in {targetInfo}",
                    "CodeRegionGenerator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                attribute.Location));
            return string.Empty;
        }

        return ProcessPlaceholders(regionContent, attribute.Placeholders);
    }

    private static (string content, string filePath) FindFileContent(string targetFilePath, ImmutableArray<AdditionalText> additionalFiles, Compilation compilation, SourceProductionContext context)
    {
        // 首先在 AdditionalFiles 中查找
        var normalizedTargetPath = targetFilePath.Replace('\\', '/');

        foreach (var file in additionalFiles)
        {
            var filePath = file.Path.Replace('\\', '/');

            // 检查完整路径匹配
            if (filePath.EndsWith(normalizedTargetPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var sourceText = file.GetText(context.CancellationToken);
                    return (sourceText?.ToString(), file.Path);
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "CRG003",
                            "File read error",
                            $"Error reading additional file '{file.Path}': {ex.Message}",
                            "CodeRegionGenerator",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None));
                    continue;
                }
            }

            // 检查文件名匹配
            var fileName = Path.GetFileName(normalizedTargetPath);
            if (Path.GetFileName(filePath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var sourceText = file.GetText(context.CancellationToken);
                    return (sourceText?.ToString(), file.Path);
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "CRG003",
                            "File read error",
                            $"Error reading additional file '{file.Path}': {ex.Message}",
                            "CodeRegionGenerator",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None));
                    continue;
                }
            }
        }

        // 然后在编译的源文件中查找
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var filePath = syntaxTree.FilePath.Replace('\\', '/');
            
            if (filePath.EndsWith(normalizedTargetPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var sourceText = syntaxTree.GetText(context.CancellationToken);
                    return (sourceText?.ToString(), syntaxTree.FilePath);
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "CRG003",
                            "File read error",
                            $"Error reading source file '{syntaxTree.FilePath}': {ex.Message}",
                            "CodeRegionGenerator",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None));
                    continue;
                }
            }

            var fileName = Path.GetFileName(normalizedTargetPath);
            if (Path.GetFileName(filePath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var sourceText = syntaxTree.GetText(context.CancellationToken);
                    return (sourceText?.ToString(), syntaxTree.FilePath);
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "CRG003",
                            "File read error",
                            $"Error reading source file '{syntaxTree.FilePath}': {ex.Message}",
                            "CodeRegionGenerator",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None));
                    continue;
                }
            }
        }

        return (null, null);
    }

    private static (string content, string filePath) SearchAllFilesForRegion(string regionName, ImmutableArray<AdditionalText> additionalFiles, Compilation compilation, SourceProductionContext context)
    {
        // 首先搜索 AdditionalFiles
        foreach (var file in additionalFiles)
        {
            try
            {
                var sourceText = file.GetText(context.CancellationToken);
                var content = sourceText?.ToString();
                if (!string.IsNullOrEmpty(content) && ContainsRegion(content, regionName))
                {
                    return (content, file.Path);
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "CRG003",
                        "File read error",
                        $"Error reading additional file '{file.Path}': {ex.Message}",
                        "CodeRegionGenerator",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    Location.None));
                continue;
            }
        }

        // 然后搜索编译的源文件
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            try
            {
                var sourceText = syntaxTree.GetText(context.CancellationToken);
                var content = sourceText?.ToString();
                if (!string.IsNullOrEmpty(content) && ContainsRegion(content, regionName))
                {
                    return (content, syntaxTree.FilePath);
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "CRG003",
                        "File read error",
                        $"Error reading source file '{syntaxTree.FilePath}': {ex.Message}",
                        "CodeRegionGenerator",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    Location.None));
                continue;
            }
        }

        return (null, null);
    }

    private static bool ContainsRegion(string fileContent, string regionName)
    {
        var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("#region") && trimmedLine.Contains(regionName))
            {
                return true;
            }
        }
        
        return false;
    }

    private const string DefaultAttributeSource = @"
using System;

namespace CodeInject
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RegionInjectAttribute : Attribute
    {
        public string FilePath { get; set; }
        public string RegionName { get; set; }
        public string[] Placeholders { get; set; } = new string[0];
        
        public RegionInjectAttribute()
        {
        }
    }
}";

    internal class ClassToGenerate
    {
        public string Name { get; }
        public string Namespace { get; }
        public RegionInjectData[] Attributes { get; }

        public ClassToGenerate(string name, string @namespace, RegionInjectData[] attributes)
        {
            this.Name = name;
            this.Namespace = @namespace;
            this.Attributes = attributes;
        }
    }

    internal class RegionInjectData
    {
        public string FilePath { get; }
        public string RegionName { get; }
        public string[] Placeholders { get; }
        public Location Location { get; }

        public RegionInjectData(string filePath, string regionName, string[] placeholders, Location location)
        {
            this.FilePath = filePath;
            this.RegionName = regionName;
            this.Placeholders = placeholders;
            this.Location = location;
        }
    }
}