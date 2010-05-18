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
using System.IO;
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using System.Threading;
using System.Linq;

namespace UserInterfaceTests
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			int pa = 0;
			bool attach = false;
			if (args [pa] == "-a") {
				attach = true;
				pa++;
			}
			if (pa >= args.Length) {
				Console.WriteLine ("Test name not provided");
				return 1;
			}
			
			string testName = args[pa];
			
			Type testType = typeof(MainClass).Assembly.GetTypes ().FirstOrDefault (t => t.FullName == testName);
			
			if (testType == null)
				testType = typeof(MainClass).Assembly.GetTypes ().FirstOrDefault (t => t.Name == testName);
			
			if (testType == null) {
				Console.WriteLine ("Test not found: " + args[0]);
				return 1;
			}
			
			StressTest test = (StressTest) Activator.CreateInstance (testType);
			TestPlan plan = new TestPlan ();
			plan.Repeat = 1;
			pa++;
			
			if (pa < args.Length) {
				int rep;
				if (int.TryParse (args[pa], out rep)) {
					plan.Repeat = rep;
					pa++;
				}
			}
			
			while (pa < args.Length) {
				string arg = args [pa];
				int i = arg.IndexOf ('*');
				string tname = arg.Substring (0, i);
				string it = arg.Substring (i+1);
				int nit;
				if (!int.TryParse (it, out nit)) {
					Console.WriteLine ("Invalid number of iterations: " + it);
					return 1;
				}
				if (tname.Length == 0)
					plan.Iterations = nit;
				else {
					if (!test.HasTest (tname)) {
						Console.Write ("Unknown test: " + tname);
						return 1;
					}
					plan.SetIterationsForTest (tname, nit);
				}
				pa++;
			}
			
			AutoTestClientSession session = new AutoTestClientSession ();
			try {
				if (attach) {
					session.AttachApplication ();
				}
				else {
					string app = typeof(AutoTestClientSession).Assembly.Location;
					app = Path.Combine (Path.GetDirectoryName (app), "MonoDevelop.exe");
					session.StartApplication (app, "");
					Console.WriteLine ("Connected");
					session.WaitForEvent ("MonoDevelop.Ide.IdeInitialized");
				}
				Console.WriteLine ("Initialized");
				TestService.Session = session;
				
				test.Run (plan);
			} finally {
				if (!attach) {
					Console.WriteLine ("Press Enter to stop the test process");
					Console.ReadLine ();
				}
				session.Stop ();
			}
			return 0;
		}
	}
}

