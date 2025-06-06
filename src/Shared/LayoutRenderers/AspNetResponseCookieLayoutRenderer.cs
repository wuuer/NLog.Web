﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !ASP_NET_CORE
using System.Web;
#else
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
#endif

using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Layouts;
using NLog.Web.Enums;
using NLog.Web.Internal;

namespace NLog.Web.LayoutRenderers
{
    /// <summary>
    /// ASP.NET Response Cookie
    /// </summary>
    /// <remarks>
    /// <code>
    /// ${aspnet-response-cookie:OutputFormat=Flat}
    /// ${aspnet-response-cookie:OutputFormat=JsonArray}
    /// ${aspnet-response-cookie:OutputFormat=JsonDictionary}
    /// ${aspnet-response-cookie:OutputFormat=JsonDictionary:Items=username}
    /// ${aspnet-response-cookie:OutputFormat=JsonDictionary:Exclude=access_token}
    /// </code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-Response-Cookie-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-response-cookie")]
    public class AspNetResponseCookieLayoutRenderer : AspNetLayoutMultiValueRendererBase
    {
        /// <summary>
        /// Separator between objects, like cookies. Only used for <see cref="AspNetRequestLayoutOutputFormat.Flat" />
        /// </summary>
        /// <remarks>Render with <see cref="GetRenderedObjectSeparator" /></remarks>
        public string ObjectSeparator { get => _objectSeparatorLayout.OriginalText; set => _objectSeparatorLayout = new SimpleLayout(value ?? ""); }
        private SimpleLayout _objectSeparatorLayout = new SimpleLayout(";");

        /// <summary>
        /// Cookie names to be rendered.
        /// If <c>null</c> or empty array, all cookies will be rendered.
        /// </summary>
        [DefaultParameter]
        public List<string>? Items { get; set; }

        /// <summary>
        /// Cookie names to be rendered.
        /// If <c>null</c> or empty array, all cookies will be rendered.
        /// </summary>
        [Obsolete("Instead use Items-property. Marked obsolete with NLog.Web 5.3")]
        public List<string>? CookieNames { get => Items; set => Items = value; }

