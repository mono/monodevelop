using System;
using System.IO;
using ICSharpCode.SharpZipLib.Tar;

namespace MonoDevelop.Gui.Utils.DirectoryArchive {
	public sealed class TarDecompressor : Decompressor {
		private ISingleFileDecompressor inputDecompressor;

		public TarDecompressor(ISingleFileDecompressor InputDecompressor)
		{
			this.inputDecompressor = InputDecompressor;
		}

		
		public override void Extract(Stream CompressedData, string OutputPath)
		{
			TarInputStream tarFile = new TarInputStream(inputDecompressor.Decompress(CompressedData));
			if (!OutputPath.EndsWith(""+Path.DirectorySeparatorChar))
				OutputPath = OutputPath + Path.DirectorySeparatorChar;
			
			while (true) {
				TarEntry entry = tarFile.GetNextEntry();
				if (entry == null)
					break;
				string outputFile = OutputPath + entry.Name;
				if (entry.IsDirectory)
					continue;
				EnsureDirectoryExists(Path.GetDirectoryName(outputFile));
				CopyStream(tarFile, File.OpenWrite(outputFile));
			}
		
		}
	}
}
