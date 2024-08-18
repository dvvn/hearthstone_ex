using System.Diagnostics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Versioning;
using Installer.Helpers;
using Microsoft.Win32;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

namespace Installer;

[Flags]
internal enum ArchitectureType
{
	X86 = 1 << 0
  , X64 = 1 << 1
  , AnyCpu = X86 | X64
}

internal static class Utils
{
	public static FileInfo FindInParentDirectory(DirectoryInfo dir, ReadOnlySpan<char> fileName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			foreach (var file in dir.EnumerateFiles( ))
			{
				if (fileName.SequenceEqual(file.Name))
				{
					return file;
				}
			}
		}

		throw new FileNotFoundException($"Unable to find {fileName}!");
	}

	public static SimpleFileInfo FindInParentDirectory(SimpleDirectoryInfo dir, ReadOnlySpan<char> fileName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			foreach (var file in dir.EnumerateFiles( ))
			{
				if (fileName.SequenceEqual(file.Name))
				{
					return file;
				}
			}
		}

		throw new FileNotFoundException($"Unable to find {fileName}!");
	}

	public static SimpleFileInfo FindInParentDirectory(ReadOnlySpan<char> dir, ReadOnlySpan<char> fileName)
	{
		for (var tmpDir = Path.TrimEndingDirectorySeparator(dir); tmpDir != null; tmpDir = Path.GetDirectoryName(tmpDir))
		{
			foreach (var file in Directory.EnumerateFiles(tmpDir.ToString( )))
			{
				if (Path.GetFileName(file.AsSpan( )).SequenceEqual(fileName))
				{
					return new(file, fileName);
				}
			}
		}

		throw new FileNotFoundException($"Unable to find {fileName}!");
	}

	public static DirectoryInfo FindParentDirectory(DirectoryInfo dir, ReadOnlySpan<char> directoryName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			if (directoryName.SequenceEqual(dir.Name))
				return dir;
		}

		throw new FileNotFoundException($"Unable to directory ${directoryName}!");
	}

	public static SimpleDirectoryInfo FindParentDirectory(SimpleDirectoryInfo dir, ReadOnlySpan<char> directoryName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			if (directoryName.SequenceEqual(dir.Name))
				return dir;
		}

		throw new FileNotFoundException($"Unable to directory ${directoryName}!");
	}

	public static ReadOnlySpan<char> FindParentDirectory(ReadOnlySpan<char> dir, ReadOnlySpan<char> directoryName)
	{
		for (var tmpDir = Path.TrimEndingDirectorySeparator(dir); tmpDir != null; tmpDir = Path.GetDirectoryName(tmpDir))
		{
			if (Path.GetFileName(tmpDir).SequenceEqual(directoryName))
				return tmpDir;
		}

		throw new FileNotFoundException($"Unable to directory ${directoryName}!");
	}

	/*public static string GetWorkingDirectory( )
	{
		var selfPath = Assembly.GetExecutingAssembly( ).Location;
		return Path.GetDirectoryName(selfPath);
	}*/

	public static ReadOnlySpan<char> GetWorkingDirectory( )
	{
		var selfPath = Assembly.GetExecutingAssembly( ).Location;
		return Path.GetDirectoryName(selfPath.AsSpan( ));
	}

	private static IEnumerable<RegistryKey> EnumerateUninstallDirectories(string applicationName)
	{
		using (var root = Registry.LocalMachine.OpenSubKey(Path.Combine(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", applicationName)))
		{
			if (root != null)
				yield return root;
		}

		using (var root = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
		{
			Debug.Assert(root != null);
			foreach (var subkeyName in root.GetSubKeyNames( ))
			{
				using var key = root.OpenSubKey(subkeyName);
				if (key == null)
					continue;
				if (key.GetValue("DisplayName") is not string name)
					continue;
				if (!name.AsSpan( ).Contains(applicationName, StringComparison.Ordinal))
					continue;
				yield return key;
			}
		}
	}

	public static bool IsSoftwareInstalled(string applicationName)
	{
		return EnumerateUninstallDirectories(applicationName).Any( );
	}

	public static ReadOnlySpan<char> TryGetInstallDirectory(string applicationName)
	{
		foreach (var key in EnumerateUninstallDirectories(applicationName))
		{
			if (key.GetValue("InstallSource") is string installSource)
				return installSource;
			if (key.GetValue("InstallLocation") is string installLocation)
				return installLocation;
			if (key.GetValue("UninstallString") is string uninstallString)
				return Path.GetDirectoryName(uninstallString.AsSpan( ));
		}

		return null;
	}

	public static ReadOnlySpan<char> GetInstallDirectory(string applicationName)
	{
		var result = TryGetInstallDirectory(applicationName);
		if (result == null)
			throw new FileNotFoundException($"Unable to find install directory for {applicationName}");
		return result;
	}

	public static ArchitectureType GetFileArchitecture(string filePath)
	{
		using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
		using var reader = new PEReader(stream);

		var corHeader = reader.PEHeaders.CorHeader;
		if (corHeader != null) //managed
		{
			var flags = corHeader.Flags;

			var isILOnly = (flags & CorFlags.ILOnly) != 0;
			var requires32Bit = (flags & CorFlags.Requires32Bit) != 0;

			if (isILOnly && !requires32Bit)
				return ArchitectureType.AnyCpu;
			if (requires32Bit)
				return ArchitectureType.X86;
		}

		var peHeader = reader.PEHeaders.PEHeader;
		if (peHeader != null) //native
		{
			switch (peHeader.Magic)
			{
				case PEMagic.PE32:
					return ArchitectureType.X86;
				case PEMagic.PE32Plus:
					return ArchitectureType.X64;
			}
		}

		switch (reader.PEHeaders.CoffHeader.Machine)
		{
			case Machine.I386:
				return ArchitectureType.X86;
			case Machine.Amd64:
				return ArchitectureType.X64;
			//case Machine.Arm:
			//	return "ARM";
			//case Machine.Arm64:
			//	return "ARM64";
		}

		throw new InvalidOperationException("Unable to determine the architecture of the file.");
	}

	public static ArchitectureType GetFileArchitecture(ReadOnlySpan<char> filePath)
	{
		return GetFileArchitecture(filePath.ToString( ));
	}

	public static Version TryGetDotNetFrameworkVersion(string filePath)
	{
		var assembly = AssemblyDefinition.ReadAssembly(filePath);

		foreach (var attribute in assembly.CustomAttributes)
		{
			if (attribute.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName)
			{
				var rawVersion = attribute.ConstructorArguments[0].Value.ToString( );
				var offset = rawVersion.TakeWhile(c => !char.IsAsciiDigit(c)).Count( );
				return Version.Parse(rawVersion.AsSpan(offset));
			}
		}

		foreach (var reference in assembly.MainModule.AssemblyReferences)
		{
			if (reference.Name is "mscorlib" or "System")
			{
				return reference.Version;
			}
		}

		return null;
	}

	public static Version GetDotNetFrameworkVersion(string filePath)
	{
		return TryGetDotNetFrameworkVersion(filePath) ?? throw new InvalidOperationException("Unable to determine the .NET Framework version.");
	}

	public static Version GetDotNetFrameworkVersion(ReadOnlySpan<char> filePath)
	{
		return GetDotNetFrameworkVersion(filePath.ToString( ));
	}
}

internal static class PathEx
{
	private class CombineHelper(int bufferLength, int partsCount)
	{
		private class ValidatorDebug(int partsCount)
		{
			private int _partsAdded;
			private readonly string[ ] _parts = new string[partsCount];

			public void StorePart(ReadOnlySpan<char> path)
			{
				_parts[_partsAdded] = path.ToString( );
				++_partsAdded;
			}

			public string ToString(char[ ] buffer)
			{
				var result = Path.Combine(_parts);
				if (!result.SequenceEqual(buffer))
					throw new InvalidOperationException("The combined path does not match the expected buffer.");
				return result;
			}
		}

		private class Validator
		{
			public static void StorePart(ReadOnlySpan<char> path)
			{
			}

			public static string ToString(char[ ] buffer)
			{
				return new(buffer);
			}
		}

		private readonly char[ ] _buffer = new char[bufferLength];
		private int _offset;

#if DEBUG
		private readonly ValidatorDebug _validator = new(partsCount);
#else
		[SuppressMessage("Style", "IDE1006")]
		// ReSharper disable once ClassNeverInstantiated.Local
		private class _validator : Validator;
#endif

		private void AppendInternal(ReadOnlySpan<char> path)
		{
			path.CopyTo(_buffer.AsSpan(_offset));
			_offset += path.Length;
			_buffer[_offset] = Path.DirectorySeparatorChar;
			++_offset;
			_validator.StorePart(path);
		}

		public void AppendFirst(ReadOnlySpan<char> path)
		{
			Debug.Assert(_offset == 0);
			AppendInternal(path);
			Debug.Assert(_offset < _buffer.Length);
		}

		public void Append(ReadOnlySpan<char> path)
		{
			Debug.Assert(_offset > 0);
			AppendInternal(path);
			Debug.Assert(_offset < _buffer.Length);
		}

		public void AppendLast(ReadOnlySpan<char> path)
		{
			path.CopyTo(_buffer.AsSpan(_offset));
			Debug.Assert((_offset += path.Length) == _buffer.Length);
			_validator.StorePart(path);
		}

		public override string ToString( )
		{
			return _validator.ToString(_buffer);
		}

		public static int PrepareFirst(ref ReadOnlySpan<char> path)
		{
			if (Path.EndsInDirectorySeparator(path))
				path = path.Slice(0, path.Length - 1);
			return path.Length + 1;
		}

		public static int Prepare(ref ReadOnlySpan<char> path)
		{
			return PrepareLast(ref path) + 1;
		}

		public static int PrepareLast(ref ReadOnlySpan<char> path)
		{
			var startOffset = path[0] == Path.AltDirectorySeparatorChar || path[0] == Path.AltDirectorySeparatorChar ? 1 : 0;
			var endOffset = Path.EndsInDirectorySeparator(path) ? 1 : 0;

			if (startOffset != 0 || endOffset != 0)
				path = path.Slice(startOffset, path.Length - endOffset);

			return path.Length;
		}
	}

	public static string Combine(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
	{
		var helper = new CombineHelper(
			CombineHelper.PrepareFirst(ref path1)
			+ CombineHelper.PrepareLast(ref path2), 2);
		helper.AppendFirst(path1);
		helper.AppendLast(path2);
		return helper.ToString( );
	}

	public static string Combine(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
	{
		var helper = new CombineHelper(
			CombineHelper.PrepareFirst(ref path1)
			+ CombineHelper.Prepare(ref path2)
			+ CombineHelper.PrepareLast(ref path3), 3);
		helper.AppendFirst(path1);
		helper.Append(path2);
		helper.AppendLast(path3);
		return helper.ToString( );
	}

	public static string Combine(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4)
	{
		var helper = new CombineHelper(
			CombineHelper.PrepareFirst(ref path1)
			+ CombineHelper.Prepare(ref path2)
			+ CombineHelper.Prepare(ref path3)
			+ CombineHelper.PrepareLast(ref path4), 4);
		helper.AppendFirst(path1);
		helper.Append(path2);
		helper.Append(path3);
		helper.AppendLast(path4);
		return helper.ToString( );
	}

	public static string Combine(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4, ReadOnlySpan<char> path5)
	{
		var helper = new CombineHelper(
			CombineHelper.PrepareFirst(ref path1)
			+ CombineHelper.Prepare(ref path2)
			+ CombineHelper.Prepare(ref path3)
			+ CombineHelper.Prepare(ref path4)
			+ CombineHelper.PrepareLast(ref path5), 5);
		helper.AppendFirst(path1);
		helper.Append(path2);
		helper.Append(path3);
		helper.Append(path4);
		helper.AppendLast(path5);
		return helper.ToString( );
	}

	public static string Combine(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4, ReadOnlySpan<char> path5
							   , ReadOnlySpan<char> path6)
	{
		var helper = new CombineHelper(
			CombineHelper.PrepareFirst(ref path1)
			+ CombineHelper.Prepare(ref path2)
			+ CombineHelper.Prepare(ref path3)
			+ CombineHelper.Prepare(ref path4)
			+ CombineHelper.Prepare(ref path5)
			+ CombineHelper.PrepareLast(ref path6), 6);
		helper.AppendFirst(path1);
		helper.Append(path2);
		helper.Append(path3);
		helper.Append(path4);
		helper.Append(path5);
		helper.AppendLast(path6);
		return helper.ToString( );
	}
}
