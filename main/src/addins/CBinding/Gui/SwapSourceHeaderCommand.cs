// 
// SwapSourceHeaderCommand.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;

namespace CBinding.Gui
{

	/// <summary>
	/// Swaps the source/header for the active view
	/// </summary>
	public class SwapSourceHeaderCommand : CommandHandler
	{
		
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			
			var cp = doc.Project as CProject;
			if (cp != null) {
				string match = cp.MatchingFile (doc.FileName);
				if (match != null)
					IdeApp.Workbench.OpenDocument (match);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = false;
			
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			
			var cp = doc.Project as CProject;
			if (cp == null)
				return;
			
			info.Visible = true;
			
			string filename = doc.FileName;
			info.Enabled = (CProject.IsHeaderFile (filename) || cp.IsCompileable (filename))
				&& cp.MatchingFile (doc.FileName) != null;
		}
	}
}
