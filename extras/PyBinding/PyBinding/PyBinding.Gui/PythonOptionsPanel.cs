// OutputOptionsPanel.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;

namespace PyBinding.Gui
{
	public class OutputOptionsPanel : MultiConfigItemOptionsPanel
	{
		PythonOptionsWidget widget;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			widget = new PythonOptionsWidget ();
			widget.Show ();
			return widget;
		}

		public override void LoadConfigData ()
		{
			PythonConfiguration config = CurrentConfiguration as PythonConfiguration;
			
			widget.DefaultModule = config.Module;
			widget.Optimize = config.Optimize;
			widget.Runtime = config.Runtime;
			widget.PythonOptions = config.PythonOptions;
			widget.PythonPaths = config.Runtime.Site.Paths;
		}

		public override void ApplyChanges ()
		{
			PythonConfiguration config = CurrentConfiguration as PythonConfiguration;
			
			config.Module = widget.DefaultModule;
			config.Optimize = widget.Optimize;
			config.Runtime = widget.Runtime;
			config.PythonOptions = widget.PythonOptions;
			
			var paths = new List<string> (widget.PythonPaths);
			
			// look for added modules
			foreach (var path in paths) {
				if (!config.Runtime.Site.ContainsPath (path)) {
					Console.WriteLine ("Adding path {0}", path);
					config.Runtime.Site.AddPath (path);
				}
			}
			
			// look for removed
			foreach (var path in config.Runtime.Site.Paths) {
				if (!paths.Contains (path)) {
					Console.WriteLine ("Removing path {0}", path);
					config.Runtime.Site.RemovePath (path);
				}
			}
		}
	}
}
