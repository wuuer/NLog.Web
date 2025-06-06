﻿#if ASP_NET_CORE

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using System.IO;

namespace NLog.Web.Tests
{
    public sealed class AspNetCoreTests : TestBase, IDisposable
    {
        public void Dispose()
        {
            LogManager.Configuration = null;
        }

        [Fact]
        public void UseNLogShouldLogTest()
        {
            Assert.True(ShouldLogTest(() => CreateWebHost()));
        }

        [Fact]
        public void AddNLogWebShouldLogTest()
        {
            Assert.True(ShouldLogTest(() =>
            {
                return CreateWebHostBuilder()
                    .ConfigureLogging(builder => builder.ClearProviders().AddNLogWeb())
                    .Build();
            }));
        }

        [Fact]
        public void AddNLogWebServiceShouldLogTest()
        {
            Assert.True(ShouldLogTest(() =>
            {
                return CreateWebHostBuilder()
                    .ConfigureServices(services => services.AddNLogWeb())
                    .Build();
            }));
        }

        private bool ShouldLogTest(Func<IWebHost> webHostBuilder)
        {
            LogManager.Configuration = null;

            var webhost = webHostBuilder.Invoke();

            var loggerFact = GetLoggerFactory(webhost.Services);

            Assert.NotNull(loggerFact);

            var configuration = CreateConfigWithMemoryTarget(out var target, "${logger}|${message}|${callsite}");

            LogManager.Setup().RegisterNLogWeb(serviceProvider: webhost.Services).LoadConfiguration(configuration);

            var logger = loggerFact.CreateLogger("logger1");

            logger.LogError("error1");

            var logged = target.Logs;

            Assert.Single(logged);
            Assert.Equal($"logger1|error1|{GetType()}.{nameof(ShouldLogTest)}", logged.First());
            return logged.Count == 1;
        }

        [Fact]
        public void UseNLog_ReplaceLoggerFactory()
        {
            LogManager.Configuration = null;

            var webhost = CreateWebHost(new NLogAspNetCoreOptions() { ReplaceLoggerFactory = true });

            // Act
            var loggerFactory = webhost.Services.GetService<ILoggerFactory>();
            var loggerProvider = webhost.Services.GetService<ILoggerProvider>();

            // Assert
            Assert.Equal(typeof(NLogLoggerFactory), loggerFactory.GetType());
            Assert.Equal(typeof(NLogLoggerProvider), loggerProvider.GetType());
        }

#if NETCOREAPP3_0_OR_GREATER
        [Fact]
        public void UseNLogContentRootTest()
        {
            LogManager.Configuration = null;

            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nameof(AspNetCoreTests), Guid.NewGuid().ToString()).Replace("\\", "/");
            var orgPath = System.IO.Directory.GetCurrentDirectory();

            try
            {
                System.IO.Directory.CreateDirectory(tempPath);
                System.IO.Directory.SetCurrentDirectory(tempPath);

                var configChanged = false;

                LogManager.ConfigurationChanged += (args, sender) => configChanged = true;

                using var webhost = CreateWebHost();

                var hostEnvironment = webhost.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
                Assert.NotNull(hostEnvironment.ContentRootPath);
                Assert.False(configChanged);    // Scanned ContentRoot without assigning any default config

                var loggerFact = GetLoggerFactory(webhost.Services);
                Assert.NotNull(loggerFact);

                var configuration = CreateConfigWithMemoryTarget(out var target, "${logger}|${message}|${callsite}");

                LogManager.Setup().RegisterNLogWeb(serviceProvider: webhost.Services).LoadConfiguration(configuration);

                var logger = loggerFact.CreateLogger("logger1");

                logger.LogError("error1");

                var logged = target.Logs;

                Assert.Single(logged);
                Assert.Equal($"logger1|error1|{GetType()}.{nameof(UseNLogContentRootTest)}", logged.First());
            }
            finally
            {
                System.IO.Directory.SetCurrentDirectory(orgPath);
                System.IO.Directory.Delete(tempPath);
            }
        }

        [Fact]
        public void LoadConfigurationFromAppSettingsShouldLogTest()
        {
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nameof(AspNetCoreTests), Guid.NewGuid().ToString()).Replace("\\", "/");
            var appSettings = System.IO.Path.Combine(tempPath, "appsettings.json");

