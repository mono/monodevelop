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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.AspNet.Execution;
using MonoDevelop.AspNet.WebForms;
using System.Threading.Tasks;

namespace MonoDevelop.AspNet.Projects
{
	[DataInclude (typeof(AspNetAppProjectConfiguration))]
	public class AspNetAppProjectFlavor : DotNetProjectExtension
	{
		[ItemProperty("XspParameters", IsExternal=true)]
		XspParameters xspParameters = new XspParameters ();

		WebFormsRegistrationCache registrationCache;
		WebFormsCodeBehindTypeNameCache codebehindTypeNameCache;

		#region properties

		protected override DotNetProjectFlags OnGetDotNetProjectFlags ()
		{
			return base.OnGetDotNetProjectFlags () | DotNetProjectFlags.IsLibrary;
		}

		public XspParameters XspParameters {
			get { return xspParameters; }
		}

		internal WebFormsRegistrationCache RegistrationCache {
			get {
				if (registrationCache == null)
					registrationCache = new WebFormsRegistrationCache (Project);
				return registrationCache;
			}
		}

		#endregion

		#region constructors

		protected override void Initialize ()
		{
			base.Initialize ();
			codebehindTypeNameCache = new WebFormsCodeBehindTypeNameCache (Project);
		}

		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			base.OnInitializeFromTemplate (projectCreateInfo, template);

			var binPath = projectCreateInfo == null? (FilePath)"bin" : projectCreateInfo.BinPath;
			foreach (var cfg in Project.Configurations.Cast<DotNetProjectConfiguration> ())
				cfg.OutputDirectory = binPath;
		}

		protected override SolutionItemConfiguration OnCreateConfiguration (string name, ConfigurationKind kind)
		{
			var conf = new AspNetAppProjectConfiguration (name);
			conf.CopyFrom (base.OnCreateConfiguration (name, kind));
			conf.OutputDirectory = Project.BaseDirectory.IsNullOrEmpty? "bin" : (string)Project.BaseDirectory.Combine ("bin");
			return conf;
		}

		public AspNetAppProjectConfiguration GetConfiguration (ConfigurationSelector configuration)
		{
			return (AspNetAppProjectConfiguration) Project.GetConfiguration (configuration);
		}

		#endregion

		public override void Dispose ()
		{
			codebehindTypeNameCache.Dispose ();
			RegistrationCache.Dispose ();
			base.Dispose ();
		}

		#region build/prebuild/execute

		protected override ProjectFeatures OnGetSupportedFeatures ()
		{
			return (base.OnGetSupportedFeatures () | ProjectFeatures.Execute) & ~ProjectFeatures.RunConfigurations;
		}

		protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			//if no files are set to compile, then some compilers will error out
			//though this is valid with ASP.NET apps, so we just avoid calling the compiler in this case
			bool needsCompile = false;
			foreach (ProjectFile pf in Project.Files) {
				if (pf.BuildAction == BuildAction.Compile) {
					needsCompile = true;
					break;
				}
			}

