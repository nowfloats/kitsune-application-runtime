using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper
{
    public sealed class EventLogger
    {
        const string EventLogName = "MovingFloats";
        const int ErrorNumber = 999;
        static readonly Dictionary<Type, EventLogExceptionFormatter> ExceptionFormatters = new Dictionary<Type, EventLogExceptionFormatter>();
        static readonly EventLogExceptionFormatter DefaultFormatter = new EventLogExceptionFormatter();
        static readonly EventLog PSXEventLog = InitializeLog();

        private EventLogger() { }

        private static EventLog InitializeLog()
        {
            try
            {
                EventLog log = new EventLog();
                log.Source = EventLogName;
                return log;
            }
            catch
            {
                return null;
            }
        }

        public static void Write(Exception ex, string message, params object[] args)
        {
            Write(ex, null, message, args);
        }

        public static void Write(TraceLevel logType, string message, params object[] args)
        {
            Write(logType, null, message, args);
        }

        public static void Write(Exception ex, int? id, string message, params object[] args)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(message);

                while (ex != null)
                {
                    EventLogExceptionFormatter formatter = null;
                    if (!ExceptionFormatters.TryGetValue(ex.GetType(), out formatter))
                        formatter = DefaultFormatter;

                    sb.AppendLine(formatter.FormatException(ex));

                    ex = ex.InnerException;
                }

                Write(TraceLevel.Error, id, sb.ToString(), args);
            }
            catch { }
        }

        private static void Write(TraceLevel logType, int? id, string message, params object[] args)
        {
            try
            {
                EventLogEntryType eventLogType = EventLogEntryType.Error;

                if (message != null && args != null)
                {
                    if (args.Length > 0)
                        message = StringExtensions.Format(message, args);
                }

                if (id.HasValue)
                    PSXEventLog.WriteEntry(message, eventLogType, id.Value);
                else
                    PSXEventLog.WriteEntry(message, eventLogType, ErrorNumber);
            }
            catch
            { }
        }

    }

    /// <summary>
    /// LogExceptionFormatter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventLogExceptionFormatter
    {
        public virtual string FormatException(Exception exception)
        {
            try
            {
                return FormatException(exception.GetType().FullName, exception.Message, exception.StackTrace);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string FormatException(string name, string message, string stackTrace)
        {
            try
            {
                string template = "Exception Type: {0}; \nException Message: {1}; \nStack Trace: {2}\n";
                return StringExtensions.Format(template, name, message, stackTrace);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public static class StringExtensions
    {

        /// <summary>
        /// Formats the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public static string Format(this string template, params object[] args)
        {
            try
            {
                return Format(template, System.Globalization.CultureInfo.InvariantCulture, args);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Formats the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public static string Format(this string template, System.Globalization.CultureInfo culture, params object[] args)
        {
            try
            {
                return args == null || args.Length == 0 ? template : String.Format(culture, template, args);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
