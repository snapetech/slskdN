// <copyright file="StartupConsoleOutput.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Serilog;
using Utility.CommandLine;
using Utility.EnvironmentVariables;

public static class StartupConsoleOutput
{
    public static void PrintCommandLineArguments(Type targetType, ILogger log)
    {
        static string GetLongName(string longName, Type type)
            => type == typeof(bool) ? longName : $"{longName} <{type.ToColloquialString().ToLowerInvariant()}>";

        var lines = new List<(string Item, string Description)>();

        void Map(Type type)
        {
            try
            {
                var defaults = Activator.CreateInstance(type);
                var props = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo property in props)
                {
                    var attribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(ArgumentAttribute));
                    var descriptionAttribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(DescriptionAttribute));
                    var isRequired = property.CustomAttributes.Any(a => a.AttributeType == typeof(RequiredAttribute));

                    if (attribute != default)
                    {
                        var shortName = attribute.ConstructorArguments[0].Value is char shortNameValue ? shortNameValue : default;
                        var longName = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                        var description = descriptionAttribute?.ConstructorArguments[0].Value;

                        var suffix = isRequired ? " (required)" : $" (default: {property.GetValue(defaults) ?? "<null>"})";
                        var item = $"{(shortName == default ? "  " : $"{shortName}|")}--{GetLongName(longName, property.PropertyType)}";
                        var desc = $"{description}{(property.PropertyType == typeof(bool) ? string.Empty : suffix)}";
                        lines.Add(new(item, desc));
                    }
                    else
                    {
                        Map(property.PropertyType);
                    }
                }
            }
            catch
            {
                return;
            }
        }

        Map(targetType);

        var longestItem = lines.Max(l => l.Item.Length);

        log.Information("\nusage: slskd [arguments]\n");
        log.Information("arguments:\n");

        foreach (var line in lines)
        {
            log.Information($"  {line.Item.PadRight(longestItem)}   {line.Description}");
        }
    }

    public static void PrintEnvironmentVariables(Type targetType, string prefix, ILogger log)
    {
        static string GetName(string name, Type type) => $"{name} <{type.ToColloquialString().ToLowerInvariant()}>";

        var lines = new List<(string Item, string Description)>();

        void Map(Type type)
        {
            try
            {
                var defaults = Activator.CreateInstance(type);
                var props = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo property in props)
                {
                    var attribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(EnvironmentVariableAttribute));
                    var descriptionAttribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(DescriptionAttribute));
                    var isRequired = property.CustomAttributes.Any(a => a.AttributeType == typeof(RequiredAttribute));

                    if (attribute != default)
                    {
                        var name = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                        var description = descriptionAttribute?.ConstructorArguments[0].Value;

                        var suffix = isRequired ? " (required)" : $" (default: {property.GetValue(defaults) ?? "<null>"})";
                        var item = $"{prefix}{GetName(name, property.PropertyType)}";
                        var desc = $"{description}{(type == typeof(bool) ? string.Empty : suffix)}";
                        lines.Add(new(item, desc));
                    }
                    else
                    {
                        Map(property.PropertyType);
                    }
                }
            }
            catch
            {
                return;
            }
        }

        Map(targetType);

        var longestItem = lines.Max(l => l.Item.Length);

        log.Information("\nenvironment variables (arguments and config file have precedence):\n");

        foreach (var line in lines)
        {
            log.Information($"  {line.Item.PadRight(longestItem)}   {line.Description}");
        }
    }

    public static void PrintLogo(string version, bool isDevelopment, bool isCanary)
    {
        try
        {
            var padding = 56 - version.Length;
            var paddingLeft = padding / 2;
            var paddingRight = paddingLeft + (padding % 2);

            var centeredVersion = new string(' ', paddingLeft) + version + new string(' ', paddingRight);

            var logos = new[]
            {
                $@"
                   ▄▄▄▄         ▄▄▄▄       ▄▄▄▄
           ▄▄▄▄▄▄▄ █  █ ▄▄▄▄▄▄▄ █  █▄▄▄ ▄▄▄█  █
           █__ --█ █  █ █__ --█ █    ◄█ █  -  █
           █▄▄▄▄▄█ █▄▄█ █▄▄▄▄▄█ █▄▄█▄▄█ █▄▄▄▄▄█",
                @$"
                    ▄▄▄▄     ▄▄▄▄     ▄▄▄▄
              ▄▄▄▄▄▄█  █▄▄▄▄▄█  █▄▄▄▄▄█  █
              █__ --█  █__ --█    ◄█  -  █
              █▄▄▄▄▄█▄▄█▄▄▄▄▄█▄▄█▄▄█▄▄▄▄▄█",
            };

            var logo = logos[new Random().Next(0, logos.Length)];

            var banner = @$"
{logo}
╒════════════════════════════════════════════════════════╕
│           GNU AFFERO GENERAL PUBLIC LICENSE            │
│                   https://slskd.org                    │
│                                                        │
│{centeredVersion}│";

            if (isDevelopment)
            {
                banner += "\n│■■■■■■■■■■■■■■■■■■■■► DEVELOPMENT ◄■■■■■■■■■■■■■■■■■■■■■│";
            }

            if (isCanary)
            {
                banner += "\n│■■■■■■■■■■■■■■■■■■■■■■■► CANARY ◄■■■■■■■■■■■■■■■■■■■■■■■│";
            }

            banner += "\n└────────────────────────────────────────────────────────┘";

            Console.WriteLine(banner);
        }
        catch
        {
            // noop. console may not be available in all cases.
        }
    }
}
