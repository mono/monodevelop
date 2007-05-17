//
// SolutionPad.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Resources;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui.Pads
{
	public class SolutionPad : TreeViewPad
	{
		public SolutionPad ()
		{
			IdeApp.ProjectOperations.CombineOpened += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (OnOpenCombine));
			IdeApp.ProjectOperations.CombineClosed += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (OnCloseCombine));
			Runtime.Properties.PropertyChanged += (PropertyEventHandler) Services.DispatchService.GuiDispatch (new PropertyEventHandler (TrackPropertyChange));
		}
		
		public override void Initialize (NodeBuilder[] builders, TreePadOption[] options)
		{
			base.Initialize (builders, options);
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null)
				LoadTree (IdeApp.ProjectOperations.CurrentOpenCombine);
		}
		
		void TrackPropertyChange (object o, MonoDevelop.Core.Properties.PropertyEventArgs e)
		{
			if (e.OldValue != e.NewValue && e.Key == "MonoDevelop.Core.Gui.ProjectBrowser.ShowExtensions") {
				RedrawContent ();
			}
		}
		
		protected virtual void OnOpenCombine (object sender, CombineEventArgs e)
		{
			LoadTree (e.Combine);
		}

		protected virtual void OnCloseCombine (object sender, CombineEventArgs e)
		{
			Clear ();
		}
	}
}
