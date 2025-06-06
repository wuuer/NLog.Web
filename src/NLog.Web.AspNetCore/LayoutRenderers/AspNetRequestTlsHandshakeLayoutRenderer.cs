﻿#if NETCOREAPP3_0_OR_GREATER
using Microsoft.AspNetCore.Connections.Features;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Web.Enums;
using NLog.Web.Internal;
using System.Text;

namespace NLog.Web.LayoutRenderers
{
    /// <summary>
    /// ASP.NET TLS Handshake
    /// </summary>
    /// <remarks>
    /// <code>
    /// ${aspnet-request-tls-handshake:Property=CipherAlgorithm}
    /// ${aspnet-request-tls-handshake:Property=CipherStrength}
    /// ${aspnet-request-tls-handshake:Property=HashAlgorithm}
    /// ${aspnet-request-tls-handshake:Property=HashStrength}
    /// ${aspnet-request-tls-handshake:Property=KeyExchangeAlgorithm}
    /// ${aspnet-request-tls-handshake:Property=KeyExchangeStrength}
    /// ${aspnet-request-tls-handshake:Property=Protocol}
    /// </code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-Request-TLS-Handshake-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-request-tls-handshake")]
    public class AspNetRequestTlsHandshakeLayoutRenderer : AspNetLayoutRendererBase
    {
        /// <summary>
        /// Specifies which of the 7 properties of ITlsHandshakeFeature to emit
        /// Defaults to the protocol
        /// </summary>
        [DefaultParameter]
        public TlsHandshakeProperty Property { get; set; } = TlsHandshakeProperty.Protocol;

        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var tlsHandshake = HttpContextAccessor?.HttpContext.TryGetFeature<ITlsHandshakeFeature>();
            if (tlsHandshake is null)
                return;

            switch (Property)
            {
                case TlsHandshakeProperty.CipherAlgorithm:
                    builder.Append(tlsHandshake.CipherAlgorithm);
                    break;
                case TlsHandshakeProperty.CipherStrength:
                    builder.Append(tlsHandshake.CipherStrength);
                    break;
                case TlsHandshakeProperty.HashAlgorithm:
                    builder.Append(tlsHandshake.HashAlgorithm);
                    break;
                case TlsHandshakeProperty.HashStrength:
                    builder.Append(tlsHandshake.HashStrength);
                    break;
                case TlsHandshakeProperty.KeyExchangeAlgorithm:
                    builder.Append(tlsHandshake.KeyExchangeAlgorithm);
                    break;
                case TlsHandshakeProperty.KeyExchangeStrength:
                    builder.Append(tlsHandshake.KeyExchangeStrength);
                    break;
                case TlsHandshakeProperty.Protocol:
                    builder.Append(tlsHandshake.Protocol);
                    break;
            }

        }
    }
}
#endif