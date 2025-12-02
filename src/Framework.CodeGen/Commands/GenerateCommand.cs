using System.CommandLine;
using Framework.CodeGen.Generation;

namespace Framework.CodeGen.Commands;

/// <summary>
/// CLI command for generating code
/// </summary>
public static class GenerateCommand
{
    public static Command Create()
    {
        var entityOption = new Option<string>(
            aliases: new[] { "--entity", "-e" },
            description: "Entity name (for reflection) or path to entity source file (for source parsing)")
        {
            IsRequired = true
        };

        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory (default: current directory)",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        var namespaceOption = new Option<string?>(
            aliases: new[] { "--namespace", "-n" },
            description: "Custom namespace for generated code");

        var assemblyOption = new Option<string?>(
            aliases: new[] { "--assembly", "-a" },
            description: "Path to the assembly containing the entity (required for reflection mode)");

        var sourceOption = new Option<bool>(
            aliases: new[] { "--source", "-s" },
            description: "Use Roslyn source parsing instead of reflection",
            getDefaultValue: () => false);

        var apiOnlyOption = new Option<bool>(
            aliases: new[] { "--api-only" },
            description: "Generate only API code (Commands, Queries, Handlers, Controller)",
            getDefaultValue: () => false);

        var adminOnlyOption = new Option<bool>(
            aliases: new[] { "--admin-only" },
            description: "Generate only Admin UI code (Pages, Services)",
            getDefaultValue: () => false);

        var overwriteOption = new Option<bool>(
            aliases: new[] { "--overwrite", "-f" },
            description: "Overwrite existing files",
            getDefaultValue: () => false);

        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run" },
            description: "Show what would be generated without creating files",
            getDefaultValue: () => false);

        var templatesOption = new Option<string?>(
            aliases: new[] { "--templates", "-t" },
            description: "Path to custom templates directory");

        var command = new Command("generate", "Generate CRUD code for an entity")
        {
            entityOption,
            outputOption,
            namespaceOption,
            assemblyOption,
            sourceOption,
            apiOnlyOption,
            adminOnlyOption,
            overwriteOption,
            dryRunOption,
            templatesOption
        };

        command.SetHandler(async (context) =>
        {
            var entity = context.ParseResult.GetValueForOption(entityOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption)!;
            var @namespace = context.ParseResult.GetValueForOption(namespaceOption);
            var assembly = context.ParseResult.GetValueForOption(assemblyOption);
            var useSource = context.ParseResult.GetValueForOption(sourceOption);
            var apiOnly = context.ParseResult.GetValueForOption(apiOnlyOption);
            var adminOnly = context.ParseResult.GetValueForOption(adminOnlyOption);
            var overwrite = context.ParseResult.GetValueForOption(overwriteOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var templates = context.ParseResult.GetValueForOption(templatesOption);

            var options = new GenerationOptions
            {
                EntityNameOrPath = entity,
                OutputDirectory = output,
                Namespace = @namespace,
                AssemblyPath = assembly,
                UseReflection = !useSource,
                GenerateApi = !adminOnly,
                GenerateAdmin = !apiOnly,
                Overwrite = overwrite,
                DryRun = dryRun,
                CustomTemplatesPath = templates
            };

            // Validate options
            if (!useSource && string.IsNullOrEmpty(assembly))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Assembly path is required for reflection mode. Use --assembly or --source flag.");
                Console.ResetColor();
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine("Framework Code Generator");
            Console.WriteLine("========================");
            Console.WriteLine();

            var generator = new CodeGenerator(options);
            var result = await generator.GenerateAsync();

            Console.WriteLine();
            Console.WriteLine("Generation Summary");
            Console.WriteLine("------------------");
            Console.WriteLine($"Files generated: {result.FilesGenerated.Count}");
            Console.WriteLine($"Files skipped:   {result.FilesSkipped.Count}");
            Console.WriteLine($"Errors:          {result.Errors.Count}");

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nCode generation completed successfully!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nCode generation failed: {result.Error}");
                Console.ResetColor();
                context.ExitCode = 1;
            }
        });

        return command;
    }
}
