// 
// FileNavigationPoint.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

namespace MonoDevelop.Ide.Gui
{
	
	
	public class FileNavigationPoint : NavigationPoint
	{
		string fileName;
		string fragment;
		
		public FileNavigationPoint (string fileName)
			: this (fileName, string.Empty)
		{
		}
		
		public FileNavigationPoint (string fileName, string fragment)
		{
			this.fileName = fileName;
			this.fragment = fragment;
		}
		
		public string FileName {
			get { return fileName; }
			internal set { fileName = value; }
		}
		
		public override string DisplayName {
			get { return System.IO.Path.GetFileName (fileName); }
		}
		
		public override string Tooltip {
			get { return fragment; }
		}
		
		protected virtual string Snippet {
			set { fragment = value; }
		}
		
		protected override void DoShow ()
		{
			IdeApp.Workbench.OpenDocument (fileName, true);
		}
		
		public override bool Equals (object o)
		{
			FileNavigationPoint fp = o as FileNavigationPoint;
			return fp != null && fp.fileName == fileName;
		}
		
//		public bool Transient {
//			get { return transient; }
//			set { transient = value; }
//		}
	}
}
