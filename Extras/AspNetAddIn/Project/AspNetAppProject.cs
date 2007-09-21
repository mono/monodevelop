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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Deployment;

using AspNetAddIn.Parser.Tree;
using AspNetAddIn.Parser;

namespace AspNetAddIn
{
	[DataInclude (typeof(AspNetAppProjectConfiguration))]
	public class AspNetAppProject : DotNetProject, IDeployable
	{
		//caching to avoid too much reparsing
		//may have to drop at some point to avoid memory issues
		private Dictionary<ProjectFile, Document> cachedDocuments = new Dictionary<ProjectFile, Document> ();
		
		[ItemProperty("XspParameters")]
		protected XspParameters xspParameters = new XspParameters ();
		
		[ItemProperty ("VerifyCodeBehindFields")]
		protected bool verifyCodeBehindFields = true;
		
		[ItemProperty ("VerifyCodeBehindEvents")]
		protected bool verifyCodeBehindEvents = true;
		
		//used true while the project is being loaded
		bool loading = false;
		
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
			commonInit ();
		}
		
		public AspNetAppProject (string languageName)
			: base (languageName)
		{
			commonInit ();
		}
		
		public AspNetAppProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			commonInit ();
		}
		
		
		private void commonInit ()
		{
			//AspNetAppProjectConfiguration needs SourceDirectory set so it can append "bin" to determine the output path
			Configurations.ConfigurationAdded += delegate (object ob, ConfigurationEventArgs args) {
				AspNetAppProjectConfiguration conf = (AspNetAppProjectConfiguration) args.Configuration;
				conf.SourceDirectory = BaseDirectory;
			};
		}
		
		public override void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			loading = true;
			base.Deserialize (handler, data);
			loading = false;
		}
		
		//AspNetAppProjectConfiguration needs SourceDirectory set so it can append "bin" to determine the output path
		public override string FileName {
			get {
				return base.FileName;
			}
			set {
				base.FileName = value;
				foreach (AspNetAppProjectConfiguration conf in Configurations)
					conf.SourceDirectory = BaseDirectory;
			}
		}		
		
		public override IConfiguration CreateConfiguration (string name)
		{
			AspNetAppProjectConfiguration conf = new AspNetAppProjectConfiguration ();
			
			conf.Name = name;
			conf.CompilationParameters = LanguageBinding.CreateCompilationParameters (null);
			conf.SourceDirectory = BaseDirectory;
			
			return conf;
		}
		
		#endregion
		
		//custom version of GetDeployFiles which puts libraries in the bin directory
		public DeployFileCollection GetDeployFiles ()
		{
			DeployFileCollection files = new DeployFileCollection ();
			
			//add files that are marked to 'deploy'
			//ASP.NET files etc all go relative to the application root
			foreach (ProjectFile pf in ProjectFiles)
				if (pf.BuildAction == BuildAction.FileCopy)
					files.Add (new DeployFile (this, pf.FilePath, pf.RelativePath, TargetDirectory.ProgramFilesRoot));
			
			//add referenced libraries
			foreach (string refFile in GetReferenceDeployFiles (false))
				files.Add (new DeployFile (this, refFile, Path.GetFileName (refFile), TargetDirectory.ProgramFiles));
			
			//add the compiled output file
			string outputFile = this.GetOutputFileName ();
			if (!string.IsNullOrEmpty (outputFile))
				files.Add (new DeployFile (this, outputFile, Path.GetFileName (outputFile), TargetDirectory.ProgramFiles));
			
			return files;
		}
		
		#region build/prebuild/execute
		
		IProcessAsyncOperation StartXsp (IProgressMonitor monitor, ExecutionContext context, IConsole console)
		{
			AspNetAppProjectConfiguration configuration = (AspNetAppProjectConfiguration) ActiveConfiguration;
			
			string xsp = (configuration.ClrVersion == ClrVersion.Net_1_1)? "xsp" : "xsp2";
			string xspOptions = XspParameters.GetXspParameters ();
			
			IExecutionHandler handler = context.ExecutionHandlerFactory.CreateExecutionHandler ("Native");
			if (handler == null)
				throw new Exception ("Could not obtain platform handler.");
			
			string exports = string.Empty;
			if (configuration.DebugMode)
				exports = string.Format ("export MONO_OPTIONS=\"--debug\"");
			
			//construct a sh command so that we can do things like environment variables
			exports = exports.Replace ("\"", "\\\"");
			xspOptions = xspOptions.Replace ("\"", "\\\"");
			string shOptions = string.Format ("-c \"{0}; '{1}' {2}\"", exports, xsp, xspOptions);
			
			try {
				return handler.Execute ("sh", shOptions, configuration.SourceDirectory, console);
			} catch (Exception ex) {
				throw new Exception ("Could not execute 'sh " + shOptions + "'.", ex);
			}
		}
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context)
		{
			CopyReferencesToOutputPath (true);
			
			IConsole console = null;
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor);
			AspNetAppProjectConfiguration configuration = (AspNetAppProjectConfiguration) ActiveConfiguration;
			
			try {
				
				if (configuration.ExternalConsole)
					console = context.ExternalConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
				else
					console = context.ConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
			
				monitor.Log.WriteLine ("Running web server...");
				
				IProcessAsyncOperation op = StartXsp (monitor, context, console);
				monitor.CancelRequested += delegate {op.Cancel ();};
				operationMonitor.AddOperation (op);
				
				//launch a separate thread to detect te running server and launch a web browser
				System.Threading.Thread t = new System.Threading.Thread (new System.Threading.ParameterizedThreadStart (LaunchWebBrowser));
				op.Completed += delegate (IAsyncOperation dummy) {t.Abort ();};
				string url = String.Format ("http://{0}:{1}", this.XspParameters.Address, this.XspParameters.Port);
				if (!op.IsCompleted)
					t.Start (url);
				
				op.WaitForCompleted ();
				monitor.Log.WriteLine ("The web server exited with code: {0}", op.ExitCode);
			} catch (Exception ex) {
				monitor.ReportError ("Could not launch web server.", ex);
			} finally {
				operationMonitor.Dispose ();
				if (console != null)
					console.Dispose ();
			}
		}
		
		#endregion
		
		#region File utility methods
		
		public Document GetDocument (ProjectFile file)
		{
			Document doc = null;
			if (cachedDocuments.TryGetValue (file, out doc))
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
			if (LanguageBinding.IsSourceCodeFile (file.FilePath))
				return WebSubtype.Code;
			
			return DetermineWebSubtype (System.IO.Path.GetExtension (file.Name));
		}
		
		public static WebSubtype DetermineWebSubtype (string extension)
		{
			extension = extension.ToLower ().TrimStart ('.');
			
			//FIXME: No way to identify WebSubtype.Code
			switch (extension)
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
				case "config":
					return WebSubtype.Config;
				case "browser":
					return WebSubtype.BrowserDefinition;
				case "axd":
					return WebSubtype.Axd;
				case "sitemap":
					return WebSubtype.Sitemap;
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
			try {
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
							string message = GettextCatalog.GetString ("Could not connect to webserver {0}", url);
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
			} catch (System.Threading.ThreadAbortException) {}
		}
		
		#endregion
		
		#region Reference handling
		
		protected override void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (loading) {
				base.OnReferenceAddedToProject (e);
				return;
			}
			
			UpdateWebConfigRefs ();

			base.OnReferenceAddedToProject (e);
		}
		
		protected override void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			UpdateWebConfigRefs ();
			base.OnReferenceRemovedFromProject (e);
		}
		
		void UpdateWebConfigRefs ()
		{
			List<string> refs = new List<string> ();
			foreach (ProjectReference reference in ProjectReferences) {
				//local copied assemblies are copied to the bin directory so ASP.NET references them automatically
				if (reference.LocalCopy && (reference.ReferenceType == ReferenceType.Project || reference.ReferenceType == ReferenceType.Assembly))
					continue;
				if (string.IsNullOrEmpty (reference.Reference))
					continue;
				//these assemblies are referenced automatically by ASP.NET
				if (IsSystemReference (reference.Reference))
				    continue;
				refs.Add (reference.Reference);
			}
						
			string webConfigPath = WebConfigPath;
			if (!File.Exists (webConfigPath))
				return;
			
			MonoDevelop.Projects.Text.IEditableTextFile textFile = 
				MonoDevelop.DesignerSupport.OpenDocumentFileProvider.Instance.GetEditableTextFile (webConfigPath);
			//use textfile API because it's write safe (writes out to another file then moves)
			if (textFile == null)
				textFile = MonoDevelop.Projects.Text.TextFile.ReadFile (webConfigPath);
				
			//can't use System.Web.Configuration.WebConfigurationManager, as it can only access virtual paths within an app
			//so need full manual handling
			try {
				System.Xml.XmlDocument doc = new XmlDocument ();
				
				//FIXME: PreserveWhitespace doesn't handle whitespace in attribute lists
				//doc.PreserveWhitespace = true;
				doc.LoadXml (textFile.Text);
				
				//hunt our way to the assemblies element, creating elements if necessary
				XmlElement configElement = doc.DocumentElement;
				if (configElement == null || string.Compare (configElement.Name, "configuration", StringComparison.InvariantCultureIgnoreCase) != 0) {
					configElement = (XmlElement) doc.AppendChild (doc.CreateNode (XmlNodeType.Document, "configuration", null));
				}
				XmlElement webElement = GetNamedXmlElement (doc, configElement, "system.web");			
				XmlElement compilationNode = GetNamedXmlElement (doc, webElement, "compilation");
				XmlElement assembliesNode = GetNamedXmlElement (doc, compilationNode, "assemblies");
				
				List<XmlNode> existingAdds = new List<XmlNode> ();
				foreach (XmlNode node in assembliesNode)
					if (string.Compare (node.Name, "add", StringComparison.InvariantCultureIgnoreCase) == 0)
					    existingAdds.Add (node);
				
				//add refs to the doc if they're not in it
				foreach (string reference in refs) {
					int index = 0;
					bool found = false;
					while (index < existingAdds.Count) {
						XmlNode node = existingAdds[index];
						XmlAttribute att = (XmlAttribute) node.Attributes.GetNamedItem ("assembly");
						if (att == null)
							continue;
						string refAtt = att.Value;
						if (refAtt != null && refAtt == reference) {
							existingAdds.RemoveAt (index);
							found = true;
							break;
						} else {
							index++;
						}
					}
					if (!found) {
						XmlElement newAdd = doc.CreateElement ("add");
						XmlAttribute newAtt = doc.CreateAttribute ("assembly");
						newAtt.Value = reference;
						newAdd.Attributes.Append (newAtt);
						assembliesNode.AppendChild (newAdd);
					}
				}
				
				//any nodes that weren't removed from the existingAdds list are old/redundant, so remove from doc
				foreach (XmlNode node in existingAdds)
					assembliesNode.RemoveChild (node);
				
				using (StringWriter writer = new StringWriter ()) {
					doc.Save (writer);
					textFile.Text = writer.ToString ();
				}
				MonoDevelop.Projects.Text.TextFile tf = textFile as MonoDevelop.Projects.Text.TextFile;
				if (tf != null)
					tf.Save ();
			} catch (Exception e) {
				Runtime.LoggingService.Warn ((object) ("Could not modify application web.config in project " + this.Name), e); 
			}
		}
		
		
		XmlElement GetNamedXmlElement (XmlDocument doc, XmlElement parent, string name)
		{
			XmlElement result = null;
			foreach (XmlNode node in parent.ChildNodes) {
				XmlElement elem = node as XmlElement;
				if (elem != null && string.Compare (elem.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0) {
					result = elem;
					break;
				}
			}
			if (result == null) {
				result = (XmlElement) parent.AppendChild (doc.CreateElement (name));
			}
			return result;
		}
		
		string WebConfigPath {
			get { return Path.Combine (this.BaseDirectory, "web.config"); }
		}
		
		bool IsSystemReference (string reference)
		{
			foreach (string defaultPrefix in defaultAssemblyRefPrefixes)
				if (reference.StartsWith (defaultPrefix))
					return true;
			return false;
		}
		
		static string[] defaultAssemblyRefPrefixes = new string[] {
			"mscorlib", 
			"System,",
			"System.Configuration,",
			"System.Web,",
			"System.Data,",
			"System.Web.Services,",
			"System.Xml,",
			"System.Drawing,",
			"System.EnterpriseServices,",
			"System.Web.Mobile,",
		};
		
		#endregion
		
		#region File event handlers
		
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (loading) {
				base.OnFileAddedToProject (e);
				return;
			}
			
			SetDefaultBuildAction (e.ProjectFile);
			InvalidateDocumentCache (e.ProjectFile);
			
			if (Path.GetFullPath (e.ProjectFile.FilePath) == Path.GetFullPath (WebConfigPath))
				UpdateWebConfigRefs ();
			
			base.OnFileAddedToProject (e);
		}
		
		protected override void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			if (!loading) {
				InvalidateDocumentCache (e.ProjectFile);
			}
			base.OnFileChangedInProject (e);
		}
		
		//protected override void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		//{
		//	base.OnFilePropertyChangedInProject (e);
		//}
		
		void InvalidateDocumentCache (ProjectFile file)
		{
			if (cachedDocuments.ContainsKey (file))
				cachedDocuments.Remove (file);
		}
		
		void SetDefaultBuildAction (ProjectFile file)
		{
			//make sure web files are deployed, not built
			WebSubtype type = DetermineWebSubtype (file);
			if (type != WebSubtype.None && type != WebSubtype.Code)
				file.BuildAction = BuildAction.FileCopy;
		}
		
		#endregion
		
		public List<string> GetNotPresentSpecialDirectories ()
		{
			List<string> notPresent = new List<string> ();
			
			if (ClrVersion == MonoDevelop.Core.ClrVersion.Net_2_0)
				foreach (string dir in specialDirs20)
					if (ProjectFiles.GetFile (Path.Combine (BaseDirectory, dir)) == null)
						notPresent.Add (dir);
			
			return notPresent;
		}
	
		static readonly string [] specialDirs20 = new string [] {
			"App_Code",
			"App_Themes",
			"App_Browsers",
			"App_Data",
			"App_WebReferences",
			"App_Resources",
			"App_LocalResources",
			"App_GlobalResources",		
		};
	}
	
	public enum WebSubtype
	{
		None = 0,
		Code,
		WebForm,
		WebService,
		WebControl,
		MasterPage,
		WebHandler,
		WebSkin,
		WebImage,
		BrowserDefinition,
		Sitemap,
		Global,
		Config,
		Axd,
	}
	
	
}
