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

namespace Mono.Instrumentation.Monitor
{
	class App
	{
		static IInstrumentationService service;
		
		public static void Main (string[] args)
		{
			Application.Init ();
			if (args.Length == 0) {
				Console.WriteLine ("Usage: mdmonitor host:port");
				return;
			}
			
			service = InstrumentationService.GetRemoteService (args[0]);
			try {
				service.GetCategories ();
			} catch (Exception ex) {
				MessageDialog md = new MessageDialog (null, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, "Could not connect to instrumentation service: " + ex.Message);
				md.Run ();
				md.Destroy ();
				return;
			}
			
			InstrumentationViewerDialog win = new InstrumentationViewerDialog ();
			win.Show ();
			Application.Run ();
		}
		
		public static IInstrumentationService Service {
			get {
				return service;
			}
		}
	}
	
}

