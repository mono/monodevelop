// 
// MoonlightProject.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Xml;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.AspNet.Gui;

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightProject : DotNetProject
	{
		
		public MoonlightProject ()
			: base ()
		{
		}
		
		public MoonlightProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			//set parameters to ones required for Moonlight build
			ClrVersion = MonoDevelop.Core.ClrVersion.Clr_2_1;
			CompileTarget = CompileTarget.Library;
			foreach (DotNetProjectConfiguration parameter in Configurations) {
				parameter.OutputDirectory = Path.Combine (".", "ClientBin");
			}
		}
		
		public override string ProjectType {
			get { return "Moonlight"; }
		}
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context, string configuration)
		{
			string[] pages = { (Name??"TestPage") + ".html", "TestPage.html", "Default.html", "default.html", "Index.html", "index.html" };
			string testPage = null;
			for (int i = 0; i < pages.Length; i++) {
				testPage = Path.Combine (BaseDirectory, pages[i]);
				if (File.Exists (testPage)) {
					break;
				}else if (i + 1 >= pages.Length) {
					monitor.ReportError (GettextCatalog.GetString ("Could not find test HTML file '{0}'.", testPage), null);
					return;
				}
			}
			
			using (AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor)) {
				//launch web browser
				string testPagePath = "file://" + testPage.Replace (Path.PathSeparator, '/');
				IAsyncOperation browserLauncher = BrowserLauncher.LaunchWhenReady (testPagePath);
				operationMonitor.AddOperation (browserLauncher);
				browserLauncher.WaitForCompleted ();
				if (!browserLauncher.Success)
					monitor.ReportError (GettextCatalog.GetString ("Failed to open test page in browser."), null);
			}
		}
	}
}
