﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NLog.Config;
using NLog.LayoutRenderers;

namespace NLog.Web.LayoutRenderers
{
    /// <summary>
    /// ASP.NET Request Duration
    /// </summary>
    /// <remarks>
    /// <code>${aspnet-request-duration}</code>
    /// </remarks>
    /// <seealso href="https://github.com/NLog/NLog/wiki/AspNet-Request-Duration-Layout-Renderer">Documentation on NLog Wiki</seealso>
    [LayoutRenderer("aspnet-request-duration")]
    public class AspNetRequestDurationLayoutRenderer : AspNetLayoutRendererBase
    {
        private static string[]? DurationMsFormat = null;
#if !ASP_NET_CORE
        private string? _formatString;
#elif !NETCOREAPP3_0_OR_GREATER
        private NLog.Layouts.SimpleLayout? _scopeTiming;
#endif

        /// <summary>
        /// When no format specified, then just total milliseconds
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets the culture used for rendering. 
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        public CultureInfo? Culture { get; set; } = CultureInfo.InvariantCulture;

        /// <inheritdoc/>
        protected override void InitializeLayoutRenderer()
        {
            if (DurationMsFormat is null && string.IsNullOrEmpty(Format) && ReferenceEquals(Culture, CultureInfo.InvariantCulture))
            {
                System.Threading.Interlocked.CompareExchange(ref DurationMsFormat, Enumerable.Range(0, 1000).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray(), null);
            }

#if !ASP_NET_CORE
            if (!string.IsNullOrEmpty(Format))
            {
                _formatString = "{0:" + Format + "}";
            }
#elif !NETCOREAPP3_0_OR_GREATER
            if (string.IsNullOrEmpty(Format) && ReferenceEquals(Culture, CultureInfo.InvariantCulture))
                _scopeTiming = new NLog.Layouts.SimpleLayout("${scopetiming}");
            else if (ReferenceEquals(Culture, CultureInfo.InvariantCulture))
                _scopeTiming = new NLog.Layouts.SimpleLayout($"${{scopetiming:Format={Format}}}");
            else
                _scopeTiming = new NLog.Layouts.SimpleLayout($"${{scopetiming:Format={Format}:Culture={Culture}}}");
#endif

            base.InitializeLayoutRenderer();
        }

        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var httpContext = HttpContextAccessor?.HttpContext;
            if (httpContext is null)
                return;

#if ASP_NET_CORE && !NETCOREAPP3_0_OR_GREATER
            _scopeTiming?.Render(logEvent, builder);
#else

#if !ASP_NET_CORE
            var duration = GetDuration(httpContext.Timestamp);
#else
            var duration = GetDuration(System.Diagnostics.Activity.Current);
#endif
            if (duration.HasValue)
            {
                if (string.IsNullOrEmpty(Format))
                {
                    RenderDurationMs(builder, duration.Value.TotalMilliseconds);
                }
                else
                {
                    builder.Append(FormatDuration(duration.Value));
                }
            }
#endif
        }

        private void RenderDurationMs(StringBuilder builder, double durationMs)
        {
            if (ReferenceEquals(Culture, CultureInfo.InvariantCulture))
            {
                var truncateMs = (long)durationMs;
                if (DurationMsFormat != null && truncateMs >= 0 && truncateMs < DurationMsFormat.Length)
                {
                    builder.Append(DurationMsFormat[truncateMs]);
                }
                else
                {
                    builder.Append(truncateMs);
                }
                var preciseMs = (int)((durationMs - truncateMs) * 1000.0);
                if (preciseMs > 0)
                {
                    builder.Append('.');
                    if (preciseMs < 100)
                        builder.Append('0');
                    if (preciseMs < 10)
                        builder.Append('0');

                    if (DurationMsFormat != null && preciseMs < DurationMsFormat.Length)
                    {
                        builder.Append(DurationMsFormat[preciseMs]);
                    }
                    else
                    {
                        builder.Append(preciseMs);
                    }
                }
                else
                {
                    builder.Append(".0");
                }
            }
            else
            {
                builder.Append(durationMs.ToString("0.###", Culture));
            }
        }

        string FormatDuration(TimeSpan duration)
        {
#if !ASP_NET_CORE
            if (string.IsNullOrEmpty(_formatString))
                return duration.ToString();
            else
                return string.Format(_formatString, duration);
#else
            return duration.ToString(Format, Culture);
#endif
        }

#if !ASP_NET_CORE
        private static TimeSpan? GetDuration(DateTime contextTimestamp)
        {
            if (contextTimestamp > DateTime.MinValue)
            {
                if (contextTimestamp.Kind == DateTimeKind.Local)
                    return DateTime.Now - contextTimestamp;
                else if (contextTimestamp.Kind == DateTimeKind.Unspecified)
                    return DateTime.UtcNow - new DateTime(contextTimestamp.Ticks, DateTimeKind.Utc);
                else
                    return DateTime.UtcNow - contextTimestamp.ToUniversalTime();
            }
            else
            {
                return default(TimeSpan?);
            }
        }
#endif

#if ASP_NET_CORE && NETCOREAPP3_0_OR_GREATER
        private static TimeSpan? GetDuration(System.Diagnostics.Activity? activity)
        {
            var startTimeUtc = activity?.StartTimeUtc;

            var parent = activity?.Parent;
            while (parent != null)
            {
                activity = parent;
                parent = activity.Parent;

                if (activity.StartTimeUtc > DateTime.MinValue)
                    startTimeUtc = activity.StartTimeUtc;
            }

            if (startTimeUtc > DateTime.MinValue && activity != null)
            {
                var duration = activity.Duration;
                if (duration == TimeSpan.Zero)
                {
                    // Not ended yet
                    var endTimeUtc = DateTime.UtcNow;
                    duration = endTimeUtc - startTimeUtc.Value;
                    if (duration < TimeSpan.Zero)
                        duration = TimeSpan.FromTicks(1);
                }

                return duration;
            }

            return default(TimeSpan?);
        }
#endif
    }
}
