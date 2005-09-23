using System;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;

namespace MonoDevelop.Gui.Utils.DirectoryArchive {
	
	public class BZip2Decompressor : ISingleFileDecompressor {
		public Stream Decompress (Stream input)
		{
			input.ReadByte();
			input.ReadByte();
			return new BZip2InputStream(input);
		}
	}
}
