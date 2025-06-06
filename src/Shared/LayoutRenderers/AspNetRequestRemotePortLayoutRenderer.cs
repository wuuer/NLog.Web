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
    /// ASP.NET Remote Port of the Connection
    /// </summary>
    /// <remarks>
    /// <code>${aspnet-request-remote-port}</code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-Request-Remote-Port-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-request-remote-port")]
    public class AspNetRequestRemotePortLayoutRenderer : AspNetLayoutRendererBase
    {
        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var httpContext = HttpContextAccessor?.HttpContext;

#if ASP_NET_CORE
            var connection = httpContext.TryGetConnection();
            if (connection is null)
                return;

            builder.Append(connection.RemotePort);
#else
            var request = httpContext.TryGetRequest();
            if (request is null)
                return;

            builder.Append(request.ServerVariables?["REMOTE_PORT"]);
#endif
        }
    }
}