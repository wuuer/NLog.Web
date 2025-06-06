﻿using System.Text;
using NLog.LayoutRenderers;
using NLog.Web.Internal;

namespace NLog.Web.LayoutRenderers
{
    /// <summary>
    /// ASP.NET Response ContentLength
    /// </summary>
    /// <remarks>
    /// <code>${aspnet-response-contentlength}</code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-Response-ContentLength-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-response-contentlength")]
    public class AspNetResponseContentLengthLayoutRenderer : AspNetLayoutRendererBase
    {
        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var httpResponse = HttpContextAccessor?.HttpContext.TryGetResponse();
            if (httpResponse is null)
                return;

#if ASP_NET_CORE
            var contentLength = httpResponse.ContentLength ?? 0L;
#else
            var contentLength = httpResponse.OutputStream?.Length ?? 0L;
#endif
            if (contentLength > 0L)
            {
                builder.Append(contentLength);
            }
        }
    }
}
