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
using System.Threading.Tasks;

namespace MonoDevelop.Ide.TypeSystem
{
	sealed class MonoDevelopSourceTextContainer : SourceTextContainer, IDisposable
	{
		readonly ITextDocument document;
		bool isDisposed;
		SourceText currentText;

		public DocumentId Id {
			get;
			private set;
		}

		public MonoDevelopSourceTextContainer (DocumentId documentId, ITextDocument document) : this (document)
		{
			Id = documentId;
		}

		public MonoDevelopSourceTextContainer (ITextDocument document)
		{
			this.document = document;
			this.document.TextChanging += HandleTextReplacing;
		}
		object replaceLock = new object ();
		void HandleTextReplacing (object sender, Core.Text.TextChangeEventArgs e)
		{
			var handler = TextChanged;
			if (handler != null) {
				lock (replaceLock) {
					var oldText = CurrentText;
					var changes = new List<Microsoft.CodeAnalysis.Text.TextChange> ();
					var changeRanges = new List<TextChangeRange> ();
					foreach (var c in e.TextChanges) {
						var span = new TextSpan (c.Offset, c.RemovalLength);
						changes.Add (new Microsoft.CodeAnalysis.Text.TextChange (span, c.InsertedText.Text));
						changeRanges.Add (new TextChangeRange (span, c.InsertionLength));
					}
					var newText = oldText.WithChanges (changes);
					currentText = newText;
					try {
						handler (this, new Microsoft.CodeAnalysis.Text.TextChangeEventArgs (oldText, newText, changeRanges));
					} catch (Exception ex) {
						LoggingService.LogError ("Error while text replacing", ex);
					}
				}
			}
		}

		public void Dispose ()
		{
			if (isDisposed)
				return;
			currentText = null;
			document.TextChanging -= HandleTextReplacing;
			isDisposed = true;
		}

		#region implemented abstract members of SourceTextContainer
		public override SourceText CurrentText {
			get {
				if (currentText == null) {
					currentText = new MonoDevelopSourceText (document.CreateDocumentSnapshot ());
				}
				return currentText;
			}
		}

		public ITextDocument Document {
			get {
				return document;
			}
		}

		public override event EventHandler<Microsoft.CodeAnalysis.Text.TextChangeEventArgs> TextChanged;
		#endregion
	}
}