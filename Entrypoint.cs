using Microsoft.Win32;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Runtime.InteropServices;
using Mono.Security.Protocol.Ntlm;

// ReSharper disable once CheckNamespace
namespace Doorstop
{
	// ReSharper disable once UnusedMember.Global
	class Entrypoint
	{
		private static IEnumerable<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
		{
			return
				assembly.GetTypes()
					.Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal));
		}

		[DllImport("User32.dll", CharSet = CharSet.Unicode)]
		public static extern int MessageBox(IntPtr h, string m, string c, int type);

		// ReSharper disable once UnusedMember.Global
		public static void Start()
		{
			try
			{
				AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) => { MessageBox((IntPtr)0, eventArgs.Exception.ToString(), "text", 0); };
				AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => { MessageBox((IntPtr)0, eventArgs.ExceptionObject.ToString(), "text", 0); };
				hearthstone_ex.Loader.Start();
			}
			catch (Exception e)
			{
				MessageBox((IntPtr)0, e.ToString(), "text", 0);
			}
		}
	}
}