﻿// 
// CommandHandlers.cs
//  
// Author:
//       Piotr Dowgiallo <sparekd@gmail.com>
// 
// Copyright (c) 2012 Piotr Dowgiallo
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
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.AspNet.Projects;

namespace MonoDevelop.AspNet.Commands
{
	static class MvcCommandsCommonHandler
	{
		public static void Update (CommandInfo info)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.Project == null || doc.ParsedDocument == null) {
				info.Enabled = info.Visible = false;
				return;
			}
			var aspFlavor = doc.Project.GetService<AspNetAppProjectFlavor> ();
			if (aspFlavor == null || !aspFlavor.IsAspMvcProject) {
				info.Enabled = info.Visible = false;
				return;
			}

			var method = MethodDeclarationAtCaret.Create (doc);
			if (method.IsMethodFound && method.IsParentMvcController () && method.IsMvcViewMethod ())
				return;

			info.Enabled = info.Visible = false;
		}
	}
}
