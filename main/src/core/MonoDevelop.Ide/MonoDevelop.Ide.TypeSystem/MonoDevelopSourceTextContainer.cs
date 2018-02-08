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
		readonly WeakReference<MonoDevelopWorkspace> workspace;
		readonly WeakReference<TextEditor> editor;
		bool isDisposed;
		SourceText currentText;

		public DocumentId Id {
			get;
			private set;
		}

		internal TextEditor Editor {
			get {
				editor.TryGetTarget (out var res);
				return res;
			}
		}

		public MonoDevelopSourceTextContainer (MonoDevelopWorkspace workspace, DocumentId documentId, TextEditor document) : this (document)
		{
			this.workspace = new WeakReference<MonoDevelopWorkspace> (workspace);
			Id = documentId;
		}

		public MonoDevelopSourceTextContainer (TextEditor editor)
		{
			this.editor = new WeakReference<TextEditor> (editor);
			editor.TextChanging += HandleTextReplacing;
		}
		object replaceLock = new object ();
		void HandleTextReplacing (object sender, Core.Text.TextChangeEventArgs e)
		{
			var handler = TextChanged;
			if (handler != null) {
				lock (replaceLock) {
					var oldText = CurrentText;
					var changes = new Microsoft.CodeAnalysis.Text.TextChange[e.TextChanges.Count];
					var changeRanges = new TextChangeRange[e.TextChanges.Count];
					for (int i = 0; i < e.TextChanges.Count; ++i) {
						var c = e.TextChanges[i];
						var span = new TextSpan (c.Offset, c.RemovalLength);
						changes[i] = new Microsoft.CodeAnalysis.Text.TextChange (span, c.InsertedText.Text);
						changeRanges[i] = new TextChangeRange (span, c.InsertionLength);
					}
					var newText = oldText.WithChanges (changes);
					currentText = newText;
					try {
						handler (this, new Microsoft.CodeAnalysis.Text.TextChangeEventArgs (oldText, newText, changeRanges));
					} catch (ArgumentException ae) {
						if (!workspace.TryGetTarget (out var ws))
							return;
						if (!editor.TryGetTarget (out var ed))
							return;
						LoggingService.LogWarning (ae.Message + " re opening " + ed.FileName + " as roslyn source text.");
						ws.InformDocumentClose (Id, ed.FileName);
						Dispose (); // 100% ensure that this object is disposed
						if (ws.GetDocument (Id) != null)
							TypeSystemService.InformDocumentOpen (Id, ed);
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
			if (editor.TryGetTarget (out var ed)) {
				ed.TextChanging -= HandleTextReplacing;
			}
			isDisposed = true;
		}

		#region implemented abstract members of SourceTextContainer
		public override SourceText CurrentText {
			get {
				if (currentText == null) {
					if (editor.TryGetTarget (out var ed)) {
						currentText = MonoDevelopSourceText.Create (ed, this);
					}
				}
				return currentText;
			}
		}

		public override event EventHandler<Microsoft.CodeAnalysis.Text.TextChangeEventArgs> TextChanged;
		#endregion
	}
}