// 
//  CompilationUnitDataProvider.cs
//  
//  Author:
//       Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
// 
//  Copyright (c) 2010 Levi Bard
// 
// This source code is licenced under The MIT License:
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
using System.Linq;

using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components;

using Gtk;
using MonoDevelop.Ide.Editor;

namespace CBinding.Parser
{
	// Yoinked from C# binding
	public class CompilationUnitDataProvider : DropDownBoxListWindow.IListDataProvider
	{
		TextEditor editor;

		DocumentContext DocumentContext { get; set; }
		
		public CompilationUnitDataProvider (TextEditor editor, DocumentContext documentContext)
		{
			this.editor = editor;
			this.DocumentContext = documentContext;
		}
		
		#region IListDataProvider implementation
		public void Reset () { }
		
		public string GetMarkup (int n)
		{
			return GLib.Markup.EscapeText (DocumentContext.ParsedDocument.UserRegions.ElementAt (n).Name);
		}
		
		internal static Xwt.Drawing.Image Pixbuf
		{
			get { return ImageService.GetIcon (Gtk.Stock.Add, IconSize.Menu); }
		}
		
		public Xwt.Drawing.Image GetIcon (int n)
		{
			return Pixbuf;
		}
		
		public object GetTag (int n)
		{
			return DocumentContext.ParsedDocument.UserRegions.ElementAt (n);
		}
		
		
		public void ActivateItem (int n)
		{
			var reg = DocumentContext.ParsedDocument.UserRegions.ElementAt (n);
			var extEditor = editor;
			if (extEditor != null) {
				extEditor.CaretLocation = new DocumentLocation (Math.Max (1, reg.Region.BeginLine), reg.Region.BeginColumn);
				extEditor.StartCaretPulseAnimation ();
			}
		}
		
		public int IconCount
		{
			get {
				if (DocumentContext.ParsedDocument == null)
					return 0;
				return DocumentContext.ParsedDocument.UserRegions.Count ();
			}
		}
		
		#endregion
	}
}

