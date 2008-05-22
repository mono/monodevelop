// SolutionItemDescriptor.cs
//
//Author:
//  Lluis Sanchez Gual
//
//Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.ComponentModel;
using MonoDevelop.Projects;

namespace MonoDevelop.DesignerSupport
{
	class SolutionItemDescriptor: CustomDescriptor
	{
		SolutionItem entry;
		
		public SolutionItemDescriptor (SolutionItem entry)
		{
			this.entry = entry;
		}
		
		[DisplayName ("Name")]
		[Description ("Name of the solution item.")]
		public string Name {
			get { return entry.Name; }
			set { entry.Name = value; }
		}
		
		[DisplayName ("File Path")]
		[Description ("File path of the solution item.")]
		public string FilePath {
			get {
				if (entry is SolutionEntityItem)
					return ((SolutionEntityItem) entry).FileName; 
				else
					return "";
			}
		}
		
		[DisplayName ("Root Directory")]
		[Description ("Root directory of source files and projects. File paths will be shown relative to this directory.")]
		public string RootDirectory {
			get {
				return entry.BaseDirectory;
			}
			set {
				if (string.IsNullOrEmpty (value) || System.IO.Directory.Exists (value))
					entry.BaseDirectory = value;
			}
		}
	}
}
