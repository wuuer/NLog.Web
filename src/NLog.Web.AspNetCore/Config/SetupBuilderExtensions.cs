﻿using System;
#if NETCOREAPP3_0_OR_GREATER
using System.IO;
using System.Linq;
#endif
using Microsoft.Extensions.Configuration;
using NLog.Config;
using NLog.Extensions.Logging;

namespace NLog.Web
{
    /// <summary>
    /// Extension methods to setup LogFactory options
    /// </summary>
    public static class SetupBuilderExtensions
    {
#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Loads NLog LoggingConfiguration from appsettings.json from the NLog-section
        /// </summary>
        /// <param name="setupBuilder"></param>
        /// <param name="basePath">Override SetBasePath for <see cref="ConfigurationBuilder"/> with AddJsonFile. Default resolves from environment variables, else fallback to current directory.</param>
        /// <param name="environment">Override Environment for appsettings.{environment}.json with AddJsonFile. Default resolves from environment variables, else fallback to "Production"</param>
        /// <param name="nlogConfigSection">Override configuration-section-name to resolve NLog-configuration</param>
        /// <param name="optional">Override optional with AddJsonFile</param>
        /// <param name="reloadOnChange">Override reloadOnChange with AddJsonFile. Required for "autoReload":true to work.</param>
        public static ISetupBuilder LoadConfigurationFromAppSettings(this ISetupBuilder setupBuilder, string? basePath = null, string? environment = null, string nlogConfigSection = "NLog", bool optional = true, bool reloadOnChange = false)
        {
            environment = environment ?? GetAspNetCoreEnvironment("ASPNETCORE_ENVIRONMENT") ?? GetAspNetCoreEnvironment("DOTNET_ENVIRONMENT") ?? "Production";
            basePath = basePath ?? GetAspNetCoreEnvironment("ASPNETCORE_CONTENTROOT") ?? GetAspNetCoreEnvironment("DOTNET_CONTENTROOT") ?? ResolveCurrentAppDirectory();

            var builder = new ConfigurationBuilder()
                // Host Configuration
                .SetBasePath(basePath)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddEnvironmentVariables(prefix: "DOTNET_")
                // App Configuration
                .AddJsonFile("appsettings.json", optional, reloadOnChange)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: reloadOnChange)
                .AddEnvironmentVariables();

            var config = builder.Build();
            if (!string.IsNullOrEmpty(nlogConfigSection) && config.GetSection(nlogConfigSection)?.GetChildren().Any() == true)
            {
                // "NLog"-section in appsettings.json has first priority
                return setupBuilder.SetupExtensions(e => e.RegisterNLogWeb().RegisterConfigSettings(config)).LoadConfigurationFromSection(config, nlogConfigSection);
            }

            setupBuilder.SetupExtensions(e => e.RegisterNLogWeb().RegisterConfigSettings(config));

            var nlogConfigFile = ResolveEnvironmentNLogConfigFile(basePath, environment);
            if (!string.IsNullOrEmpty(nlogConfigFile))
            {
                return setupBuilder.LoadConfigurationFromFile(nlogConfigFile, optional: true);
            }

            return setupBuilder.LoadConfigurationFromFile();    // No effect, if config already loaded
        }

        private static string ResolveEnvironmentNLogConfigFile(string basePath, string environmentName)
        {
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    var nlogConfigEnvFilePath = Path.Combine(basePath, $"nlog.{environmentName}.config");
                    if (File.Exists(nlogConfigEnvFilePath))
                        return Path.GetFullPath(nlogConfigEnvFilePath);
                    nlogConfigEnvFilePath = Path.Combine(basePath, $"NLog.{environmentName}.config");
                    if (File.Exists(nlogConfigEnvFilePath))
                        return Path.GetFullPath(nlogConfigEnvFilePath);
                }

                var nlogConfigFilePath = Path.Combine(basePath, "nlog.config");
                if (File.Exists(nlogConfigFilePath))
                    return Path.GetFullPath(nlogConfigFilePath);
                nlogConfigFilePath = Path.Combine(basePath, "NLog.config");
                if (File.Exists(nlogConfigFilePath))
                    return Path.GetFullPath(nlogConfigFilePath);
            }

            if (!string.IsNullOrWhiteSpace(environmentName))
                return $"nlog.{environmentName}.config";

            return string.Empty;
        }

        private static string ResolveCurrentAppDirectory()
        {
            var currentBasePath = Environment.CurrentDirectory;

            var normalizeCurDir = Path.GetFullPath(currentBasePath).TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var normalizeAppDir = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(normalizeCurDir) || !normalizeCurDir.StartsWith(normalizeAppDir, StringComparison.OrdinalIgnoreCase))
            {
                currentBasePath = AppContext.BaseDirectory; // Avoid using Windows-System32 as current directory
            }

            return currentBasePath;
        }

        private static string? GetAspNetCoreEnvironment(string variableName)
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable(variableName);
                if (string.IsNullOrWhiteSpace(environment))
                    return null;

                return environment.Trim();
            }
            catch (Exception ex)
            {
                NLog.Common.InternalLogger.Error(ex, "Failed to lookup environment variable {0}", variableName);
                return null;
            }
        }
#endif

        /// <summary>
        /// Convience method to register aspnet-layoutrenders in NLog.Web as one-liner before loading NLog.config
        /// </summary>
        /// <remarks>
        /// If not providing <paramref name="serviceProvider"/>, then output from aspnet-layoutrenderers will remain empty
        /// </remarks>
        public static ISetupBuilder RegisterNLogWeb(this ISetupBuilder setupBuilder, IConfiguration? configuration = null, IServiceProvider? serviceProvider = null)
        {
            setupBuilder.SetupExtensions(e => e.RegisterNLogWeb(serviceProvider));

            if (configuration == null && serviceProvider != null)
            {
                configuration = serviceProvider.GetService(typeof(IConfiguration)) as IConfiguration;
            }

            setupBuilder.SetupExtensions(e => e.RegisterConfigSettings(configuration));
            return setupBuilder;
        }
    }
}
