using Microsoft.Win32;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Runtime.InteropServices;

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
				var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var a = Assembly.LoadFrom(Path.Combine(dir, "hearthstone_ex.dll"));
				
				var loader = GetTypesInNamespace(a, "hearthstone_ex").First(t => t.Name == "Loader");
				var fn = loader.GetMethod("Start");
				fn.Invoke(null, null);
				File.WriteAllText("ok.txt", "started");
			}
			catch (Exception e)
			{
				File.WriteAllText("fuck.txt", e.ToString());
			}
		}
	}
}