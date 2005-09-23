//
// SqlDefinitionPad.cs: Displays definition of a sql object.
//
// Author:
//   Christian Hergert <chris@mosaix.net>
//
// Copyright (C) 2005 Christian Hergert
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
using System.Resources;

using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Gui;

using Gtk;
using GtkSourceView;

using Mono.Data.Sql;

namespace MonoQuery
{
	public class SqlDefinitionPad : AbstractPadContent
	{
		MonoQueryService service = (MonoQueryService)ServiceManager.GetService (typeof (MonoQueryService));
		
		Gtk.Frame frame;
		Gtk.ScrolledWindow sw;
		GtkSourceView.SourceView textView;
		GtkSourceView.SourceBuffer textBuffer;
		
		public SqlDefinitionPad () : base ("SQL Definition", "md-mono-query-view")
		{
			frame = new Gtk.Frame ();
			sw = new Gtk.ScrolledWindow ();
			frame.Add (sw);
			SourceLanguagesManager lm = new SourceLanguagesManager ();
			textBuffer = new SourceBuffer(lm.GetLanguageFromMimeType("text/x-sql"));
			textBuffer.Highlight = true;
			textView = new SourceView (textBuffer);
			textView.ShowLineNumbers = false;
			textView.ShowMargin = false;
			textView.TabsWidth = 2;
			textView.Editable = false;
			sw.Add (textView);
			frame.ShowAll ();
			
			service.SqlDefinitionPad = this;
		}
		
		public override Gtk.Widget Control {
			get {
				return frame;
			}
		}
		
		public void SetText (string text)
		{
			this.textBuffer.Text = text;
		}
	}
}