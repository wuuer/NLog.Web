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
    /// ASP.NET Local IP of the Connection
    /// </summary>
    /// <remarks>
    /// <code>${aspnet-request-local-ip}</code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-Request-Local-IP-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-request-local-ip")]
    public class AspNetRequestLocalIpLayoutRenderer : AspNetLayoutRendererBase
    {
        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var httpContext = HttpContextAccessor?.HttpContext;

#if ASP_NET_CORE
            var connection = httpContext.TryGetConnection();
            if (connection is null)
                return;

            builder.Append(connection.LocalIpAddress?.ToString());
#else
            var request = httpContext.TryGetRequest();
            if (request is null)
                return;

            builder.Append(request.ServerVariables?["LOCAL_ADDR"]);
#endif
        }
    }
}