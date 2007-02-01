//
// AspNetAppProject.cs: ASP.NET "Web Application" project type
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.IO;
using System.Xml;
using System.Collections;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Serialization;

using AspNetAddIn.Parser.Tree;
using AspNetAddIn.Parser;

namespace AspNetAddIn
{
	[DataInclude (typeof(AspNetAppProjectConfiguration))]
	public class AspNetAppProject : DotNetProject
	{
		//caching to avoid too much reparsing
		//may have to drop at some point to avoid memory issues
		private Hashtable cachedDocuments = new Hashtable ();
		
		[ItemProperty("XspParameters")]
		protected XspParameters xspParameters = new XspParameters ();
		
		[ItemProperty ("VerifyCodeBehindFields")]
		protected bool verifyCodeBehindFields = true;
		
		[ItemProperty ("VerifyCodeBehindEvents")]
		protected bool verifyCodeBehindEvents = true;
		
		#region properties
		
		public override string ProjectType {
			get  { return "AspNetApp"; }
		}
		
		public XspParameters XspParameters {
			get { return xspParameters; }
		}
		
		public bool VerifyCodeBehindFields {
			get { return verifyCodeBehindFields; }
			set { verifyCodeBehindFields = value; }
		}
		
		//TODO: make this do something
		public bool VerifyCodeBehindEvents {
			get { return verifyCodeBehindEvents; }
			set { verifyCodeBehindEvents = value; }
		}
		
		#endregion
		
		#region constructors
		
		public AspNetAppProject ()
		{
		}
		
		public AspNetAppProject (string languageName)
			: base (languageName)
		{
		}
		
		public AspNetAppProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
		}
		
		public override IConfiguration CreateConfiguration (string name)
		{
			AspNetAppProjectConfiguration conf = new AspNetAppProjectConfiguration ();
			
			conf.Name = name;
			conf.CompilationParameters = LanguageBinding.CreateCompilationParameters (null);
			
			return conf;
		}
		
		#endregion
		
