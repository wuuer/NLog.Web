﻿using NLog.Web.LayoutRenderers;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

#if NETCOREAPP3_0_OR_GREATER
using Microsoft.Extensions.Hosting;
#else
using Microsoft.AspNetCore.Hosting;
using IHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace NLog.Web.Tests.LayoutRenderers
{
    public class AspNetEnvironmentLayoutRendererTests : TestBase
    {
        [Fact]
        public void SuccessTest()
        {
            var renderer = new AspNetEnvironmentLayoutRenderer();
            var hostEnvironment = Substitute.For<IHostEnvironment>();
            hostEnvironment.EnvironmentName.Returns("NLogTestEnvironmentName");

            renderer.HostEnvironment = hostEnvironment;
            string actual = renderer.Render(new LogEventInfo());
            Assert.Equal("NLogTestEnvironmentName", actual);
        }

        [Fact]
        public void NullTest()
        {
            var renderer = new AspNetEnvironmentLayoutRenderer();
            var hostEnvironment = Substitute.For<IHostEnvironment>();
            hostEnvironment.EnvironmentName.ReturnsNull();

            renderer.HostEnvironment = hostEnvironment;
            string actual = renderer.Render(new LogEventInfo());
            Assert.Equal("Production", actual);
        }

        [Fact]
        public void InitCloseTest()
        {
            var logFactory = new LogFactory().Setup().RegisterNLogWeb().LoadConfiguration(builder =>
            {
                builder.ForTarget().WriteTo(new NLog.Targets.MemoryTarget() { Layout = "${aspnet-environment}" });
            }).LogFactory;
            Assert.NotNull(logFactory);
            logFactory.Shutdown();
        }
    }
}
