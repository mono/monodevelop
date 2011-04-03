// 
// SqlQueryDisplayBinding.cs
//  
// Author:
//       Luciano N. Callero <lnc19@hotmail.com>
// 
// Copyright (c) 2009 Luciano N. Callero
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Database.Query
{
	
	public class SqlQueryDisplayBinding : IDisplayBinding
	{
		public string Name {
			get {
				return GettextCatalog.GetString ("Query Editor");
			}
		}

		public bool CanHandle (FilePath fileName, string mimeType, Project ownerProject)
		{
			return fileName.IsNotNull && fileName.HasExtension (".sql");
		}
		
		public IViewContent CreateContentForFile (string uri)
		{
			return new SqlQueryView ();
		}
		
		public bool CanHandleMimeType (string mimetype)
		{
			return mimetype == "text/x-sql";
		}

		public IViewContent CreateContentForMimeType (string mimeType, System.IO.Stream content)
		{
			return new SqlQueryView ();
		}
		
		public bool CanUseAsDefault { get { return false; } }
	}
}
