using System.CommandLine;
using Framework.CodeGen.Commands;

// Create root command
var rootCommand = new RootCommand("Framework Code Generator - Generate CRUD code for your entities")
{
    GenerateCommand.Create(),
    InitCommand.Create()
};

// Set default to generate command
rootCommand.SetHandler(() =>
{
    Console.WriteLine(@"
  ___                                     _      ___         _       ___
 | __| _ __ _ _ __  _____ __ _____ _ _| |__  / __|___  __| |___  / __|___ _ _
 | _| '_/ _` | '  \/ -_) V  V / _ \ '_| / / | (__/ _ \/ _` / -_) | (_ / -_) ' \
 |_||_| \__,_|_|_|_\___|\_/\_/\___/_| |_\_\  \___\___/\__,_\___|  \___\___|_||_|

Framework Code Generator v1.0.0
================================

Generate CRUD code for your Clean Architecture entities.

USAGE:
    fwgen generate -e <entity> -a <assembly> [options]
    fwgen init [options]

EXAMPLES:
    # Generate code using reflection (requires compiled DLL)
    fwgen generate -e Product -a ./bin/Debug/net9.0/Framework.Domain.dll -o ./src

    # Generate code using source parsing
    fwgen generate -e ./src/Framework.Domain/Entities/Product.cs --source -o ./src

    # Generate API only
    fwgen generate -e Product -a ./bin/Debug/net9.0/Framework.Domain.dll --api-only

    # Generate Admin UI only
    fwgen generate -e Product -a ./bin/Debug/net9.0/Framework.Domain.dll --admin-only

    # Initialize custom templates
    fwgen init -o ./templates

For more information, run: fwgen --help
");
});

return await rootCommand.InvokeAsync(args);
