// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text;
using System.IO;
using System.Collections;

namespace MonoDevelop.Gui.Search
{
	internal class FileDocumentInformation: IDocumentInformation
	{
		string fileName;
		int currentOffset;
		ForwardTextFileIterator iterator;
		
		public FileDocumentInformation(string fileName, int currentOffset)
		{
			this.fileName      = fileName;
			this.currentOffset = currentOffset;
		}
		
		public string FileName {
			get {
				return fileName;
			}
		}
		
		public int CurrentOffset {
			get {
				return currentOffset;
			}
			set {
				currentOffset = value;
			}
		}
		
		public int EndOffset {
			get {
				return currentOffset;
			}
		}
		
		public ITextIterator GetTextIterator ()
		{
			if (iterator == null)
				iterator = new ForwardTextFileIterator (this, fileName);
			return iterator;
		}
		
		public string GetLineTextAtOffset (int offset)
		{
			FileStream stream = new FileStream (fileName, FileMode.Open, FileAccess.Read);
			try {
				ExtendedStreamReader streamReader = new ExtendedStreamReader (new StreamReader (stream));
				streamReader.Position = offset;
				
				int lastPos;
				int b;
				do {
					lastPos = streamReader.Position;
					b = streamReader.ReadBack ();
				} while (b != -1 && b != 10 && b != 13);
				
				streamReader.Position = lastPos;
				StringBuilder sb = new StringBuilder ();
				b = streamReader.Read ();
				while (b != -1 && b != 10 && b != 13) {
					sb.Append ((char)b);
					b = streamReader.Read ();
				}
				return sb.ToString ();
			} finally {
				stream.Close ();
			}
		}
	}
}
