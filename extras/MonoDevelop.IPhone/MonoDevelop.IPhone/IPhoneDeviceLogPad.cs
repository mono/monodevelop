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
using System.Collections;
using System.Diagnostics;

using Gtk;

using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.IPhone
{
	class IPhoneDeviceLogPad : AbstractPadContent
	{
		IPhoneDeviceLog widget;
		
		public override void Initialize (IPadWindow container)
		{
			base.Initialize (container);
			widget = new IPhoneDeviceLog (container);
		}
		
		public override Widget Control {
			get { return widget; }
		}
		
		public override void Dispose ()
		{
			widget.Destroy ();
		}
	}
	
	class IPhoneDeviceLog : Bin
	{
		LogView log;
		ProcessWrapper process;
		
		public IPhoneDeviceLog (IPadWindow container)
		{
			Stetic.BinContainer.Attach (this);
			DockItemToolbar toolbar = container.GetToolbar (PositionType.Top);
			
			var connectButton = new Button () {
				Label = GettextCatalog.GetString ("Connect"),
			};
			toolbar.Add (connectButton);
			
			connectButton.Clicked += delegate {
				Disconnect ();
				Connect ();
			};
			
			log = new LogView ();
			this.Add (log);
			
			toolbar.ShowAll ();
			ShowAll ();
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
			var mtouch = IPhoneSdks.MonoTouch.BinDir.Combine ("mtouch");
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
			Disconnect ();
			base.OnDestroyed ();
		}
	}
}

