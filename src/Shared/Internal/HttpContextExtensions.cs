using System;
using System.Collections.Generic;
#if !ASP_NET_CORE
using System.Web;
#else
using System.Text;
#if NETCOREAPP3_0_OR_GREATER
using Microsoft.AspNetCore.Connections.Features;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
#endif
using NLog.Common;

namespace NLog.Web.Internal
{
    internal static class HttpContextExtensions
    {
#if !ASP_NET_CORE
        internal static bool HasActiveHttpContext(this IHttpContextAccessor? httpContextAccessor)
        {
            if (httpContextAccessor is DefaultHttpContextAccessor defaultHttpContextAccessor)
                return defaultHttpContextAccessor.HasActiveHttpContext();    // Skip allocating HttpContextWrapper
            else
                return httpContextAccessor?.HttpContext != null;
        }

        internal static HttpRequestBase? TryGetRequest(this HttpContextBase? context)
        {
            try
            {
                var request = context?.Request;
                if (request is null)
                    InternalLogger.Debug("HttpContext Request Lookup returned null");
                return request;
            }
            catch (HttpException ex)
            {
                InternalLogger.Debug(ex, "HttpContext Request Lookup failed.");
                return null;
            }
        }

        internal static HttpResponseBase? TryGetResponse(this HttpContextBase? context)
        {
            try
            {
                var response = context?.Response;
                if (response is null)
                    InternalLogger.Debug("HttpContext Response Lookup returned null");
                return response;
            }
            catch (HttpException ex)
            {
                InternalLogger.Debug(ex, "HttpContext Response Lookup failed.");
                return null;
            }
        }
#else
        internal static bool HasActiveHttpContext(this IHttpContextAccessor? httpContextAccessor)
        {
            return httpContextAccessor?.HttpContext != null;
        }

        internal static WebSocketManager? TryGetWebSocket(this HttpContext? context)
        {
            var websocket = context?.WebSockets;
            if (websocket is null)
                InternalLogger.Debug("HttpContext WebSocket Lookup returned null");
            return websocket;
        }

        internal static ConnectionInfo? TryGetConnection(this HttpContext? context)
        {
            var connection = context?.Connection;
            if (connection is null)
                InternalLogger.Debug("HttpContext Connection Lookup returned null");
            return connection;
        }

        internal static HttpRequest? TryGetRequest(this HttpContext? context)
        {
            var request = context?.Request;
            if (request is null)
                InternalLogger.Debug("HttpContext Request Lookup returned null");
            return request;
        }

        internal static HttpResponse? TryGetResponse(this HttpContext? context)
        {
            var response = context?.Response;
            if (response is null)
                InternalLogger.Debug("HttpContext Response Lookup returned null");
            return response;
        }

        internal static T? TryGetFeature<T>(this HttpContext? context) where T : class
        {
            var feature = context?.Features?.Get<T>();
            if (feature is null)
                InternalLogger.Debug("HttpContext Features Lookup returned null - {0}", typeof(T));
            return feature;
        }
#endif

#if ASP_NET_CORE && !NETCOREAPP3_0_OR_GREATER
        internal static string? GetString(this ISession session, string key)
        {
            if (!session.TryGetValue(key, out var data))
                return null;

            if (data is null)
                return null;

            if (data.Length == 0)
                return string.Empty;

            return Encoding.UTF8.GetString(data);
        }

        public static int? GetInt32(this ISession session, string key)
        {
            if (!session.TryGetValue(key, out var data))
                return null;

            if (data is null || data.Length < 4)
                return null;

            return data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
        }
#endif

#if !ASP_NET_CORE
        internal static HttpSessionStateBase? TryGetSession(this HttpContextBase? context)
        {
            var session = context?.Session;
            if (session is null)
                InternalLogger.Debug("HttpContext Session Lookup returned null");
            return session;
        }
#else
        internal static ISession? TryGetSession(this HttpContext? context)
        {
            try
            {
                var sessionFeature = context.TryGetFeature<ISessionFeature>();
                if (sessionFeature is null)
                    return null;
                    
                if (sessionFeature.Session is null)
                {
                    InternalLogger.Debug("HttpContext Session Feature returned null");
                    return null;
                }

                return context?.Session;
            }
            catch (ObjectDisposedException ex)
            {
                InternalLogger.Debug(ex, "HttpContext Session Disposed.");
                return null; // System.ObjectDisposedException: IFeatureCollection has been disposed.
            }
            catch (InvalidOperationException ex)
            {
                InternalLogger.Debug(ex, "HttpContext Session Lookup failed.");
                return null; // System.InvalidOperationException: Session has not been configured for this application or request.
            }
        }
#endif

        internal static bool HasAllowedContentType(this HttpContext? context, IList<KeyValuePair<string, string>> allowContentTypes)
        {
            if (allowContentTypes?.Count > 0)
            {
                var contentType = context?.Request?.ContentType;
                if (contentType is null || string.IsNullOrEmpty(contentType))
                    return true;

                for (int i = 0; i < allowContentTypes.Count; ++i)
                {
                    var allowed = allowContentTypes[i];
                    if (contentType.StartsWith(allowed.Key, StringComparison.OrdinalIgnoreCase) && contentType.IndexOf(allowed.Value, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }
    }
}