// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez" email="lluis@ximian.com"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Text;

namespace MonoDevelop.Gui.Search
{
	/// <summary>
	///   This is a wrapper around StreamReader which is seekable.
	/// </summary>
	internal class ExtendedStreamReader
	{
		StreamReader reader;

		private const int DefaultCacheSize = 4096;
		private const int MAX_CACHED_BUFFERS = 2;

		char[][] cachedBuffers = new char[MAX_CACHED_BUFFERS][];
		int[] cachedBufferStartPos = new int[MAX_CACHED_BUFFERS];
		int[] cachedBufferCharCount = new int[MAX_CACHED_BUFFERS];
		
		int streamReadPosition;
		int currentBuffer;
		
		int pos;                // index into buffer[]
		
		string changeBuffer;
		int changePos = -1;
		int curChangePosition = 0;
		bool inChangeSection = false;
		
		public ExtendedStreamReader (StreamReader reader)
		{
			this.reader = reader;
			
			for (int n=0; n<MAX_CACHED_BUFFERS; n++) {
				cachedBufferStartPos [n] = -1;
				cachedBufferCharCount [n] = 0;
			}
		}

		public int Position {
			get {
				if (inChangeSection) {
					return changePos + curChangePosition;
				}
				else {
					int p = cachedBufferStartPos [currentBuffer] + pos;
					return p >= 0 ? p : 0;
				}
			}

			set {
				if (changePos != -1 && value >= changePos) {
					curChangePosition = value - changePos;
					inChangeSection = true;
				}
				else {
					inChangeSection = false;
					
					for (int n=0; n<MAX_CACHED_BUFFERS; n++)
					{
						int start = cachedBufferStartPos[n];
						if (value >= start && value < start + cachedBufferCharCount [n]) {
							pos = value - start;
							currentBuffer = n;
							return;
						}
					}
					
					reader.BaseStream.Position = 0;
					reader.DiscardBufferedData ();
					streamReadPosition = 0;
					currentBuffer = 0;
					
					for (int n=0; n<MAX_CACHED_BUFFERS; n++) {
						cachedBufferStartPos [n] = -1;
						cachedBufferCharCount [n] = 0;
					}
						
					int cp = 0;
					while (cp < value) {
						Read ();
						cp++;
					}
				}
			}
		}
		
		private bool ReadBuffer ()
		{
			pos = 0;
			if (currentBuffer > 0) {
				currentBuffer--;
				return cachedBufferCharCount [currentBuffer] > 0;
			}
				
			// Shift buffers

			char[] lastBuffer = cachedBuffers [MAX_CACHED_BUFFERS-1];
			for (int n=MAX_CACHED_BUFFERS-1; n>0; n--)
			{
				cachedBufferStartPos[n] = cachedBufferStartPos[n-1];
				cachedBufferCharCount[n] = cachedBufferCharCount[n-1];
				cachedBuffers[n] = cachedBuffers[n-1];
			}
			
			cachedBufferStartPos[0] = streamReadPosition;
			
			if (lastBuffer == null)
				lastBuffer = new char [DefaultCacheSize];
				
			cachedBuffers [0] = lastBuffer;
				
			int char_count = reader.Read (cachedBuffers [0], 0, cachedBuffers [0].Length);
			cachedBufferCharCount [0] = char_count;
			streamReadPosition += char_count;
			return char_count > 0;
		}

		public int Peek ()
		{
			if (inChangeSection) {
				if (curChangePosition >= changeBuffer.Length) return -1;
				return changeBuffer [curChangePosition];
			}

			if (pos >= cachedBufferCharCount [currentBuffer] && !ReadBuffer ())
				return -1;

			return cachedBuffers[currentBuffer] [pos];
		}

		public int Read ()
		{
			if (inChangeSection) {
				if (curChangePosition >= changeBuffer.Length) return -1;
				return changeBuffer [curChangePosition++];
			}
			
			if (pos >= cachedBufferCharCount [currentBuffer] && !ReadBuffer ())
				return -1;

			int res = cachedBuffers[currentBuffer][pos++];
			
			// Check if it has just entered the changed section
			
			if (changePos != -1 && changePos >= Position) {
				if (Position > changePos) {
					throw new Exception ("Wrong position");
				}
				if (Position == changePos) {
					inChangeSection = true;
					curChangePosition = 0;
				}
			}
			
			return res;
		}
		
		public int ReadBack ()
		{
			if (Position == 0) return -1;
			
			if (inChangeSection) {
				if (curChangePosition > 0) {
					curChangePosition--;
					return changeBuffer [curChangePosition];
				}
				Position--;
				return cachedBuffers[currentBuffer][pos];
			}
			
			if (pos > 0) {
				pos--;
				return cachedBuffers[currentBuffer][pos];
			}
			
			Position--;
			return cachedBuffers[currentBuffer][pos];
		}
		
		public string ReadToEnd ()
		{
			if (changePos == -1) {
				StringBuilder sb = new StringBuilder ();
				int c;
				while ((c = Read()) != -1)
					sb.Append ((char)c);
					
				return sb.ToString ();
			}
			else if (inChangeSection) {
				string doc = changeBuffer.Substring (curChangePosition);
				curChangePosition = changeBuffer.Length;
				return doc;
			}
			else {
				StringBuilder sb = new StringBuilder ();
				while (!inChangeSection)
					sb.Append ((char)Read ());
				
				inChangeSection = true;
				curChangePosition = changeBuffer.Length;
				return sb.ToString() + changeBuffer;
			}
		}
		
		public void Remove (int length)
		{
			EnterChangeSection ();
			changeBuffer = changeBuffer.Substring (0, curChangePosition) + changeBuffer.Substring (curChangePosition + length);
		}

		public void Insert (string newText)
		{
			EnterChangeSection ();
			changeBuffer = changeBuffer.Substring (0, curChangePosition) + newText + changeBuffer.Substring (curChangePosition);
			curChangePosition += newText.Length;
		}
		
		void EnterChangeSection ()
		{
			if (changePos == -1) 
			{
				// This is the first change in the stream.
				
				int curPos = Position;
				changeBuffer = ReadToEnd ();
				changePos = curPos;
				curChangePosition = 0;
				inChangeSection = true;
			}
			else if (!inChangeSection) 
			{
				// There is already a change section, but we are not inside.
				// Expand the change section to include the current position.
				
				int curPos = Position;
				StringBuilder sb = new StringBuilder ();
				while (!inChangeSection)
					sb.Append ((char)Read ());
				changeBuffer = sb.ToString () + changeBuffer;
				Position = curPos;
				changePos = curPos;
				curChangePosition = 0;
				inChangeSection = true;
			}
		}
		
		public void Close ()
		{
			reader.Close ();
		}
		
		public bool Modified {
			get { return changePos != -1; }
		}
		
		public void SaveToFile (string file)
		{
			StreamWriter swriter = new StreamWriter (file, false, reader.CurrentEncoding);
			
			try {
				// Write the old file until the replace position
				
				reader.BaseStream.Position = 0;
				reader.DiscardBufferedData ();
				
				char[] buffer = new char [16*1024];
				int count = changePos != -1 ? changePos : int.MaxValue;
				while (count > 0) {
					int toread = count > buffer.Length ? buffer.Length : count;
					int nread = reader.Read (buffer, 0, toread);
					if (nread == 0) break;
					swriter.Write (buffer, 0, nread);
					count -= nread;
				}
				
				if (changePos == -1)
					return;
				
				// Write the modified text
				
				swriter.Write (changeBuffer);
			}
			finally
			{
				swriter.Close ();
			}
		}
	}
}