		#region build/prebuild/execute
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context)
		{
			CopyReferencesToOutputPath (true);
			
			AspNetAppProjectConfiguration configuration = (AspNetAppProjectConfiguration) ActiveConfiguration;
			monitor.Log.WriteLine ("Running " + configuration.CompiledOutputName + " ...");
			
			IConsole console;
			if (configuration.ExternalConsole)
				console = context.ExternalConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
			else
				console = context.ConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
			
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor);
			
			string xsp = (configuration.ClrVersion == ClrVersion.Net_1_1)? "xsp" : "xsp2";
			string xspOptions = this.XspParameters.GetXspParameters ();
			string url = String.Format ("http://{0}:{1}", this.XspParameters.Address, this.XspParameters.Port);
			
			try {
				IExecutionHandler handler = context.ExecutionHandlerFactory.CreateExecutionHandler ("Native");
				if (handler == null) {
					monitor.ReportError ("Error launching web server: cannot obtain platform handler.", null);
					return;
				}
				
				IProcessAsyncOperation op = handler.Execute (xsp, xspOptions, configuration.SourceDirectory, console);
				monitor.CancelRequested += delegate {op.Cancel ();};
				operationMonitor.AddOperation (op);
				
				System.Threading.Thread t = new System.Threading.Thread (new System.Threading.ParameterizedThreadStart (LaunchWebBrowser));
				t.Start (url);
				
				op.WaitForCompleted ();
				monitor.Log.WriteLine ("The web server exited with code: {0}", op.ExitCode);
			} catch (Exception ex) {
				monitor.ReportError ("Error launching web server: cannot execute \"" + xsp + "\".", ex);
			} finally {
				operationMonitor.Dispose ();
				console.Dispose ();
			}
		}
		
		protected override void DoPreBuild (IProgressMonitor monitor)
		{
			AspNetAppProjectConfiguration conf = (AspNetAppProjectConfiguration) ActiveConfiguration;
			conf.SourceDirectory = BaseDirectory;
			
			base.DoPreBuild (monitor);
		}
		
		#endregion
		
		#region File utility methods
		
		public Document GetDocument (ProjectFile file)
		{
			Document doc = this.cachedDocuments [file] as Document;
			
			if (doc != null)
				return doc;
			
			switch (DetermineWebSubtype (file)) {
				case WebSubtype.WebForm:
				case WebSubtype.MasterPage:
				case WebSubtype.WebHandler:
				case WebSubtype.WebControl:
				case WebSubtype.WebService:
				case WebSubtype.Global:
					doc = new Document (file);
					this.cachedDocuments [file] = doc;
					return doc;
				default:
					return null;
			}
		}
		
		public WebSubtype DetermineWebSubtype (ProjectFile file)
		{
			return DetermineWebSubtype (System.IO.Path.GetExtension (file.Name));
		}
		
		public static WebSubtype DetermineWebSubtype (string extension)
		{
			//determine file type
			switch (extension.ToLower ().TrimStart ('.'))
			{
				case "aspx":
					return WebSubtype.WebForm;
				case "master":
					return WebSubtype.MasterPage;
				case "ashx":
					return WebSubtype.WebHandler;
				case "ascx":
					return WebSubtype.WebControl;
				case "asmx":
					return WebSubtype.WebService;
				case "asax":
					return WebSubtype.Global;
				case "gif":
				case "png":
				case "jpg":
					return WebSubtype.WebImage;
				case "skin":
					return WebSubtype.WebSkin;
				default:
					return WebSubtype.None;
			}
		}
		
		#endregion
		
		#region special files
		
		#endregion
		
		#region server/browser-related
		
		//confirm we can connect to server before opening browser; wait up to ten seconds
		private static void LaunchWebBrowser (object o)
		{
			string url = (string) o;
			
			//wait a bit for server to start
			System.Threading.Thread.Sleep (2000);
			
			//try to contact web server several times, because server may take a while to start
			int noOfRequests = 5;
			int timeout = 8000; //ms
			int wait = 1000; //ms
			
			for (int i = 0; i < noOfRequests; i++) {
				System.Net.WebRequest req = null;
				System.Net.WebResponse resp = null;
				
				try {
					req = System.Net.HttpWebRequest.Create (url);
					req.Timeout = timeout;
					resp = req.GetResponse ();
				} catch (System.Net.WebException exp) {
					
					// server has returned 404, 500 etc, which user will still want to see
					if (exp.Status == System.Net.WebExceptionStatus.ProtocolError) {
						resp = exp.Response;
						
					//last request has failed so show user the error
					} else if (i >= (noOfRequests - 1)) {
						string message = String.Format ("Could not connect to webserver {0}", url);
						MonoDevelop.Ide.Gui.IdeApp.Services.MessageService.ShowError (exp, message);
						
					//we still have requests to go, so cancel the current one and sleep for a bit
					} else {
						req.Abort ();
						System.Threading.Thread.Sleep (wait);
						continue;
					}
				}
			
				if (resp != null) {
					//TODO: a choice of browsers
					Gnome.Url.Show (url);
					break;
				}
			}
		}
		
		#endregion
		
		#region File event handlers
		
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			OnFileEvent (e);
			base.OnFileAddedToProject (e);
		}
		
		protected override void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			OnFileEvent (e);
			base.OnFileChangedInProject (e);
		}
		
		protected override void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			OnFileEvent (e);
			base.OnFilePropertyChangedInProject (e);
		}
		
		void OnFileEvent (ProjectFileEventArgs e)
		{
			this.cachedDocuments [e.ProjectFile] = null;
			
			//WebSubtype type = DetermineWebSubtype (e.ProjectFile);
			
			//switch (type)
			//{
				//special actions for various types
			//}
		}
		
		#endregion
	}
	
	public enum WebSubtype
	{
		WebForm,
		WebService,
		WebControl,
		MasterPage,
		WebHandler,
		WebSkin,
		WebImage,
		Global,
		None
	}
	
	public enum SpecialFiles
	{
	}
	
	public enum SpecialFiles20
	{
	}
}
