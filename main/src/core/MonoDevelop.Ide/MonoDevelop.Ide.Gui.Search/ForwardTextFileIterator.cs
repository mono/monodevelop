// ForwardTextFileIterator.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Search
{/*
	public class ForwardTextFileIterator : ITextIterator
	{
		string fileName;
		string text;
		int position;
		bool started;
		public ForwardTextFileIterator (IDocumentInformation document, string fileName)
		{
			this.DocumentInformation = document;
			this.fileName = fileName;
			text = File.ReadAllText (fileName);
			Reset ();
		}
		
		#region ITextIterator implementation 
		
		public char GetCharRelative (int offset)
		{
			return text [position + offset];
		}
		
		public bool MoveAhead (int numChars)
		{
			if (!started) {
				started = true;
				return !String.IsNullOrEmpty (text);
			}
			position += numChars;
			inSync = false;
			return position < text.Length;
		}
		
		public void MoveToEnd ()
		{
			position = text.Length - 1;
			inSync = false;
		}
		
		public string ReadToEnd ()
		{
			return text.Substring (position);
		}
		
		public void Reset ()
		{
			position = 0;
			started  = false;
		}
		
		public void Replace (int length, string pattern)
		{
			text = text.Substring (0, position) + pattern + text.Substring (position + length);
		}
		
		public void Close ()
		{
			if (File.Exists (fileName)) 
				File.Delete (fileName);
			File.WriteAllText (fileName, text);
		}
		
		public bool SupportsSearch (SearchOptions options, bool reverse)
		{
			return false;
		}
		
		public bool SearchNext (string text, SearchOptions options, bool reverse)
		{
			throw new NotSupportedException ();
		}
		
		public char Current {
			get {
				return text [position];
			}
		}
		
		public int Position {
			get {
				return position;
			}
			set {
				position = value;
				inSync = false;
			}
		}
		
		public int DocumentOffset {
			get { return Position; }
		}
		bool inSync = false;
		int line;
		int req = 0;
		public int Line {
			get {
				System.Console.WriteLine(req++);
				if (!inSync)
					SyncLinePosition ();
				return line;
			}
		}
		
		int column;
		public int Column {
			get {
				if (!inSync)
					SyncLinePosition ();
				return column;
			}
		}
		
		void SyncLinePosition ()
		{
			line = 0;
			int lastLineStart = 0;
			for (int i = 0; i < position; i++) {
				if (text[i] == '\n') {
					lastLineStart = i;
					line++;
				}
			}
			this.column = position -lastLineStart;
			inSync = true;
		}
		public IDocumentInformation DocumentInformation {
			get;
			set;
		}
		
		#endregion 

	}*/
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
			stream = new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
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
				FileService.NotifyFileChanged (fileName);
			}
			else
				reader.Close ();

			streamReader.Close ();
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
