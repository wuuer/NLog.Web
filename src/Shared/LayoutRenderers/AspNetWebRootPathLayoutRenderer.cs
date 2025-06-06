﻿using System;
using System.Text;
using NLog.Config;
using NLog.LayoutRenderers;
#if ASP_NET_CORE
#if NETCOREAPP3_0_OR_GREATER
using Microsoft.AspNetCore.Hosting;
#else
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif
using NLog.Web.DependencyInjection;
#else
using System.Web.Hosting;
using IWebHostEnvironment = NLog.Web.Internal.IHostEnvironment;
#endif

namespace NLog.Web.LayoutRenderers
{
#if ASP_NET_CORE
    /// <summary>
    /// Rendering WebRootPath. <see cref="IWebHostEnvironment.WebRootPath" />
    /// </summary>
    /// <remarks>
    /// <code>${aspnet-webrootpath}</code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-WebRootPath-layout-renderer">Documentation on NLog Wiki</seealso>
#else
    /// <summary>
    /// Rendering WebRootPath. <see cref="HostingEnvironment.MapPath"/>("/")
    /// </summary>
    /// <remarks>
    /// <code>${aspnet-webrootpath}</code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-WebRootPath-layout-renderer">Documentation on NLog Wiki</seealso>
#endif
    [LayoutRenderer("aspnet-webrootpath")]
    [ThreadAgnostic]
    public class AspNetWebRootPathLayoutRenderer : LayoutRenderer
    {
        /// <summary>
        /// Provides access to the current IHostEnvironment
        /// </summary>
        /// <returns>IHostEnvironment or <c>null</c></returns>
        internal IWebHostEnvironment? WebHostEnvironment
        {
            get => _webHostEnvironment ?? (_webHostEnvironment = ResolveHostEnvironment());
            set => _webHostEnvironment = value;
        }
        private IWebHostEnvironment? _webHostEnvironment;
        private string? _webRootPath;

        /// <inheritdoc />
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var webRootPath = _webRootPath ?? (_webRootPath = ResolveWebRootPath());
            builder.Append(webRootPath);
        }

        private IWebHostEnvironment? ResolveHostEnvironment()
        {
#if ASP_NET_CORE
            return ServiceLocator.ResolveService<IWebHostEnvironment>(ResolveService<IServiceProvider>(), LoggingConfiguration);
#else
            return Internal.HostEnvironment.Default;
#endif
        }

        private string? ResolveWebRootPath()
        {
#if ASP_NET_CORE
            var webRootPath = WebHostEnvironment?.WebRootPath;
#else
            var webRootPath = WebHostEnvironment?.MapPath("/");
#endif
            return string.IsNullOrEmpty(webRootPath) ? null : webRootPath;
        }

        /// <inheritdoc/>
        protected override void CloseLayoutRenderer()
        {
            _webHostEnvironment = null;
            _webRootPath = null;
            base.CloseLayoutRenderer();
        }
    }
}