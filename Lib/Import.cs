using System.Runtime.InteropServices;
using System;

internal static class Import
{
	[DllImport("User32.dll", CharSet = CharSet.Unicode)]
	public static extern int MessageBox(IntPtr hwnd, string text, string caption, int type);

	[DllImport("kernel32")]
	public static extern bool AllocConsole( );

	[DllImport("kernel32")]
	public static extern bool FreeConsole( );

	[DllImport("kernel32")]
	public static extern void Sleep(int msecs);
}
