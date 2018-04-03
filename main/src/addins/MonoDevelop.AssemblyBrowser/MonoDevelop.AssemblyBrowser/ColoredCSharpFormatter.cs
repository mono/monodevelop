// 
// ColoredCSharpFormatter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System;
using System.Text;
using ICSharpCode.Decompiler;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace MonoDevelop.AssemblyBrowser
{
	class ReferenceSegment
	{
		public TextSegment Segment {
			get;
			set;
		}

		public object Reference {
			get;
			set;
		}
		
		public ReferenceSegment (int offset, int length, object reference)
		{
			this.Reference = reference;
			this.Segment = new TextSegment (offset, length);
		}

		public int Offset {
			get {
				return Segment.Offset;
			}
		}

		public int Length {
			get {
				return Segment.Length;
			}
		}

		public int EndOffset {
			get {
				return Segment.EndOffset;
			}
		}

		public static implicit operator TextSegment (ReferenceSegment referenceSegment)
		{
			return referenceSegment.Segment;
		}
	}
	
		
	class ColoredCSharpFormatter : ICSharpCode.Decompiler.ITextOutput
	{
		public StringBuilder sb = new StringBuilder();
		TextEditor doc;
		bool write_indent;
		int indent;
		public List<IFoldSegment>     FoldSegments       = new List<IFoldSegment>();
		public List<ReferenceSegment> ReferencedSegments = new List<ReferenceSegment>();
		
		public ColoredCSharpFormatter (TextEditor doc)
		{
			this.doc = doc;
		}
		
		public void SetDocumentData ()
		{
			doc.Text = sb.ToString ();
			doc.SetFoldings (FoldSegments);
		}
		
		#region ITextOutput implementation
		int currentLine;
		public int CurrentLine {
			get {
				return currentLine;
			}
		}
		
		public TextLocation Location {
			get {
				return new TextLocation (currentLine, 1);
			}
		}
		
		public void Indent ()
		{
			indent++;
		}

		public void Unindent ()
		{
			indent--;
		}

		public void Write (char ch)
		{
			WriteIndent ();
			sb.Append (ch);
		}

		void ITextOutput.Write (string text)
		{
			WriteIndent ();
			sb.Append (text);
		}

		void WriteIndent ()
		{
			if (!write_indent)
				return;
			write_indent = false;
			for (int i = 0; i < indent; i++)
				sb.Append ("\t");
		}
		
		public void WriteLine ()
		{
			sb.AppendLine ();
			write_indent = true;
			currentLine++;
		}

		public void WriteDefinition (string text, object definition, bool isLocal)
		{
			WriteIndent ();
			sb.Append (text);
		}

		public void WriteReference (string text, object reference, bool isLocal)
		{
			WriteIndent ();
			ReferencedSegments.Add (new ReferenceSegment (sb.Length, text.Length, reference));
			sb.Append (text);
		}
		
		Stack<Tuple<int, string, bool>> foldSegmentStarts =new Stack<Tuple<int, string, bool>> ();
		
		public void MarkFoldStart (string collapsedText, bool defaultCollapsed)
		{
			foldSegmentStarts.Push (Tuple.Create (sb.Length, collapsedText, defaultCollapsed));
		}

		public void MarkFoldEnd ()
		{
			var curFold = foldSegmentStarts.Pop ();
			var seg = FoldSegmentFactory.CreateFoldSegment (doc, curFold.Item1, sb.Length - curFold.Item1, curFold.Item3, curFold.Item2);
			FoldSegments.Add (seg);
		}
		#endregion
		
	}
}

