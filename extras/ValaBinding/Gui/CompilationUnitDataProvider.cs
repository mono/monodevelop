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

namespace MonoDevelop.ValaBinding
{
    // Yoinked from C# binding
    public class CompilationUnitDataProvider : DropDownBoxListWindow.IListDataProvider
    {
        Document Document { get; set; }

        public CompilationUnitDataProvider(Document document)
        {
            this.Document = document;
        }// constructor

        #region IListDataProvider implementation
        public void Reset() { }

        public string GetMarkup(int n)
        {
            return Document.ParsedDocument.UserRegions.ElementAt(n).Name;
        }// GetText

        internal static Image Pixbuf
        {
            get { return ImageService.GetImage(Gtk.Stock.Add, IconSize.Menu); }
        }// Pixbuf

        public Xwt.Drawing.Image GetIcon(int n)
        {
            return ImageService.GetIcon(Gtk.Stock.Add, IconSize.Menu);
        }// GetIcon

        public object GetTag(int n)
        {
            return Document.ParsedDocument.UserRegions.ElementAt(n);
        }// GetTag

        public void ActivateItem(int n)
		{
			var reg = Document.ParsedDocument.UserRegions.ElementAt (n);
			MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor extEditor = Document.GetContent<MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor> ();
			if (extEditor != null)
            {
				extEditor.SetCaretTo(Math.Max (1, reg.Region.BeginLine), reg.Region.BeginColumn);
            }
		}// ActivateItem

        public int IconCount
        {
            get
            {
                if (Document.ParsedDocument == null)
                    return 0;
                return Document.ParsedDocument.UserRegions.Count();
            }
        }// IconCount

        #endregion
    }// CompilationUnitDataProvider
}