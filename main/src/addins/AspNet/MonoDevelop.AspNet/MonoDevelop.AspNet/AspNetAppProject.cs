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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Deployment;
using MonoDevelop.Core.Assemblies;

using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.AspNet.Deployment;
using MonoDevelop.AspNet.Gui;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;

namespace MonoDevelop.AspNet
{
	[DataInclude (typeof(AspNetAppProjectConfiguration))]
	public class AspNetAppProject : DotNetAssemblyProject
	{
		[ItemProperty("XspParameters", IsExternal=true)]
		protected XspParameters xspParameters = new XspParameters ();
		
		[ItemProperty ("VerifyCodeBehindFields", IsExternal=true)]
		protected bool verifyCodeBehindFields = true;
		
		[ItemProperty ("VerifyCodeBehindEvents", IsExternal=true)]
		protected bool verifyCodeBehindEvents = true;
		
		[ItemProperty("WebDeployTargets", IsExternal=true)]
		[ItemProperty ("Target", ValueType=typeof(WebDeployTarget), Scope="*")]
		protected WebDeployTargetCollection webDeployTargets = new WebDeployTargetCollection ();
		
		RegistrationCache registrationCache;
		CodeBehindTypeNameCache codebehindTypeNameCache;
		
		#region properties
		
		public override string ProjectType {
			get  { return "AspNetApp"; }
		}
		
		public override bool IsLibraryBasedProjectType {
			get { return true; }
		}
		
		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();
			
			if (FileFormat.Id == "MD1") {
				foreach (AspNetAppProjectConfiguration conf in Configurations) {
					//migrate settings once
					if (!conf.nonStandardOutputDirectory) {
						conf.nonStandardOutputDirectory = true;
						conf.OutputDirectory = String.IsNullOrEmpty (BaseDirectory)? "bin" : Path.Combine (BaseDirectory, "bin");
					}
				}
			}
			base.OnEndLoad ();
			
			//FIX: old version of MD didn't set CompileTarget to Library for ASP.NET projects, 
			// but implicitly assumed they were always libraries. This is not compatible with VS/MSBuild,
			// so we automatically "upgrade" this value. 
			if (CompileTarget != CompileTarget.Library)
				CompileTarget = CompileTarget.Library;
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
		
		internal RegistrationCache RegistrationCache {
			get {
				if (registrationCache == null)
					registrationCache = new RegistrationCache (this);
				return registrationCache;
			}
		}
		
		#endregion
		
		#region constructors
		
		public AspNetAppProject ()
		{
			Init ();
		}
		
		public AspNetAppProject (string languageName)
			: base (languageName)
		{
			Init ();
		}
		
		public AspNetAppProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			Init ();
			
