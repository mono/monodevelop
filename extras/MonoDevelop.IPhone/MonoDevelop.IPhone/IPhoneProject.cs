// 
// IPhoneProject.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;

namespace MonoDevelop.IPhone
{
	
	public class IPhoneProject : DotNetProject
	{
		internal const string PLAT_IPHONE = "iPhone";
		internal const string PLAT_SIM = "iPhoneSimulator";
		internal const string FX_IPHONE = "IPhone";
		
		#region Properties
		
		[ProjectPathItemProperty ("MainNibFile")]
		string mainNibFile;
		
		[ItemProperty ("BundleDevelopmentRegion")]
		string bundleDevelopmentRegion;
		
		[ItemProperty ("BundleIdentifier")]
		string bundleIdentifier;
		
		[ItemProperty ("BundleVersion")]
		string bundleVersion;
		
		[ItemProperty ("BundleDisplayName")]
		string bundleDisplayName;
		
		[ProjectPathItemProperty ("BundleIcon")]
		string bundleIcon;
		
		public override string ProjectType {
			get { return "IPhone"; }
		}
		
		public FilePath MainNibFile {
			get { return mainNibFile; }
			set {
				if (value == (FilePath) mainNibFile)
					return;
				NotifyModified ("MainNibFile");
				mainNibFile = value;
			}
		}
		
		public string BundleDevelopmentRegion {
			get { return bundleDevelopmentRegion; }
			set {
				if (value == "English" || value == "")
					value = null;
				if (value == bundleDevelopmentRegion)
					return;
				NotifyModified ("BundleDevelopmentRegion");
				bundleDevelopmentRegion = value;
			}
		}
		
		public string BundleIdentifier {
			get { return bundleIdentifier; }
			set {
				if (value == "")
					value = null;
				if (value == bundleIdentifier)
					return;
				NotifyModified ("BundleIdentifier");
				bundleIdentifier = value;
			}
		}
		
		public string BundleVersion {
			get { return bundleVersion; }
			set {
				if (value == "")
					value = null;
				if (value == bundleVersion)
					return;
				NotifyModified ("BundleVersion");
				bundleVersion = value;
			}
		}
		
		public string BundleDisplayName {
			get { return bundleDisplayName; }
			set {
				if (value == "")
					value = null;
				if (value == bundleDisplayName)
					return;
				NotifyModified ("BundleDisplayName");
				bundleDisplayName = value;
			}
		}
		
		public FilePath BundleIcon {
			get { return bundleIcon; }
			set {
				if (value == (FilePath) bundleIcon)
					return;
				NotifyModified ("BundleIcon");
				bundleIcon = value;
			}
		}
		
		#endregion
		
		#region Constructors
		
		public IPhoneProject ()
		{
			Init ();
		}
		
		public IPhoneProject (string languageName)
			: base (languageName)
		{
			Init ();
		}
		
		public IPhoneProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			Init ();
			
			var mainNibAtt = projectOptions.Attributes ["MainNibFile"];
			if (mainNibAtt != null) {
				this.mainNibFile = mainNibAtt.InnerText;	
			}
			
			var sdkVersionAtt = projectOptions.Attributes ["SdkVersion"];
			string sdkVersion = sdkVersionAtt != null? sdkVersionAtt.InnerText : null;
			
			FilePath binPath = (info != null)? info.BinPath : new FilePath ("bin");
			
			int confCount = Configurations.Count;
			for (int i = 0; i < confCount; i++) {
				var simConf = (IPhoneProjectConfiguration)Configurations[i];
				simConf.Platform = PLAT_SIM;
				var deviceConf = (IPhoneProjectConfiguration) simConf.Clone ();
				deviceConf.Platform = PLAT_IPHONE;
				deviceConf.CodesignKey = Keychain.DEV_CERT_PREFIX;
				Configurations.Add (deviceConf);
				
				if (sdkVersion != null)
					deviceConf.MtouchSdkVersion = simConf.MtouchSdkVersion = sdkVersion;
				
				if (simConf.Name == "Debug")
					simConf.MtouchDebug = deviceConf.MtouchDebug = true;
				
				simConf.MtouchLink = MtouchLinkMode.None;
				
				simConf.OutputDirectory = binPath.Combine (simConf.Platform, simConf.Name);
				deviceConf.OutputDirectory = binPath.Combine (deviceConf.Platform, deviceConf.Name);
			}
		}
		
		void Init ()
		{
			//set parameters to ones required for IPhone build
			TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (FX_IPHONE);
		}
		
//		protected override void OnEndLoad ()
//		{
//			//fix target framework if it's incorrect
//			if (TargetFramework != null && TargetFramework.Id != FX_IPHONE)
//				TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (FX_IPHONE);
//			
//			base.OnEndLoad ();
//		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new IPhoneProjectConfiguration (name);
			
			var dir = new FilePath ("bin");
			if (!String.IsNullOrEmpty (conf.Platform))
				dir.Combine (conf.Platform);
			dir.Combine (conf.Name);
			
