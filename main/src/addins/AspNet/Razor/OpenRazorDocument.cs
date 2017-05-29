//
// OpenRazorDocument.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using System.Threading;
using System.Web.Razor;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.AspNet.Razor
{
	class OpenRazorDocument : IDisposable
	{
		ITextDocument document;
		ChangeInfo lastChange;

		public OpenRazorDocument (ITextDocument document)
		{
			this.document = document;
			document.TextChanging += OnTextReplacing;
		}

		public void Dispose ()
		{
			document.TextChanging -= OnTextReplacing;
			if (ParseComplete != null) {
				ParseComplete.Dispose ();
				ParseComplete = null;
			}
			if (EditorParser != null) {
				EditorParser.Dispose ();
				EditorParser = null;
			}
		}

		public ITextDocument Document {
			get { return document; }
		}

		public string FileName {
			get { return document.FileName; }
		}

		public MonoDevelop.Web.Razor.EditorParserFixed.RazorEditorParser EditorParser { get; set; }
		public DocumentParseCompleteEventArgs CapturedArgs { get; set; }
		public AutoResetEvent ParseComplete { get; set; }

		public ChangeInfo LastTextChange {
			get { return lastChange; }
		}

		public void ClearLastTextChange ()
		{
			lock (document)
				lastChange = null;
		}

		void OnTextReplacing (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			lock (document) {
				var change = e.TextChanges[0];
				if (lastChange == null)
					lastChange = new ChangeInfo (change.Offset, new System.Web.Razor.Text.SeekableTextReader ((sender as MonoDevelop.Ide.Editor.ITextDocument).Text));
				if (change.ChangeDelta > 0) {
					lastChange.Length += change.InsertionLength;
				} else {
					lastChange.Length -= change.RemovalLength;
				}
			}
		}
	}
}

