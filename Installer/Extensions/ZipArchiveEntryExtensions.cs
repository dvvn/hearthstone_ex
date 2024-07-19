using System.IO.Compression;

namespace Installer.Extensions;

internal static class ZipArchiveEntryExtensions
{
	public static async Task WriteTo(this ZipArchiveEntry entry, Stream stream)
	{
		var pos = stream.Position;
		InvalidDataException exception = null;
		for (;;)
		{
			try
			{
				await using var entryStream = (DeflateStream)entry.Open( );
				await entryStream.CopyToAsync(stream);
				break;
			}
			catch (InvalidDataException ex)
			{
				if (exception != null)
					throw exception;
				exception = ex;
			}
			finally
			{
				stream.Seek(pos, SeekOrigin.Begin);
			}
		}
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
