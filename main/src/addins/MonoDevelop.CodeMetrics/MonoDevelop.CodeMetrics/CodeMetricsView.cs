//
// CodeMetricsView.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.CodeMetrics
{
	public class CodeMetricsView : AbstractViewContent
	{
		CodeMetricsWidget widget;
		public override Gtk.Widget Control {
			get {
				return widget;
			}
		}
		
		public CodeMetricsView ()
		{
			widget = new CodeMetricsWidget ();
			base.IsViewOnly  = true;
			base.ContentName = "Code Metrics";
		}
		
		public override void Dispose ()
		{
			if (widget != null) {
				widget.Destroy ();
				widget = null;
			}
			base.Dispose ();
		}

		public override void Load (string fileName)
		{
			throw new System.InvalidOperationException (); 
		}
		
		public void Add (string fileName)
		{
			widget.Add (fileName);
		}
		
		public void Add (ProjectFile projectFile)
		{
			widget.Add (projectFile);
		}
		
		public void Add (Project project)
		{
			widget.Add (project);
		}
		
		public void Add (SolutionFolder combine)
		{
			widget.Add (combine);
		}
		
		public void Add (WorkspaceItem item)
		{
			widget.Add (item);
		}
		
		public void Run ()
		{
			widget.Run ();
		}
	}
}
