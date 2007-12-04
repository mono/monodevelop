//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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

using Gtk;
using GtkSourceView;
using System;
using System.Collections.Generic;

namespace MonoDevelop.Database.Components
{
	public partial class ShowTextDialog : Gtk.Dialog
	{
		private SourceView sourceView;
		
		public ShowTextDialog (string text, string mimeType)
		{
			this.Build();
			
			SourceLanguagesManager lm = new SourceLanguagesManager ();
			SourceLanguage lang = null;
			
			if (String.IsNullOrEmpty (mimeType))
				lang = lm.GetLanguageFromMimeType (mimeType);
			
			SourceBuffer buf = null;
			if (lang == null) {
				SourceTagTable table = new SourceTagTable ();
				buf = new SourceBuffer (table);
			} else {
				buf = new SourceBuffer (lang);
				buf.Highlight = true;
			}
			sourceView = new SourceView (buf);
			sourceView.ShowLineNumbers = true;
			sourceView.Editable = false;
			
			vboxContent.PackStart (sourceView, true, true, 0);
			vboxContent.ShowAll ();
			if (text != null)
				sourceView.Buffer.Text = text;
		}

		protected virtual void CloseClicked (object sender, System.EventArgs e)
		{
			Destroy ();
		}
	}
}
