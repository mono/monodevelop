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
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Deployment;

using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.AspNet.Deployment;
using MonoDevelop.AspNet.Gui;

namespace MonoDevelop.AspNet
{
	[DataInclude (typeof(AspNetAppProjectConfiguration))]
	public class AspNetAppProject : DotNetProject, IDeployable
	{
		[ItemProperty("XspParameters", IsExternal=true)]
		protected XspParameters xspParameters = new XspParameters ();
		
		[ItemProperty ("VerifyCodeBehindFields", IsExternal=true)]
		protected bool verifyCodeBehindFields = true;
		
		[ItemProperty ("VerifyCodeBehindEvents", IsExternal=true)]
		protected bool verifyCodeBehindEvents = true;
		
		[ItemProperty("WebDeployTargets", IsExternal=true)]
		[ItemProperty ("Target", ValueType=typeof(WebDeployTarget), Scope=1)]
		protected WebDeployTargetCollection webDeployTargets = new WebDeployTargetCollection ();
		
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
		
		public WebDeployTargetCollection WebDeployTargets {
			get { return webDeployTargets; }
		}
		
		public override ClrVersion[] SupportedClrVersions {
			get {
				ClrVersion[] versions = base.SupportedClrVersions;
				if (versions == null)
					return null;
				
				bool ver1 = false, ver2 = false;
				foreach (ClrVersion version in versions) {
					if (version == ClrVersion.Net_1_1)
						ver1 = true;
					if (version == ClrVersion.Net_2_0)
						ver2 = true;
				}
				if (ver1) {
					if (ver2)
						return new ClrVersion[] { ClrVersion.Net_1_1, ClrVersion.Net_2_0 };
					else return new ClrVersion[] { ClrVersion.Net_1_1 };
				} else if (ver2) {
					return new ClrVersion[] { ClrVersion.Net_2_0 };
				}
				return null;
			}
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
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			AspNetAppProjectConfiguration conf = new AspNetAppProjectConfiguration ();
			
			conf.Name = name;
			conf.CompilationParameters = LanguageBinding.CreateCompilationParameters (null);
			conf.SourceDirectory = BaseDirectory;
			
			return conf;
		}
		
		#endregion
		
		//custom version of GetDeployFiles which puts libraries in the bin directory
		public DeployFileCollection GetDeployFiles (string configuration)
		{
			DeployFileCollection files = new DeployFileCollection ();
			
			//add files that are marked to 'deploy'
			//ASP.NET files etc all go relative to the application root
			foreach (ProjectFile pf in Files)
				if (pf.BuildAction == BuildAction.FileCopy)
					files.Add (new DeployFile (this, pf.FilePath, pf.RelativePath, WebTargetDirectory.SiteRoot));
			
			//add referenced libraries
			foreach (string refFile in GetReferenceDeployFiles (false, configuration))
				files.Add (new DeployFile (this, refFile, Path.GetFileName (refFile), WebTargetDirectory.AspNetBin));
			
			//add the compiled output file
			string outputFile = this.GetOutputFileName (configuration);
			if (!string.IsNullOrEmpty (outputFile))
				files.Add (new DeployFile (this, outputFile, Path.GetFileName (outputFile), WebTargetDirectory.AspNetBin));
			
			return files;
		}
		
		#region build/prebuild/execute
		
		
		protected override ICompilerResult DoBuild (IProgressMonitor monitor, string configuration)
		{
			//if no files are set to compile, then some compilers will error out
			//though this is valid with ASP.NET apps, so we just avoid calling the compiler in this case
			bool needsCompile = false;
			foreach (ProjectFile pf in Files) {
				if (pf.BuildAction == BuildAction.Compile) {
					needsCompile = true;
					break;
				}
			}
			
			ICompilerResult ret;
			if (needsCompile)
				ret = base.DoBuild (monitor, configuration);
			else
				ret = new DefaultCompilerResult ();
			
			// all this does is makes sure that references are copied after building
			// it's not strictly necessary, as the Run/Deploy commands do it too..
			// but some users expect it to happen during a compile, so it's easier all round this way
			//need to do this after the compile, as the compile phase removes copied references
			CopyReferencesToOutputPath (false, configuration);
			return ret;
		}
		