			var binPath = info == null? (FilePath)"bin" : info.BinPath;
			foreach (AspNetAppProjectConfiguration cfg in Configurations)
				cfg.OutputDirectory = binPath;
		}	
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new AspNetAppProjectConfiguration (name);
			conf.CopyFrom (base.CreateConfiguration (name));
			conf.OutputDirectory = BaseDirectory.IsNullOrEmpty? "bin" : (string)BaseDirectory.Combine ("bin");			
			return conf;
		}
		
		void Init ()
		{
			codebehindTypeNameCache = new CodeBehindTypeNameCache (this);
		}
		
		#endregion
		
		public override void Dispose ()
		{
			codebehindTypeNameCache.Dispose ();
			RegistrationCache.Dispose ();
			base.Dispose ();
		}
		
		#region build/prebuild/execute
		
		
		protected override BuildResult DoBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
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
			
			if (needsCompile)
				return base.DoBuild (monitor, configuration);
			else
				return new BuildResult ();
		}
		
		ExecutionCommand CreateExecutionCommand (ConfigurationSelector config, AspNetAppProjectConfiguration configuration)
		{
			return new AspNetExecutionCommand () {
				ClrVersion = configuration.ClrVersion,
				DebugMode = configuration.DebugMode,
				XspParameters = XspParameters,
				BaseDirectory = BaseDirectory,
				TargetRuntime = TargetRuntime,
				TargetFramework = TargetFramework,
				UserAssemblyPaths = this.GetUserAssemblyPaths (config),
				EnvironmentVariables = configuration.EnvironmentVariables,
			};
		}
		
		protected override bool OnGetCanExecute (MonoDevelop.Projects.ExecutionContext context, ConfigurationSelector config)
		{
			var configuration = (AspNetAppProjectConfiguration) GetConfiguration (config);
			var cmd = CreateExecutionCommand (config, configuration);
			return context.ExecutionHandler.CanExecute (cmd);
		}
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector config)
		{
			//check XSP is available
			
			var configuration = (AspNetAppProjectConfiguration) GetConfiguration (config);
			var cmd = CreateExecutionCommand (config, configuration);
			
			IConsole console = null;
			var operationMonitor = new AggregatedOperationMonitor (monitor);

			bool isXsp = true; //FIXME: fix this when it might not be true - should delegate to the ExecutionHandler
			
			try {
				//HACK: check XSP exists first, because error UX is cleaner w/o displaying a blank console pad.
				if (isXsp) {
					try {
						AspNetExecutionHandler.GetXspPath ((AspNetExecutionCommand)cmd);
					} catch (UserException ex) {
						MessageService.ShowError (
							GettextCatalog.GetString ("Could not launch ASP.NET web server"),
						    ex.Message);
						throw;
					}
				}

				if (configuration.ExternalConsole)
					console = context.ExternalConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
				else
					console = context.ConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
				
				string url = String.Format ("http://{0}:{1}", this.XspParameters.Address, this.XspParameters.Port);
				
				
				if (isXsp) {
					console = new XspBrowserLauncherConsole (console, delegate {
						BrowserLauncher.LaunchDefaultBrowser (url);
					});
				}
			
				monitor.Log.WriteLine ("Running web server...");
				
				var op = context.ExecutionHandler.Execute (cmd, console);
				operationMonitor.AddOperation (op); //handles cancellation
				
				if (!isXsp)
					BrowserLauncher.LaunchDefaultBrowser (url);
				
				op.WaitForCompleted ();
				
				monitor.Log.WriteLine ("The web server exited with code: {0}", op.ExitCode);
				
			} catch (Exception ex) {
				if (!(ex is UserException)) {
					LoggingService.LogError ("Could not launch ASP.NET web server.", ex);
				}
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
			return DetermineWebSubtype (file.Name);
		}
		
		public static WebSubtype DetermineWebSubtype (string fileName)
		{
			string extension = System.IO.Path.GetExtension (fileName);
			if (extension == null)
				return WebSubtype.None;
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
			case "css":
				return WebSubtype.Css;
			case "xhtml":
			case "html":
			case "htm":
				return WebSubtype.Html;
			case "js":
				return WebSubtype.JavaScript;
			default:
				return WebSubtype.None;
			}
		}
		
		#endregion
		
		#region special files
		
		#endregion
		
		public ICompilation ResolveAssemblyDom (string assemblyName)
		{
			var parsed = SystemAssemblyService.ParseAssemblyName (assemblyName);
			if (string.IsNullOrEmpty (parsed.Name))
				return null;
			
			var dllName = parsed.Name + ".dll";
			
			foreach (var reference in References) {
				if (reference.ReferenceType == ReferenceType.Package || reference.ReferenceType == ReferenceType.Assembly) {
					foreach (string refPath in reference.GetReferencedFileNames (null))
						if (Path.GetFileName (refPath) == dllName)
							return new ICSharpCode.NRefactory.TypeSystem.Implementation.SimpleCompilation (TypeSystemService.LoadAssemblyContext (TargetRuntime, TargetFramework, refPath));
				} else if (reference.ReferenceType == ReferenceType.Project && parsed.Name == reference.Reference) {
					var p = ParentSolution.FindProjectByName (reference.Reference) as DotNetProject;
					if (p == null) {
						LoggingService.LogWarning ("Project '{0}' referenced from '{1}' could not be found", reference.Reference, this.Name);
						continue;
					}
					return TypeSystemService.GetCompilation (p);
				}
			}
			
			string path = GetAssemblyPath (assemblyName);
			if (path != null)
				return new ICSharpCode.NRefactory.TypeSystem.Implementation.SimpleCompilation (TypeSystemService.LoadAssemblyContext (TargetRuntime, TargetFramework, path));
			return null;
		}
		
		string GetAssemblyPath (string assemblyName)
		{
			var parsed = SystemAssemblyService.ParseAssemblyName (assemblyName);
			if (string.IsNullOrEmpty (parsed.Name))
				return null;
			
			string localName = Path.Combine (Path.Combine (BaseDirectory, "bin"), parsed.Name + ".dll");
			if (File.Exists (localName))
				return localName;
			
			assemblyName = AssemblyContext.GetAssemblyFullName (assemblyName, TargetFramework);
			if (assemblyName == null)
				return null;
			assemblyName = AssemblyContext.GetAssemblyNameForVersion (assemblyName, TargetFramework);
			if (assemblyName == null)
				return null;
			return AssemblyContext.GetAssemblyLocation (assemblyName, TargetFramework);
		}
		
		public ProjectFile ResolveVirtualPath (string virtualPath, string relativeToFile)
		{
			string name = VirtualToLocalPath (virtualPath, relativeToFile);
			if (name == null)
				return null;
			return Files.GetFile (name);
		}
		
		public string VirtualToLocalPath (string virtualPath, string relativeToFile)
		{
			if (virtualPath == null || virtualPath.Length == 0 || virtualPath[0] == '/'
			    	|| virtualPath.IndexOf (':') > -1)
				return null;

			FilePath relativeToDir;
			if (virtualPath.Length > 1 && virtualPath[0] == '~') {
				if (virtualPath[1] == '/')
					virtualPath = virtualPath.Substring (2);
				else
					virtualPath = virtualPath.Substring (1);
				relativeToDir = this.BaseDirectory;
			} else {
				relativeToDir = String.IsNullOrEmpty (relativeToFile)
					? BaseDirectory
					: (FilePath) Path.GetDirectoryName (relativeToFile);
			}
			
			virtualPath = virtualPath.Replace ('/', Path.DirectorySeparatorChar);
			return relativeToDir.Combine (virtualPath).FullPath;
		}
		
		public string LocalToVirtualPath (string filename)
		{
			string rel = FileService.AbsoluteToRelativePath (BaseDirectory, filename);
			return "~/" + rel.Replace (Path.DirectorySeparatorChar, '/');
		}
		
		public string LocalToVirtualPath (ProjectFile file)
		{
			return LocalToVirtualPath (file.FilePath);
		}
		
		#region Reference handling
		
		protected override void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (Loading) {
				base.OnReferenceAddedToProject (e);
				return;
			}
			
			UpdateWebConfigRefs ();
			
			base.OnReferenceAddedToProject (e);
		}
		
		protected override void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (Loading) {
				base.OnReferenceAddedToProject (e);
				return;
			}
			
			UpdateWebConfigRefs ();
			
			base.OnReferenceRemovedFromProject (e);
		}
		
		void UpdateWebConfigRefs ()
		{
			var refs = new List<string> ();
			foreach (var reference in References) {
				//local copied assemblies are copied to the bin directory so ASP.NET references them automatically
				if (reference.LocalCopy && (reference.ReferenceType == ReferenceType.Project || reference.ReferenceType == ReferenceType.Assembly))
					continue;
				if (string.IsNullOrEmpty (reference.Reference))
					continue;
				//these assemblies are referenced automatically by ASP.NET
				if (IsSystemReference (reference.Reference))
				    continue;
				//bypass non dotnet projects
				if ((reference.ReferenceType == ReferenceType.Project) &&
				    (!(reference.OwnerProject.ParentSolution.FindProjectByName (reference.Reference) is DotNetProject)))
						continue;
				refs.Add (reference.Reference);
			}
						
			var webConfig = GetWebConfig ();
			if (webConfig == null || !File.Exists (webConfig.FilePath))
				return;
			
			var textFile = MonoDevelop.Ide.TextFileProvider.Instance.GetEditableTextFile (webConfig.FilePath);
			//use textfile API because it's write safe (writes out to another file then moves)
			if (textFile == null)
				textFile = MonoDevelop.Projects.Text.TextFile.ReadFile (webConfig.FilePath);
				
			//can't use System.Web.Configuration.WebConfigurationManager, as it can only access virtual paths within an app
			//so need full manual handling
			try {
				System.Xml.XmlDocument doc = new XmlDocument ();
				
				//FIXME: PreserveWhitespace doesn't handle whitespace in attribute lists
				//doc.PreserveWhitespace = true;
				doc.LoadXml (textFile.Text);
				
				//hunt our way to the assemblies element, creating elements if necessary
				XmlElement configElement = doc.DocumentElement;
				if (configElement == null || string.Compare (configElement.Name, "configuration", StringComparison.OrdinalIgnoreCase) != 0) {
					configElement = (XmlElement) doc.AppendChild (doc.CreateNode (XmlNodeType.Document, "configuration", null));
				}
				XmlElement webElement = GetNamedXmlElement (doc, configElement, "system.web");			
				XmlElement compilationNode = GetNamedXmlElement (doc, webElement, "compilation");
				XmlElement assembliesNode = GetNamedXmlElement (doc, compilationNode, "assemblies");
				
				List<XmlNode> existingAdds = new List<XmlNode> ();
				foreach (XmlNode node in assembliesNode)
					if (string.Compare (node.Name, "add", StringComparison.OrdinalIgnoreCase) == 0)
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
				
				StringWriter sw = new StringWriter ();
				XmlTextWriter tw = new XmlTextWriter (sw);
				tw.Formatting = Formatting.Indented;
				doc.WriteTo (tw);
				tw.Flush ();
				textFile.Text = sw.ToString ();
				
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
				if (elem != null && string.Compare (elem.Name, name, StringComparison.OrdinalIgnoreCase) == 0) {
					result = elem;
					break;
				}
			}
			if (result == null) {
				result = (XmlElement) parent.AppendChild (doc.CreateElement (name));
			}
			return result;
		}
		
		ProjectFile GetWebConfig ()
		{
			var webConf = this.BaseDirectory.Combine ("web.config");
			foreach (var file in this.Files)
				if (string.Compare (file.FilePath.ToString (), webConf, StringComparison.OrdinalIgnoreCase) == 0)
					return file;
			return null;
		}
		
		bool IsWebConfig (FilePath file)
		{
			var webConf = this.BaseDirectory.Combine ("web.config");
			return (string.Compare (file, webConf, StringComparison.OrdinalIgnoreCase) == 0);
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
		
		protected override void OnFileChangedInProject (MonoDevelop.Projects.ProjectFileEventArgs e)
		{
			//if (!DisableCodeBehindGeneration) {
			//FIXME implement codebehind updates
			
			base.OnFileChangedInProject (e);
		}
		
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (Loading) {
				base.OnFileAddedToProject (e);
				return;
			}

			bool webConfigChange = false;
			List<string> filesToAdd = new List<string> ();
			
			foreach (ProjectFileEventInfo fargs in e) {
				IEnumerable<string> files = MonoDevelop.DesignerSupport.CodeBehind.GuessDependencies
					(this, fargs.ProjectFile, groupedExtensions);
				if (files != null)
					filesToAdd.AddRange (files);
				if (IsWebConfig (fargs.ProjectFile.FilePath))
					webConfigChange = true;
			}
			
			if (webConfigChange)
				UpdateWebConfigRefs ();
			
			//let the base fire the event before we add files
			//don't want to fire events out of order of files being added
			base.OnFileAddedToProject (e);
			
			//make sure that the parent and child files are in the project
			foreach (string file in filesToAdd) {
				//NOTE: this only adds files if they are not already in the project
				AddFile (file);
			}
		}
		
		public override string GetDefaultBuildAction (string fileName)
		{
			
			WebSubtype type = DetermineWebSubtype (fileName);
			if (type == WebSubtype.Code)
				return BuildAction.Compile;
			if (type != WebSubtype.None)
				return BuildAction.Content;
			else
				return base.GetDefaultBuildAction (fileName);
		}
		
		static string[] groupedExtensions =  { ".aspx", ".master", ".ashx", ".ascx", ".asmx", ".asax" };
		
		#endregion
		
		public virtual IEnumerable<string> GetSpecialDirectories ()
		{
			if (TargetFramework.ClrVersion != MonoDevelop.Core.ClrVersion.Net_2_0)
				yield break;
			yield return "App_Browsers";
			yield return "App_Data";
			yield return "App_GlobalResources";
			yield return "App_LocalResources";
			yield return "Theme";
			
			// For "web site" projects
			// "App_WebReferences", "App_Resources","App_Themes", "App_Code",
		}
		
		protected override IList<string> GetCommonBuildActions ()
		{
			return new string[] {
				BuildAction.None,
				BuildAction.Compile,
				BuildAction.Content,
				BuildAction.EmbeddedResource,
			};
		}
		
		public IType GetCodebehindType (string fileName)
		{
			string typeName = GetCodebehindTypeName (fileName);
			if (typeName != null) {
				var dom = TypeSystemService.GetCompilation (this);
				if (dom != null)
					return ReflectionHelper.ParseReflectionName (typeName).Resolve (dom);
			}
			return null;
		}
		
		public string GetCodebehindTypeName (string fileName)
		{
			lock (codebehindTypeNameCache)
				return codebehindTypeNameCache.GetCodeBehindTypeName (fileName);
		}
		
		class CodeBehindTypeNameCache : ProjectFileCache<AspNetAppProject,string>
		{
			public CodeBehindTypeNameCache (AspNetAppProject proj) : base (proj)
			{
			}
			
			protected override string GenerateInfo (string filename)
			{
				try {
					var doc = TypeSystemService.ParseFile (filename, DesktopService.GetMimeTypeForUri (filename), File.ReadAllText (filename)) as AspNetParsedDocument;
					if (doc != null && !string.IsNullOrEmpty (doc.Info.InheritedClass))
						return doc.Info.InheritedClass;
				} catch (Exception ex) {
					LoggingService.LogError ("Error reading codebehind name for file '" + filename + "'", ex);
				}
				return null;
			}
			
			public string GetCodeBehindTypeName (string fileName)
			{
				return Get (fileName);
			}
		}
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
		Css,
		Html,
		JavaScript,
	}
	
	
	class XspBrowserLauncherConsole : IConsole
	{
		IConsole real;
		LineInterceptingTextWriter outWriter;
		Action launchBrowser;
		
		const int MAX_WATCHED_LINES = 30;
		
		public XspBrowserLauncherConsole (IConsole real, Action launchBrowser)
		{
			this.real = real;
			this.launchBrowser = launchBrowser;
		}
		
		public void Dispose ()
		{
			real.Dispose ();
		}
		
		public event EventHandler CancelRequested {
			add { real.CancelRequested += value; }
			remove { real.CancelRequested -= value; }
		}
		
		public TextReader In {
			get { return real.In; }
		}
		
		public TextWriter Out {
			get {
				if (outWriter == null)
					outWriter = new LineInterceptingTextWriter (real.Out, delegate {
						if (outWriter.GetLine ().StartsWith ("Listening on port: ")) {
							launchBrowser ();
							outWriter.FinishedIntercepting = true;
						} else if (outWriter.LineCount > MAX_WATCHED_LINES) {
							outWriter.FinishedIntercepting = true;
						}
					});
				return outWriter;
			}
		}
		
		public TextWriter Error {
			get { return real.Error; }
		}
		
		public TextWriter Log {
			get { return real.Log; }
		}
		
		public bool CloseOnDispose {
			get { return real.CloseOnDispose; }
		}
	}
}