			conf.OutputDirectory = BaseDirectory.IsNullOrEmpty? dir : BaseDirectory.Combine (dir);
			conf.OutputAssembly = Name;
			if (conf.Platform == PLAT_IPHONE) {
				conf.CodesignKey = Keychain.DEV_CERT_PREFIX;
			} else if (conf.Platform == PLAT_SIM) {
				conf.MtouchLink = MtouchLinkMode.None;
			}
			
			if (LanguageBinding != null)
				conf.CompilationParameters = LanguageBinding.CreateCompilationParameters (null);
			return conf;
		}
		
		#endregion
		
		#region Execution
		
		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel,
		                                                            DotNetProjectConfiguration configuration)
		{
			var conf = (IPhoneProjectConfiguration) configuration;
			return new IPhoneExecutionCommand (TargetRuntime, TargetFramework, conf.AppDirectory, conf.OutputDirectory,
			                                   conf.Platform != PLAT_IPHONE, conf.DebugMode && conf.MtouchDebug,
			                                   conf.MtouchSdkVersion) {
				UserAssemblyPaths = GetUserAssemblyPaths (configSel)
			};
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configSel)
		{
			var conf = (IPhoneProjectConfiguration) GetConfiguration (configSel);
			
			if (!Directory.Exists (conf.AppDirectory)) {
				Gtk.Application.Invoke (delegate {
					MessageService.ShowError (GettextCatalog.GetString ("The application has not been built."));
				});
				return;
			}
			
			if (conf.Platform == PLAT_IPHONE) {
				if (NeedsUploading (conf)) {
					using (var opMon = new AggregatedOperationMonitor (monitor)) {
						using (var op = IPhoneUtility.Upload (TargetRuntime, TargetFramework, conf.AppDirectory)) {
							opMon.AddOperation (op);
							op.WaitForOutput ();
							if (op.ExitCode != 0)
								return;
						}
						TouchUploadMarker (conf);
					}
				}
			}
			
			base.OnExecute (monitor, context, configSel);
		}
		
		static bool NeedsUploading (IPhoneProjectConfiguration conf)
		{
			var markerFile = conf.OutputDirectory.Combine (".monotouch_last_uploaded");
			return Directory.Exists (conf.AppDirectory) && (!File.Exists (markerFile) 
				|| File.GetLastWriteTime (markerFile) < Directory.GetLastWriteTime (conf.AppDirectory));
		}
				
		static void TouchUploadMarker (IPhoneProjectConfiguration conf)
		{
			var markerFile = conf.OutputDirectory.Combine (".monotouch_last_uploaded");
			if (File.Exists (markerFile))
				File.SetLastWriteTime (markerFile, DateTime.Now);
			else
				File.WriteAllText (markerFile, "This file is used to determine when the app was last uploaded to a device");
		}
		
		#endregion
		
		#region Platform properties
		
		public override bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			return framework.Id == FX_IPHONE;
		}

		public override string[] SupportedPlatforms {
			get {
				return new string [] { PLAT_IPHONE, PLAT_SIM };
			}
		}
		
		public override ClrVersion[] SupportedClrVersions {
			get {
				return new ClrVersion[] { ClrVersion.Clr_2_1 };
			}
		}
		
		#endregion
		
		#region CodeBehind files
		
		public override string GetDefaultBuildAction (string fileName)
		{
			if (fileName.EndsWith (groupedExtensions[0]))
				return BuildAction.Page;
			return base.GetDefaultBuildAction (fileName);
		}
		
		static string[] groupedExtensions = { ".xib" };
		
		//based on MoonlightProject
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (Loading) {
				base.OnFileAddedToProject (e);
				return;
			}
			
			if (String.IsNullOrEmpty (MainNibFile) && Path.GetFileName (e.ProjectFile.FilePath) == "MainWindow.xib") {
				MainNibFile = e.ProjectFile.FilePath;
			}
			
			//find any related files, e.g codebehind
			//FIXME: base this on the controller class names defined in the xib
			IEnumerable<string> filesToAdd = MonoDevelop.DesignerSupport.CodeBehind.GuessDependencies
				(this, e.ProjectFile, groupedExtensions);
			
			//let the base fire the event before we add files
			//don't want to fire events out of order of files being added
			base.OnFileAddedToProject (e);
			
			//make sure that the parent and child files are in the project
			if (filesToAdd != null) {
				foreach (string file in filesToAdd) {
					//NOTE: this only adds files if they are not already in the project
					AddFile (file);
				}
			}
		}
		
		protected override void OnFileChangedInProject (MonoDevelop.Projects.ProjectFileEventArgs e)
		{
			//update codebehind
			if (e.ProjectFile.BuildAction == BuildAction.Page && e.ProjectFile.FilePath.Extension ==".xib")
				System.Threading.ThreadPool.QueueUserWorkItem (delegate { CodeBehind.UpdateXibCodebehind (e.ProjectFile); });
			base.OnFileChangedInProject (e);
		}
		
		#endregion
	}
}
