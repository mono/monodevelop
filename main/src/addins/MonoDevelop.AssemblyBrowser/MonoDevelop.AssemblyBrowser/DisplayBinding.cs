//
// DisplayBinding.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AssemblyBrowser
{
	public class DisplayBinding : IDisplayBinding
	{
		string IDisplayBinding.DisplayName {
			get {
				return "Assembly Browser";
			}
		}
		
		bool IDisplayBinding.CanCreateContentForFile (string fileName)
		{
			string ext = Path.GetExtension (fileName).ToLower ();
			return ext == ".exe" || ext == ".dll";
		}
		AssemblyBrowserViewContent viewContent = null;
		
		AssemblyBrowserViewContent GetViewContent ()
		{
			if (viewContent == null || viewContent.IsDisposed)
				viewContent = new AssemblyBrowserViewContent();
			return viewContent;
		}
		
		IViewContent IDisplayBinding.CreateContentForFile (string fileName)
		{
			return GetViewContent ();
		}
		
		bool IDisplayBinding.CanCreateContentForMimeType (string mimeType)
		{
			return mimeType == "application/x-ms-dos-executable" || mimeType == "application/x-executable";
		}
		
		IViewContent IDisplayBinding.CreateContentForMimeType (string mimeType, System.IO.Stream content)
		{
			return GetViewContent ();
		}
	}
}
