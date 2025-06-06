﻿using System.Text;
using NLog.LayoutRenderers;
using NLog.Web.Internal;
#if ASP_NET_CORE
using Microsoft.AspNetCore.Http;
#else
using System.Web;
#endif

namespace NLog.Web.LayoutRenderers
{
    /// <summary>
    /// ASP.NET Local Port of the Connection
    /// </summary>
    /// <remarks>
    /// <code>${aspnet-request-local-port}</code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-Request-Local-Port-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-request-local-port")]
    public class AspNetRequestLocalPortLayoutRenderer : AspNetLayoutRendererBase
    {
        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var httpContext = HttpContextAccessor?.HttpContext;
#if ASP_NET_CORE
            var connection = httpContext.TryGetConnection();
            if (connection is null)
                return;

            builder.Append(connection.LocalPort);
#else
            var request = httpContext.TryGetRequest();
            if (request is null)
                return;

            builder.Append(request.ServerVariables?["LOCAL_PORT"]);
#endif
        }
    }
}