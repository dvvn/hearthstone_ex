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

		public static void AppendBrackets([NotNull] this StringBuilder buffer, params object[ ] args)
		{
			AppendBrackets(buffer, args.AsEnumerable( ));
		}
	}

	public class CallerInfo
	{
		public readonly string MemberName;
		public readonly string SourceFilePath;
		public readonly int SourceLineNumber;

		public CallerInfo([CallerMemberName] string memberName = "", /*[CallerFilePath]*/ string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = -1)
		{
			MemberName = memberName;
			SourceFilePath = sourceFilePath;
			SourceLineNumber = sourceLineNumber;
		}
	}

	public class CallerInfoMin : CallerInfo
	{
		public CallerInfoMin([CallerMemberName] string memberName = "")
			: base(memberName, null, 0)
		{
		}
	}

	public abstract class LoggerBase
	{
		private readonly string _prefix;

		//public string ApplyFilter(string str) => this.m_filter(str);

		protected LoggerBase([NotNull] Type type, [NotNull] string rootPrefix = "HS_EX", bool fullTypeName = false, [NotNull] params object[ ] extra)
		{
			var buffer = new StringBuilder( );

			buffer.AppendBrackets(rootPrefix);
			var typeName = fullTypeName ? type.FullName : type.Name;
			buffer.AppendBrackets(typeName);
			foreach (var str in extra)
			{
				buffer.Append(' ');
				buffer.Append(str);
			}

			_prefix = buffer.ToString( );
		}

		[NotNull]
		protected string PrepareMessage([CanBeNull] string memberName, string sourceFilePath, int sourceLineNumber, object message)
		{
			var buffer = new StringBuilder(_prefix);

			if (!string.IsNullOrEmpty(memberName) /*&& !member_name.StartsWith(".", StringComparison.OrdinalIgnoreCase)*/)
			{
				if (sourceLineNumber <= 0)
					buffer.AppendBrackets(memberName);
				else
					buffer.AppendBrackets(memberName, ' ', "-", ' ', sourceLineNumber);
			}

			buffer.Append(' ');
			buffer.Append(message);

			return buffer.ToString( );
		}

		protected abstract void ErrorImpl(object msg);
		protected abstract void WarningImpl(object msg);
		protected abstract void MessageImpl(object msg);

		public void Error(object message, [CallerMemberName] string memberName = "", /*[CallerFilePath]*/ string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = -1)
		{
			var msg = PrepareMessage(memberName, sourceFilePath, sourceLineNumber, message);
			ErrorImpl(msg);
		}

		public void Warning(object message, [CallerMemberName] string memberName = "", /*[CallerFilePath]*/ string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = -1)
		{
			var msg = PrepareMessage(memberName, sourceFilePath, sourceLineNumber, message);
			WarningImpl(msg);
		}

		[Conditional("DEBUG")]
		public void Message(object message, [CallerMemberName] string memberName = "", /*[CallerFilePath]*/ string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = -1)
		{
			var msg = PrepareMessage(memberName, sourceFilePath, sourceLineNumber, message);
			MessageImpl(msg);
		}

		[Obsolete]
		public void Error(object message, [NotNull] CallerInfo info) => Error(message, info.MemberName, info.SourceFilePath, info.SourceLineNumber);

		[Obsolete]
		public void Warning(object message, [NotNull] CallerInfo info) => Warning(message, info.MemberName, info.SourceFilePath, info.SourceLineNumber);

		[Obsolete]
		[Conditional("DEBUG")]
		public void Message(object message, [NotNull] CallerInfo info) => Message(message, info.MemberName, info.SourceFilePath, info.SourceLineNumber);

		[Conditional("DEBUG")]
		public void Message(object message, string memberName, int sourceLineNumber) => Message(message, memberName, "", sourceLineNumber);
	}

	public class LoggerFile : LoggerBase
	{
		public class Static<T>
		{
			public static readonly LoggerBase Logger = new LoggerFile(typeof(T));
		}

		public LoggerFile([NotNull] Type type)
			: base(type)
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

		public LoggerGui([NotNull] Type type, [CanBeNull] SceneDebugger window = null)
			: base(type)
		{
			// ReSharper disable once ArrangeConstructorOrDestructorBody
			Window = window ?? DefaultWindow ?? throw new NullReferenceException($"{nameof(DefaultWindow)} is null!");
		}

		protected override void ErrorImpl([NotNull] object msg)
		{
			Window.AddMessage(Log.LogLevel.Error, msg.ToString( ));
		}

		protected override void WarningImpl([NotNull] object msg)
		{
			Window.AddMessage(Log.LogLevel.Warning, msg.ToString( ));
		}

		protected override void MessageImpl([NotNull] object msg)
		{
			Window.AddMessage(Log.LogLevel.Info, msg.ToString( ));
		}
	}
}
