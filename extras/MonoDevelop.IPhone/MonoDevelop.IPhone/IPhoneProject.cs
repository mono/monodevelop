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
using System.Reflection;
using MonoDevelop.MacDev.Plist;
using System.Text;

namespace MonoDevelop.IPhone
{
	[Flags]
	public enum TargetDevice
	{
		NotSet = 0,
		IPhone = 1,
		IPad =   1 << 1,
		IPhoneAndIPad = IPhone | IPad,
	}

	public class IPhoneProject : DotNetProject
	{
		internal const string PLAT_IPHONE = "iPhone";
		internal const string PLAT_SIM = "iPhoneSimulator";
		internal const string FX_IPHONE = "IPhone";
		
		#region Properties
		
		[ProjectPathItemProperty ("MainNibFile")]
		string mainNibFile;
		
		[ProjectPathItemProperty ("MainNibFileIPad")]
		string mainNibFileIPad;
		
		[ItemProperty ("SupportedDevices", DefaultValue = TargetDevice.IPhone)]
		TargetDevice supportedDevices = TargetDevice.IPhone;
		
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
		
		[ProjectPathItemProperty ("BundleIconHigh")]
		string bundleIconHigh;
		
		[ProjectPathItemProperty ("BundleIconIPad")]
		string bundleIconIPad;
		
		[ProjectPathItemProperty ("BundleIconSpotlight")]
		string bundleIconSpotlight;
		
		[ProjectPathItemProperty ("BundleIconSpotlightHigh")]
		string bundleIconSpotlightHigh;
		
		[ProjectPathItemProperty ("BundleIconIPadSpotlight")]
		string bundleIconIPadSpotlight;
		
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
		
		public FilePath MainNibFileIPad {
			get { return mainNibFileIPad; }
			set {
				if (value == (FilePath) mainNibFileIPad)
					return;
				NotifyModified ("MainNibFileIPad");
				mainNibFileIPad = value;
			}
		}
		
