namespace EpicMorg.Atlassian.Downloader.Models;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

internal class BellsAndWhistles
{
    private static readonly string assemblyEnvironment = string.Format("[{1}, {0}]", RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(), RuntimeInformation.FrameworkDescription);

    private static readonly Assembly entryAssembly = Assembly.GetEntryAssembly()!;

    private static readonly string assemblyVersion = entryAssembly.GetName().Version!.ToString();

    private static readonly string fileVersion = entryAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;

    private static readonly string assemblyName = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>()!.Product;
    const string assemblyBuildType =
#if DEBUG
            "[Debug]"
#else
        
            "[Release]"
#endif
        ;

    private const ConsoleColor DEFAULT = ConsoleColor.Blue;

    public static void ShowVersionInfo(ILogger logger)
    {
        logger.LogInformation(
            "{assemblyName} {assemblyVersion} {assemblyEnvironment} {assemblyBuildType}",
            assemblyName,
            assemblyVersion,
            assemblyEnvironment,
            assemblyBuildType);
        Console.BackgroundColor = ConsoleColor.Black;
        WriteColorLine("%╔═╦═══════════════════════════════════════════════════════════════════════════════════════╦═╗");
        WriteColorLine("%╠═╝                  .''.                                                                 %╚═%╣");
        WriteColorLine("%║                 .:cc;.                                                                    %║");
        WriteColorLine("%║                .;cccc;.                                                                   %║");
        WriteColorLine("%║               .;cccccc;.             !╔══════════════════════════════════════════════╗     %║");
        WriteColorLine($"%║               .:ccccccc;.            !║    {assemblyName}                      !║     %║");
        WriteColorLine("%║               'ccccccccc;.           !╠══════════════════════════════════════════════╣     %║");
        WriteColorLine("%║               ,cccccccccc;.          !║    &Code:    @kasthack, @stam                  !║     %║");
        WriteColorLine("%║               ,ccccccccccc;.         !║    &GFX:     @stam                             !║     %║");
        WriteColorLine("%║          .... .:ccccccccccc;.        !╠══════════════════════════════════════════════╣     %║");
        WriteColorLine($"%║         .',,'..;cccccccccccc;.       !║    &Version: {fileVersion}                          !║     %║");
        WriteColorLine("%║        .,,,,,'.';cccccccccccc;.      !║    &GitHub:  $EpicMorg/atlassian-downloader    !║     %║");
        WriteColorLine("%║       .,;;;;;,'.':cccccccccccc;.     !╚══════════════════════════════════════════════╝     %║");
        WriteColorLine("%║      .;:;;;;;;,...:cccccccccccc;.                                                         %║");
        WriteColorLine("%║     .;:::::;;;;'. .;:ccccccccccc;.                                                        %║");
        WriteColorLine("%║    .:cc::::::::,.  ..:ccccccccccc;.                                                       %║");
        WriteColorLine("%║   .:cccccc:::::'     .:ccccccccccc;.                                                      %║");
        WriteColorLine("%║  .;:::::::::::,.      .;:::::::::::,.                                                     %║");
        WriteColorLine("%╠═╗ ............          ............                                                    %╔═╣");
        WriteColorLine("%╚═╩═══════════════════════════════════════════════════════════════════════════════════════╩═╝");
        Console.ResetColor();
    }
    public static void SetConsoleTitle() => Console.Title = $@"{assemblyName} {assemblyVersion} {assemblyEnvironment} - {assemblyBuildType}";

    private static void WriteColorLine(string text, params object[] args)
    {
        Dictionary<char, ConsoleColor> colors = new()
        {
            { '!', ConsoleColor.Red },
            { '@', ConsoleColor.Green },
            { '#', ConsoleColor.Blue },
            { '$', ConsoleColor.Magenta },
            { '&', ConsoleColor.Yellow },
            { '%', ConsoleColor.Cyan }
        };
        // TODO: word wrap, backslash escapes
        text = string.Format(text, args);
        var chunk = "";
        var paren = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (colors.ContainsKey(c) && StringNext(text, i) != ' ')
            {
                Console.Write(chunk);
                chunk = "";
                if (StringNext(text, i) == '(')
                {
                    i++; // skip past the paren
                    paren = true;
                }

                Console.ForegroundColor = colors[c];
            }
            else if (paren && c == ')')
            {
                paren = false;
                Console.ForegroundColor = DEFAULT;
            }
            else if (Console.ForegroundColor != DEFAULT)
            {
                Console.Write(c);
                if (c == ' ' && !paren)
                {
                    Console.ForegroundColor = DEFAULT;
                }
            }
            else
            {
                chunk += c;
            }
        }

        Console.WriteLine(chunk);
        Console.ForegroundColor = DEFAULT;
    }

    private static char StringNext(string text, int index) => index < text.Length ? text[index + 1] : '\0';

}