            try
            {
                // Arrange
                System.IO.Directory.CreateDirectory(tempPath);
                System.IO.File.AppendAllText(appSettings, @"{
                  ""basepath"": """ + tempPath + @""",
                  ""NLog"": {
                    ""throwConfigExceptions"": true,
                    ""targets"": {
                        ""logfile"": {
                            ""type"": ""File"",
                            ""fileName"": ""${configsetting:basepath}/hello.txt"",
                            ""layout"": ""${message}""
                        }
                    },
                    ""rules"": [
                      {
                        ""logger"": ""*"",
                        ""minLevel"": ""Debug"",
                        ""writeTo"": ""logfile""
                      }
                    ]
                  }
                }");

                // Act
                var logFactory = new LogFactory();
                var logger = logFactory.Setup().LoadConfigurationFromAppSettings(basePath: tempPath).GetCurrentClassLogger();
                logger.Info("Hello World");

                // Assert
                logFactory.Dispose();
                var fileOutput = System.IO.File.ReadAllText(System.IO.Path.Combine(tempPath, "hello.txt"));
                Assert.Contains("Hello World", fileOutput);
            }
            finally
            {
                if (System.IO.Directory.Exists(tempPath))
                {
                    System.IO.Directory.Delete(tempPath, true);
                }
            }
        }

        [Fact]
        public void LoadConfigurationFromAppSettingsShouldLogTest2()
        {
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nameof(AspNetCoreTests), Guid.NewGuid().ToString()).Replace("\\", "/");
            var appSettings = System.IO.Path.Combine(tempPath, "appsettings.json");

            try
            {
                // Arrange
                System.IO.Directory.CreateDirectory(tempPath);
                System.IO.File.AppendAllText(appSettings, @"{
                  ""basepath"": """ + tempPath + @"""
                }");

                System.IO.File.AppendAllText(System.IO.Path.Combine(tempPath, "nlog.config"), @"<nlog>
                    <targets>
                        <target type=""file"" name=""logfile"" layout=""${message}"" fileName=""${configsetting:basepath}/hello.txt"" />
                    </targets>
                    <rules>
                        <logger name=""*"" minLevel=""Debug"" writeTo=""logfile"" />
                    </rules>
                </nlog>");

                // Act
                var logFactory = new LogFactory();
                var logger = logFactory.Setup().LoadConfigurationFromAppSettings(basePath: tempPath).GetCurrentClassLogger();
                logger.Info("Hello World");

                // Assert
                logFactory.Dispose();
                var fileOutput = System.IO.File.ReadAllText(System.IO.Path.Combine(tempPath, "hello.txt"));
                Assert.Contains("Hello World", fileOutput);
            }
            finally
            {
                if (System.IO.Directory.Exists(tempPath))
                {
                    System.IO.Directory.Delete(tempPath, true);
                }
            }
        }

        [Fact]
        public void LoadConfigurationFromAppSettingsShouldLogTest3()
        {
            var orgCurrentDirectory = Environment.CurrentDirectory;

            var contentPath = System.IO.Path.Combine(AppContext.BaseDirectory, nameof(AspNetCoreTests), Guid.NewGuid().ToString()).Replace("\\", "/");
            var appSettings = System.IO.Path.Combine(contentPath, "appsettings.json");

            try
            {
                // Arrange
                System.IO.Directory.CreateDirectory(contentPath);
                Environment.CurrentDirectory = contentPath;
                System.IO.File.AppendAllText(appSettings, @"{
                  ""basepath"": """ + contentPath + @"""
                }");

                System.IO.File.AppendAllText(System.IO.Path.Combine(contentPath, "nlog.config"), @"<nlog>
                    <targets>
                        <target type=""file"" name=""logfile"" layout=""${message}"" fileName=""${configsetting:basepath}/hello.txt"" />
                    </targets>
                    <rules>
                        <logger name=""*"" minLevel=""Debug"" writeTo=""logfile"" />
                    </rules>
                </nlog>");

                // Act
                var logFactory = new LogFactory();
                var logger = logFactory.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
                logger.Info("Hello World");

                // Assert
                logFactory.Dispose();
                var fileOutput = System.IO.File.ReadAllText(System.IO.Path.Combine(contentPath, "hello.txt"));
                Assert.Contains("Hello World", fileOutput);
            }
            finally
            {
                Environment.CurrentDirectory = orgCurrentDirectory;

                if (System.IO.Directory.Exists(contentPath))
                {
                    System.IO.Directory.Delete(contentPath, true);
                }
            }
        }

        [Fact]
        public void LoadConfigurationFromAppSettingsUNC()
        {
            // Arrange
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nameof(AspNetCoreTests), Guid.NewGuid().ToString()).Replace("\\", "/");
            var UNCtempPath = @"\\?\" + tempPath.Replace('/', '\\');
            Directory.CreateDirectory(tempPath);

            try
            {
                // Act
                var logFactory = new LogFactory();

                // Asssert
                Assert.Throws<UriFormatException>(() => new Uri(UNCtempPath));

                var logger = logFactory.Setup().LoadConfigurationFromAppSettings(basePath: UNCtempPath).GetCurrentClassLogger();
                logger.Info("Hello World");
            }
            finally
            {
                Directory.Delete(tempPath);
            }
        }
