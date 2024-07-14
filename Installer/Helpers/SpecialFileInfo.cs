using System.Reflection.PortableExecutable;

namespace Installer.Helpers;

internal class FileArchitectureInfo
{
	public readonly string Architecture;

	public FileArchitectureInfo(string path)
	{
		Architecture = GetFileArchitecture(path);
	}

	public FileArchitectureInfo(FileInfo file)
		: this(file.FullName)
	{
	}

	private static string GetFileArchitecture(string filePath)
	{
		using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
		using var reader = new PEReader(stream);

		if (reader.PEHeaders.PEHeader != null)
		{
			switch (reader.PEHeaders.PEHeader.Magic)
			{
				case PEMagic.PE32:
					return "x86";
				case PEMagic.PE32Plus:
					return "x64";
			}
		}

		if (reader.PEHeaders.CorHeader != null)
		{
			var flags = reader.PEHeaders.CorHeader.Flags;
			if ((flags & CorFlags.ILOnly) != 0 && (flags & CorFlags.Requires32Bit) == 0)
				return "AnyCPU";
			if ((flags & CorFlags.Requires32Bit) != 0)
				return "x86";
		}

		throw new InvalidOperationException("Unable to determine the architecture of the File.");
	}
}
