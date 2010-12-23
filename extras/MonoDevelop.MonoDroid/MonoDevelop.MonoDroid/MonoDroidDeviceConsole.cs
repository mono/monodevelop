// 
// MonoDroidDeviceConsole.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Core;
using Gtk;
using System.Diagnostics;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Docking;

namespace MonoDevelop.MonoDroid
{
	public class MonoDroidDeviceLog : Bin
	{
		LogView log;
		ProcessWrapper process;
		Label deviceLabel;
		
		public MonoDroidDeviceLog (IPadWindow container)
		{
			Stetic.BinContainer.Attach (this);
			DockItemToolbar toolbar = container.GetToolbar (PositionType.Top);
			
			var chooseDeviceButton = new Button () {
				Label = GettextCatalog.GetString ("Choose Device"),
			};
			deviceLabel = new Label () {
				Xalign = 0,
			};
			SetDeviceLabel ();
			var reconnectButton = new Button () {
				Label = GettextCatalog.GetString ("Reconnect"),
			};
			
			toolbar.Add (deviceLabel);
			toolbar.Add (chooseDeviceButton);
			toolbar.Add (reconnectButton);
			
			reconnectButton.Clicked += delegate {
				Disconnect ();
				if (Device != null)
					Connect ();
				else
					SetDeviceLabel ();
			};
			chooseDeviceButton.Clicked += delegate {
				Device = MonoDroidUtility.ChooseDevice (null);
			};
			
			log = new LogView ();
			this.Add (log);
			
			toolbar.ShowAll ();
			ShowAll ();
		}
		
		void SetDeviceLabel ()
		{
			if (Device == null)
				deviceLabel.Text = GettextCatalog.GetString ("Device: (none)");
			else
				deviceLabel.Text = GettextCatalog.GetString ("Device: {0}", Device.ID);
		}
		
		AndroidDevice device;
		public AndroidDevice Device {
			get { return device; }
			set {
				if (value == device)
					return;
				device = value;
				SetDeviceLabel ();
				if (device != null)
					Connect ();
			}
		}
		
		void Disconnect ()
		{
			if (this.process == null)
				return;
			var process = this.process;
			this.process = null;
			
			log.WriteConsoleLogText ("\nDisconnected\n");
			
			if (!process.HasExited)
				process.Kill ();
			else if (process.ExitCode != 0)
				log.WriteError (string.Format ("Unknown error {0}\n", process.ExitCode));
			
			process.Dispose ();
		}
		
		void Connect ()
		{
			log.Clear ();
			log.WriteConsoleLogText ("Connecting...\n");
			
			process = MonoDroidFramework.Toolbox.LogCat (Device, OnProcessOutput, OnProcessError);
			process.Exited += delegate {
				Disconnect ();
			};
			process.EnableRaisingEvents = true;
		}
		
		void OnProcessOutput (object sender, string message)
		{
			log.WriteText (message);
		}
		
		void OnProcessError (object sender, string message)
		{
			log.WriteText (message);
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			Disconnect ();
		}
	}
	
	class MonoDroidDeviceLogPad : AbstractPadContent
	{
		MonoDroidDeviceLog widget;
		
		public override void Initialize (IPadWindow container)
		{
			base.Initialize (container);
			widget = new MonoDroidDeviceLog (container);
		}
		
		public override Widget Control {
			get { return widget; }
		}
	}
}

