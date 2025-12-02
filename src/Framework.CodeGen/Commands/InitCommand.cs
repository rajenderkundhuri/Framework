using System.CommandLine;
using System.Reflection;

namespace Framework.CodeGen.Commands;

/// <summary>
/// CLI command for initializing custom templates
/// </summary>
public static class InitCommand
{
    public static Command Create()
    {
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for templates",
            getDefaultValue: () => ".fwgen/templates");

        var command = new Command("init", "Initialize custom templates in your project")
        {
            outputOption
        };

        command.SetHandler(async (context) =>
        {
            var output = context.ParseResult.GetValueForOption(outputOption)!;

            Console.WriteLine("Framework Code Generator - Template Initialization");
            Console.WriteLine("=================================================");
            Console.WriteLine();
            Console.WriteLine($"Extracting templates to: {output}");
            Console.WriteLine();

            try
            {
                await ExtractTemplatesAsync(output);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nTemplates extracted successfully!");
                Console.WriteLine($"You can now customize the templates in: {output}");
                Console.WriteLine("Use --templates flag with generate command to use custom templates.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFailed to extract templates: {ex.Message}");
                Console.ResetColor();
                context.ExitCode = 1;
            }
        });

        return command;
    }

    private static async Task ExtractTemplatesAsync(string outputPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(".scriban"));

        foreach (var resourceName in resourceNames)
        {
            // Parse template path from resource name
            var templatePath = resourceName;

            // Determine subdirectory (Api or Admin)
            string subDir;
            string fileName;

            if (resourceName.Contains("Api"))
            {
                subDir = "Api";
                fileName = resourceName.Split('.').Last(s => s != "scriban") + ".scriban";
            }
            else if (resourceName.Contains("Admin"))
            {
                subDir = "Admin";
                fileName = resourceName.Split('.').Last(s => s != "scriban") + ".scriban";
            }
            else
            {
                continue;
            }

            var outputFilePath = Path.Combine(outputPath, subDir, fileName);
            var directory = Path.GetDirectoryName(outputFilePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            await File.WriteAllTextAsync(outputFilePath, content);
            Console.WriteLine($"  [OK] {Path.Combine(subDir, fileName)}");
        }
    }
}
