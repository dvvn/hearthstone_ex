using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Doorstop
{
	// ReSharper disable once UnusedMember.Global
	internal static class Entrypoint
	{
		private static IEnumerable<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
		{
			return assembly.GetTypes( ).Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal));
		}

		// ReSharper disable once UnusedMember.Global
		public static void Start( )
		{
			try
			{
				AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
				{
					Import.MessageBox((IntPtr)0, eventArgs.Exception.ToString( ), "Exception", 0);
				};
				AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
				{
					Import.MessageBox((IntPtr)0, eventArgs.ExceptionObject.ToString( ), "Unhandled exception", 0);
				};
				hearthstone_ex.Loader.Start( );
			}
			catch (Exception e)
			{
				Import.MessageBox((IntPtr)0, e.ToString( ), "Error", 0);
			}
		}
	}
}
