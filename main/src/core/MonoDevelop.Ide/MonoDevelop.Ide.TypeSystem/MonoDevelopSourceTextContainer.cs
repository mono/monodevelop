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
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Ide.TypeSystem
{
	class MonoDevelopSourceText : SourceText
	{
		readonly ITextSource doc;

		public override System.Text.Encoding Encoding {
			get {
				return doc.Encoding;
			}
		}

		public MonoDevelopSourceText (ITextSource doc)
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
				return doc.Length;
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
		readonly ITextDocument document;

		public MonoDevelopSourceTextContainer (ITextDocument document)
		{
			this.document = document;
			this.document.TextChanging += HandleTextReplacing;
			this.document.TextChanged += HandleTextReplaced;
		}
		
		~MonoDevelopSourceTextContainer ()
		{
			document.TextChanged -= HandleTextReplaced;
			document.TextChanging -= HandleTextReplacing;
		}

		SourceText oldText;
		void HandleTextReplacing (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			oldText = new MonoDevelopSourceText (document.CreateSnapshot ());
		}
		
		void HandleTextReplaced (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			var handler = TextChanged;
			if (handler != null)
				handler (this, new Microsoft.CodeAnalysis.Text.TextChangeEventArgs (oldText, CurrentText, new TextChangeRange (TextSpan.FromBounds (e.Offset, e.Offset + e.RemovalLength), e.InsertionLength)));
		}

		#region implemented abstract members of SourceTextContainer
		public override SourceText CurrentText {
			get {
				return new MonoDevelopSourceText (document.CreateSnapshot ());
			}
		}

		public override event EventHandler<Microsoft.CodeAnalysis.Text.TextChangeEventArgs> TextChanged;
		#endregion
	}
}