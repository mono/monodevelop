/* Decompressor.cs
 *
 * Iain McCoy <iain@mccoy.id.au>, 2004
 *
 * Just a little abstraction so that any sort of compressed archive can be
 * extracted in the same way
 *
 * Supports zips, straight tar files and tarballs of the bz2 and gz varieties
 */

using System;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;

namespace MonoDevelop.Gui.Utils.DirectoryArchive {
	public enum CompressionType { Zip, TarGz, TarBz2, Tar }

	public interface ISingleFileDecompressor {
		Stream Decompress(Stream input);
	}
	
	public abstract class Decompressor {
		public static Decompressor Load(string fileEnding)
		{
			return Load(GetTypeFromString(fileEnding));
		}
	
		public static Decompressor Load(CompressionType compression)
		{
			switch (compression) {
			case CompressionType.Zip:
				return new ZipDecompressor();
			case CompressionType.TarGz:
				return new TarDecompressor(new GZipDecompressor());
			case CompressionType.TarBz2:
				return new TarDecompressor(new BZip2Decompressor());
			case CompressionType.Tar:
				return new TarDecompressor(null);
			default:
				throw new ArgumentOutOfRangeException("compression");
			}
		}
	
		protected void CopyStream (Stream inp, Stream outp)
		{
			byte[] buf = new byte[32 * 1024];
			long amount = 0;
			while (true) {
				int numRead = inp.Read(buf, 0, buf.Length);
				if (numRead <= 0) {
					break;
				}
				amount += numRead;
				outp.Write(buf, 0, numRead);
				
			}
		}
	
		protected void EnsureDirectoryExists(string path)
		{
			EnsureDirectoryExists(new DirectoryInfo(path));
		}
		protected void EnsureDirectoryExists(DirectoryInfo path)
		{
			if (path.Parent != null)
				EnsureDirectoryExists(path.Parent);
			if (!path.Exists)
				path.Create();
		}
	
		public static CompressionType GetTypeFromString(string fileEnding)
		{
			return GetTypeFromString(fileEnding, true);
		}
		
		public static CompressionType GetTypeFromString(string fileEnding, bool ThrowException)
		{
			if (fileEnding.EndsWith(".zip"))
				return CompressionType.Zip;
			else if (fileEnding.EndsWith(".tgz") || fileEnding.EndsWith(".tar.gz"))
				return CompressionType.TarGz;
			else if (fileEnding.EndsWith(".tbz2") || fileEnding.EndsWith(".tar.bz2"))
				return CompressionType.TarBz2;
			else if (fileEnding.EndsWith(".tar"))
				return CompressionType.Tar;
			else
				if (ThrowException)
					throw new ArgumentOutOfRangeException("fileEnding");
				else
					return (CompressionType)(-1);
		}
	
		public abstract void Extract(Stream CompressedData, string OutputPath);
	}
}