		public TargetDevice SupportedDevices {
			get { return supportedDevices; }
			set {
				if (value == supportedDevices)
					return;
				NotifyModified ("SupportedDevices");
				supportedDevices = value;
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
		
		public FilePath BundleIconHigh {
			get { return bundleIconHigh; }
			set {
				if (value == (FilePath) bundleIconHigh)
					return;
				NotifyModified ("BundleIconHigh");
				bundleIconHigh = value;
			}
		}
		
		public FilePath BundleIconIPad {
			get { return bundleIconIPad; }
			set {
				if (value == (FilePath) bundleIconIPad)
					return;
				NotifyModified ("BundleIconIPad");
				bundleIconIPad = value;
			}
		}
		
		public FilePath BundleIconSpotlight {
			get { return bundleIconSpotlight; }
			set {
				if (value == (FilePath) bundleIconSpotlight)
					return;
				NotifyModified ("BundleIconSpotlight");
				bundleIconSpotlight = value;
			}
		}
		
		public FilePath BundleIconSpotlightHigh {
			get { return bundleIconSpotlightHigh; }
			set {
				if (value == (FilePath) bundleIconSpotlightHigh)
					return;
				NotifyModified ("BundleIconSpotlightHigh");
				bundleIconSpotlightHigh = value;
			}
		}
		
		public FilePath BundleIconIPadSpotlight {
			get { return bundleIconIPadSpotlight; }
			set {
				if (value == (FilePath) bundleIconIPadSpotlight)
					return;
				NotifyModified ("BundleIconIPadSpotlight");
				bundleIconIPadSpotlight = value;
			}
		}
		
		public IPhoneCodeBehind CodeBehindGenerator {
			get; private set;
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
			
			var ipadNibAtt = projectOptions.Attributes ["MainNibFileIPad"];
			if (ipadNibAtt != null) {
				this.mainNibFileIPad = ipadNibAtt.InnerText;	
			}
			
			var supportedDevicesAtt = projectOptions.Attributes ["SupportedDevices"];
			if (supportedDevicesAtt != null) {
				this.supportedDevices = (TargetDevice) Enum.Parse (typeof (TargetDevice), supportedDevicesAtt.InnerText);	
			}
			
			var sdkVersionAtt = projectOptions.Attributes ["SdkVersion"];
			IPhoneSdkVersion? sdkVersion = null;
			if (sdkVersionAtt != null)
				sdkVersion = IPhoneSdkVersion.Parse (sdkVersionAtt.InnerText);
			
			FilePath binPath = (info != null)? info.BinPath : new FilePath ("bin");
			
			int confCount = Configurations.Count;
			for (int i = 0; i < confCount; i++) {
				var simConf = (IPhoneProjectConfiguration)Configurations[i];
				simConf.Platform = PLAT_SIM;
				var deviceConf = (IPhoneProjectConfiguration) simConf.Clone ();
				deviceConf.Platform = PLAT_IPHONE;
				deviceConf.CodesignKey = Keychain.DEV_CERT_PREFIX;
				Configurations.Add (deviceConf);
				
				deviceConf.MtouchSdkVersion = simConf.MtouchSdkVersion = sdkVersion ?? IPhoneSdkVersion.UseDefault;
				
				if (simConf.Name == "Debug")
					simConf.MtouchDebug = deviceConf.MtouchDebug = true;
				
				simConf.MtouchLink = MtouchLinkMode.None;
				
				simConf.OutputDirectory = binPath.Combine (simConf.Platform, simConf.Name);
				deviceConf.OutputDirectory = binPath.Combine (deviceConf.Platform, deviceConf.Name);
				simConf.SanitizeAppName ();
				deviceConf.SanitizeAppName ();
			}
		}
		
		void Init ()
		{
			CodeBehindGenerator = new IPhoneCodeBehind (this);
			
			//set parameters to ones required for IPhone build
			TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (FX_IPHONE);
		}
		
		protected override void OnEndLoad ()
		{
			//fix target framework if it's incorrect
			//if (TargetFramework != null && TargetFramework.Id != FX_IPHONE)
			//	TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (FX_IPHONE);
			
			FixCSharpPlatformTarget ();
			
			base.OnEndLoad ();
		}
		
		// HACK: Using older MD, C# projects may have become created with the wrong platform target
		// Fix this without adding a hard dependency on the C# addin
		void FixCSharpPlatformTarget ()
		{
			if (LanguageName != "C#")
				return;
			PropertyInfo prop = null;
			foreach (IPhoneProjectConfiguration cfg in Configurations) {
				if (prop == null) {
					if (cfg.CompilationParameters == null)
						return;
					prop = cfg.CompilationParameters.GetType ().GetProperty ("PlatformTarget");
					if (prop == null)
						return;
				}
				prop.SetValue (cfg.CompilationParameters, "anycpu", null);
			}
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new IPhoneProjectConfiguration (name);
			conf.CopyFrom (base.CreateConfiguration (name));
			
			if (conf.Platform == PLAT_IPHONE) {
				conf.CodesignKey = Keychain.DEV_CERT_PREFIX;
			} else if (conf.Platform == PLAT_SIM) {
				conf.MtouchLink = MtouchLinkMode.None;
			}
			conf.SanitizeAppName ();
			return conf;
		}

		#endregion
		
		#region Execution
		
		/// <summary>
		/// User setting of device for running app in simulator. Null means use default.
		/// </summary>
		public IPhoneSimulatorTarget GetSimulatorTarget (IPhoneProjectConfiguration conf)
		{
			return UserProperties.GetValue<IPhoneSimulatorTarget> (GetSimulatorTargetKey (conf));
		}
		
		public void SetSimulatorTarget (IPhoneProjectConfiguration conf, IPhoneSimulatorTarget value)
		{
			UserProperties.SetValue<IPhoneSimulatorTarget> (GetSimulatorTargetKey (conf), value);
		}
		
		string GetSimulatorTargetKey (IPhoneProjectConfiguration conf)
		{
			return "IPhoneSimulatorTarget-" + conf.Id;
		}
		
		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel,
		                                                            DotNetProjectConfiguration configuration)
		{
			var conf = (IPhoneProjectConfiguration) configuration;
			
			IPhoneSimulatorTarget simTarget = null;
			
			var minOS = string.IsNullOrEmpty (conf.MtouchMinimumOSVersion)?
				IPhoneSdkVersion.GetDefault () : IPhoneSdkVersion.Parse (conf.MtouchMinimumOSVersion);
			
			if (conf.Platform != PLAT_IPHONE) {
				simTarget = GetSimulatorTarget (conf);
				if (simTarget == null) {
					var defaultDevice = ((IPhoneProject)conf.ParentItem).SupportedDevices == TargetDevice.IPad?
						TargetDevice.IPad : TargetDevice.IPhone;
					simTarget = new IPhoneSimulatorTarget (defaultDevice, conf.MtouchSdkVersion.ResolveIfDefault ());
				}
			}
			
			return new IPhoneExecutionCommand (TargetRuntime, TargetFramework, conf.AppDirectory, conf.OutputDirectory,
			                                   conf.DebugMode && conf.MtouchDebug, simTarget, minOS, SupportedDevices) {
				UserAssemblyPaths = GetUserAssemblyPaths (configSel)
			};
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configSel)
		{
			var conf = (IPhoneProjectConfiguration) GetConfiguration (configSel);
			bool isDevice = conf.Platform == PLAT_IPHONE;
			
			if (!Directory.Exists (conf.AppDirectory) || (isDevice && !File.Exists (conf.AppDirectory.Combine ("PkgInfo")))) {
				Gtk.Application.Invoke (delegate {
					MessageService.ShowError (GettextCatalog.GetString ("The application has not been built."));
				});
				return;
			}
			
			if (isDevice) {
				var deviceId = "default";
				if (NeedsUploading (conf, deviceId)) {
					using (var opMon = new AggregatedOperationMonitor (monitor)) {
						using (var op = IPhoneUtility.Upload (TargetRuntime, TargetFramework, conf.AppDirectory)) {
							opMon.AddOperation (op);
							op.WaitForCompleted ();
							if (op.ExitCode != 0)
								return;
						}
						TouchUploadMarker (conf, deviceId);
					}
				}
			}
			
			base.OnExecute (monitor, context, configSel);
		}
		
		protected override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			RemoveUploadMarker ((IPhoneProjectConfiguration)GetConfiguration (configuration));
			return base.OnBuild (monitor, configuration);
		}
		
