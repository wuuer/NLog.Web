using System.Text;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Web.Internal;

namespace NLog.Web.LayoutRenderers
{
    /// <summary>
    /// ASP.NET Request DNS name of the remote client
    /// </summary>
    /// <remarks>
    /// <code>${aspnet-request-host}</code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNetRequest-Host-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-request-host")]
    public class AspNetRequestHostLayoutRenderer : AspNetLayoutRendererBase
    {
        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var httpRequest = HttpContextAccessor?.HttpContext.TryGetRequest();
#if ASP_NET_CORE
            var host = httpRequest?.Host.ToString();
#else
            var host = httpRequest?.UserHostName?.ToString();
#endif
            builder.Append(host);
        }
    }
}