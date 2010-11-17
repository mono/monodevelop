// 
// Main.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Instrumentation;
using OSXIntegration.Framework;

namespace Mono.Instrumentation.Monitor
{
	class App
	{
		static IInstrumentationService service;
		
		public static void Main (string[] args)
		{
			Application.Init ();
			if (args.Length == 0) {
				ShowHelp ();
				return;
			}
			if (args [0] == "-c") {
				if (args.Length != 2) {
					ShowHelp ();
					return;
				}
				service = InstrumentationService.GetRemoteService (args[1]);
				try {
					service.GetCategories ();
				} catch (Exception ex) {
					MessageDialog md = new MessageDialog (null, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, "Could not connect to instrumentation service: " + ex.Message);
					md.Run ();
					md.Destroy ();
					return;
				}
			} else if (args.Length == 1) {
				LoadServiceData (args[0]);
			} else {
				ShowHelp ();
				return;
			}
			
			InstrumentationViewerDialog win = new InstrumentationViewerDialog ();
			win.Show ();
			
			if (MacIntegration.PlatformDetection.IsMac) {
				try {
					Carbon.SetProcessName ("MDMonitor");
					
					ApplicationEvents.Quit += delegate (object sender, ApplicationQuitEventArgs e) {
						Application.Quit ();
						e.Handled = true;
					};
					
					ApplicationEvents.Reopen += delegate (object sender, ApplicationEventArgs e) {
						if (win != null) {
							win.Deiconify ();
							win.Visible = true;
							e.Handled = true;
						}
					};
				} catch (Exception ex) {
					Console.Error.WriteLine ("Installing Mac AppleEvent handlers failed. Skipping.\n" + ex);
				}
				try {
					win.InstallMacGlobalMenu ();
				} catch (Exception ex) {
					Console.Error.WriteLine ("Installing Mac IGE Main Menu failed. Skipping.\n" + ex);
				}
			}
			
			Application.Run ();
		}
		
		static void ShowHelp ()
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("  mdmonitor <log-file>       : Open log file");
			Console.WriteLine ("  mdmonitor -c <host>:<port> : Connect to running service");
		}
		
		public static IInstrumentationService Service {
			get {
				return service;
			}
			set {
				service = value;
			}
		}
		
		public static void LoadServiceData (string file)
		{
			service = InstrumentationService.LoadServiceDataFromFile (file);
			FromFile = true;
		}
		
		public static bool FromFile { get; private set; }
	}
	
}