		protected override void OnClean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			RemoveUploadMarker ((IPhoneProjectConfiguration)GetConfiguration (configuration));
			base.OnClean (monitor, configuration);
		}
		
		static bool NeedsUploading (IPhoneProjectConfiguration conf, string deviceId)
		{
			var markerFile = conf.OutputDirectory.Combine (".monotouch_uploaded");
			if (File.Exists (markerFile))
				foreach (var line in File.ReadAllLines (markerFile))
					if (!line.StartsWith ("# ") && line == deviceId)
						return false;
			return true;
		}
				
		static void TouchUploadMarker (IPhoneProjectConfiguration conf, string deviceId)
		{
			var markerFile = conf.OutputDirectory.Combine (".monotouch_uploaded");
			if (File.Exists (markerFile))
				File.AppendAllText (markerFile, "\n" + deviceId);
			else
				File.WriteAllText (markerFile,
					"# This file is used to determine when the app was last uploaded to a device\n" + deviceId);
		}
		
		static void RemoveUploadMarker (IPhoneProjectConfiguration conf)
		{
			var markerFile = conf.OutputDirectory.Combine (".monotouch_uploaded");
			if (File.Exists (markerFile))
				File.Delete (markerFile);
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
			var filesToAdd = MonoDevelop.DesignerSupport.CodeBehind.GuessDependencies (this, e.ProjectFile, groupedExtensions);
			
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
		
		#endregion
		
		public ProjectFile GetInfoPlist ()
		{
			var name = BaseDirectory.Combine ("Info.plist");
			var pf = Files.GetFile (name);
			if (pf != null)
				return pf;
			var doc = new PlistDocument ();
			doc.Root = new PlistDictionary ();
			doc.WriteToFile (name);
			return AddFile (name);
		}
	}
}
