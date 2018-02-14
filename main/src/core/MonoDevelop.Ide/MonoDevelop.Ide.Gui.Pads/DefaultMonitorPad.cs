// DefaultMonitorPad.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections;
using System.CodeDom.Compiler;
using System.IO;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using Gtk;
using Pango;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Core.Execution;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Pads
{	
	internal class DefaultMonitorPad : PadContent
	{
		LogView logView;
		Button buttonStop;
		ToggleButton buttonPin;
		Button buttonClear;
		bool progressStarted;
		LogViewProgressMonitor monitor;
		Pad statusSourcePad;
		
		string icon;
		int instanceNum;
		string typeTag;

		public DefaultMonitorPad (string typeTag, string icon, int instanceNum, string title, int titleInstanceNum)
		{
			this.instanceNum = instanceNum;
			this.typeTag = typeTag;
			this.Title = title;
			this.TitleInstanceNum = titleInstanceNum;
			
			this.icon = icon;

			logView = new LogView { Name = typeTag };
			if (instanceNum > 0)
				logView.Name += $"-{instanceNum}";

			IdeApp.Workspace.FirstWorkspaceItemOpened += OnCombineOpen;
			IdeApp.Workspace.LastWorkspaceItemClosed += OnCombineClosed;

			logView.ShowAll ();
		}

		protected override void Initialize (IPadWindow window)
		{
			window.Icon = icon;
			
			DockItemToolbar toolbar = window.GetToolbar (DockPositionType.Right);

			buttonStop = new Button (new ImageView (Stock.Stop, IconSize.Menu));
			buttonStop.Clicked += new EventHandler (OnButtonStopClick);
			buttonStop.TooltipText = GettextCatalog.GetString ("Stop");
			toolbar.Add (buttonStop);

			buttonClear = new Button (new ImageView (Stock.Broom, IconSize.Menu));
			buttonClear.Clicked += new EventHandler (OnButtonClearClick);
			buttonClear.TooltipText = GettextCatalog.GetString ("Clear console");
			toolbar.Add (buttonClear);

			buttonPin = new ToggleButton ();
			buttonPin.Image = new ImageView (Stock.PinUp, IconSize.Menu);
			buttonPin.Image.ShowAll ();
			buttonPin.Clicked += new EventHandler (OnButtonPinClick);
			buttonPin.TooltipText = GettextCatalog.GetString ("Pin output pad");
			toolbar.Add (buttonPin);
			toolbar.ShowAll ();
		}
		
		public LogView LogView {
			get { return logView; }
		}
		
		public Pad StatusSourcePad {
			get { return this.statusSourcePad; }
			set { this.statusSourcePad = value; }
		}
		
		internal OutputProgressMonitor CurrentMonitor {
			get { return monitor; }
		}
		
		void OnButtonClearClick (object sender, EventArgs e)
		{
			logView.Clear ();
		}

		void OnButtonStopClick (object sender, EventArgs e)
		{
			monitor.Cancel ();
		}

		void OnCombineOpen (object sender, EventArgs e)
		{
			logView.Clear ();
		}

		void OnCombineClosed (object sender, EventArgs e)
		{
			logView.Clear ();
		}
		
		void OnButtonPinClick (object sender, EventArgs e)
		{
			if (buttonPin.Active)
				((ImageView)buttonPin.Image).SetIcon (Stock.PinDown, IconSize.Menu);
			else
				((ImageView)buttonPin.Image).SetIcon (Stock.PinUp, IconSize.Menu);
		}
		
		public bool AllowReuse {
			get { return !progressStarted && !buttonPin.Active; }
		}

		internal bool ClearOnBeginProgress { get; set; } = true;

		public OutputProgressMonitor BeginProgress (string title)
		{
			progressStarted = true;

			if (ClearOnBeginProgress)
				logView.Clear ();

			monitor = (LogViewProgressMonitor) logView.GetProgressMonitor (ClearOnBeginProgress);

			Runtime.RunInMainThread (delegate {
				Window.HasNewData = false;
				Window.HasErrors = false;
				Window.IsWorking = true;
				buttonStop.Sensitive = true;
			});
			
			monitor.Completed += delegate {
				EndProgress ();
			};
			
			return monitor;
		}

		public void EndProgress ()
		{
			Runtime.RunInMainThread (delegate {
				if (Window != null) {
					Window.IsWorking = false;
					if (monitor.Errors.Length > 0)
						Window.HasErrors = true;
					else
						Window.HasNewData = true;
				}
				buttonStop.Sensitive = false;
				progressStarted = false;
				if (Window == null)
					buttonClear.Sensitive = false;
				
				if (monitor.Errors.Length > 0) {
					var e = monitor.Errors [monitor.Errors.Length - 1];
					IdeApp.Workbench.StatusBar.ShowMessage (Stock.Error, e.DisplayMessage);
					IdeApp.Workbench.StatusBar.SetMessageSourcePad (statusSourcePad);
				} else if (monitor.SuccessMessages.Length > 0) {
					IdeApp.Workbench.StatusBar.ShowMessage (monitor.SuccessMessages [monitor.SuccessMessages.Length - 1]);
					IdeApp.Workbench.StatusBar.SetMessageSourcePad (statusSourcePad);
				} else if (monitor.Warnings.Length > 0) {
					IdeApp.Workbench.StatusBar.ShowMessage (Stock.Warning, monitor.Warnings [monitor.Warnings.Length - 1]);
					IdeApp.Workbench.StatusBar.SetMessageSourcePad (statusSourcePad);
				}
			});
		}
	
		public override Control Control {
			get { return logView; }
		}
		
		public string DefaultPlacement {
			get { return "Bottom"; }
		}

		public string TypeTag {
			get {
				return typeTag;
			}
		}

		public string Title { get; set; }

		public int TitleInstanceNum { get; set; }

		public int InstanceNum {
			get {
				return instanceNum;
			}
		}
		
		public override void Dispose ()
		{
			logView.Clear ();
			IdeApp.Workspace.FirstWorkspaceItemOpened -= OnCombineOpen;
			IdeApp.Workspace.LastWorkspaceItemClosed -= OnCombineClosed;

			base.Dispose ();
		}
	}
}
