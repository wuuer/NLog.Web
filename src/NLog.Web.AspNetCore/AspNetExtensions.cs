﻿using System;
using System.Linq;
using System.Reflection;
using NLog.Config;
#if !NETCOREAPP3_0_OR_GREATER
using IHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web.Internal;

namespace NLog.Web
{
    /// <summary>
    /// Helpers for ASP.NET
    /// </summary>
    public static class AspNetExtensions
    {
        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="builder">The logging builder</param>
        public static ILoggingBuilder AddNLogWeb(this ILoggingBuilder builder)
        {
            return builder.AddNLogWeb(NLogAspNetCoreOptions.Default);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="builder">The logging builder</param>
        /// <param name="options">Options for registration of the NLog LoggingProvider and enabling features.</param>
        public static ILoggingBuilder AddNLogWeb(this ILoggingBuilder builder, NLogAspNetCoreOptions options)
        {
            Guard.ThrowIfNull(builder);
            AddNLogLoggerProvider(builder.Services, null, null, options, CreateNLogLoggerProvider);
            return builder;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and provide isolated LogFactory
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Options for registration of the NLog LoggingProvider and enabling features.</param>
        /// <param name="factoryBuilder">Initialize NLog LogFactory with NLog LoggingConfiguration.</param>
        public static ILoggingBuilder AddNLogWeb(this ILoggingBuilder builder, NLogAspNetCoreOptions options, Func<IServiceProvider, LogFactory> factoryBuilder)
        {
            Guard.ThrowIfNull(builder);
            Guard.ThrowIfNull(factoryBuilder);

            AddNLogLoggerProvider(builder.Services, null, null, options, (serviceProvider, config, env, opt) =>
            {
                config = SetupNLogConfigSettings(serviceProvider, config, LogManager.LogFactory);
                // Delay initialization of targets until we have loaded config-settings
                var logFactory = factoryBuilder(serviceProvider);
                var provider = CreateNLogLoggerProvider(serviceProvider, config, env, opt, logFactory);
                return provider;
            });
            return builder;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and explicit load NLog.config from path
        /// </summary>
        /// <remarks>Recommended to use AddNLogWeb() to avoid name-collission issue with NLog.Extension.Logging namespace</remarks>
        /// <param name="builder">The logging builder</param>
        /// <param name="configFileName">Path to NLog configuration file, e.g. nlog.config. </param>
        [Obsolete("Instead use AddNLogWeb() for explicit registration of NLog.Web.AspNetCore extensions. Marked obsolete with Nlog 6.0")]
        public static ILoggingBuilder AddNLog(this ILoggingBuilder builder, string configFileName)
        {
            return AddNLogWeb(builder, configFileName);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and explicit load NLog.config from path
        /// </summary>
        /// <param name="builder">The logging builder</param>
        /// <param name="configFileName">Path to NLog configuration file, e.g. nlog.config. </param>
        public static ILoggingBuilder AddNLogWeb(this ILoggingBuilder builder, string configFileName)
        {
            Guard.ThrowIfNull(builder);

            AddNLogLoggerProvider(builder.Services, null, null, NLogAspNetCoreOptions.Default, (serviceProvider, config, env, options) =>
            {
                var provider = CreateNLogLoggerProvider(serviceProvider, config, env, options);
                // Delay initialization of targets until we have loaded config-settings
                provider.LogFactory.Setup().LoadConfigurationFromFile(configFileName);
                return provider;
            });
            return builder;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and explicit load NLog LoggingConfiguration
        /// </summary>
        /// <remarks>Recommended to use AddNLogWeb() to avoid name-collission issue with NLog.Extension.Logging namespace</remarks>
        /// <param name="builder">The logging builder</param>
        /// <param name="configuration">Config for NLog</param>
        [Obsolete("Instead use AddNLogWeb() for explicit registration of NLog.Web.AspNetCore extensions. Marked obsolete with Nlog 6.0")]
        public static ILoggingBuilder AddNLog(this ILoggingBuilder builder, LoggingConfiguration configuration)
        {
            return AddNLogWeb(builder, configuration);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and explicit load NLog LoggingConfiguration
        /// </summary>
        /// <param name="builder">The logging builder</param>
        /// <param name="configuration">Config for NLog</param>
        public static ILoggingBuilder AddNLogWeb(this ILoggingBuilder builder, LoggingConfiguration configuration)
        {
            return AddNLogWeb(builder, configuration, NLogAspNetCoreOptions.Default);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and explicit load NLog LoggingConfiguration
        /// </summary>
        /// <remarks>Recommended to use AddNLogWeb() to avoid name-collission issue with NLog.Extension.Logging namespace</remarks>
        /// <param name="builder">The logging builder</param>
        /// <param name="configuration">Config for NLog</param>
        /// <param name="options">Options for registration of the NLog LoggingProvider and enabling features.</param>
        [Obsolete("Instead use AddNLogWeb() for explicit registration of NLog.Web.AspNetCore extensions. Marked obsolete with Nlog 6.0")]
        public static ILoggingBuilder AddNLog(this ILoggingBuilder builder, LoggingConfiguration configuration, NLogAspNetCoreOptions options)
        {
            return AddNLogWeb(builder, configuration, options);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and explicit load NLog LoggingConfiguration
        /// </summary>
        /// <param name="builder">The logging builder</param>
        /// <param name="configuration">Config for NLog</param>
        /// <param name="options">Options for registration of the NLog LoggingProvider and enabling features.</param>
        public static ILoggingBuilder AddNLogWeb(this ILoggingBuilder builder, LoggingConfiguration configuration, NLogAspNetCoreOptions options)
        {
            Guard.ThrowIfNull(builder);

            AddNLogLoggerProvider(builder.Services, null, null, options, (serviceProvider, config, env, opt) =>
            {
                var logFactory = configuration?.LogFactory ?? LogManager.LogFactory;
                var provider = CreateNLogLoggerProvider(serviceProvider, config, env, opt, logFactory);
                // Delay initialization of targets until we have loaded config-settings
                logFactory.Configuration = configuration;
                return provider;
            });
            return builder;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and provide isolated LogFactory
        /// </summary>
        /// <remarks>Recommended to use AddNLogWeb() to avoid name-collission issue with NLog.Extension.Logging namespace</remarks>
        /// <param name="builder"></param>
        /// <param name="factoryBuilder">Initialize NLog LogFactory with NLog LoggingConfiguration.</param>
        [Obsolete("Instead use AddNLogWeb() for explicit registration of NLog.Web.AspNetCore extensions. Marked obsolete with Nlog 6.0")]
        public static ILoggingBuilder AddNLog(this ILoggingBuilder builder, Func<IServiceProvider, LogFactory> factoryBuilder)
        {
            return AddNLogWeb(builder, factoryBuilder);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and provide isolated LogFactory
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="factoryBuilder">Initialize NLog LogFactory with NLog LoggingConfiguration.</param>
        public static ILoggingBuilder AddNLogWeb(this ILoggingBuilder builder, Func<IServiceProvider, LogFactory> factoryBuilder)
        {
            Guard.ThrowIfNull(builder);
            Guard.ThrowIfNull(factoryBuilder);

            AddNLogLoggerProvider(builder.Services, null, null, NLogAspNetCoreOptions.Default, (serviceProvider, config, env, options) =>
            {
                config = SetupNLogConfigSettings(serviceProvider, config, LogManager.LogFactory);
                // Delay initialization of targets until we have loaded config-settings
                var logFactory = factoryBuilder(serviceProvider);
                var provider = CreateNLogLoggerProvider(serviceProvider, config, env, options, logFactory);
                return provider;
            });
            return builder;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging, and provide isolated LogFactory
        /// </summary>
        /// <param name="builder">The logging builder</param>
        /// <param name="logFactory">NLog LogFactory</param>
        /// <param name="options">Options for registration of the NLog LoggingProvider and enabling features.</param>
        public static ILoggingBuilder AddNLogWeb(this ILoggingBuilder builder, LogFactory logFactory, NLogAspNetCoreOptions options)
        {
            Guard.ThrowIfNull(builder);

            AddNLogLoggerProvider(builder.Services, null, null, options, (serviceProvider, config, env, opt) =>
            {
                logFactory = logFactory ?? LogManager.LogFactory;
                var provider = CreateNLogLoggerProvider(serviceProvider, config, env, opt, logFactory);
                return provider;
            });
            return builder;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="collection"></param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddNLogWeb(this IServiceCollection collection)
        {
            return AddNLogWeb(collection, NLogAspNetCoreOptions.Default);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="options">NLog Logging Provider options</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddNLogWeb(this IServiceCollection collection, NLogAspNetCoreOptions options)
        {
            Guard.ThrowIfNull(collection);
            AddNLogLoggerProvider(collection, null, null, options, CreateNLogLoggerProvider);
            return collection;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="options">NLog Logging Provider options</param>
        /// <param name="factoryBuilder">Initialize NLog LogFactory with NLog LoggingConfiguration.</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddNLogWeb(this IServiceCollection collection, NLogAspNetCoreOptions options, Func<IServiceProvider, LogFactory> factoryBuilder)
        {
            Guard.ThrowIfNull(collection);
            Guard.ThrowIfNull(factoryBuilder);
            AddNLogLoggerProvider(collection, null, null, options, (serviceProvider, config, env, opt) =>
            {
                config = SetupNLogConfigSettings(serviceProvider, config, LogManager.LogFactory);

                // Delay initialization of targets until we have loaded config-settings
                var logFactory = factoryBuilder(serviceProvider);
                var provider = CreateNLogLoggerProvider(serviceProvider, config, env, options, logFactory);
                return provider;
            });
            return collection;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging.
        /// </summary>
        public static IWebHostBuilder UseNLog(this IWebHostBuilder builder)
        {
            return UseNLog(builder, NLogAspNetCoreOptions.Default);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Options for registration of the NLog LoggingProvider and enabling features.</param>
        public static IWebHostBuilder UseNLog(this IWebHostBuilder builder, NLogAspNetCoreOptions options)
        {
            Guard.ThrowIfNull(builder);
            builder.ConfigureServices((builderContext, services) => AddNLogLoggerProvider(services, builderContext.Configuration, builderContext.HostingEnvironment as IHostEnvironment, options, CreateNLogLoggerProvider));
            return builder;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging.
        /// </summary>
        public static IHostBuilder UseNLog(this IHostBuilder builder)
        {
            return UseNLog(builder, NLogAspNetCoreOptions.Default);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Options for registration of the NLog LoggingProvider and enabling features.</param>
        public static IHostBuilder UseNLog(this IHostBuilder builder, NLogAspNetCoreOptions options)
        {
            Guard.ThrowIfNull(builder);
#if NETCOREAPP3_0_OR_GREATER
            builder.ConfigureServices((builderContext, services) => AddNLogLoggerProvider(services, builderContext.Configuration, builderContext.HostingEnvironment, options, CreateNLogLoggerProvider));
#else
            builder.ConfigureServices((builderContext, services) => AddNLogLoggerProvider(services, builderContext.Configuration, null, options, CreateNLogLoggerProvider));
#endif
            return builder;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="builder"></param>
        /// <returns>IHostApplicationBuilder for chaining</returns>
        public static IHostApplicationBuilder UseNLog(this IHostApplicationBuilder builder)
        {
            Guard.ThrowIfNull(builder);
            return builder.UseNLog(NLogAspNetCoreOptions.Default);
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Options for registration of the NLog LoggingProvider and enabling features.</param>
        /// <returns>IHostApplicationBuilder for chaining</returns>
        public static IHostApplicationBuilder UseNLog(this IHostApplicationBuilder builder, NLogAspNetCoreOptions options)
        {
            Guard.ThrowIfNull(builder);
            AddNLogLoggerProvider(builder.Services, builder.Configuration, builder.Environment, options, CreateNLogLoggerProvider);
            return builder;
        }

        /// <summary>
        /// Enable NLog as logging provider for Microsoft Extension Logging
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">NLogProviderOptions object to configure NLog behavior</param>
        /// <param name="factoryBuilder">Initialize NLog LogFactory with NLog LoggingConfiguration.</param>
        /// <returns>IHostApplicationBuilder for chaining</returns>
        public static IHostApplicationBuilder UseNLog(this IHostApplicationBuilder builder, NLogAspNetCoreOptions options, Func<IServiceProvider, LogFactory> factoryBuilder)
        {
            Guard.ThrowIfNull(builder);
            Guard.ThrowIfNull(factoryBuilder);

            AddNLogLoggerProvider(builder.Services, builder.Configuration, builder.Environment, options, (serviceProvider, config, env, options) =>
            {
                config = SetupNLogConfigSettings(serviceProvider, config, LogManager.LogFactory);
                // Delay initialization of targets until we have loaded config-settings
                var logFactory = factoryBuilder(serviceProvider);
                var provider = CreateNLogLoggerProvider(serviceProvider, config, env, options, logFactory);
                return provider;
            });
            return builder;
        }
#endif

        private static void AddNLogLoggerProvider(IServiceCollection services, IConfiguration? hostConfiguration, IHostEnvironment? hostEnvironment, NLogAspNetCoreOptions options, Func<IServiceProvider, IConfiguration?, IHostEnvironment?, NLogAspNetCoreOptions, NLogLoggerProvider> factory)
        {
            options = options ?? NLogAspNetCoreOptions.Default;
            options.Configure(hostConfiguration?.GetSection("Logging:NLog"));

            var sharedFactory = factory;

            if (options.ReplaceLoggerFactory)
            {
                NLogLoggerProvider? singleInstance = null;   // Ensure that registration of ILoggerFactory and ILoggerProvider shares the same single instance
                sharedFactory = (provider, cfg, env, opt) => singleInstance ?? (singleInstance = factory(provider, cfg, env, opt));

                services.AddLogging(builder => builder?.ClearProviders());  // Cleanup the existing LoggerFactory, before replacing it with NLogLoggerFactory
                services.Replace(ServiceDescriptor.Singleton<ILoggerFactory, NLogLoggerFactory>(serviceProvider => new NLogLoggerFactory(sharedFactory(serviceProvider, hostConfiguration, hostEnvironment, options))));
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, NLogLoggerProvider>(serviceProvider => sharedFactory(serviceProvider, hostConfiguration, hostEnvironment, options)));

            if (options.RemoveLoggerFactoryFilter)
            {
                // Will forward all messages to NLog if not specifically overridden by user
                services.AddLogging(builder => builder?.AddFilter<NLogLoggerProvider>(null, Microsoft.Extensions.Logging.LogLevel.Trace));
            }

            //note: this one is called before  services.AddSingleton<ILoggerFactory>
            if (options.RegisterHttpContextAccessor)
            {
                services.AddHttpContextAccessor();
            }
        }

        private static NLogLoggerProvider CreateNLogLoggerProvider(IServiceProvider serviceProvider, IConfiguration? hostConfiguration, IHostEnvironment? hostEnvironment, NLogAspNetCoreOptions options)
        {
            return CreateNLogLoggerProvider(serviceProvider, hostConfiguration, hostEnvironment, options, LogManager.LogFactory);
        }

        private static NLogLoggerProvider CreateNLogLoggerProvider(IServiceProvider serviceProvider, IConfiguration? hostConfiguration, IHostEnvironment? hostEnvironment, NLogAspNetCoreOptions options, NLog.LogFactory logFactory)
        {
            NLogLoggerProvider provider = new NLogLoggerProvider(options, logFactory ?? LogManager.LogFactory);

            var configuration = SetupNLogConfigSettings(serviceProvider, hostConfiguration, provider.LogFactory);

            if (configuration != null && (!ReferenceEquals(configuration, hostConfiguration) || options is null))
            {
                provider.Configure(configuration.GetSection("Logging:NLog"));
            }

            if (serviceProvider != null && provider.Options.RegisterServiceProvider)
            {
                provider.LogFactory.ServiceRepository.RegisterService(typeof(IServiceProvider), serviceProvider);
            }

            if (configuration is null || !TryLoadConfigurationFromSection(provider, configuration))
            {
                string? nlogConfigFile = null;
                var contentRootPath = hostEnvironment?.ContentRootPath;
                var environmentName = hostEnvironment?.EnvironmentName;
                if (!string.IsNullOrWhiteSpace(contentRootPath) || !string.IsNullOrWhiteSpace(environmentName))
                {
                    provider.LogFactory.Setup().LoadConfiguration(cfg =>
                    {
                        if (!IsLoggingConfigurationLoaded(cfg.Configuration))
                        {
                            nlogConfigFile = ResolveEnvironmentNLogConfigFile(contentRootPath, environmentName);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                            cfg.Configuration = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                        }
                    });
                }

                if (!string.IsNullOrEmpty(nlogConfigFile))
                {
                    provider.LogFactory.Setup().LoadConfigurationFromFile(nlogConfigFile, optional: true);
                }
            }

            if (provider.Options.ShutdownOnDispose || !provider.Options.AutoShutdown)
            {
                provider.LogFactory.AutoShutdown = false;
            }

            return provider;
        }

        private static string? ResolveEnvironmentNLogConfigFile(string? basePath, string? environmentName)
        {
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    var nlogConfigEnvFilePath = System.IO.Path.Combine(basePath, $"nlog.{environmentName}.config");
                    if (System.IO.File.Exists(nlogConfigEnvFilePath))
                        return System.IO.Path.GetFullPath(nlogConfigEnvFilePath);
                    nlogConfigEnvFilePath = System.IO.Path.Combine(basePath, $"NLog.{environmentName}.config");
                    if (System.IO.File.Exists(nlogConfigEnvFilePath))
                        return System.IO.Path.GetFullPath(nlogConfigEnvFilePath);
                }

                var nlogConfigFilePath = System.IO.Path.Combine(basePath, "nlog.config");
                if (System.IO.File.Exists(nlogConfigFilePath))
                    return System.IO.Path.GetFullPath(nlogConfigFilePath);
                nlogConfigFilePath = System.IO.Path.Combine(basePath, "NLog.config");
                if (System.IO.File.Exists(nlogConfigFilePath))
                    return System.IO.Path.GetFullPath(nlogConfigFilePath);
            }

            if (!string.IsNullOrWhiteSpace(environmentName))
                return $"nlog.{environmentName}.config";

            return null;
        }

        private static bool IsLoggingConfigurationLoaded(LoggingConfiguration cfg)
        {
            return cfg?.LoggingRules?.Count > 0 && cfg?.AllTargets?.Count > 0;
        }

        private static IConfiguration? SetupNLogConfigSettings(IServiceProvider serviceProvider, IConfiguration? configuration, LogFactory logFactory)
        {
            configuration = configuration ?? (serviceProvider?.GetService(typeof(IConfiguration)) as IConfiguration);
            logFactory.Setup()
                .SetupExtensions(ext => ext.RegisterConfigSettings(configuration).RegisterNLogWeb(serviceProvider))
                .SetupLogFactory(ext => ext.AddCallSiteHiddenAssembly(typeof(AspNetExtensions).GetTypeInfo().Assembly));
            return configuration;
        }

        private static bool TryLoadConfigurationFromSection(NLogLoggerProvider loggerProvider, IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(loggerProvider.Options.LoggingConfigurationSectionName))
                return false;

            var nlogConfig = configuration.GetSection(loggerProvider.Options.LoggingConfigurationSectionName);
            if (nlogConfig?.GetChildren()?.Any() == true)
            {
                loggerProvider.LogFactory.Setup().LoadConfiguration(configBuilder =>
                {
                    if (configBuilder.Configuration.LoggingRules.Count == 0 && configBuilder.Configuration.AllTargets.Count == 0)
                    {
                        configBuilder.Configuration = new NLogLoggingConfiguration(nlogConfig, loggerProvider.LogFactory);
                    }
                });
                return true;
            }
            else
            {
                Common.InternalLogger.Debug("Skip loading NLogLoggingConfiguration from empty config section: {0}", loggerProvider.Options.LoggingConfigurationSectionName);
                return false;
            }
        }
    }
}
