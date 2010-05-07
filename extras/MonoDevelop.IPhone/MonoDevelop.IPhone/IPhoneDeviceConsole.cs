// 
// IPhoneDeviceConsole.cs
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
using Gtk;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;
using System.Diagnostics;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.IPhone
{
	public class IPhoneDeviceConsole : Window
	{
		LogView log;
		ProcessWrapper process;
		
		public IPhoneDeviceConsole () : base ("iPhone Device Console")
		{
			BorderWidth = 6;
			
			//FIXME: persist these values
			DefaultWidth = 400;
			DefaultHeight = 400;
			
			var vbox = new VBox () {
				Spacing = 12
			};
			
			var bbox = new HButtonBox () {
				Layout = ButtonBoxStyle.End,
			};
			
			var closeButton = new Button (Gtk.Stock.Close);
			var reconnectButton = new Button () {
				Label = "Reconnect"
			};
			
			log = new LogView ();
			
			this.Add (vbox);
			vbox.PackEnd (bbox, false, false, 0);
			vbox.PackEnd (log, true, true, 0);
			
			bbox.PackEnd (reconnectButton);
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
			
			ShowAll ();
			Connect ();
		}
		
		void Disconnect ()
		{
			if (process == null)
				return;
			
			log.WriteConsoleLogText ("\nDisconnected\n");
			
			if (!process.HasExited)
				process.Kill ();
			else if (process.ExitCode != 0)
				log.WriteError (string.Format ("Unknown error {0}\n", process.ExitCode));
			
			process.Dispose ();
			
			process = null;
		}
		
		void Connect ()
		{
			log.WriteConsoleLogText ("Connecting...\n");
			var mtouch = "/Developer/MonoTouch/usr/bin/mtouch";
			var psi = new ProcessStartInfo (mtouch, "-logdev") {
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			process = Runtime.ProcessService.StartProcess (psi, OnProcessOutput, OnProcessError, delegate {
				Disconnect ();
			});
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
			instance = null;
			
			base.OnDestroyed ();
		}

		static IPhoneDeviceConsole instance;
		
		public static void Run ()
		{
			if (instance == null)
				instance = new IPhoneDeviceConsole ();
			instance.Show ();
			instance.Present ();
		}
	}
}