        /// <summary>
        /// Render all of the cookie properties, such as Daom and Path, not merely Name and Value
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets or sets the keys to exclude from the output. If omitted, none are excluded.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
#if ASP_NET_CORE
        public ISet<string> Exclude { get; set; }
#else
        public HashSet<string> Exclude { get; set; }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetResponseCookieLayoutRenderer" /> class.
        /// </summary>
        public AspNetResponseCookieLayoutRenderer()
        {
            Exclude = new HashSet<string>(new[] { "AUTH", "SESS_ID" }, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var httpResponse = HttpContextAccessor?.HttpContext.TryGetResponse();
            if (httpResponse is null)
                return;

            var cookies = GetCookies(httpResponse);
            if (cookies.Count > 0)
            {
                if (!Verbose)
                {
                    var cookieValues = GetCookieValues(cookies);
                    SerializePairs(cookieValues, builder, logEvent);
                }
                else
                {
                    var verboseCookieValues = GetVerboseCookieValues(cookies);
                    SerializeAllProperties(verboseCookieValues, builder, logEvent);
                }
            }
        }

        /// <summary>
        /// Get the rendered <see cref="ObjectSeparator" />
        /// </summary>
        private string GetRenderedObjectSeparator(LogEventInfo logEvent)
        {
            return logEvent != null ? _objectSeparatorLayout.Render(logEvent) : ObjectSeparator;
        }

        /// <summary>
        /// Append the quoted name and value separated by a colon
        /// </summary>
        private static bool AppendJsonProperty(StringBuilder builder, string name, string? value, bool includePropertySeparator)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (includePropertySeparator)
                {
                    builder.Append(',');
                }
                AppendQuoted(builder, name);
                builder.Append(':');
                AppendQuoted(builder, value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Append the quoted name and value separated by a value separator
        /// and ended by item separator
        /// </summary>
        private static bool AppendFlatProperty(
            StringBuilder builder,
            string name,
            string? value,
            string valueSeparator,
            string itemSeparator)
        {
            if (!string.IsNullOrEmpty(value))
            {
                builder.Append(itemSeparator);
                builder.Append(name);
                builder.Append(valueSeparator);
                builder.Append(value);
                return true;
            }
            return false;
        }

#if !ASP_NET_CORE

        /// <summary>
        /// Method to get cookies for .NET Framework
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static HttpCookieCollection GetCookies(HttpResponseBase response)
        {
            return response.Cookies;
        }

        private IEnumerable<KeyValuePair<string, string?>> GetCookieValues(HttpCookieCollection cookies)
        {
            var expandMultiValue = OutputFormat != AspNetRequestLayoutOutputFormat.Flat;
            return HttpCookieCollectionValues.GetCookieValues(cookies, Items, Exclude, expandMultiValue);
        }

        private IEnumerable<HttpCookie> GetVerboseCookieValues(HttpCookieCollection cookies)
        {
            var expandMultiValue = OutputFormat != AspNetRequestLayoutOutputFormat.Flat;
            return HttpCookieCollectionValues.GetVerboseCookieValues(cookies, Items, Exclude, expandMultiValue);
        }

        private void SerializeAllProperties(IEnumerable<HttpCookie> verboseCookieValues, StringBuilder builder, LogEventInfo logEvent)
        {
            switch (OutputFormat)
            {
                case AspNetRequestLayoutOutputFormat.Flat:
                    SerializeAllPropertiesFlat(verboseCookieValues, builder, logEvent);
                    break;
                case AspNetRequestLayoutOutputFormat.JsonArray:
                case AspNetRequestLayoutOutputFormat.JsonDictionary:
                    SerializeAllPropertiesJson(verboseCookieValues, builder);
                    break;
            }
        }

        private void SerializeAllPropertiesJson(IEnumerable<HttpCookie> verboseCookieValues, StringBuilder builder)
        {
            var firstItem = true;
            var includeSeparator = false;

            foreach (var cookie in verboseCookieValues)
            {
                if (firstItem)
                {
                    if (OutputFormat == AspNetRequestLayoutOutputFormat.JsonDictionary)
                    {
                        builder.Append('{');
                    }
                    else
                    {
                        builder.Append('[');
                    }
                }
                else
                {
                    builder.Append(',');
                }

                builder.Append('{');

                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Name), cookie.Name, false);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Value), cookie.Value, includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Domain), cookie.Domain, includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Path), cookie.Path, includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Expires), cookie.Expires.ToUniversalTime().ToString("u"), includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Secure), cookie.Secure.ToString(), includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.HttpOnly), cookie.HttpOnly.ToString(), includeSeparator);

                builder.Append('}');

                firstItem = false;
            }

            if (!firstItem)
            {
                if (OutputFormat == AspNetRequestLayoutOutputFormat.JsonDictionary)
                {
                    builder.Append('}');
                }
                else
                {
                    builder.Append(']');
                }
            }
        }

        private void SerializeAllPropertiesFlat(IEnumerable<HttpCookie> verboseCookieValues, StringBuilder builder, LogEventInfo logEvent)
        {
            var propertySeparator = GetRenderedItemSeparator(logEvent);
            var valueSeparator = GetRenderedValueSeparator(logEvent);
            var objectSeparator = GetRenderedObjectSeparator(logEvent);

            var firstObject = true;
            var includeSeparator = false;
            foreach (var cookie in verboseCookieValues)
            {
                if (!firstObject)
                {
                    builder.Append(objectSeparator);
                }
                firstObject = false;

                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Name),     cookie.Name,   valueSeparator, "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Value),    cookie.Value,  valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Domain),   cookie.Domain, valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Path),     cookie.Path,   valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Expires),  cookie.Expires.ToUniversalTime().ToString("u"), valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Secure),   cookie.Secure.ToString(),   valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.HttpOnly), cookie.HttpOnly.ToString(), valueSeparator, includeSeparator ? propertySeparator : "");
            }
        }

