//  IDisplayBinding.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	public interface IDisplayBinding : IBaseDisplayBinding
	{
		bool CanCreateContentForMimeType (string mimeType);
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mimeType">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="content">
		/// A <see cref="System.IO.Stream"/> may be null. If it's null the contents are loaded with an ordinary "Load" call.
		/// </param>
		/// <returns>
		/// A <see cref="IViewContent"/>
		/// </returns>
		IViewContent CreateContentForMimeType (string mimeType, System.IO.Stream content);
		
		bool CanCreateContentForUri (string uri);
		IViewContent CreateContentForUri (string uri);
	}
}
