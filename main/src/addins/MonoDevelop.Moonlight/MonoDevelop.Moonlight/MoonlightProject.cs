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
using System.Collections.Generic;
using System.Xml;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using MonoDevelop.AspNet.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightProject : DotNetProject
	{
		
		public MoonlightProject ()
			: base ()
		{
		}
		
		public MoonlightProject (string languageName)
			: base (languageName)
		{
		}
		
		public MoonlightProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			XmlNode silverAppNode = projectOptions.SelectSingleNode ("SilverlightApplication");
			if (silverAppNode == null || !Boolean.TryParse (silverAppNode.InnerText, out silverlightApplication))
				throw new Exception ("Moonlight template is missing SilverlightApplication node");
			
			
			XmlNode validateXmlNode = projectOptions.SelectSingleNode ("ValidateXaml");
			if (validateXmlNode != null && !Boolean.TryParse (validateXmlNode.InnerText, out validateXaml))
				throw new Exception ("Bad value in ValidateXaml template element");
			
			XmlNode throwErrorsNode = projectOptions.SelectSingleNode ("ThrowErrorsInValidation");
			if (throwErrorsNode != null && !Boolean.TryParse (throwErrorsNode.InnerText, out throwErrorsInValidation))
				throw new Exception ("Bad value in ThrowErrorsInValidation template element");
			
			if (!silverlightApplication)
				//it's a classlib, and the rest of the options don't apply
				return;
			
			XmlNode manifestNode = projectOptions.SelectSingleNode ("SilverlightManifestTemplate");
			if (manifestNode != null) {
				generateSilverlightManifest = true;
				silverlightManifestTemplate = manifestNode.InnerText;
			}
			
			xapOutputs = true;
			xapFilename = info.ProjectName + ".xap";
			
			XmlNode testPageNode = projectOptions.SelectSingleNode ("SilverlightTestPage");
			if (testPageNode != null)
				testPageFileName = testPageNode.InnerText;
			else
				createTestPage = true;
			
			//default namespace isn't initialised yet, but this should be okay OOTB
			silverlightAppEntry = info.ProjectName + ".App";
			generateSilverlightManifest = true;
		}
		
		public override TargetFrameworkMoniker GetDefaultTargetFrameworkForFormat (FileFormat format)
		{
			switch (format.Id) {
			case "MSBuild08":
				return new TargetFrameworkMoniker ("Silverlight", "3.0");
			default:
				return new TargetFrameworkMoniker ("Silverlight", "4.0");
			}
		}
		
		public override TargetFrameworkMoniker GetDefaultTargetFrameworkId ()
		{
			return new TargetFrameworkMoniker ("Silverlight", "4.0");
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new MoonlightProjectConfiguration (name);
			conf.CopyFrom (base.CreateConfiguration (name));		
			return conf;
		}
		
		public override bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			return framework.Id.Identifier == "Silverlight";
		}
		
		public override bool SupportsFormat (FileFormat format)
		{
			return format.Id == "MSBuild08" || format.Id == "MSBuild10";
		}
		
		public override string ProjectType {
			get { return "Moonlight"; }
		}
		
		public override bool IsLibraryBasedProjectType {
			get { return true; }
		}
		
		ExecutionCommand CreateExecutionCommand (ConfigurationSelector solutionConfig, MoonlightProjectConfiguration configuration)
		{
			string url = GetUrl (configuration);
			if (url != null) {
				return new MoonlightExecutionCommand (this.Name, url) {
					UserAssemblyPaths = GetUserAssemblyPaths (solutionConfig)
				};
			}
			return null;
		}
		
		string GetUrl (MoonlightProjectConfiguration config)
		{
			if (!this.SilverlightApplication || config == null)
				return null;
			
			string url = this.StartPageUrl;
			
			if (string.IsNullOrEmpty (url) && this.CreateTestPage) {
				string testPage = this.TestPageFileName;
				if (String.IsNullOrEmpty (testPage))
					testPage = "TestPage.html";
				url = Path.Combine (config.OutputDirectory, testPage);
			}
			
			if (!url.StartsWith ("http://", StringComparison.OrdinalIgnoreCase)
				&& !url.StartsWith ("https://", StringComparison.OrdinalIgnoreCase))
			{
				url = "file://" + url.Replace (Path.PathSeparator, '/');
			}
			
			return url;
		}
		
		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector solutionConfiguration)
		{
			var conf = (MoonlightProjectConfiguration) GetConfiguration (solutionConfiguration);
			return context.ExecutionHandler.CanExecute (CreateExecutionCommand (solutionConfiguration, conf));
		}
		
		// do this directly instead of relying on the commands handler
		// to stop MD from opening an output pad
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			var conf = (MoonlightProjectConfiguration) GetConfiguration (configuration);
			
			IConsole console = null;
			
			try {
				// The MoonlightExecutionHandler doesn't output anything to a console, so special-case it
				// Other handlers, like the debug handler, do need a console, so we still need to create one in that case
				// HACK: we can't get the type of the MoonlightExecutionHandler directly, so for now assume that
				// we don't want to show a console for DefaultExecutionHandler
				if (!(context.ExecutionHandler is MoonlightExecutionHandler)
				    && !(context.ExecutionHandler.GetType ().Name == "DefaultExecutionHandler"))
				{
					console = conf.ExternalConsole
						? context.ExternalConsoleFactory.CreateConsole (!conf.PauseConsoleOutput)
						: context.ConsoleFactory.CreateConsole (!conf.PauseConsoleOutput);
				}
				
				var cmd = CreateExecutionCommand (configuration, conf);
				using (var opMon = new AggregatedOperationMonitor (monitor)) {
					var ex = context.ExecutionHandler.Execute (cmd, console);
					opMon.AddOperation (ex);
					ex.WaitForCompleted ();
				}
			} finally {
				if (console != null)
					console.Dispose ();
			}
		}
		
		#region VS-compatible MSBuild properties
		
		[ItemProperty("SilverlightApplication")]
		bool silverlightApplication = false;
		
		[ItemProperty("XapOutputs", DefaultValue=false)]
		bool xapOutputs = false;
		
		[ItemProperty("GenerateSilverlightManifest", DefaultValue=false)]
		bool generateSilverlightManifest = false;
		
		[ItemProperty("XapFilename", DefaultValue="")]
		string xapFilename = string.Empty;
		
		[ProjectPathItemProperty("SilverlightManifestTemplate")]
		string silverlightManifestTemplate;
		
		[ItemProperty("SilverlightAppEntry", DefaultValue="")]
		string silverlightAppEntry = string.Empty;
		
		[ItemProperty("TestPageFileName", DefaultValue="")]
		string testPageFileName = string.Empty;
		
		[ItemProperty("CreateTestPage", DefaultValue=false)]
		bool createTestPage = false;
		
		[ProjectPathItemProperty("StartPageUrl", DefaultValue="", IsExternal=true)]
		string startPageUrl = string.Empty;
		
		[ItemProperty("ValidateXaml")]
		bool validateXaml = true;
		
		[ItemProperty("ThrowErrorsInValidation")]
		bool throwErrorsInValidation = false;
		
		//FIXME: how can we ensure this goes after the TargetFrameworkVersion element?
		//why do we even need to deserialize it?
		[ItemProperty("SilverlightVersion")]
		string silverlightVersion = "$(TargetFrameworkVersion)";
		
		//whether it's an application or a classlib
		public bool SilverlightApplication {
			get { return silverlightApplication; }
			set {
				if (silverlightApplication == value)
					return;
				NotifyModified ("SilverlightApplication");
				silverlightApplication = value;
			}
		}
		
		public bool XapOutputs {
			get { return xapOutputs; }
			set {
				if (xapOutputs == value)
					return;
				NotifyModified ("XapOutputs");
				xapOutputs = value;
			}
		}
		
		public bool GenerateSilverlightManifest {
			get { return generateSilverlightManifest; }
			set {
				if (generateSilverlightManifest == value)
					return;
				NotifyModified ("GenerateSilverlightManifest");
				generateSilverlightManifest = value;
			}
		}
		
		public string XapFilename {
			get { return xapFilename; }
			set {
				if (xapFilename == value)
					return;
				NotifyModified ("XapFilename");
				xapFilename = value;
			}
			//VBSilver.xap
		}
		
		public string SilverlightManifestTemplate {
			get { return silverlightManifestTemplate; }
			set {
				if (silverlightManifestTemplate == value)
					return;
				NotifyModified ("SilverlightManifestTemplate");
				silverlightManifestTemplate = value;
			}
			//My Project\AppManifest.xml
		}
		
		public string SilverlightAppEntry {
			get { return silverlightAppEntry; }
			set {
				if (silverlightAppEntry == value)
					return;
				NotifyModified ("SilverlightAppEntry");
				silverlightAppEntry = value;
			}
			//VBSilver.App
		}
		
		public string TestPageFileName {
			get { return testPageFileName; }
			set {
				if (testPageFileName == value)
					return;
				NotifyModified ("TestPageFileName");
				testPageFileName = value;
			}
			// TestPage.html
		}
		
		public bool CreateTestPage {
			get { return createTestPage; }
			set {
				if (createTestPage == value)
					return;
				NotifyModified ("CreateTestPage");
				createTestPage = value;
			}
		}
		
		public bool ValidateXaml {
			get { return validateXaml; }
			set {
				if (validateXaml == value)
					return;
				NotifyModified ("ValidateXaml");
				validateXaml = value;
			}
		}
		
		public bool ThrowErrorsInValidation {
			get { return throwErrorsInValidation; }
			set {
				if (throwErrorsInValidation == value)
					return;
				NotifyModified ("ThrowErrorsInValidation");
				throwErrorsInValidation = value;
			}
		}
		
		public string StartPageUrl {
			get { return startPageUrl; }
			set {
				if (startPageUrl == value)
					return;
				NotifyModified ("StartPageUrl");
				startPageUrl = value;
			}
		}
		
		#endregion
		
		#region File events
		
		static string[] groupedExtensions = { ".xaml" };
		
		protected override void OnFileAddedToProject (ProjectFileEventArgs args)
		{
			//short-circuit if the project is being deserialised
			if (Loading) {
				base.OnFileAddedToProject (args);
				return;
			}
			
			List<string> filesToAdd = new List<string> ();
			foreach (ProjectFileEventInfo e in args) {
				//set some properties automatically
				if (Path.GetExtension (e.ProjectFile.FilePath) == ".xaml") {
					e.ProjectFile.Generator = "MSBuild:MarkupCompilePass1";
					e.ProjectFile.ContentType = "Designer";
					
					//fixme: detect Application xaml?
					//if (e.ProjectFile.BuildAction == BuildAction.Page
					//    && Path.GetFileName (e.ProjectFile.Name).Contains ("Application"))
					//{
					//	e.ProjectFile.BuildAction = BuildAction.ApplicationDefinition;
					//}
				}
				
				//find any related files, e.g codebehind
				IEnumerable<string> files = MonoDevelop.DesignerSupport.CodeBehind.GuessDependencies
					(this, e.ProjectFile, groupedExtensions);
				if (files != null)
					filesToAdd.AddRange (files);
			}
			
			//let the base fire the event before we add files
			//don't want to fire events out of order of files being added
			base.OnFileAddedToProject (args);
			
			//make sure that the parent and child files are in the project
			foreach (string file in filesToAdd) {
				//NOTE: this only adds files if they are not already in the project
				AddFile (file);
			}
		}
		
		public override string GetDefaultBuildAction (string fileName)
		{
			if (Path.GetExtension (fileName) == ".xaml") {
				return BuildAction.Page;
			} else {
				return base.GetDefaultBuildAction (fileName);
			}
		}
		
		protected override IList<string> GetCommonBuildActions ()
		{
			return new string[] {
				BuildAction.None,
				BuildAction.Compile,
				BuildAction.Content,
				BuildAction.EmbeddedResource,
				BuildAction.Resource,
				BuildAction.ApplicationDefinition,
			};
		}
		
		#endregion
	}
}
