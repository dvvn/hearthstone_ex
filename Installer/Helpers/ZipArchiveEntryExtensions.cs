using System.IO.Compression;

namespace Installer.Helpers;

internal static class ZipArchiveEntryExtensions
{
	public static async Task WriteTo(this ZipArchiveEntry entry, Stream stream)
	{
		var pos = stream.Position;

		uint errorCount = 3;
		do
		{
			try
			{
				await using var entryStream = entry.Open( );
				await entryStream.CopyToAsync(stream);
				errorCount = 0;
			}
			catch (InvalidDataException _)
			{
				if (--errorCount == 0)
					throw;
			}
			finally
			{
				stream.Seek(pos, SeekOrigin.Begin);
			}
		}
		while (errorCount != 0);
	}

	public static async Task WriteTo(this ZipArchiveEntry entry, string path)
	{
		await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
		await entry.WriteTo(fileStream);
	}

	public static async Task<MemoryStream> WriteToMemory(this ZipArchiveEntry entry)
	{
		var stream = new MemoryStream((int)entry.Length);
		await entry.WriteTo(stream);
		return stream;
	}

	public static bool HasExtension(this ZipArchiveEntry entry, ReadOnlySpan<char> extension)
	{
		return Path.GetExtension(entry.FullName.AsSpan( )).SequenceEqual(extension);
	}
}
