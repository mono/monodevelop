//
// RegexLibraryWindow.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Mono.Addins;

namespace MonoDevelop.Projects.Text
{
	public static class TextFileService
	{
		static List<IFormatter> formatters = new List<IFormatter>();
		
		static TextFileService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/TextFormatters", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					formatters.Add ((IFormatter) args.ExtensionObject);
					break;
				case ExtensionChange.Remove:
					formatters.Remove ((IFormatter) args.ExtensionObject);
					break;
				}
			});
		}
		
		public static IFormatter GetFormatter (string mimeType)
		{
			System.Console.WriteLine("format:" + formatters.Count);
			return formatters.Find (x => x.CanFormat (mimeType));
		}
		
		public static void FireLineCountChanged (ITextFile textFile, int lineNumber, int lineCount, int column)
		{
			if (LineCountChanged != null)
				LineCountChanged (textFile, new LineCountEventArgs (textFile, lineNumber, lineCount, column));
		}
		
		public static void FireResetCountChanges (ITextFile textFile)
		{
			if (ResetCountChanges != null)
				ResetCountChanges (textFile, new TextFileEventArgs (textFile));
		}
		
		public static void FireCommitCountChanges (ITextFile textFile)
		{
			if (CommitCountChanges != null)
				CommitCountChanges (textFile, new TextFileEventArgs (textFile));
		}
		
		public static event EventHandler<LineCountEventArgs> LineCountChanged;
		public static event EventHandler<TextFileEventArgs> ResetCountChanges;
		public static event EventHandler<TextFileEventArgs> CommitCountChanges;
	}

	public class TextFileEventArgs : EventArgs
	{
		ITextFile textFile;
		
		public ITextFile TextFile {
			get {
				return textFile;
			}
		}

		public TextFileEventArgs (ITextFile textFile)
		{
			this.textFile = textFile;
		}
	}
	
	public class LineCountEventArgs : TextFileEventArgs
	{
		int lineNumber;
		int lineCount;
		int column;
		
		public int LineNumber {
			get {
				return lineNumber;
			}
		}
		
		public int LineCount {
			get {
				return lineCount;
			}
		}
		
		public int Column {
			get {
				return column;
			}
		}
		
		public LineCountEventArgs (ITextFile textFile, int lineNumber, int lineCount, int column) : base (textFile)
		{
			this.lineNumber = lineNumber;
			this.lineCount  = lineCount;
			this.column     = column;
		}
		
		public override string ToString ()
		{
			 return String.Format ("[LineCountEventArgs: LineNumber={0}, LineCount={1}, Column={2}]", lineNumber, lineCount, column);
		}

	}	
}
