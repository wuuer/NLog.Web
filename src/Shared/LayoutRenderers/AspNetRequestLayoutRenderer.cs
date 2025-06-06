using System.Text;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Web.Internal;
#if !ASP_NET_CORE
using System.Web;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
#endif

namespace NLog.Web.LayoutRenderers
{
    /// <summary>
    /// ASP.NET Request Context variable.
    /// </summary>
    /// <remarks>
    /// <code>
    /// ${aspnet-request:item=v}
    /// ${aspnet-request:querystring=v}
    /// ${aspnet-request:form=v}
    /// ${aspnet-request:cookie=v}
    /// ${aspnet-request:header=h}
    /// ${aspnet-request:serverVariable=v}
    /// </code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNetRequest-layout-renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-request")]
    public class AspNetRequestLayoutRenderer : AspNetLayoutRendererBase
    {
        /// <summary>
        /// Gets or sets the HttpContext Item to be rendered.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        [DefaultParameter]
        public string? Item { get; set; }

        /// <summary>
        /// Gets or sets the QueryString variable to be rendered.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        public string? QueryString { get; set; }

        /// <summary>
        /// Gets or sets the form variable to be rendered.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        public string? Form { get; set; }

        /// <summary>
        /// Gets or sets the cookie to be rendered.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        public string? Cookie { get; set; }

#if !ASP_NET_CORE || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets or sets the ServerVariables item to be rendered.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        public string? ServerVariable { get; set; }
#endif

        /// <summary>
        /// Gets or sets the Headers item to be rendered.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        public string? Header { get; set; }

        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var httpRequest = HttpContextAccessor?.HttpContext.TryGetRequest();
            if (httpRequest is null)
                return;

            var value = string.Empty;
            if (QueryString != null)
            {
                value = LookupQueryString(QueryString, httpRequest);
            }
            else if (Form != null)
            {
                value = LookupFormValue(Form, httpRequest);
            }
            else if (Cookie != null)
            {
                value = LookupCookieValue(Cookie, httpRequest);
            }
#if !ASP_NET_CORE || NETCOREAPP3_0_OR_GREATER
            else if (ServerVariable != null)
            {
                value = LookupServerVariableValue(ServerVariable, httpRequest);
            }
#endif
            else if (Header != null)
            {
                value = LookupHeaderValue(Header, httpRequest);
            }
            else if (Item != null)
            {
                value = LookupItemValue(Item, httpRequest);
            }

            builder.Append(value);
        }

#if !ASP_NET_CORE
        private static string? LookupQueryString(string key, HttpRequestBase httpRequest)
        {
            var collection = httpRequest.QueryString;
            return collection?.Count > 0 ? collection[key] : null;
        }

        private static string? LookupFormValue(string key, HttpRequestBase httpRequest)
        {
            var collection = httpRequest.Form;
            return collection?.Count > 0 ? collection[key] : null;
        }

        private static string? LookupCookieValue(string key, HttpRequestBase httpRequest)
        {
            var cookieCollection = httpRequest.Cookies;
            return cookieCollection?.Count > 0 ? cookieCollection[key]?.Value : null;
        }

        private static string? LookupHeaderValue(string key, HttpRequestBase httpRequest)
        {
            var collection = httpRequest.Headers;
            return collection?.Count > 0 ? collection[key] : null;
        }

        private static string? LookupItemValue(string key, HttpRequestBase httpRequest)
        {
            return httpRequest[key];
        }

        private static string? LookupServerVariableValue(string key, HttpRequestBase httpRequest)
        {
            var collection = httpRequest.ServerVariables;
            return collection?.Count > 0 ? collection[key] : null;
        }
#else
        private static string? LookupQueryString(string key, HttpRequest httpRequest)
        {
            var query = httpRequest.Query;
            if (query != null && query.TryGetValue(key, out var queryValue))
            {
                return queryValue.ToString();
            }

            return null;
        }

        private static string? LookupFormValue(string key, HttpRequest httpRequest)
        {
            if (httpRequest.HasFormContentType)
            {
                var form = httpRequest.Form;
                if (form != null && form.TryGetValue(key, out var queryValue))
                {
                    return queryValue.ToString();
                }
            }

            return null;
        }

        private static string? LookupCookieValue(string key, HttpRequest httpRequest)
        {
            if (httpRequest.Cookies?.TryGetValue(key, out var cookieValue) ?? false)
            {
                return cookieValue;
            }

            return null;
        }

        private static string? LookupHeaderValue(string key, HttpRequest httpRequest)
        {
            var headers = httpRequest.Headers;
            if (headers != null && headers.TryGetValue(key, out var headerValue))
            {
                return headerValue.ToString();
            }

            return null;
        }

        private static string? LookupItemValue(string key, HttpRequest httpRequest)
        {
            if (httpRequest.HttpContext.Items?.TryGetValue(key, out var itemValue) ?? false)
            {
                return itemValue?.ToString();
            }

            return null;
        }
#endif

#if NETCOREAPP3_0_OR_GREATER
        private static string? LookupServerVariableValue(string key, HttpRequest httpRequest)
        {
            return httpRequest?.HttpContext.TryGetFeature<IServerVariablesFeature>()?[key];
        }
#endif
    }
}