#endif

        private static LoggingConfiguration CreateConfigWithMemoryTarget(out MemoryTarget target, Layout layout)
        {
            var configuration = new LoggingConfiguration();
            target = new MemoryTarget("target1") { Layout = layout };

            configuration.AddRuleForAllLevels(target);
            return configuration;
        }

        [Fact]
        public void UseAspNetWithoutRegister()
        {
            try
            {
                //clear so next time it's rebuild
                ConfigurationItemFactory.Default = null;

                var webhost = CreateWebHost();

                var configuration = CreateConfigWithMemoryTarget(out var target, "${logger}|${message}|${aspnet-item:key1}");

                LogManager.Setup().RegisterNLogWeb(serviceProvider: webhost.Services).LoadConfiguration(configuration);

                var httpContext = webhost.Services.GetService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();
                httpContext.Items["key1"] = "value1";

                var loggerFact = GetLoggerFactory(webhost.Services);

                var logger = loggerFact.CreateLogger("logger1");

                logger.LogError("error1");

                var logged = target.Logs;

                Assert.Single(logged);
                Assert.Equal("logger1|error1|value1", logged.First());
            }
            finally
            {
                //clear so next time it's rebuild
                ConfigurationItemFactory.Default = null;
            }
        }

        [Fact]
        public void RegisterHttpContext()
        {
            var webhost = CreateWebHost();
            Assert.NotNull(webhost.Services.GetService<IHttpContextAccessor>());
        }

        [Fact]
        public void SkipRegisterHttpContext()
        {
            var webhost = CreateWebHost(new NLogAspNetCoreOptions { RegisterHttpContextAccessor = false });
            Assert.Null(webhost.Services.GetService<IHttpContextAccessor>());
        }


        [Fact]
        public void UseNLog_LoadConfigurationFromSection()
        {
            var host = CreateWebHostBuilder().ConfigureAppConfiguration((context, config) =>
            {
                var memoryConfig = new Dictionary<string, string>();
                memoryConfig["NLog:Rules:0:logger"] = "*";
                memoryConfig["NLog:Rules:0:minLevel"] = "Trace";
                memoryConfig["NLog:Rules:0:writeTo"] = "inMemory";
                memoryConfig["NLog:Targets:inMemory:type"] = "Memory";
                memoryConfig["NLog:Targets:inMemory:layout"] = "${logger}|${message}|${configsetting:NLog.Targets.inMemory.type}";
                config.AddInMemoryCollection(memoryConfig);
            }).UseNLog(new NLogAspNetCoreOptions() { LoggingConfigurationSectionName = "NLog", ReplaceLoggerFactory = true }).Build();

            var loggerFact = host.Services.GetService<ILoggerFactory>();
            var logger = loggerFact.CreateLogger("logger1");
            logger.LogError("error1");

            var loggerProvider = host.Services.GetService<ILoggerProvider>() as NLogLoggerProvider;
            var logged = loggerProvider.LogFactory.Configuration.FindTargetByName<NLog.Targets.MemoryTarget>("inMemory").Logs;

            Assert.Single(logged);
            Assert.Equal("logger1|error1|Memory", logged[0]);
        }

        [Fact]
        public void UseNLogWithNullWebHostBuilderThrowsArgumentNullException()
        {
            IWebHostBuilder builder = null;
            Assert.Throws<ArgumentNullException>(() => builder.UseNLog());

            try
            {
                builder.UseNLog();
            }
            catch (Exception e)
            {
                var argNullException = Assert.IsType<ArgumentNullException>(e);
                Assert.Equal("builder", argNullException.ParamName);    // Verify CallerArgumentExpressionAttribute works
            }
        }

        [Fact]
        public void UseNLog_ReplaceLoggerFactory_FromConfiguration()
        {
            var host = CreateWebHostBuilder().ConfigureAppConfiguration((context, config) =>
            {
                var memoryConfig = new Dictionary<string, string>();
                memoryConfig["Logging:NLog:ReplaceLoggerFactory"] = "True";
                memoryConfig["Logging:NLog:RemoveLoggerFactoryFilter"] = "False";
                config.AddInMemoryCollection(memoryConfig);
            }).UseNLog().Build();

            // Act
            var loggerFactory = host.Services.GetService<ILoggerFactory>();
            var loggerProvider = host.Services.GetService<ILoggerProvider>();

            // Assert
            Assert.Equal(typeof(NLogLoggerFactory), loggerFactory.GetType());
            Assert.Equal(typeof(NLogLoggerProvider), loggerProvider.GetType());
        }

        private static IWebHostBuilder CreateWebHostBuilder()
        {
            var builder =
                Microsoft.AspNetCore.WebHost.CreateDefaultBuilder()
                    .Configure(c => c.New());//.New needed, otherwise:
                                             // Unhandled Exception: System.ArgumentException: A valid non-empty application name must be provided.
                                             // Parameter name: applicationName
            return builder;
        }

        /// <summary>
        /// Create webhost with UseNlog
        /// </summary>
        /// <returns></returns>
        private static IWebHost CreateWebHost(NLogAspNetCoreOptions options = null)
        {
            return CreateWebHostBuilder()
                .UseNLog(options)
                .Build();
        }

        private static ILoggerFactory GetLoggerFactory(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
        }
    }
}

#endif