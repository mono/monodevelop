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

namespace MonoDevelop.MonoDroid
{
	public class MonoDroidDeviceConsole : Window
	{
		LogView log;
		ProcessWrapper process;
		Label deviceLabel;
		
		public MonoDroidDeviceConsole () : base ("MonoDroid Device Console")
		{
			BorderWidth = 6;
			
			//FIXME: persist these values
			DefaultWidth = 600;
			DefaultHeight = 400;
			
			var vbox = new VBox () {
				Spacing = 12
			};
			
			var bbox = new HButtonBox () {
				Layout = ButtonBoxStyle.End,
			};
			
			HBox deviceBox = new HBox () {
				Spacing = 6
			};
			var chooseDeviceButton = new Button () {
				Label = "Choose Device"
			};
			deviceLabel = new Label () {
				Xalign = 0,
			};
			SetDeviceLabel ();
			var reconnectButton = new Button () {
				Label = "Reconnect"
			};
			
			deviceBox.PackStart (deviceLabel, true, true, 0);
			deviceBox.PackStart (chooseDeviceButton, false, false, 0);
			deviceBox.PackStart (reconnectButton, false, false, 0);
			
			var closeButton = new Button (Gtk.Stock.Close);
			
			log = new LogView ();
			
			this.Add (vbox);
			vbox.PackStart (deviceBox, false, false, 0);
			vbox.PackStart (log, true, true, 0);
			vbox.PackStart (bbox, false, false, 0);
			
			bbox.PackEnd (closeButton);
			
			closeButton.Clicked += delegate {
				 Destroy ();
			};
			DeleteEvent += delegate {
				Destroy ();
			};
			reconnectButton.Clicked += delegate {
				Disconnect ();
				Connect ();
			};
			chooseDeviceButton.Clicked += delegate {
				Device = MonoDroidUtility.ChooseDevice (this);
			};
			
			ShowAll ();
		}
		
		void SetDeviceLabel ()
		{
			if (Device == null)
				deviceLabel.Text = GettextCatalog.GetString ("Device: (none)");
			else
				deviceLabel.Text = GettextCatalog.GetString ("Device: {0}", Device.ID);
		}
		
		AndroidDevice _device;
		public AndroidDevice Device {
			get { return _device; }
			set {
				if (value == _device)
					return;
				_device = value;
				SetDeviceLabel ();
				if (_device != null)
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

		void DevicesUpdated (object sender, EventArgs e)
		{
			Connect ();
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
			instance = null;
			Disconnect ();
			base.OnDestroyed ();
		}

		static MonoDroidDeviceConsole instance;
		
		public static void Run ()
		{
			if (instance == null) {
				instance = new MonoDroidDeviceConsole ();
				MessageService.PlaceDialog (instance, MessageService.RootWindow);
				instance.Show ();
			}
			instance.Present ();
		}
	}
}

