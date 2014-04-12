//
// RoslynTypeSystemService.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.Composition;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Ide.TypeSystem
{
	class MonoDevelopSourceText : SourceText
	{
		readonly Mono.TextEditor.TextDocument doc;

		public MonoDevelopSourceText (Mono.TextEditor.TextDocument doc)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");
			this.doc = doc;
		}

		#region implemented abstract members of SourceText
		public override void CopyTo (int sourceIndex, char[] destination, int destinationIndex, int count)
		{
			while (count --> 0) {
				destination[destinationIndex++] = doc.GetCharAt (sourceIndex++);
			}
		}
		
		public override int Length {
			get {
				return doc.TextLength;
			}
		}
		public override char this [int index] {
			get {
				return doc.GetCharAt (index);
			}
		}
		#endregion
		
	}
	class MonoDevelopSourceTextContainer : SourceTextContainer
	{
		readonly Mono.TextEditor.TextDocument document;

		public MonoDevelopSourceTextContainer (Mono.TextEditor.TextEditorData document)
		{
			this.document = document.Document;
			this.document.TextReplacing += HandleTextReplacing;
			this.document.TextReplaced += HandleTextReplaced;
		}
		
		~MonoDevelopSourceTextContainer ()
		{
			document.TextReplaced -= HandleTextReplaced;
			document.TextReplaced -= HandleTextReplacing;
		}

		SourceText oldText;
		void HandleTextReplacing (object sender, Mono.TextEditor.DocumentChangeEventArgs e)
		{
			oldText = SourceText.From (document.Text);
		}
		
		void HandleTextReplaced (object sender, Mono.TextEditor.DocumentChangeEventArgs e)
		{
			var handler = TextChanged;
			if (handler != null)
				handler (this, new TextChangeEventArgs (oldText, CurrentText, new TextChangeRange (TextSpan.FromBounds (e.Offset, e.Offset + e.RemovalLength), e.InsertionLength)));
		}

		#region implemented abstract members of SourceTextContainer
		public override SourceText CurrentText {
			get {
				return new MonoDevelopSourceText (document);
			}
		}

		public override event EventHandler<TextChangeEventArgs> TextChanged;
		#endregion
	}
}