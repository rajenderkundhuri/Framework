using System.Reflection;
using Framework.CodeGen.Analysis;
using Framework.CodeGen.Analysis.Models;

namespace Framework.CodeGen.Generation;

/// <summary>
/// Main code generator that orchestrates the generation process
/// </summary>
public class CodeGenerator
{
    private readonly GenerationOptions _options;
    private readonly TemplateRenderer _renderer;

    public CodeGenerator(GenerationOptions options)
    {
        _options = options;
        _renderer = new TemplateRenderer(options.CustomTemplatesPath);
    }

    /// <summary>
    /// Generates code for the specified entity
    /// </summary>
    public async Task<GenerationResult> GenerateAsync()
    {
        var result = new GenerationResult();

        try
        {
            // Analyze entity
            var metadata = AnalyzeEntity();
            var context = GenerationContext.FromMetadata(metadata, _options);

            Console.WriteLine($"Generating code for entity: {metadata.Name}");
            Console.WriteLine($"  - Plural: {metadata.PluralName}");
            Console.WriteLine($"  - Auditable: {metadata.IsAuditable}");
            Console.WriteLine($"  - Soft Delete: {metadata.IsSoftDelete}");
            Console.WriteLine($"  - Foreign Keys: {metadata.ForeignKeys.Count}");
            Console.WriteLine();

            // Generate API code
            if (_options.GenerateApi)
            {
                await GenerateApiCodeAsync(context, result);
            }

            // Generate Admin UI code
            if (_options.GenerateAdmin)
            {
                await GenerateAdminCodeAsync(context, result);
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    private EntityMetadata AnalyzeEntity()
    {
        if (_options.UseReflection)
        {
            if (string.IsNullOrEmpty(_options.AssemblyPath))
            {
                throw new InvalidOperationException("Assembly path is required for reflection mode");
            }

            var analyzer = new ReflectionEntityAnalyzer(_options.AssemblyPath);
            return analyzer.Analyze(_options.EntityNameOrPath);
        }
        else
        {
            var analyzer = new RoslynEntityAnalyzer();
            return analyzer.Analyze(_options.EntityNameOrPath);
        }
    }

    private async Task GenerateApiCodeAsync(GenerationContext context, GenerationResult result)
    {
        var basePath = Path.Combine(_options.OutputDirectory, "Framework.Application", context.EntityPlural);

        // Commands
        await GenerateFileAsync("Api/CreateCommand.scriban",
            Path.Combine(basePath, "Commands", $"Create{context.EntityName}Command.cs"),
            context, result);

        await GenerateFileAsync("Api/UpdateCommand.scriban",
            Path.Combine(basePath, "Commands", $"Update{context.EntityName}Command.cs"),
            context, result);

        await GenerateFileAsync("Api/DeleteCommand.scriban",
            Path.Combine(basePath, "Commands", $"Delete{context.EntityName}Command.cs"),
            context, result);

        // Command Handlers
        await GenerateFileAsync("Api/CreateCommandHandler.scriban",
            Path.Combine(basePath, "Commands", $"Create{context.EntityName}CommandHandler.cs"),
            context, result);

        await GenerateFileAsync("Api/UpdateCommandHandler.scriban",
            Path.Combine(basePath, "Commands", $"Update{context.EntityName}CommandHandler.cs"),
            context, result);

        await GenerateFileAsync("Api/DeleteCommandHandler.scriban",
            Path.Combine(basePath, "Commands", $"Delete{context.EntityName}CommandHandler.cs"),
            context, result);

        // Validators
        await GenerateFileAsync("Api/CreateCommandValidator.scriban",
            Path.Combine(basePath, "Commands", $"Create{context.EntityName}CommandValidator.cs"),
            context, result);

        await GenerateFileAsync("Api/UpdateCommandValidator.scriban",
            Path.Combine(basePath, "Commands", $"Update{context.EntityName}CommandValidator.cs"),
            context, result);

        // Queries
        await GenerateFileAsync("Api/GetByIdQuery.scriban",
            Path.Combine(basePath, "Queries", $"Get{context.EntityName}ByIdQuery.cs"),
            context, result);

        await GenerateFileAsync("Api/GetListQuery.scriban",
            Path.Combine(basePath, "Queries", $"Get{context.EntityPlural}Query.cs"),
            context, result);

        // Query Handlers
        await GenerateFileAsync("Api/GetByIdQueryHandler.scriban",
            Path.Combine(basePath, "Queries", $"Get{context.EntityName}ByIdQueryHandler.cs"),
            context, result);

        await GenerateFileAsync("Api/GetListQueryHandler.scriban",
            Path.Combine(basePath, "Queries", $"Get{context.EntityPlural}QueryHandler.cs"),
            context, result);

        // DTOs
        await GenerateFileAsync("Api/ListRequest.scriban",
            Path.Combine(basePath, $"{context.EntityName}ListRequest.cs"),
            context, result);

        await GenerateFileAsync("Api/ListResponse.scriban",
            Path.Combine(basePath, $"{context.EntityName}ListResponse.cs"),
            context, result);

        await GenerateFileAsync("Api/DetailResponse.scriban",
            Path.Combine(basePath, $"{context.EntityName}DetailResponse.cs"),
            context, result);

        // Controller
        await GenerateFileAsync("Api/Controller.scriban",
            Path.Combine(_options.OutputDirectory, "Framework.Api", "Controllers", $"{context.EntityPlural}Controller.cs"),
            context, result);

        // Permissions (output to console for manual addition)
        var permissionsContent = _renderer.Render("Api/Permissions.scriban", context);
        Console.WriteLine("\nAdd these permissions to Framework.Application/Identity/Permissions.cs:");
        Console.WriteLine(permissionsContent);
    }

    private async Task GenerateAdminCodeAsync(GenerationContext context, GenerationResult result)
    {
        var adminPath = Path.Combine(_options.OutputDirectory, "Framework.Admin");

        // API Service
        await GenerateFileAsync("Admin/ApiService.scriban",
            Path.Combine(adminPath, "Services", $"{context.EntityName}ApiService.cs"),
            context, result);

        // List Page
        await GenerateFileAsync("Admin/ListPage.scriban",
            Path.Combine(adminPath, "Pages", $"{context.EntityPlural}.razor"),
            context, result);

        // Dialog
        await GenerateFileAsync("Admin/Dialog.scriban",
            Path.Combine(adminPath, "Pages", $"{context.EntityName}Dialog.razor"),
            context, result);

        // Output service registration
        Console.WriteLine($"\nAdd this service registration to Framework.Admin/Program.cs:");
        Console.WriteLine($"builder.Services.AddScoped<I{context.EntityName}ApiService, {context.EntityName}ApiService>();");

        // Output navigation
        Console.WriteLine($"\nAdd this navigation item to MainLayout.razor:");
        Console.WriteLine($"<MudNavLink Href=\"/{context.RoutePath}\" Icon=\"@Icons.Material.Filled.List\">{context.EntityPlural}</MudNavLink>");
    }

    private async Task GenerateFileAsync(string templateName, string outputPath, GenerationContext context, GenerationResult result)
    {
        try
        {
            var content = _renderer.Render(templateName, context);

            if (_options.DryRun)
            {
                Console.WriteLine($"[DRY RUN] Would generate: {outputPath}");
                result.FilesGenerated.Add(outputPath);
                return;
            }

            // Check if file exists
            if (File.Exists(outputPath) && !_options.Overwrite)
            {
                Console.WriteLine($"[SKIP] File exists: {outputPath}");
                result.FilesSkipped.Add(outputPath);
                return;
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(outputPath, content);
            Console.WriteLine($"[OK] Generated: {outputPath}");
            result.FilesGenerated.Add(outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {outputPath}: {ex.Message}");
            result.Errors.Add($"{outputPath}: {ex.Message}");
        }
    }
}

/// <summary>
/// Result of code generation
/// </summary>
public class GenerationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> FilesGenerated { get; } = new();
    public List<string> FilesSkipped { get; } = new();
    public List<string> Errors { get; } = new();
}
