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
		
		public override void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			base.Deserialize (handler, data);
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
					monitor.ReportError ("Can not obtain platform handler to execute \"" + xsp + "\"", null);
					return;
				}
				
				IProcessAsyncOperation op = handler.Execute (xsp, xspOptions, configuration.SourceDirectory, console);
				operationMonitor.AddOperation (op);
				
				//TODO: a choice of browsers, maybe an internal browser too
				System.Threading.Thread t = new System.Threading.Thread (new System.Threading.ParameterizedThreadStart (LaunchWebBrowser));
				t.Start (url);
				
				op.WaitForCompleted ();
				monitor.Log.WriteLine ("The application exited with code: {0}", op.ExitCode);
			} catch (Exception ex) {
				monitor.ReportError ("Can not execute \"" + xsp + "\"", ex);
			} finally {
				operationMonitor.Dispose ();
				console.Dispose ();
			}
		}
		
		protected override void DoPreBuild (IProgressMonitor monitor)
		{ 
			foreach (ProjectFile file in ProjectFiles) {
				if (DetermineWebSubtype (file) == WebSubtype.WebForm)
					VerifyCodeBehind (file, monitor);
			}
		}
		
		
		protected override ICompilerResult DoBuild (IProgressMonitor monitor)
		{
			AspNetAppProjectConfiguration conf = (AspNetAppProjectConfiguration) ActiveConfiguration;
			conf.SourceDirectory = BaseDirectory;
			
			string binDir = System.IO.Path.Combine (BaseDirectory, "bin");
			if (!System.IO.Directory.Exists (binDir)) {
				monitor.ReportWarning ("Directory \"" + binDir + "\" directory does not exist; creating it...");
				System.IO.Directory.CreateDirectory (binDir);
			}
			
			ICompilerResult res = LanguageBinding.Compile (ProjectFiles, ProjectReferences, conf, monitor);
			CopyReferencesToOutputPath (false);
			return res;
		}
		
		#endregion
		
		#region CodeBehind verification 
		
		public void VerifyCodeBehind (ProjectFile file, IProgressMonitor monitor)
		{
			Document doc = GetDocument (file);
			IClass cls = GetCodeBehindClass (file);
			//TODO: close file views before modifying them
			
			if (cls == null) {
				monitor.ReportWarning ("Cannot find CodeBehind class \"" + doc.Info.InheritedClass  + "\" for  file \"" + file.Name + "\".");
				return;
			}
			
			//TODO: currently case-sensitive, so some languages may not like this
			bool ignoreCase = false;
			
			if (cls != null) {
				foreach (System.CodeDom.CodeMemberField member in doc.MemberList.List.Values) {
					bool found = false;
					
					//check for identical property names
					foreach (IProperty prop in cls.Properties) {
						if (string.Compare (prop.Name, member.Name, ignoreCase) == 0) {
							monitor.ReportWarning ("Cannot add field \"" + member.Name + "\" from \"" + file.Name +
							                            "\" to CodeBehind file \"" + cls.BodyRegion.FileName +
							                            "\", because there is already a property with that name.");
							found = true;
							break;
						}
					}
					
					//check for identical method names
					foreach (IMethod meth in cls.Methods) {
						if (string.Compare (meth.Name, member.Name, ignoreCase) == 0) {
							monitor.ReportWarning ("Cannot add field \"" + member.Name + "\" from \"" + file.Name +
							                            "\" to CodeBehind file \"" + cls.BodyRegion.FileName +
							                            "\", because there is already a method with that name on line " + meth.BodyRegion.BeginLine + ".");
							found = true;
							break;
						}
					}
					
					//check for matching fields
					foreach (IField field in cls.Fields) {
						if (string.Compare (field.Name, member.Name, ignoreCase) == 0) {
							found = true;
							
							//check whether they're accessible
							if (!(field.IsPublic || field.IsProtected || field.IsProtectedOrInternal)) {
								monitor.ReportWarning ("Cannot add field \"" + member.Name + "\" from \"" + file.Name +
							                            "\" to CodeBehind file \"" + cls.BodyRegion.FileName +
							                            "\", because there is already a field with that name, " +
							                            "which cannot be accessed by the deriving page.");
								break;
							}
							
							//check they're the same type
							//TODO: check for base type compatibility
							if (string.Compare (member.Type.BaseType, field.ReturnType.FullyQualifiedName, ignoreCase) != 0) {
								monitor.ReportWarning ("Cannot add field \"" + member.Name + "\" with type \"" + member.Type.BaseType + "\" from \"" + file.Name +
							                            "\" to CodeBehind file \"" + cls.BodyRegion.FileName +
							                            "\", because there is already a field with that name, " +
							                            "which appears to have a different type, \"" + field.ReturnType.FullyQualifiedName + "\".");
								break;
							}
						}
					}
					
					if (!found)
						MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.CodeRefactorer.AddMember (cls, member);
				}
			}
		}
		
		public IList GetAllCodeBehindClasses ()
		{
			System.Collections.ArrayList classes = new System.Collections.ArrayList ();
			
			foreach (ProjectFile file in this.ProjectFiles) {
				Document doc = GetDocument (file);
			
				if (doc.Info.InheritedClass == null)
					continue;
			
				IParserContext ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (this);
				IClass cls = ctx.GetClass (doc.Info.InheritedClass);
				if (cls == null)
					throw new Exception ("The CodeBehind class \"" + doc.Info.InheritedClass + "\" cannot be found");
				
				classes.Add (cls);
			}
			
			return classes;
		}
		
		public IClass GetCodeBehindClass (ProjectFile file)
		{
			Document doc = GetDocument (file);
			
			if (doc.Info.InheritedClass == null)
				return null;
			
			IParserContext ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (this);
			IClass cls = ctx.GetClass (doc.Info.InheritedClass);
			if (cls == null)
				throw new Exception ("The CodeBehind class \"" + doc.Info.InheritedClass + "\" cannot be found");
			
			return cls;
		}
		
		#endregion
		
		#region File utility methods
		
		public Document GetDocument (ProjectFile file)
		{
			if (this.cachedDocuments [file] == null)
				this.cachedDocuments [file] =  new Document (file);
			
			return (Document) this.cachedDocuments [file];
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
				case "gif":
					goto case "jpg"; //mono compiler messing up; trivial fallthroughs should be possible
				case "png":
					goto case "jpg";
				case "jpg":
					return WebSubtype.WebImage;
				case "skin":
					return WebSubtype.WebSkin;
				default:
					return WebSubtype.None;
			}
		}
		
		#endregion
		
		#region server/browser-related
		
		//confirm we can connect to server before opening browser; wait up to ten seconds
		private static void LaunchWebBrowser (object o)
		{
			string url = (string) o;
			
			for (int i = 0; i < 10; i++) {
				try {
					System.Net.WebRequest req = System.Net.HttpWebRequest.Create (url);
					req.Timeout = 1000;
					System.Net.WebResponse resp = req.GetResponse ();
					if (resp != null) {
						Gnome.Url.Show (url);
						return;
					}
				} catch (System.Net.WebException) {
					System.Threading.Thread.Sleep (1000);
				}
			}
			
			MonoDevelop.Ide.Gui.IdeApp.Services.MessageService.ShowErrorFormatted ("Could not connect to webserver {0}", new string [] {url});
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
		None
	}
}
