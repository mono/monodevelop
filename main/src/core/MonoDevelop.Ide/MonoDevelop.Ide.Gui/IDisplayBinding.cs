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
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	public interface IDisplayBinding
	{
		/// <summary>
		/// Whether this instance can handle the specified item. ownerProject may be null, and either 
		/// fileName or mimeType may be null, but not both.
		/// </summary>
		bool CanHandle (FilePath fileName, string mimeType, Project ownerProject);
		
		/// <summary>
		/// Whether the display binding can be used as the default handler for the content types
		/// that it handles. If this is false, the binding is only used when the user explicitly picks it.
		/// </summary>
		bool CanUseAsDefault { get; }
	}
	
	///<summary>A display binding that opens a new view within the workspace.</summary>
	public interface IViewDisplayBinding : IDisplayBinding
	{
		ViewContent CreateContent (FilePath fileName, string mimeType, Project ownerProject);
		string Name { get; }
	}
	
	///<summary>A display binding that opens an external application.</summary>
	public interface IExternalDisplayBinding : IDisplayBinding
	{
		DesktopApplication GetApplication (FilePath fileName, string mimeType, Project ownerProject);
	}
	
	///<summary>A display binding that attaches to an existing view in the workspace.</summary>
	public interface IAttachableDisplayBinding
	{
		bool CanAttachTo (ViewContent content);
		BaseViewContent CreateViewContent (ViewContent viewContent);
	}
}