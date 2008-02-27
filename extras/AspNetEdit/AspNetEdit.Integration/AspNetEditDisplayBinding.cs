//
// AspNetEditDisplayBinding.cs: A secondary display binding for AspNetEdit.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Projects;

using MonoDevelop.AspNet;

namespace AspNetEdit.Integration
{
	
	public class AspNetEditDisplayBinding : ISecondaryDisplayBinding
	{
		public bool CanAttachTo (IViewContent content)
		{
			if (content.GetContent (typeof(MonoDevelop.Ide.Gui.Content.IEditableTextBuffer)) == null)
				return false;
			
			string s = Path.GetExtension (content.IsUntitled? content.UntitledName : content.ContentName);
			WebSubtype type = AspNetAppProject.DetermineWebSubtype (s);
			
			switch (type) {
				case WebSubtype.WebForm:
					return true;
				default:
					return false;
			}
		}
		
		public ISecondaryViewContent CreateSecondaryViewContent (IViewContent viewContent)
		{			
			return new AspNetEditViewContent (viewContent);
		}
	}
}
