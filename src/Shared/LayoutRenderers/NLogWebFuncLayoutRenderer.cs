﻿using System;
#if ASP_NET_CORE
using Microsoft.AspNetCore.Http;
using HttpContextBase = Microsoft.AspNetCore.Http.HttpContext;
#else
using System.Web;
#endif
using NLog.Config;
using NLog.LayoutRenderers;

namespace NLog.Web.LayoutRenderers
{
    /// <summary>
    /// Specialized layout render which has a cached <see cref="IHttpContextAccessor"/>
    /// </summary>
    internal class NLogWebFuncLayoutRenderer : FuncLayoutRenderer
    {
        private readonly Func<LogEventInfo, HttpContextBase?, LoggingConfiguration?, object?> _func;

        internal IHttpContextAccessor? HttpContextAccessor
        {
            get => _httpContextAccessor ?? (_httpContextAccessor = AspNetLayoutRendererBase.RetrieveHttpContextAccessor(ResolveService<IServiceProvider>(), LoggingConfiguration));
            set => _httpContextAccessor = value;
        }
        private IHttpContextAccessor? _httpContextAccessor;

        /// <inheritdoc />
        protected override void CloseLayoutRenderer()
        {
            _httpContextAccessor = null;
            base.CloseLayoutRenderer();
        }

        public NLogWebFuncLayoutRenderer(string name, Func<LogEventInfo, HttpContextBase?, LoggingConfiguration?, object?> func) : base(name)
        {
            _func = func;
        }

        /// <inheritdoc />
        protected override object? RenderValue(LogEventInfo logEvent)
        {
            var httpContext = HttpContextAccessor?.HttpContext;
            return _func.Invoke(logEvent, httpContext, LoggingConfiguration);
        }
    }
}