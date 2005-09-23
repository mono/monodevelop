// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez" email="lluis@ximian.com"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace MonoDevelop.Gui.Search
{
	public class ForwardTextFileIterator : ITextIterator
	{
		string fileName;
		FileStream stream;
		StreamReader streamReader;
		ExtendedStreamReader reader;
		bool started;
		IDocumentInformation document;
		int line;
		int lineStartOffset;
		bool lineInSync;
		
		public ForwardTextFileIterator (IDocumentInformation document, string fileName)
		{
			this.document = document;
			this.fileName = fileName;
			stream = new FileStream (fileName, FileMode.Open, FileAccess.Read);
			streamReader = new StreamReader (stream);
			reader = new ExtendedStreamReader (streamReader);
			Reset();
			line = lineStartOffset = 0;
			lineInSync = true;
		}
		
		public IDocumentInformation DocumentInformation {
			get { return document; }
		}
		
		public char Current {
			get {
				return (char) reader.Peek ();
			}
		}
		
		public int Position {
			get {
				return reader.Position;
			}
			set {
				int pos = reader.Position;
				if (value == pos)
					return;
				if (value > pos && lineInSync)
					MoveAhead (value - pos);	// This updates the line count
				else {
					if (value < lineStartOffset)
						lineInSync = false;
					reader.Position = value;
				}
			}
		}
		
		public int DocumentOffset {
			get { return Position; }
		}
		
		public int Line {
			get {
				if (!lineInSync)
					SyncLinePosition ();
				return line;
			}
		}
		public int Column {
			get {
				if (!lineInSync)
					SyncLinePosition ();
				return Position - lineStartOffset;
			}
		}
		
		void SyncLinePosition ()
		{
			int pos = reader.Position;
			reader.Position = 0;
			line = lineStartOffset = 0;
			MoveAhead (pos);
			lineInSync = true;
		}
		
		public char GetCharRelative(int offset)
		{
			int pos = reader.Position;
			
			if (offset < 0) {
				offset = -offset;
				for (int n=0; n<offset; n--)
					if (reader.ReadBack () == -1) {
						reader.Position = pos;
						return char.MinValue;
					}
			}
			else {
				for (int n=0; n<offset; n++) {
					if (reader.Read () == -1) {
						reader.Position = pos;
						return char.MinValue;
					}
				}
			}
			
			char c = (char) reader.Peek ();
			reader.Position = pos;
			return c;
		}
		
		public bool MoveAhead(int numChars)
		{
			Debug.Assert(numChars > 0);
			
			if (!started) {
				started = true;
				return (reader.Peek () != -1);
			}
			
			int nc = 0;
			while (nc != -1 && numChars > 0) {
				numChars--;
				nc = reader.Read ();
				if ((char)nc == '\n') {
					line++;
					lineStartOffset = reader.Position;
				}
			}
			
			if (nc == -1) return false;
			return reader.Peek() != -1;
		}
		
		public void MoveToEnd ()
		{
			int pos = Position;
			while (MoveAhead (1)) {
				pos = Position;
			}
			Position = pos;
		}
		
		public string ReadToEnd ()
		{
			return reader.ReadToEnd ();
		}
		
		public void Replace (int length, string pattern)
		{
			reader.Remove (length);
			reader.Insert (pattern);
		}

		public void Reset()
		{
			reader.Position = 0;
		}
		
		public void Close ()
		{
			if (reader.Modified)
			{
				string fileBackup = Path.GetTempFileName ();
				File.Copy (fileName, fileBackup, true);
				
				try {
					File.Delete (fileName);
					reader.SaveToFile (fileName);
					reader.Close ();
				}
				catch
				{
					reader.Close ();
					if (File.Exists (fileName)) File.Delete (fileName);
					File.Move (fileBackup, fileName);
					throw;
				}
				
				File.Delete (fileBackup);
			}
			else
				reader.Close ();
		}
		
		public bool SupportsSearch (SearchOptions options, bool reverse)
		{
			return false;
		}
		
		public bool SearchNext (string text, SearchOptions options, bool reverse)
		{
			throw new NotSupportedException ();
		}
	}
}
