//
// FileViewer.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Desktop;

namespace MonoDevelop.Ide.Gui
{
	public class FileViewer
	{
		IDisplayBinding binding;
		DesktopApplication app;
		
		internal FileViewer (DesktopApplication app)
		{
			this.app = app;
		}
		
		internal FileViewer (IDisplayBinding binding)
		{
			this.binding = binding;
		}
		
		public string Title {
			get { return binding != null ? binding.Name : app.DisplayName; }
		}
		
		public bool IsExternal {
			get { return binding == null; }
		}
		
		public bool CanUseAsDefault {
			get {
				if (binding != null)
					return binding.CanUseAsDefault;
				else
					return app.IsDefault;
			}
		}
		
		public override bool Equals (object ob)
		{
			FileViewer fv = ob as FileViewer;
			if (fv == null)
				return false;
			if (binding != null)
				return binding == fv.binding;
			else
				return app.Equals (fv.app);
		}
		
		public override int GetHashCode ()
		{
			if (binding != null)
				return binding.GetHashCode ();
			else
				return app.GetHashCode ();
		}
		
		public Document OpenFile (string filePath)
		{
			return OpenFile (filePath, null);
		}
		
		public Document OpenFile (string filePath, string encoding)
		{
			if (binding != null)
				return IdeApp.Workbench.OpenDocument (filePath, -1, -1, true, encoding, binding);
			else {
				app.Launch (filePath);
				return null;
			}
		}
	}
}
