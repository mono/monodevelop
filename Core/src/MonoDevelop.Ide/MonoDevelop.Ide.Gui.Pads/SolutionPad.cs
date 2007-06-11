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

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui.Pads
{
	public class SolutionPad : TreeViewPad
	{
		public SolutionPad ()
		{
// TODO: Dispatching ?
//			ProjectService.SolutionOpened += (EventHandler<SolutionEventArgs>) Services.DispatchService.GuiDispatch (new EventHandler<SolutionEventArgs> (OnOpenCombine));
//			ProjectService.SolutionClosed += (EventHandler<SolutionEventArgs>) Services.DispatchService.GuiDispatch (new EventHandler<SolutionEventArgs> (OnCloseCombine));
			ProjectService.SolutionOpened += new EventHandler<SolutionEventArgs> (OnOpenCombine);
			ProjectService.SolutionClosed += new EventHandler<SolutionEventArgs> (OnCloseCombine);
			Runtime.Properties.PropertyChanged += (PropertyEventHandler) Services.DispatchService.GuiDispatch (new PropertyEventHandler (TrackPropertyChange));
		}
		
		void TrackPropertyChange (object o, MonoDevelop.Core.Properties.PropertyEventArgs e)
		{
			if (e.OldValue != e.NewValue && e.Key == "MonoDevelop.Core.Gui.ProjectBrowser.ShowExtensions") {
				RedrawContent ();
			}
		}
		
		protected virtual void OnOpenCombine (object sender, SolutionEventArgs e)
		{
			LoadTree (e.Solution);
		}

		protected virtual void OnCloseCombine (object sender, SolutionEventArgs e)
		{
			Clear ();
		}
	}
}
