using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace MonoDevelop.Gui.Utils.DirectoryArchive {
	public sealed class ZipDecompressor : Decompressor {
		public override void Extract(Stream CompressedData, string OutputPath)
		{
			ZipFile zipFile = new ZipFile(CompressedData);
			if (!OutputPath.EndsWith(""+Path.DirectorySeparatorChar))
				OutputPath = OutputPath + Path.DirectorySeparatorChar;
			foreach (ZipEntry entry in zipFile) {
				string outputFile = OutputPath + entry.Name;
				if (entry.IsDirectory)
					continue;
				EnsureDirectoryExists(Path.GetDirectoryName(outputFile));
				CopyStream(zipFile.GetInputStream(entry), File.OpenWrite(outputFile));
			}
		
		}
	}
}
