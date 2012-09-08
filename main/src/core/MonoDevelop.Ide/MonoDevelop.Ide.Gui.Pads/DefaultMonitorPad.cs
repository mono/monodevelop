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

namespace MonoDevelop.Ide.Gui.Pads
{	
	internal class DefaultMonitorPad : IPadContent
	{
		IPadWindow window;
		LogView logView;
		Button buttonStop;
		ToggleButton buttonPin;
		Button buttonClear;
		bool progressStarted;
		IAsyncOperation asyncOperation;
		LogViewProgressMonitor monitor;
		Pad statusSourcePad;
		
		string icon;
		string id;
		int instanceNum;
		string typeTag;

		public DefaultMonitorPad (string typeTag, string icon, int instanceNum)
		{
			this.instanceNum = instanceNum;
			this.typeTag = typeTag;
			
			this.icon = icon;

			logView = new LogView ();

			IdeApp.Workspace.FirstWorkspaceItemOpened += OnCombineOpen;
			IdeApp.Workspace.LastWorkspaceItemClosed += OnCombineClosed;

			Control.ShowAll ();
		}

		void IPadContent.Initialize (IPadWindow window)
		{
			this.window = window;
			window.Icon = icon;
			
			DockItemToolbar toolbar = window.GetToolbar (PositionType.Right);

			buttonStop = new Button (new Gtk.Image (Stock.Stop, IconSize.Menu));
			buttonStop.Clicked += new EventHandler (OnButtonStopClick);
			buttonStop.TooltipText = GettextCatalog.GetString ("Stop");
			toolbar.Add (buttonStop);

			buttonClear = new Button (new Gtk.Image (Stock.Broom, IconSize.Menu));
			buttonClear.Clicked += new EventHandler (OnButtonClearClick);
			buttonClear.TooltipText = GettextCatalog.GetString ("Clear console");
			toolbar.Add (buttonClear);

			buttonPin = new ToggleButton ();
			buttonPin.Image = new Gtk.Image (Stock.PinUp, IconSize.Menu);
			buttonPin.Image.ShowAll ();
			buttonPin.Clicked += new EventHandler (OnButtonPinClick);
			buttonPin.TooltipText = GettextCatalog.GetString ("Pin output pad");
			toolbar.Add (buttonPin);
			toolbar.ShowAll ();
		}
		
		public LogView LogView {
			get { return logView; }
		}
		
		public IPadWindow Window {
			get { return this.window; }
		}
		
		public Pad StatusSourcePad {
			get { return this.statusSourcePad; }
			set { this.statusSourcePad = value; }
		}
		
		internal IProgressMonitor CurrentMonitor {
			get { return monitor; }
		}
		
		void OnButtonClearClick (object sender, EventArgs e)
		{
			logView.Clear ();
		}

		void OnButtonStopClick (object sender, EventArgs e)
		{
			asyncOperation.Cancel ();
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
				((Gtk.Image)buttonPin.Image).Stock = (IconId) "md-pin-down";
			else
				((Gtk.Image)buttonPin.Image).Stock = (IconId) "md-pin-up";
		}
		
		public bool AllowReuse {
			get { return !progressStarted && !buttonPin.Active; }
		}
		
		public IProgressMonitor BeginProgress (string title)
		{
			progressStarted = true;
			
			logView.Clear ();
			monitor = logView.GetProgressMonitor ();
			asyncOperation = monitor.AsyncOperation;
			
			DispatchService.GuiDispatch (delegate {
				window.HasNewData = false;
				window.HasErrors = false;
				window.IsWorking = true;
				buttonStop.Sensitive = true;
			});
			
			monitor.AsyncOperation.Completed += delegate {
				EndProgress ();
			};
			
			return monitor;
		}

		public void EndProgress ()
		{
			DispatchService.GuiDispatch (delegate {
				if (window != null) {
					window.IsWorking = false;
					if (!asyncOperation.Success)
						window.HasErrors = true;
					else
						window.HasNewData = true;
				}
				buttonStop.Sensitive = false;
				progressStarted = false;
				if (window == null)
					buttonClear.Sensitive = false;
				
				if (monitor.Errors.Length > 0) {
					IdeApp.Workbench.StatusBar.ShowMessage (Stock.Error, monitor.Errors [monitor.Errors.Length - 1].Message);
					IdeApp.Workbench.StatusBar.SetMessageSourcePad (statusSourcePad);
				} else if (monitor.Messages.Length > 0) {
					IdeApp.Workbench.StatusBar.ShowMessage (monitor.Messages [monitor.Messages.Length - 1]);
					IdeApp.Workbench.StatusBar.SetMessageSourcePad (statusSourcePad);
				} else if (monitor.Warnings.Length > 0) {
					IdeApp.Workbench.StatusBar.ShowMessage (Stock.Warning, monitor.Warnings [monitor.Warnings.Length - 1]);
					IdeApp.Workbench.StatusBar.SetMessageSourcePad (statusSourcePad);
				}
			});
		}
	
		public virtual Gtk.Widget Control {
			get { return logView; }
		}
		
		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public string DefaultPlacement {
			get { return "Bottom"; }
		}

		public string TypeTag {
			get {
				return typeTag;
			}
		}

		public int InstanceNum {
			get {
				return instanceNum;
			}
		}
		
		public virtual void Dispose ()
		{
			logView.Clear ();
			IdeApp.Workspace.FirstWorkspaceItemOpened -= OnCombineOpen;
			IdeApp.Workspace.LastWorkspaceItemClosed -= OnCombineClosed;
		}
	
		public void RedrawContent()
		{
		}
	}
}
