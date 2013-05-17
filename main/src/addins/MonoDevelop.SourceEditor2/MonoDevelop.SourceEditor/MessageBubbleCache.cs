// 
// MessageBubbleCache.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Fonts;
using Mono.TextEditor.Highlighting;

namespace MonoDevelop.SourceEditor
{
	class MessageBubbleCache : IDisposable
	{
		internal Gdk.Pixbuf errorPixbuf;
		internal Gdk.Pixbuf warningPixbuf;
		
		internal Dictionary<string, LayoutDescriptor> textWidthDictionary = new Dictionary<string, LayoutDescriptor> ();
		internal Dictionary<DocumentLine, double> lineWidthDictionary = new Dictionary<DocumentLine, double> ();
		
		internal TextEditor editor;
		
		internal Pango.FontDescription fontDescription;

		public MessageBubbleCache (TextEditor editor)
		{
			this.editor = editor;
			errorPixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Error, Gtk.IconSize.Menu);
			warningPixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Warning, Gtk.IconSize.Menu);
			
			editor.EditorOptionsChanged += HandleEditorEditorOptionsChanged;
			fontDescription = FontService.GetFontDescription ("MessageBubbles");
		}

		public bool RemoveLine (DocumentLine line)
		{
			if (!lineWidthDictionary.ContainsKey (line))
				return false;
			lineWidthDictionary.Remove (line);
			return true;
		}

		public void Dispose ()
		{
			editor.EditorOptionsChanged -= HandleEditorEditorOptionsChanged;
			if (textWidthDictionary != null) {
				foreach (var l in textWidthDictionary.Values) {
					l.Layout.Dispose ();
				}
			}
		}

		static string GetFirstLine (ErrorText errorText)
		{
			string firstLine = errorText.ErrorMessage ?? "";
			int idx = firstLine.IndexOfAny (new [] {'\n', '\r'});
			if (idx > 0)
				firstLine = firstLine.Substring (0, idx);
			return firstLine;
		}

		internal LayoutDescriptor CreateLayoutDescriptor (ErrorText errorText)
		{
			LayoutDescriptor result;
			if (!textWidthDictionary.TryGetValue (errorText.ErrorMessage, out result)) {
				Pango.Layout layout = new Pango.Layout (editor.PangoContext);
				layout.FontDescription = fontDescription;
				layout.SetText (GetFirstLine (errorText));
				int w, h;
				layout.GetPixelSize (out w, out h);
				textWidthDictionary[errorText.ErrorMessage] = result = new LayoutDescriptor (layout, w, h);
			}
			return result;
		}

		void HandleEditorEditorOptionsChanged (object sender, EventArgs e)
		{
			lineWidthDictionary.Clear ();
			OnChanged (EventArgs.Empty);
		}	

		internal class LayoutDescriptor
		{
			public Pango.Layout Layout { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }

			public LayoutDescriptor (Pango.Layout layout, int width, int height)
			{
				this.Layout = layout;
				this.Width = width;
				this.Height = height;
			}
		}
	
		protected virtual void OnChanged (EventArgs e)
		{
			EventHandler handler = this.Changed;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler Changed;
	}
}

