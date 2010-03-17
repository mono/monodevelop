//
// TaskListViewCodon.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2006 David Makovský
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
using System.Collections;
using System.ComponentModel;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode (Description="Registers a task list view to be shown in the task list pad.")]
	internal class TaskListViewCodon : ExtensionNode
	{
		ITaskListView view;
		
		[NodeAttribute("_label", "Display name of the view.", Localizable=true)]
		string label = null;
		
		[NodeAttribute("class", "Class of the view.")]
		string className;
		
		public ITaskListView View {
			get {
				if (view == null)
					view = CreateView ();
				return view; 
			}
		}
		
		public string Class {
			get { return className; }
		}
		
		public string Label {
			get { return label; }
		}		

		protected virtual ITaskListView CreateView ()
		{
			return (ITaskListView) Addin.CreateInstance (className, true);
		}
	}
}