		static bool CheckXsp (string command)
		{
			try {
				ProcessWrapper p = Runtime.ProcessService.StartProcess (command, "--version", null, null);
				p.WaitForOutput ();
				return true;
			} catch {
				return false;
			}
		}
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context, string config)
		{
			//check XSP is available
			
			AspNetAppProjectConfiguration configuration = (AspNetAppProjectConfiguration) GetConfiguration (config);
			
			ClrVersion clrVersion = configuration.ClrVersion;
			string xspVersion = (clrVersion == ClrVersion.Net_1_1)? "xsp" : "xsp2";
			if (!CheckXsp (xspVersion)) {
				monitor.ReportError (string.Format ("The \"{0}\" web server cannot be started. Please ensure that it is installed.",xspVersion), null);
				return;
			}
			
			CopyReferencesToOutputPath (false, config);
			
			IConsole console = null;
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor);
			
			try {
				IExecutionHandler handler = context.ExecutionHandlerFactory.CreateExecutionHandler ("Native");
				if (handler == null)
					throw new Exception ("Could not obtain platform handler.");
				
				if (configuration.ExternalConsole)
					console = context.ExternalConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
				else
					console = context.ConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
			
				monitor.Log.WriteLine ("Running web server...");
				
				//set mono debug mode if project's in debug mode
				Dictionary<string, string> envVars = new Dictionary<string,string> (); 
				if (configuration.DebugMode)
					envVars ["MONO_OPTIONS"] = "--debug";
				
				IProcessAsyncOperation op = handler.Execute (xspVersion, XspParameters.GetXspParameters (), configuration.SourceDirectory, envVars, console);
				operationMonitor.AddOperation (op); //handles cancellation
				
				//launch a separate thread to detect the running server and launch a web browser
				string url = String.Format ("http://{0}:{1}", this.XspParameters.Address, this.XspParameters.Port);
				BrowserLauncherOperation browserLauncher = BrowserLauncher.LaunchWhenReady (url);
				operationMonitor.AddOperation (browserLauncher);
				
				//report errors from the browser launcher
				browserLauncher.Completed += delegate (IAsyncOperation blop) {
					if (!blop.Success)
						MessageService.ShowError (
						    GettextCatalog.GetString ("Error launching web browser"),
						    ((BrowserLauncherOperation)blop).Error.ToString ()
						);
				};
				
				op.WaitForCompleted ();
				monitor.Log.WriteLine ("The web server exited with code: {0}", op.ExitCode);
				
				//if server shut down before browser launched, abort browser launch
				if (!browserLauncher.IsCompleted) {
					browserLauncher.Cancel ();
					browserLauncher.WaitForCompleted ();
				}
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
		
		public WebSubtype DetermineWebSubtype (ProjectFile file)
		{
			if (LanguageBinding != null && LanguageBinding.IsSourceCodeFile (file.FilePath))
				return WebSubtype.Code;
			
			return DetermineWebSubtype (System.IO.Path.GetExtension (file.Name));
		}
		
		public static WebSubtype DetermineWebSubtype (string extension)
		{
			extension = extension.ToLower ().TrimStart ('.');
			
			//NOTE: No way to identify WebSubtype.Code from here
			//use the instance method for that
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
		
		#region Reference handling
		
		protected override void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (FileName == null) {
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
			foreach (ProjectReference reference in References) {
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
				LoggingService.LogWarning ("Could not modify application web.config in project " + this.Name, e); 
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
			if (FileName == null) {
				base.OnFileAddedToProject (e);
				return;
			}
			
			SetDefaultBuildAction (e.ProjectFile);
			
			if (Path.GetFullPath (e.ProjectFile.FilePath) == Path.GetFullPath (WebConfigPath))
				UpdateWebConfigRefs ();
			
			base.OnFileAddedToProject (e);
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
					if (Files.GetFile (Path.Combine (BaseDirectory, dir)) == null)
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
