// 
// IPlistEditingHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using MonoDevelop.Projects;

namespace MonoDevelop.MacDev.PlistEditor
{
	public interface IPlistEditingHandler
	{
		bool CanHandle (Project project, string projectVirtualPath);
		IEnumerable<PlistEditingSection> GetSections (Project project, PDictionary dictionary);
	}
	
	public class PlistEditingSection
	{
		public PlistEditingSection (string name, Gtk.Widget widget)
		{
			this.Name = name;
			this.Widget = widget;
		}
		
		public string Name { get; private set; }
		public Gtk.Widget Widget { get; private set; }
		public bool IsAdvanced { get; set; }
		public Func<PDictionary,bool> CheckVisible { get; set; }
	}
}