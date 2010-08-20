// 
// MonoDroidProject.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Text;
using System.Linq;

namespace MonoDevelop.MonoDroid
{

	public class MonoDroidProject : DotNetProject
	{
		internal const string FX_MONODROID = "MonoDroid";
		
		#region Properties
		
		[ProjectPathItemProperty ("AndroidResgenFile")]
		string androidResgenFile;
		
		[ItemProperty ("MonoDroidResourcePrefix")]
		string monoDroidResourcePrefix;
		
		[ProjectPathItemProperty ("MonoDroidExtraArgs")]
		string monoDroidExtraArgs;
		
		[ProjectPathItemProperty ("AndroidManifest")]
		string androidManifest;
		
		public override string ProjectType {
			get { return "MonoDroid"; }
		}
		
		public string AndroidResgenFile {
			get { return androidResgenFile; }
			set {
				if (value == "")
					value = null;
				if (value == androidResgenFile)
					return;
				NotifyModified ("AndroidResgenFile");
				androidResgenFile = value;
			}
		}
		
		public string MonoDroidResourcePrefix {
			get { return monoDroidResourcePrefix; }
			set {
				if (value == "")
					value = null;
				if (value == monoDroidResourcePrefix)
					return;
				NotifyModified ("MonoDroidResourcePrefix");
				monoDroidResourcePrefix = value;
			}
		}
		
		public string AndroidManifest {
			get { return androidManifest; }
			set {
				if (value == "")
					value = null;
				if (value == androidManifest)
					return;
				NotifyModified ("AndroidManifest");
				androidManifest = value;
			}
		}
		
		#endregion
		
		#region Constructors
		
		public MonoDroidProject ()
		{
			Init ();
		}
		
		public MonoDroidProject (string languageName)
			: base (languageName)
		{
			Init ();
		}
		
		public MonoDroidProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			Init ();
			
			var androidResgenFileAtt = projectOptions.Attributes ["AndroidResgenFile"];
			if (androidResgenFileAtt != null)
				this.androidResgenFile = androidResgenFileAtt;
			
			var androidManifestAtt = projectOptions.Attributes ["AndroidManifest"];
			if (androidManifestAtt != null)
				this.androidManifest = androidManifestAtt;
			
			monoDroidResourcePrefix = "Resources";
		}
		
		void Init ()
		{
			//set parameters to ones required for MonoDroid build
			TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (FX_MONODROID);
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new MonoDroidProjectConfiguration (name);
			conf.CopyFrom (base.CreateConfiguration (name));
			return conf;
		}

		#endregion
		
		#region Execution
		
		/// <summary>
		/// User setting of device for running app in simulator. Null means use default.
		/// </summary>
		public MonoDroidDeviceTarget GetDeviceTarget (MonoDroidProjectConfiguration conf)
		{
			return UserProperties.GetValue<MonoDroidDeviceTarget> (GetDeviceTargetKey (conf));
		}
		
		public void SetDeviceTarget (MonoDroidProjectConfiguration conf, MonoDroidDeviceTarget value)
		{
			UserProperties.SetValue<MonoDroidDeviceTarget> (GetDeviceTargetKey (conf), value);
		}
		
		string GetDeviceTargetKey (MonoDroidProjectConfiguration conf)
		{
			return "MonoDroidDeviceTarget-" + conf.Id;
		}
		
		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel,
		                                                            DotNetProjectConfiguration configuration)
		{
			var conf = (MonoDroidProjectConfiguration) configuration;
			
			MonoDroidSimulatorTarget simTarget = null;
			
			var minOS = string.IsNullOrEmpty (conf.MtouchMinimumOSVersion)?
				MonoDroidSdkVersion.Default : MonoDroidSdkVersion.Parse (conf.MtouchMinimumOSVersion);
			
			if (conf.Platform != PLAT_IPHONE) {
				simTarget = GetSimulatorTarget (conf);
				if (simTarget == null) {
					var defaultDevice = ((MonoDroidProject)conf.ParentItem).SupportedDevices == TargetDevice.IPad?
						TargetDevice.IPad : TargetDevice.MonoDroid;
					var sdk = string.IsNullOrEmpty (conf.MtouchSdkVersion)?
						MonoDroidSdkVersion.Default : MonoDroidSdkVersion.Parse (conf.MtouchSdkVersion);
					simTarget = new MonoDroidSimulatorTarget (defaultDevice, sdk);
				}
			}
			
			return new MonoDroidExecutionCommand (TargetRuntime, TargetFramework, conf.AppDirectory, conf.OutputDirectory,
			                                   conf.DebugMode && conf.MtouchDebug, simTarget, minOS, SupportedDevices) {
				UserAssemblyPaths = GetUserAssemblyPaths (configSel)
			};
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configSel)
		{
			var conf = (MonoDroidProjectConfiguration) GetConfiguration (configSel);
			
			if (!Directory.Exists (conf.AppDirectory)) {
				Gtk.Application.Invoke (delegate {
					MessageService.ShowError (GettextCatalog.GetString ("The application has not been built."));
				});
				return;
			}
			
			if (conf.Platform == PLAT_IPHONE) {
				if (NeedsUploading (conf)) {
					using (var opMon = new AggregatedOperationMonitor (monitor)) {
						using (var op = MonoDroidUtility.Upload (TargetRuntime, TargetFramework, conf.AppDirectory)) {
							opMon.AddOperation (op);
							op.WaitForCompleted ();
							if (op.ExitCode != 0)
								return;
						}
						TouchUploadMarker (conf);
					}
				}
			}
			
			base.OnExecute (monitor, context, configSel);
		}
		
		static bool NeedsUploading (MonoDroidProjectConfiguration conf)
		{
			var markerFile = conf.OutputDirectory.Combine (".monotouch_last_uploaded");
			return Directory.Exists (conf.AppDirectory) && (!File.Exists (markerFile) 
				|| File.GetLastWriteTime (markerFile) < Directory.GetLastWriteTime (conf.AppDirectory));
		}
				
		static void TouchUploadMarker (MonoDroidProjectConfiguration conf)
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
			return framework.Id == FX_MONODROID;
		}
		
		#endregion
		
		#region CodeBehind files
		
		protected override IList<string> GetCommonBuildActions ()
		{
			return new string[] {
				BuildAction.Compile,
				MonoDroidBuildAction.AndroidResource,
				BuildAction.None,
			};
		}
		
		public override string GetDefaultBuildAction (string fileName)
		{
			var baseAction = base.GetDefaultBuildAction (fileName);
			if (baseAction == BuildAction.Compile)
				return baseAction;
			
			FilePath f = fileName;
			f = f.ToRelative (BaseDirectory);
			
			var prefixes = MonoDroidResourcePrefix.Split (";", StringSplitOptions.RemoveEmptyEntries)
				.Select (p => p.Trim ().Replace ("/", Path.PathSeparator));
			
			foreach (var prefix in prefixes)
				if (f.ToString ().StartsWith (prefix))
					return MonoDroidBuildAction.AndroidResource;
				
			return baseAction;
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
	
	static class MonoDroidBuildAction
	{
		public static readonly string AndroidResource = "AndroidResource";
	}
}