#else
        /// <summary>
        /// Method to get cookies for all ASP.NET Core versions
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static IList<SetCookieHeaderValue> GetCookies(HttpResponse response)
        {
            var queryResults = response.Headers[HeaderNames.SetCookie];
            if (queryResults.Count > 0 && SetCookieHeaderValue.TryParseList(queryResults, out var result))
                return result;
            else
                return Array.Empty<SetCookieHeaderValue>();
        }

        private IEnumerable<KeyValuePair<string, string?>> GetCookieValues(IList<SetCookieHeaderValue> cookies)
        {
            if (Items?.Count > 0)
            {
                return GetCookieNameValues(cookies, Items);
            }
            else
            {
                return GetCookieAllValues(cookies, Exclude);
            }
        }

        private IEnumerable<SetCookieHeaderValue> GetVerboseCookieValues(IList<SetCookieHeaderValue> cookies)
        {
            if (Items?.Count > 0)
            {
                return GetCookieVerboseValues(cookies, Items);
            }
            else
            {
                return GetCookieVerboseAllValues(cookies, Exclude);
            }
        }

        private static IEnumerable<KeyValuePair<string, string?>> GetCookieNameValues(IList<SetCookieHeaderValue> cookies, List<string> cookieNames)
        {
            foreach (var needle in cookieNames)
            {
                for (int i = 0; i < cookies.Count; ++i)
                {
                    var cookie = cookies[i];
                    var cookieName = cookie.Name.ToString();
                    if (string.Equals(needle, cookieName, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return new KeyValuePair<string, string?>(cookieName, cookie.Value.ToString());
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<string, string?>> GetCookieAllValues(IList<SetCookieHeaderValue> cookies, ICollection<string> excludeNames)
        {
            var checkForExclude = excludeNames?.Count > 0 ? excludeNames : null;
            for (int i = 0; i < cookies.Count; ++i)
            {
                var cookie = cookies[i];
                var cookieName = cookie.Name.ToString();
                if (checkForExclude?.Contains(cookieName) == true)
                    continue;

                yield return new KeyValuePair<string, string?>(cookieName, cookie.Value.ToString());
            }
        }

        private static IEnumerable<SetCookieHeaderValue> GetCookieVerboseValues(IList<SetCookieHeaderValue> cookies, List<string> cookieNames)
        {
            foreach (var needle in cookieNames)
            {
                for (int i = 0; i < cookies.Count; ++i)
                {
                    var cookie = cookies[i];
                    var cookieName = cookie.Name.ToString();
                    if (string.Equals(needle, cookieName, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return cookie;
                    }
                }
            }
        }

        private static IEnumerable<SetCookieHeaderValue> GetCookieVerboseAllValues(IList<SetCookieHeaderValue> cookies, ICollection<string> excludeNames)
        {
            var checkForExclude = excludeNames?.Count > 0 ? excludeNames : null;
            for (int i = 0; i < cookies.Count; ++i)
            {
                var cookie = cookies[i];
                var cookieName = cookie.Name.ToString();
                if (checkForExclude?.Contains(cookieName) == true)
                    continue;

                yield return cookie;
            }
        }

        private void SerializeAllProperties(IEnumerable<SetCookieHeaderValue> verboseCookieValues, StringBuilder builder, LogEventInfo logEvent)
        {
            switch (OutputFormat)
            {
                case AspNetRequestLayoutOutputFormat.Flat:
                    SerializeAllPropertiesFlat(verboseCookieValues, builder, logEvent);
                    break;
                case AspNetRequestLayoutOutputFormat.JsonArray:
                case AspNetRequestLayoutOutputFormat.JsonDictionary:
                    SerializeAllPropertiesJson(verboseCookieValues, builder);
                    break;
            }
        }

        private void SerializeAllPropertiesJson(IEnumerable<SetCookieHeaderValue> verboseCookieValues, StringBuilder builder)
        {
            var firstItem = true;
            var includeSeparator = false;

            foreach (var cookie in verboseCookieValues)
            {
                if (firstItem)
                {
                    if (OutputFormat == AspNetRequestLayoutOutputFormat.JsonDictionary)
                    {
                        builder.Append('{');
                    }
                    else
                    {
                        builder.Append('[');
                    }
                }
                else
                {
                    builder.Append(',');
                }

                builder.Append('{');

                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Name),    cookie.Name.ToString(), false);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Value),   cookie.Value.ToString(), includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Domain),  cookie.Domain.ToString(), includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Path),    cookie.Path.ToString(), includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Expires), cookie.Expires?.ToUniversalTime().ToString("u"), includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.Secure),  cookie.Secure.ToString(), includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.HttpOnly), cookie.HttpOnly.ToString(), includeSeparator);
                includeSeparator |= AppendJsonProperty(builder, nameof(cookie.SameSite), cookie.SameSite.ToString(), includeSeparator);

                builder.Append('}');

                firstItem = false;
            }

            if (!firstItem)
            {
                if (OutputFormat == AspNetRequestLayoutOutputFormat.JsonDictionary)
                {
                    builder.Append('}');
                }
                else
                {
                    builder.Append(']');
                }
            }
        }

        private void SerializeAllPropertiesFlat(IEnumerable<SetCookieHeaderValue> verboseCookieValues, StringBuilder builder, LogEventInfo logEvent)
        {
            var propertySeparator = GetRenderedItemSeparator(logEvent);
            var valueSeparator = GetRenderedValueSeparator(logEvent);
            var objectSeparator = GetRenderedObjectSeparator(logEvent);

            var firstObject = true;
            var includeSeparator = false;
            foreach (var cookie in verboseCookieValues)
            {
                if (!firstObject)
                {
                    builder.Append(objectSeparator);
                }
                firstObject = false;

                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Name),     cookie.Name.ToString(), valueSeparator, "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Value),    cookie.Value.ToString(),   valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Domain),   cookie.Domain.ToString(),  valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Path),     cookie.Path.ToString(),    valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Expires),  cookie.Expires?.ToUniversalTime().ToString("u"), valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.Secure),   cookie.Secure.ToString(),   valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.HttpOnly), cookie.HttpOnly.ToString(), valueSeparator, includeSeparator ? propertySeparator : "");
                includeSeparator |= AppendFlatProperty(builder, nameof(cookie.SameSite), cookie.SameSite.ToString(), valueSeparator, includeSeparator ? propertySeparator : "");
            }
        }
#endif
    }
}