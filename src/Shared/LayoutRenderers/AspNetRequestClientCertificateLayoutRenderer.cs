﻿using System.Text;
using NLog.LayoutRenderers;
using NLog.Web.Internal;
#if !ASP_NET_CORE
using System.Security.Cryptography.X509Certificates;
#endif

namespace NLog.Web.LayoutRenderers
{
    /// <summary>
    /// ASP.NET Client Certificate of the Connection
    /// </summary>
    /// <remarks>
    /// <code>
    /// ${aspnet-request-client-certificate}
    /// ${aspnet-request-client-certificate:Verbose=True}
    /// </code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-Request-Client-Certificate-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-request-client-certificate")]
    public class AspNetRequestClientCertificateLayoutRenderer : AspNetLayoutRendererBase
    {
        /// <summary>
        /// This is passed to the X509Certificate2.ToString(bool) method
        /// </summary>
        public bool Verbose { get; set; }

        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var httpContext = HttpContextAccessor?.HttpContext;
#if ASP_NET_CORE
            builder.Append(httpContext.TryGetConnection()?.ClientCertificate?.ToString(Verbose));
#else
            var certificate = httpContext?.Request?.ClientCertificate?.Certificate;
            if (certificate?.Length > 0)
            {
                // Convert to an X509Certificate2, which does have the proper overridden ToString() method.
                // HttpClientCertificate class only use object.ToString() which is useless.
                builder.Append(new X509Certificate2(certificate).ToString(Verbose));
            }
#endif
        }
    }
}