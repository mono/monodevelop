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

namespace MonoDevelop.IPhone
{
	public class IPhoneDeviceConsole : Window
	{
		TextBuffer buffer = new TextBuffer (null);
		
		Queue<ConsoleMessage> queue = new Queue<ConsoleMessage> ();
		bool handlerRunning;
		
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
			
			var view = new TextView (buffer);
			view.Editable = false;
			
			var sw = new ScrolledWindow () { 
				ShadowType = ShadowType.In,
			};
			
			var closeButton = new Button (Gtk.Stock.Close);
			var reconnectButton = new Button () {
				Label = "Reconnect"
			};
			
			sw.Add (view);
			
			this.Add (vbox);
			vbox.PackEnd (bbox, false, false, 0);
			vbox.PackEnd (sw, true, true, 0);
			
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
			
			AppendText ("\nDisconnected\n", false);
			
			if (!process.HasExited)
				process.Kill ();
			else if (process.ExitCode != 0)
				AppendText (string.Format ("Unknown error {0}\n", process.ExitCode), true);
			
			process.Dispose ();
			
			process = null;
		}
		
		void Connect ()
		{
			AppendText ("Connecting...\n", false);
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
		
		void AppendText (string text, bool error)
		{
			//FIXME: discard items if queue too big
			lock (queue) {
				queue.Enqueue (new ConsoleMessage (text, error));
				if (!handlerRunning) {
					handlerRunning = true;
					GLib.Idle.Add (IdleHandler);
				}
			}
		}
		
		bool IdleHandler ()
		{
			ConsoleMessage item;
			bool moreinQueue;
			lock (queue) {
				item = queue.Dequeue ();
				handlerRunning = moreinQueue = queue.Count > 0;
			}
			
			//TODO: prune old text from the buffer, different color for error, maybe some processing and highlighting
			var iter = buffer.EndIter;
			buffer.Insert (ref iter, item.Message);
			
			return moreinQueue;
		}
		
		void OnProcessOutput (object sender, string message)
		{
			AppendText (message, false);
		}
		
		void OnProcessError (object sender, string message)
		{
			AppendText (message, true);
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
		
		class ConsoleMessage
		{
			public ConsoleMessage (string message, bool isError)
			{
				this.Message = message;
				this.IsError = isError;
			}
			
			public string Message { get; private set; }
			public bool IsError { get; private set; }
		}
	}
}

