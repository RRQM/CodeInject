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

        // 查找所有标记了 RegionInjectAttribute 的类
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // 组合类声明和附加文件
        var combined = classDeclarations.Collect().Combine(additionalFiles.Collect());

        context.RegisterSourceOutput(combined,
            static (spc, source) => Execute(source.Left, source.Right, spc));
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

                if (attributeData.ConstructorArguments.Length < 2)
                {
                    continue;
                }

                var filePath = attributeData.ConstructorArguments[0].Value?.ToString();
                var regionName = attributeData.ConstructorArguments[1].Value?.ToString();

                if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(regionName))
                {
                    continue;
                }

                var placeholders = new List<string>();

                // 从构造函数参数获取占位符
                // 检查是否使用了带 params string[] 的构造函数
                if (attributeData.ConstructorArguments.Length >= 3)
                {
                    // 第3个参数是 params string[] placeholders
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

                // 如果构造函数中没有占位符，再从 Placeholders 属性获取
                if (placeholders.Count == 0)
                {
                    foreach (var namedArgument in attributeData.NamedArguments)
                    {
                        if (namedArgument.Key == "Placeholders")
                        {
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
                        }
                    }
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

    private static void Execute(ImmutableArray<ClassToGenerate> classes, ImmutableArray<AdditionalText> additionalFiles, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var classToGenerate in classes)
        {
            var source = GenerateClass(classToGenerate, additionalFiles, context);
            if (!string.IsNullOrEmpty(source))
            {
                context.AddSource($"{classToGenerate.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    private static string GenerateClass(ClassToGenerate classInfo, ImmutableArray<AdditionalText> additionalFiles, SourceProductionContext context)
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
            //Debugger.Launch();
            var injectedCode = ExtractAndProcessRegion(attribute, additionalFiles, context);
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

    private static string ExtractAndProcessRegion(RegionInjectData attribute, ImmutableArray<AdditionalText> additionalFiles, SourceProductionContext context)
    {
        // 首先尝试从 AdditionalFiles 中查找文件
        string fileContent = null;
        AdditionalText targetFile = null;

        // 规范化路径以进行比较
        var normalizedTargetPath = attribute.FilePath.Replace('\\', '/');

        foreach (var file in additionalFiles)
        {
            var filePath = file.Path.Replace('\\', '/');

            // 检查完整路径匹配
            if (filePath.EndsWith(normalizedTargetPath, StringComparison.OrdinalIgnoreCase))
            {
                targetFile = file;
                break;
            }

            // 检查文件名匹配
            var fileName = Path.GetFileName(normalizedTargetPath);
            if (Path.GetFileName(filePath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                targetFile = file;
                break;
            }
        }

        if (targetFile != null)
        {
            try
            {
                var sourceText = targetFile.GetText(context.CancellationToken);
                fileContent = sourceText?.ToString();
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "CRG003",
                        "File read error",
                        $"Error reading additional file '{targetFile.Path}': {ex.Message}",
                        "CodeRegionGenerator",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    attribute.Location));
                return string.Empty;
            }
        }

        if (fileContent == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CRG001",
                    "Template file not found",
                    $"Could not find template file: {attribute.FilePath}. Make sure the file is included as AdditionalFiles in the project that uses the source generator.",
                    "CodeRegionGenerator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                attribute.Location));
            return string.Empty;
        }

        var regionContent = ExtractRegion(fileContent, attribute.RegionName);
        if (string.IsNullOrEmpty(regionContent))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "CRG002",
                    "Region not found",
                    $"Could not find region '{attribute.RegionName}' in file '{attribute.FilePath}'",
                    "CodeRegionGenerator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                attribute.Location));
            return string.Empty;
        }

        return ProcessPlaceholders(regionContent, attribute.Placeholders);
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

    private const string DefaultAttributeSource = @"
using System;

namespace CodeInject
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RegionInjectAttribute : Attribute
    {
        public string FilePath { get; }
        public string RegionName { get; }
        public string[] Placeholders { get; set; } = new string[0];
        
        public RegionInjectAttribute(string filePath, string regionName)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            RegionName = regionName ?? throw new ArgumentNullException(nameof(regionName));
        }
        
        public RegionInjectAttribute(string filePath, string regionName, params string[] placeholders)
            : this(filePath, regionName)
        {
            Placeholders = placeholders ?? new string[0];
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