using System;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;

namespace MonoDevelop.Gui.Utils.DirectoryArchive {
	
	public class GZipDecompressor : ISingleFileDecompressor {
		public Stream Decompress (Stream input)
		{
			return new GZipInputStream(input);
		}
	}
}
