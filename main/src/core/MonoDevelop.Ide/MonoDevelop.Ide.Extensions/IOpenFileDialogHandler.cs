// 
// IFileOpenDialogHandler.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components.Extensions;
using MonoDevelop.Ide.Gui;
using System.Text;


namespace MonoDevelop.Ide.Extensions
{
	/// <summary>
	/// This interface can be implemented to provide a custom implementation
	/// for the OpenFileDialog (used to select a file or project to open)
	/// </summary>
	public interface IOpenFileDialogHandler : IDialogHandler<OpenFileDialogData>
	{
	}

	/// <summary>
	/// Data for the IOpenFileDialogHandler implementation
	/// </summary>
	public class OpenFileDialogData: SelectFileDialogData
	{
		/// <summary>
		/// Set to true if the encoding selector has to be shown
		/// </summary>
		public bool ShowEncodingSelector { get; set; }
		
		/// <summary>
		/// Set to true if the viewer selector has to be shown
		/// </summary>
		public bool ShowViewerSelector { get; set; }
		
		/// <summary>
		/// Selected encoding. To be set by the handler.
		/// </summary>
		public Encoding Encoding { get; set; }
		
		/// <summary>
		/// Set to true if the workspace has to be closed before opening a solution. To be set by the handler.
		/// </summary>
		public bool CloseCurrentWorkspace { get; set; }
		
		/// <summary>
		/// Selected viewer. To be set by the handler.
		/// </summary>
		public FileViewer SelectedViewer { get; set; }
	}
}
