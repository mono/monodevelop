//
// GettextEditorDisplayBinding.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2007 David Makovský
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
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Gettext
{	
	public class GettextEditorDisplayBinding : IDisplayBinding
	{
		public string DisplayName 
		{
			get { return GettextCatalog.GetString ("Gettext Editor"); }
		}
		
		public bool CanCreateContentForFile (string fileName)
		{
			return Path.GetExtension (fileName).Equals (".po", StringComparison.OrdinalIgnoreCase);
		}
		public bool CanCreateContentForMimeType (string mimeType)
		{
			return mimeType == "text/x-gettext-translation";
		}
		
		public IViewContent CreateContentForFile (string fileName)
		{
			return new Editor.CatalogEditorView (fileName);
		}
		
		public IViewContent CreateContentForMimeType (string mimeType, Stream content)
		{
//			StreamReader sr = new StreamReader (content);
//			string text = sr.ReadToEnd ();
//			sr.Close ();
			
			// text/x-gettext-translation
			// TODO: implement such loading
			return null;
		}
	}
}
