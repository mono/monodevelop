// WorkspaceItemDescriptor.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Projects;
using System.ComponentModel;

namespace MonoDevelop.DesignerSupport.Projects
{
	public class WorkspaceItemDescriptor: CustomDescriptor
	{
		WorkspaceItem item;
		
		public WorkspaceItemDescriptor (WorkspaceItem item)
		{
			this.item = item;
		}
		
		[DisplayName ("Name")]
		[Description ("Name of the item.")]
		public string Name {
			get { return item.Name; }
			set { item.Name = value; }
		}
		
		[DisplayName ("File Path")]
		[Description ("File path of the item.")]
		public string FilePath {
			get {
				return item.FileName;
			}
		}
		
		[DisplayName ("Root Directory")]
		[Description ("Root directory of source files and projects. File paths will be shown relative to this directory.")]
		public string RootDirectory {
			get {
				return item.BaseDirectory;
			}
			set {
				if (string.IsNullOrEmpty (value) || System.IO.Directory.Exists (value))
					item.BaseDirectory = value;
			}
		}
		
		[DisplayName ("File Format")]
		[Description ("File format of the project file.")]
		public string FileFormat {
			get {
				return item.FileFormat.Name;
			}
		}
	}
}