			if (needsCompile)
				return base.OnBuild (monitor, configuration, operationContext);
			return Task.FromResult (BuildResult.CreateSuccess ());
		}

		ExecutionCommand CreateExecutionCommand (ConfigurationSelector config, AspNetAppProjectConfiguration configuration)
		{
			return new AspNetExecutionCommand {
				ClrVersion = configuration.ClrVersion,
				DebugMode = configuration.DebugSymbols,
				XspParameters = XspParameters,
				BaseDirectory = Project.BaseDirectory,
				TargetRuntime = Project.TargetRuntime,
				TargetFramework = Project.TargetFramework,
				EnvironmentVariables = configuration.EnvironmentVariables,
			};
		}

		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			var cmd = CreateExecutionCommand (configuration, GetConfiguration (configuration));
			return context.ExecutionHandler.CanExecute (cmd);
		}

		protected async override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			//check XSP is available

			var cfg = GetConfiguration (configuration);
			var cmd = (AspNetExecutionCommand) CreateExecutionCommand (configuration, cfg);
			var browserExcTarget = context.ExecutionTarget as BrowserExecutionTarget;

			cmd.UserAssemblyPaths = await Project.GetUserAssemblyPaths (configuration, monitor.CancellationToken);

			OperationConsole console = null;

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

				console = context.ConsoleFactory.CreateConsole (monitor.CancellationToken);

				// The running Port value is now captured in the XspBrowserLauncherConsole object
				string url = String.Format ("http://{0}", XspParameters.Address);


				if (isXsp) {
					console = new XspBrowserLauncherConsole (console, delegate (string port) {
						if (browserExcTarget != null)
							browserExcTarget.DesktopApp.Launch (String.Format("{0}:{1}", url, port));
						else
							BrowserLauncher.LaunchDefaultBrowser (String.Format("{0}:{1}", url, port));
					});
				}

				monitor.Log.WriteLine (GettextCatalog.GetString ("Running web server..."));

				var op = context.ExecutionHandler.Execute (cmd, console);

				if (!isXsp) {
					if (browserExcTarget != null)
						browserExcTarget.DesktopApp.Launch (url);
					else
						BrowserLauncher.LaunchDefaultBrowser (url);
				}

				using (monitor.CancellationToken.Register (op.Cancel))
					await op.Task;

				monitor.Log.WriteLine (GettextCatalog.GetString ("The web server exited with code: {0}", op.ExitCode));

			} catch (Exception ex) {
				if (!(ex is UserException)) {
					LoggingService.LogError ("Could not launch ASP.NET web server.", ex);
				}
				monitor.ReportError (GettextCatalog.GetString ("Could not launch web server."), ex);
			} finally {
				if (console != null)
					console.Dispose ();
			}
		}

		#endregion

		#region File utility methods

		public WebSubtype DetermineWebSubtype (ProjectFile file)
		{
			if (Project.LanguageBinding != null && Project.LanguageBinding.IsSourceCodeFile (file.FilePath))
				return WebSubtype.Code;
			return DetermineWebSubtype (file.Name);
		}

		public static WebSubtype DetermineWebSubtype (string fileName)
		{
			string extension = Path.GetExtension (fileName);
			if (extension == null)
				return WebSubtype.None;
			extension = extension.ToUpperInvariant ().TrimStart ('.');

			//NOTE: No way to identify WebSubtype.Code from here
			//use the instance method for that
			switch (extension) {
			case "ASPX":
				return WebSubtype.WebForm;
			case "MASTER":
				return WebSubtype.MasterPage;
			case "ASHX":
				return WebSubtype.WebHandler;
			case "ASCX":
				return WebSubtype.WebControl;
			case "ASMX":
				return WebSubtype.WebService;
			case "ASAX":
				return WebSubtype.Global;
			case "GIF":
			case "PNG":
			case "JPG":
				return WebSubtype.WebImage;
			case "SKIN":
				return WebSubtype.WebSkin;
			case "CONFIG":
				return WebSubtype.Config;
			case "BROWSER":
				return WebSubtype.BrowserDefinition;
			case "AXD":
				return WebSubtype.Axd;
			case "SITEMAP":
				return WebSubtype.Sitemap;
			case "CSS":
				return WebSubtype.Css;
			case "XHTML":
			case "HTML":
			case "HTM":
				return WebSubtype.Html;
			case "JS":
				return WebSubtype.JavaScript;
			case "LESS":
				return WebSubtype.Less;
			case "SASS":
			case "SCSS":
				return WebSubtype.Sass;
			case "EOT":
			case "TTF":
			case "OTF":
			case "WOFF":
				return WebSubtype.Font;
			case "SVG":
				return WebSubtype.Svg;
			case "STYL":
				return WebSubtype.Stylus;
			case "CSHTML":
				return WebSubtype.Razor;
			default:
				return WebSubtype.None;
			}
		}

		#endregion

		#region special files

		#endregion

		public ProjectFile ResolveVirtualPath (string virtualPath, string relativeToFile)
		{
			string name = VirtualToLocalPath (virtualPath, relativeToFile);
			if (name == null)
				return null;
			return Project.Files.GetFile (name);
		}

		public string VirtualToLocalPath (string virtualPath, string relativeToFile)
		{
			if (string.IsNullOrEmpty (virtualPath) || virtualPath [0] == '/' || virtualPath.IndexOf (':') > -1)
				return null;

			FilePath relativeToDir;
			if (virtualPath.Length > 1 && virtualPath[0] == '~') {
				if (virtualPath[1] == '/')
					virtualPath = virtualPath.Substring (2);
				else
					virtualPath = virtualPath.Substring (1);
				relativeToDir = Project.BaseDirectory;
			} else {
				relativeToDir = String.IsNullOrEmpty (relativeToFile)
					? Project.BaseDirectory
					: (FilePath) Path.GetDirectoryName (relativeToFile);
			}

			virtualPath = virtualPath.Replace ('/', Path.DirectorySeparatorChar);
			return relativeToDir.Combine (virtualPath).FullPath;
		}

		public string LocalToVirtualPath (string filename)
		{
			string rel = FileService.AbsoluteToRelativePath (Project.BaseDirectory, filename);
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
			if (Project.Loading) {
				base.OnReferenceAddedToProject (e);
				return;
			}

			UpdateWebConfigRefs ();

			base.OnReferenceAddedToProject (e);
		}

		protected override void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (Project.Loading) {
				base.OnReferenceAddedToProject (e);
				return;
			}

			UpdateWebConfigRefs ();

			base.OnReferenceRemovedFromProject (e);
		}

		void UpdateWebConfigRefs ()
		{
			var refs = new List<string> ();
			foreach (var reference in Project.References) {
				//local copied assemblies are copied to the bin directory so ASP.NET references them automatically
				if (reference.LocalCopy && (reference.ReferenceType == ReferenceType.Project || reference.ReferenceType == ReferenceType.Assembly))
					continue;
				if (string.IsNullOrEmpty (reference.Reference))
					continue;
				//these assemblies are referenced automatically by ASP.NET
				if (WebFormsRegistrationCache.IsDefaultReference (reference.Reference))
				    continue;
				//bypass non dotnet projects
				if ((reference.ReferenceType == ReferenceType.Project) &&
				    (!(reference.ResolveProject (reference.OwnerProject.ParentSolution) is DotNetProject)))
						continue;
				refs.Add (reference.Reference);
			}

			var webConfig = GetWebConfig ();
			if (webConfig == null || !File.Exists (webConfig.FilePath))
				return;
			
			//use textfile API because it's write safe (writes out to another file then moves)
			var textFile = MonoDevelop.Ide.TextFileProvider.Instance.GetEditableTextFile (webConfig.FilePath);
				
			//can't use System.Web.Configuration.WebConfigurationManager, as it can only access virtual paths within an app
			//so need full manual handling
			try {
				var doc = new XmlDocument ();

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
						XmlNode node = existingAdds [index];
						XmlAttribute att = (XmlAttribute)node.Attributes.GetNamedItem ("assembly");
						if (att == null)
							continue;
						string refAtt = att.Value;
						if (refAtt != null && refAtt == reference) {
							existingAdds.RemoveAt (index);
							found = true;
							break;
						}
						index++;
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
				textFile.WriteTextTo (textFile.FileName); 
			} catch (Exception e) {
				LoggingService.LogWarning ("Could not modify application web.config in project " + Project.Name, e);
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
			var webConf = Project.BaseDirectory.Combine ("web.config");
			foreach (var file in Project.Files)
				if (string.Compare (file.FilePath.ToString (), webConf, StringComparison.OrdinalIgnoreCase) == 0)
					return file;
			return null;
		}

		bool IsWebConfig (FilePath file)
		{
			var webConf = Project.BaseDirectory.Combine ("web.config");
			return (string.Compare (file, webConf, StringComparison.OrdinalIgnoreCase) == 0);
		}

		#endregion

		#region File event handlers

		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (Project.Loading) {
				base.OnFileAddedToProject (e);
				return;
			}

			bool webConfigChange = false;
			List<string> filesToAdd = new List<string> ();

			foreach (ProjectFileEventInfo fargs in e) {
				IEnumerable<string> files = MonoDevelop.DesignerSupport.CodeBehind.GuessDependencies
					(Project, fargs.ProjectFile, groupedExtensions);
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
				Project.AddFile (file);
			}
		}

		protected override string OnGetDefaultBuildAction (string fileName)
		{

			WebSubtype type = DetermineWebSubtype (fileName);
			switch (type) {
			case WebSubtype.Code:
				return BuildAction.Compile;
			case WebSubtype.None:
				return base.OnGetDefaultBuildAction (fileName);
			default:
				return BuildAction.Content;
			}
		}

		static string[] groupedExtensions =  { ".aspx", ".master", ".ashx", ".ascx", ".asmx", ".asax" };

		#endregion

		public virtual IEnumerable<string> GetSpecialDirectories ()
		{
			yield return "App_Browsers";
			yield return "App_Data";
			yield return "App_GlobalResources";
			yield return "App_LocalResources";
			yield return "Theme";

			if (IsAspMvcProject) {
				yield return "Views";
				yield return "Models";
				yield return "Controllers";
			}

			// For "web site" projects
			// "App_WebReferences", "App_Resources","App_Themes", "App_Code",
		}

		protected override IList<string> OnGetCommonBuildActions ()
		{
			return new [] {
				BuildAction.None,
				BuildAction.Compile,
				BuildAction.Content,
				BuildAction.EmbeddedResource,
			};
		}

		public string GetCodebehindTypeName (string fileName)
		{
			lock (codebehindTypeNameCache)
				return codebehindTypeNameCache.GetCodeBehindTypeName (fileName);
		}

		public IList<string> GetCodeTemplates (string type, string subtype = null)
		{
			var files = new List<string> ();
			var names = new HashSet<string> ();

			string asmDir = Path.GetDirectoryName (GetType().Assembly.Location);
			string lang = Project.LanguageName;
			if (lang == "C#") {
				lang = "CSharp";
			}

			if (subtype != null) {
				type = Path.Combine (type, subtype);
			}

			var dirs = new [] {
				Path.Combine (Project.BaseDirectory, "CodeTemplates", type),
				Path.Combine (Project.BaseDirectory, "CodeTemplates", lang, type),
				Path.Combine (asmDir, "CodeTemplates", type),
				Path.Combine (asmDir, "CodeTemplates", lang, type),
			};

			foreach (string directory in dirs)
				if (Directory.Exists (directory))
					foreach (string file in Directory.GetFiles (directory, "*.tt", SearchOption.TopDirectoryOnly))
						if (names.Add (Path.GetFileName (file)))
							files.Add (file);

			return files;
		}

		public string GetAspNetMvcVersion ()
		{
			foreach (var pref in Project.References) {
				if (pref.Reference.IndexOf ("System.Web.Mvc", StringComparison.OrdinalIgnoreCase) < 0)
					continue;
				switch (pref.ReferenceType) {
				case ReferenceType.Assembly:
				case ReferenceType.Package:
					foreach (var f in pref.GetReferencedFileNames (null)) {
						if (Path.GetFileNameWithoutExtension (f) != "System.Web.Mvc" || !File.Exists (f))
							continue;
						return AssemblyName.GetAssemblyName (f).Version.ToString ();
					}
					break;
				default:
					continue;
				}
			}

			if (IsAspMvcProject)
				return GetDefaultAspNetMvcVersion ();

			return null;
		}

		public bool SupportsRazorViewEngine {
			get {
				return Project.References.Any (r => r.Reference.StartsWith ("System.Web.WebPages.Razor", StringComparison.Ordinal));
			}
		}

		protected virtual string GetDefaultAspNetMvcVersion ()
		{
			return "5.2";
		}

		public virtual bool IsAspMvcProject {
			get {
				return Project.References.Any (r => r.Reference.StartsWith ("System.Web.Mvc", StringComparison.Ordinal));
			}
		}

		public bool IsAspWebApiProject {
			get {
				return Project.References.Any (r => r.Reference.StartsWith ("System.Web.Http.WebHost", StringComparison.Ordinal));
			}
		}

		public virtual bool IsAspWebFormsProject {
			get {
				return Project.Files.Any (f => f.Name.EndsWith (".aspx", StringComparison.Ordinal));
			}
		}

		class BrowserExecutionTarget : ExecutionTarget
		{
			string name, id;
			public BrowserExecutionTarget (string id, string displayName, DesktopApplication app){
				this.name = displayName;
				this.id = id;
				this.DesktopApp = app;
			}

			public override string Name {
				get { return name; }
			}

			public override string Id {
				get { return id; }
			}

			public DesktopApplication DesktopApp { get; private set; }
		}

		protected override IEnumerable<ExecutionTarget> OnGetExecutionTargets (ConfigurationSelector configuration)
		{
			var apps = new List<ExecutionTarget> ();
			foreach (var browser in MonoDevelop.Ide.DesktopService.GetApplications ("test.html")) {
				if (browser.IsDefault)
					apps.Insert (0, new BrowserExecutionTarget (browser.Id,browser.DisplayName,browser));
				else
					apps.Add (new BrowserExecutionTarget (browser.Id,browser.DisplayName,browser));
			}
			return apps;
		}
	}
}
