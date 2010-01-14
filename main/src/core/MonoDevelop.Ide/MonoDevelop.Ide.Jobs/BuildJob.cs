// 
// Job.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui;
namespace MonoDevelop.Ide.Jobs
{
public class BuildJob: OutputPadJob
	{
		IBuildTarget target;
		ConfigurationSelector config;
		
		public BuildJob (IBuildTarget target, ConfigurationSelector config)
		{
			this.target = target;
			this.config = config;
			Title = GettextCatalog.GetString ("Build");
			Icon = MonoDevelop.Core.Gui.Stock.BuildCombine;
			if (target is SolutionEntityItem)
				Description = GettextCatalog.GetString ("Build project '{0}' using configuration '{1}'", target.Name, config.ToString ());
			else if (target is Solution)
				Description = GettextCatalog.GetString ("Build solution '{0}' using configuration '{1}'", target.Name, config.ToString ());
			else if (target is Workspace)
				Description = GettextCatalog.GetString ("Build workspace '{0}' using configuration '{1}'", target.Name, config.ToString ());
			else if (target is SolutionFolder)
				Description = GettextCatalog.GetString ("Build projects in folder '{1}' using configuration '{1}'", target.Name, config.ToString ());
			else
				Description = GettextCatalog.GetString ("Build item '{0}' using configuration '{1}'", target.Name, config.ToString ());
		}
		
		protected override void OnRun (IProgressMonitor monitor)
		{
			IdeApp.ProjectOperations.InternalBuild (monitor, target, config);
		}
		
		public override bool Reusable {
			get {
				return true;
			}
		}

		public override void FillExtendedStatusPanel (JobInstance jobi, Gtk.HBox expandedPanel, out Gtk.Widget mainWidget)
		{
			base.FillExtendedStatusPanel (jobi, expandedPanel, out mainWidget);
			Gtk.Button b = new Gtk.Button (GettextCatalog.GetString ("Error List"));
			b.Relief = Gtk.ReliefStyle.None;
			b.Show ();
			b.Clicked += delegate {
				MonoDevelop.Ide.Tasks.TaskService.ShowErrors ();
			};
			expandedPanel.PackStart (b, false, false, 0);
			expandedPanel.ReorderChild (b, 0);
			mainWidget = b;
		}

	}
}
