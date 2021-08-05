using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Debug = UnityEngine.Debug;

//-
//using _CallerMemberName = System.Runtime.CompilerServices.CallerMemberNameAttribute;
//using _CallerFilePath = System.Runtime.CompilerServices.CallerFilePathAttribute;
//using _CallerLineNumber = System.Runtime.CompilerServices.CallerLineNumberAttribute;

#if false
namespace System.Runtime.CompilerServices
{
    // ReSharper disable UnusedMember.Global

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class CallerMemberNameAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class CallerFilePathAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class CallerLineNumberAttribute : Attribute { }

    // ReSharper restore UnusedMember.Global
}
#endif

namespace hearthstone_ex.Utils
{
    internal static class LoggerTools
    {
        public static void AppendBrackets([NotNull] this StringBuilder buffer, [NotNull] IEnumerable<object> args)
        {
            buffer.Append('[');
            foreach (var a in args)
                buffer.Append(a);
            buffer.Append(']');
        }

        public static void AppendBrackets([NotNull] this StringBuilder buffer, params object[] args)
        {
            AppendBrackets(buffer, args.AsEnumerable());
        }
    }

    public class CallerInfo
    {
        public readonly string MemberName;
        public readonly string SourceFilePath;
        public readonly int? SourceLineNumber;

        public CallerInfo([CallerMemberName] string member_name = "", /*[CallerFilePath]*/ string source_file_path = "", [CallerLineNumber] int? source_line_number = 0)
        {
            this.MemberName = member_name;
            this.SourceFilePath = source_file_path;
            this.SourceLineNumber = source_line_number;
        }
    }

    public class CallerInfoMin : CallerInfo
    {
        public CallerInfoMin([CallerMemberName] string member_name = "") : base(member_name, null, null)
        {
        }
    }

    public abstract class LoggerBase
    {
        private readonly string m_prefix;

        private const string m_space = " ";

        //public string ApplyFilter(string str) => this.m_filter(str);

        protected LoggerBase([NotNull] Type type, [NotNull] string root_prefix = "HS_EX", bool full_type_name = false,
                             [NotNull] params object[] extra)
        {
            var buffer = new StringBuilder();

            buffer.AppendBrackets(root_prefix);
            var type_name = full_type_name ? type.FullName : type.Name;
            buffer.AppendBrackets((type_name));
            foreach (var str in extra)
            {
                buffer.Append(m_space);
                buffer.Append(str);
            }

            this.m_prefix = buffer.ToString();
        }

        [NotNull]
        protected string PrepareMessage([CanBeNull] string member_name, string source_file_path, int? source_line_number, object message)
        {
            var buffer = new StringBuilder(this.m_prefix);

            if (!string.IsNullOrEmpty(member_name) /*&& !member_name.StartsWith(".", StringComparison.OrdinalIgnoreCase)*/)
            {
                if (!source_line_number.HasValue || source_line_number <= 0)
                    buffer.AppendBrackets(member_name);
                else
                    buffer.AppendBrackets(member_name, m_space, "-", m_space, source_line_number);
            }

            buffer.Append(m_space);
            buffer.Append(message);

            return buffer.ToString();
        }

        protected abstract void ErrorImpl(object msg);
        protected abstract void WarningImpl(object msg);
        protected abstract void MessageImpl(object msg);

        public void Error(object message,
                          [CallerMemberName] string member_name = "", /*[CallerFilePath]*/ string source_file_path = "", [CallerLineNumber] int? source_line_number = 0)
        {
            var msg = this.PrepareMessage(member_name, source_file_path, source_line_number, message);
            this.ErrorImpl(msg);
        }

        public void Warning(object message,
                            [CallerMemberName] string member_name = "", /*[CallerFilePath]*/ string source_file_path = "", [CallerLineNumber] int? source_line_number = 0)
        {
            var msg = this.PrepareMessage(member_name, source_file_path, source_line_number, message);
            this.WarningImpl(msg);
        }

        [Conditional("DEBUG")]
        public void Message(object message,
                            [CallerMemberName] string member_name = "", /*[CallerFilePath]*/ string source_file_path = "", [CallerLineNumber] int? source_line_number = 0)
        {
            var msg = this.PrepareMessage(member_name, source_file_path, source_line_number, message);
            this.MessageImpl(msg);
        }

        public void Error(object message, [NotNull] CallerInfo info) => this.Error(message, info.MemberName, info.SourceFilePath, info.SourceLineNumber);

        public void Warning(object message, [NotNull] CallerInfo info) => this.Warning(message, info.MemberName, info.SourceFilePath, info.SourceLineNumber);

        [Conditional("DEBUG")]
        public void Message(object message, [NotNull] CallerInfo info) => this.Message(message, info.MemberName, info.SourceFilePath, info.SourceLineNumber);
    }

    public class LoggerFile : LoggerBase
    {
        public class Static<T>
        {
            public static readonly LoggerBase Logger = new LoggerFile(typeof(T));
        }

        public LoggerFile([NotNull] Type type) : base(type)
        {
        }

        protected override void ErrorImpl(object msg)
        {
            Debug.LogError(msg);
        }

        protected override void WarningImpl(object msg)
        {
            Debug.LogWarning(msg);
        }

        protected override void MessageImpl(object msg)
        {
            Debug.Log(msg);
        }
    }

    public class LoggerGui : LoggerBase
    {
        public class Static<T>
        {
            public static readonly LoggerBase Logger = new LoggerGui(typeof(T));
        }

        protected readonly SceneDebugger Window;
        public static SceneDebugger DefaultWindow { get; private set; }

        public static void SetDefaultWindow(SceneDebugger wnd)
        {
            if (DefaultWindow != null)
                throw new FieldAccessException($"{nameof(DefaultWindow)} already initialized!");
            DefaultWindow = wnd;
        }

        public LoggerGui([NotNull] Type type, [CanBeNull] SceneDebugger window = null) : base(type)
        {
            // ReSharper disable once ArrangeConstructorOrDestructorBody
            this.Window = window ?? DefaultWindow ?? throw new NullReferenceException($"{nameof(DefaultWindow)} is null!");
        }

        protected override void ErrorImpl([NotNull] object msg)
        {
            this.Window.AddMessage(Log.LogLevel.Error, msg.ToString());
        }

        protected override void WarningImpl([NotNull] object msg)
        {
            this.Window.AddMessage(Log.LogLevel.Warning, msg.ToString());
        }

        protected override void MessageImpl([NotNull] object msg)
        {
            this.Window.AddMessage(Log.LogLevel.Info, msg.ToString());
        }
    }
}
