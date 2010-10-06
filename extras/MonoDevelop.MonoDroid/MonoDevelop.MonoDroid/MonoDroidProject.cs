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
using System.Xml.Linq;
using MonoDevelop.Ide.Tasks;

namespace MonoDevelop.MonoDroid
{

	public class MonoDroidProject : DotNetProject
	{
		internal const string FX_MONODROID = "MonoDroid";
		
		#region Properties
		
		[ProjectPathItemProperty ("AndroidResgenFile")]
		string androidResgenFile;
		
		[ItemProperty ("AndroidResgenClass")]
		string androidResgenClass;
		
		[ProjectPathItemProperty ("AndroidManifest")]
		string androidManifest;
		
		[ItemProperty ("MonoDroidResourcePrefix")]
		string monoDroidResourcePrefix;
		
		public override string ProjectType {
			get { return "MonoDroid"; }
		}
		
		public override bool IsLibraryBasedProjectType {
			get { return true; }
		}
		
		public FilePath AndroidResgenFile {
			get { return androidResgenFile; }
			set {
				if (value == "")
					value = null;
				if (value == androidResgenFile)
					return;
				androidResgenFile = value;
				NotifyModified ("AndroidResgenFile");
			}
		}
		
		public FilePath AndroidResgenClass {
			get { return androidResgenClass; }
			set {
				if (value == "")
					value = null;
				if (value == androidResgenClass)
					return;
				androidResgenClass = value;
				NotifyModified ("AndroidResgenClass");
			}
		}
		
		public FilePath AndroidManifest {
			get { return androidManifest; }
			set {
				if (value == "")
					value = null;
				if (value == androidManifest)
					return;
				androidManifest = value;
				NotifyModified ("AndroidManifest");
			}
		}
		
		public string MonoDroidResourcePrefix {
			get { return monoDroidResourcePrefix; }
			set {
				if (value == "")
					value = null;
				if (value == monoDroidResourcePrefix)
					return;
				monoDroidResourcePrefix = value;
				resPrefixes = null;
				NotifyModified ("MonoDroidResourcePrefix");
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
				this.androidResgenFile = MakePathNative (androidResgenFileAtt.Value);
			
			var androidResgenClassAtt = projectOptions.Attributes ["AndroidResgenClass"];
			if (androidResgenClassAtt != null)
				this.androidResgenClass = androidResgenClassAtt.Value;
			
			var androidManifestAtt = projectOptions.Attributes ["AndroidManifest"];
			if (androidManifestAtt != null) {
				this.AndroidManifest = MakePathNative (androidManifestAtt.Value);
			}
			
			monoDroidResourcePrefix = "Resources";
		}
		
		string MakePathNative (string path)
		{
			char c = Path.DirectorySeparatorChar == '\\'? '/' : '\\'; 
			return path.Replace (c, Path.DirectorySeparatorChar);
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
		
		public override bool SupportsFormat (FileFormat format)
		{
			return format.Id == "MSBuild10";
		}

		#endregion
		
		#region Execution
		
		/// <summary>
		/// User setting of device for running app in simulator. Null means use default.
		/// </summary>
		public AndroidDevice GetDeviceTarget (MonoDroidProjectConfiguration conf)
		{
			return UserProperties.GetValue<AndroidDevice> (GetDeviceTargetKey (conf));
		}
		
		public void SetDeviceTarget (MonoDroidProjectConfiguration conf, AndroidDevice value)
		{
			UserProperties.SetValue<AndroidDevice> (GetDeviceTargetKey (conf), value);
		}
		
		string GetDeviceTargetKey (MonoDroidProjectConfiguration conf)
		{
			return "AndroidDevice-" + conf.Id;
		}
		
		bool HackGetUserAssemblyPaths = false;
		
		//HACK: base.GetUserAssemblyPaths depends on GetOutputFileName being an assembly
		new IList<string> GetUserAssemblyPaths (ConfigurationSelector configuration)
		{
			try {
				HackGetUserAssemblyPaths = true;
				return base.GetUserAssemblyPaths (configuration);
			} finally {
				HackGetUserAssemblyPaths = false;
			}
		}
		
		public override FilePath GetOutputFileName (ConfigurationSelector configuration)
		{
			if (HackGetUserAssemblyPaths)
				return base.GetOutputFileName (configuration);
			
			var cfg = GetConfiguration (configuration);
			if (cfg == null)
				return FilePath.Null;
			return cfg.ApkPath;
		}
		
		protected override bool CheckNeedsBuild (ConfigurationSelector configuration)
		{
			var apkBuildTime = GetLastBuildTime (configuration);
			if (apkBuildTime == DateTime.MinValue)
				return true;
			
			var dllBuildTime = base.GetLastBuildTime (configuration);
			if (dllBuildTime == DateTime.MinValue || dllBuildTime > apkBuildTime)
				return true;
			
			if (base.CheckNeedsBuild (configuration))
				return true;
			
			if  (Files.Any (file => file.BuildAction == MonoDroidBuildAction.AndroidResource
					&& File.Exists (file.FilePath) && File.GetLastWriteTime (file.FilePath) > apkBuildTime))
				return true;
				
			var conf = GetConfiguration (configuration);
			var manifestFile = GetManifestFileName (conf);
			if (!manifestFile.IsNullOrEmpty && File.Exists (manifestFile)
				&& File.GetLastWriteTime (manifestFile) > apkBuildTime)
				return true;
			
			return false;
		}
		
		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel,
		                                                            DotNetProjectConfiguration configuration)
		{
			var conf = (MonoDroidProjectConfiguration) configuration;
			var devTarget = GetDeviceTarget (conf);
			
			return new MonoDroidExecutionCommand (conf.PackageName, devTarget,
				conf.ApkSignedPath, TargetRuntime, TargetFramework, conf.DebugMode) {
				UserAssemblyPaths = GetUserAssemblyPaths (configSel)
			};
		}
		
		protected override bool OnGetCanExecute (MonoDevelop.Projects.ExecutionContext context, ConfigurationSelector config)
		{
			var cfg = GetConfiguration (config);
			if (cfg == null)
				return false;
			var cmd = CreateExecutionCommand (config, cfg);
			return context.ExecutionHandler.CanExecute (cmd);
		}
		
		AndroidDevice GetDevice ()
		{
			var dlg = new MonoDevelop.MonoDroid.Gui.DeviceChooserDialog ();
			try {
				var result = MessageService.ShowCustomDialog (dlg);
				if (result != (int)Gtk.ResponseType.Ok)
					return null;
				return dlg.Device;
			} finally {
				dlg.Destroy ();
			}
		}
		
		T InvokeSynch<T> (Func<T> func)
		{
			if (DispatchService.IsGuiThread)
				return func ();
			
			var ev = new System.Threading.AutoResetEvent (false);
			T val = default (T);
			Gtk.Application.Invoke (delegate {
				val = func ();
				ev.Set ();
			});
			System.Threading.WaitHandle.WaitAll (new[] { ev });
			return val;
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configSel)
		{
			var conf = (MonoDroidProjectConfiguration) GetConfiguration (configSel);
			var toolbox = MonoDroidFramework.Toolbox;
			
			if (OnGetNeedsBuilding (configSel)) {
				monitor.ReportError ("MonoDroid projects must be built before executing", null);
				return;
			}
			
			var manifestFile = BaseDirectory.Combine ("obj").Combine (conf.Name)
				.Combine ("android").Combine ("AndroidManifest.xml");
			if (!File.Exists (manifestFile)) {
				monitor.ReportError ("Intermediate manifest file is missing", null);
				return;
			}
			
			var manifest = AndroidAppManifest.Load (manifestFile);
			var activity = manifest.GetLaunchableActivityName ();
			if (string.IsNullOrEmpty (activity)) {
				monitor.ReportError ("Application does not contain a launchable activity", null);
				return;
			}
			activity = manifest.PackageName + "/" + activity;
			
			AndroidDevice device = InvokeSynch (GetDevice);
			if (device == null)
				return;
			
			var parentMonitor = monitor;
			monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString ("Deploy to Device"), MonoDevelop.Ide.Gui.Stock.RunProgramIcon, true, true);
			
			var opMon = new AggregatedOperationMonitor (parentMonitor);
			try {
				var command = (MonoDroidExecutionCommand) CreateExecutionCommand (configSel, conf);
				command.Device = device;
				command.Activity = activity;
				
				monitor.BeginTask ("Starting adb server", 0);
				using (var ensureServerOp = toolbox.EnsureServerRunning (monitor.Log, monitor.Log)) {
					ensureServerOp.WaitForCompleted ();
					if (!ensureServerOp.Success) {
						monitor.ReportError ("Failed to start adb server", null);
						return;
					}
				}
				monitor.EndTask ();
				
				if (parentMonitor.IsCancelRequested)
					return;
				
				monitor.BeginTask ("Waiting for device", 0);
				using (var waitForDeviceOp = toolbox.WaitForDevice (device, monitor.Log, monitor.Log)) {
					waitForDeviceOp.WaitForCompleted ();
					if (!waitForDeviceOp.Success) {
						monitor.ReportError ("Failed to get device", null);
						return;
					}
				}
				monitor.EndTask ();
				
				if (parentMonitor.IsCancelRequested)
					return;
				
				monitor.BeginTask ("Getting package list from device", 0);
				List<string> packages;
				using (var getPackagesOp = toolbox.GetInstalledPackagesOnDevice (device, monitor.Log)) {
					opMon.AddOperation (getPackagesOp);
					getPackagesOp.WaitForCompleted ();
					if (!getPackagesOp.Success) {
						monitor.ReportError ("Failed to get package list", null);
						return;
					}
					packages = getPackagesOp.Result;
					monitor.EndTask ();
				}
				
				if (parentMonitor.IsCancelRequested)
					return;
				
				if (!toolbox.IsSharedRuntimeInstalled (packages)) {
					monitor.BeginTask ("Installing shared runtime package on device", 0);
					var pkg = MonoDroidFramework.SharedRuntimePackage;
					if (!File.Exists (pkg)) {
						monitor.ReportError ("Could not find shared runtime package file", null);
						LoggingService.LogError ("Could not find MonoDroid shared runtime package '{0}'", pkg);
						return;
					}
					using (var installRuntimeOp = toolbox.Install (device, pkg, monitor.Log, monitor.Log)) {
						opMon.AddOperation (installRuntimeOp);
						installRuntimeOp.WaitForCompleted ();
						if (!installRuntimeOp.Success) {
							monitor.ReportError ("Failed to install shared runtime package", null);
							return;
						}
						monitor.EndTask ();
					}
					
					if (parentMonitor.IsCancelRequested)
						return;
				}
				
				if (!File.Exists (conf.ApkSignedPath)
					|| File.GetLastWriteTime (conf.ApkSignedPath) <= File.GetLastWriteTime (conf.ApkPath))
				{
					monitor.BeginTask ("Signing package", 0);
					var signResults = OnRunTarget (monitor, "SignAndroidPackage", configSel);
					if (signResults.ErrorCount > 0) {
						monitor.ReportError ("Signing failed", null);
						TaskService.Errors.Clear ();
						foreach (var err in signResults.Errors)
							TaskService.Errors.Add (new Task (
									err.FileName, err.ErrorText, err.Column, err.Line,
								err.IsWarning? TaskSeverity.Warning : TaskSeverity.Error));
						TaskService.ShowErrors ();
						return;
					}
					monitor.EndTask ();
					
					if (parentMonitor.IsCancelRequested)
						return;
				}
				
				//TODO: use per-device flag file and installed packages list to skip re-installing unchanged packages
				
				if (packages.Contains ("package:" + conf.PackageName)) {
					monitor.BeginTask ("Uninstalling old version of package", 0);
					using (var uninstallOp = toolbox.Uninstall (device, conf.PackageName, monitor.Log, monitor.Log)) {
						opMon.AddOperation (uninstallOp);
						uninstallOp.WaitForCompleted ();
						if (!uninstallOp.Success) {
							monitor.ReportError ("Failed to install package", null);
							return;
						}
						monitor.EndTask ();
					}
					
					if (parentMonitor.IsCancelRequested)
						return;
				}
				
				monitor.BeginTask ("Installing package", 0);
				using (var installOp = toolbox.Install (device, conf.ApkSignedPath, monitor.Log, monitor.Log)) {
					opMon.AddOperation (installOp);
					installOp.WaitForCompleted ();
					if (!installOp.Success) {
						monitor.ReportError ("Failed to install package", null);
						return;
					}
					monitor.EndTask ();
				}
				
				if (parentMonitor.IsCancelRequested)
					return;
				
				using (var console = context.ConsoleFactory.CreateConsole (false)) {
					var executeOp = context.ExecutionHandler.Execute (command, console);
					opMon.AddOperation (executeOp);
					executeOp.WaitForCompleted ();
				}
			} finally {
				opMon.Dispose ();
				monitor.Dispose ();
			}
		}
		
		#endregion
		
		#region Platform properties
		
		public override bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			return framework.Id == FX_MONODROID;
		}
		
		#endregion
		
		#region Resgen
		
		protected override void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			base.OnFileChangedInProject (e);
			if (!Loading && e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
				QueueResgenUpdate ();
		}
		
		protected override void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			base.OnFileRemovedFromProject (e);
			if (!Loading && e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
				QueueResgenUpdate ();
		}
		
		protected override void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			base.OnFileRenamedInProject (e);
			if (!Loading && e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
				QueueResgenUpdate ();
		}
		
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			base.OnFileAddedToProject (e);
			if (!Loading && e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
				QueueResgenUpdate ();
		}
		
		protected override void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			base.OnFilePropertyChangedInProject (e);
			if (!Loading && e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
				QueueResgenUpdate ();
		}
		
		bool resgenUpdateQueued;
		object resgenLockObj = new object ();
		//this is fired off with a timeout, so it's effectively rate-limited
		//if multiple changes take place at once
		void QueueResgenUpdate ()
		{
			lock (resgenLockObj) {
				if (resgenUpdateQueued)
					return;
				resgenUpdateQueued = true;
				GLib.Timeout.Add (3000, delegate {
					lock (resgenLockObj)
						resgenUpdateQueued = false;
					using (var monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ())
						RunTarget (monitor, "UpdateAndroidResources", IdeApp.Workspace.ActiveConfiguration);
					return false;
				});
			}
		}
		
		#endregion
		
		protected override IList<string> GetCommonBuildActions ()
		{
			return new string[] {
				BuildAction.Compile,
				MonoDroidBuildAction.AndroidResource,
				BuildAction.None,
			};
		}
		
		public new MonoDroidProjectConfiguration GetConfiguration (ConfigurationSelector configuration)
		{
			return (MonoDroidProjectConfiguration) base.GetConfiguration (configuration);
		}
		
		public override string GetDefaultBuildAction (string fileName)
		{
			var baseAction = base.GetDefaultBuildAction (fileName);
			if (baseAction == BuildAction.Compile)
				return baseAction;
			
			var parentOfParentDir = ((FilePath)fileName).ToRelative (BaseDirectory).ParentDirectory.ParentDirectory;
			foreach (var prefix in MonoDroidResourcePrefixes)
				if (prefix == parentOfParentDir)
					return MonoDroidBuildAction.AndroidResource;
				
			return baseAction;
		}
		
		public IEnumerable<KeyValuePair<string,ProjectFile>> GetAndroidResources (string kind)
		{
			var alreadyReturned = new HashSet<string> ();
			var splitChars = new[] { '/' };
			foreach (var pf in Files) {
				if (pf.BuildAction != MonoDroidBuildAction.AndroidResource)
					continue;
				var id = GetAndroidResourceID (pf);
				var split = id.Split (splitChars, StringSplitOptions.RemoveEmptyEntries);
				if (split.Length != 2 || !split[0].StartsWith (kind))
					continue;
				id = Path.GetFileNameWithoutExtension (split[1]);
				if (alreadyReturned.Add (id))
					yield return new KeyValuePair<string, ProjectFile> (id, pf);
			}		
		}
		
		string GetAndroidResourceID (ProjectFile pf)
		{
			if (!string.IsNullOrEmpty (pf.ResourceId))
				return pf.ResourceId;
			var f = pf.ProjectVirtualPath.ToString ();
			foreach (var prefix in MonoDroidResourcePrefixes) {
				var s = prefix.ToString ();
				if (f.StartsWith (s)) {
					f = f.Substring (s.Length);
					break;
				}
			}
			return f.Replace ('\\', '/');
		}
		
		FilePath[] resPrefixes;
		
		FilePath[] MonoDroidResourcePrefixes {
			get {
				if (resPrefixes == null) {
					var split = MonoDroidResourcePrefix.Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					var list = new List<FilePath> ();
					for (int i = 0; i < split.Length; i++) {
						var s = split[i].Trim ();
						if (s.Length == 0)
							continue;
						list.Add (MakePathNative (s));
					}
					resPrefixes = list.ToArray ();
				}
				return resPrefixes;
			}
		}
		
		AndroidPackageNameCache packageNameCache;
		
		public bool IsAndroidApplication {
			get { return !AndroidManifest.IsNullOrEmpty; }
		}
		
		public string GetPackageName (MonoDroidProjectConfiguration conf)
		{
			var pf = GetManifestFile (conf);
			if (pf == null)
				return null;
			
			if (packageNameCache == null)
				packageNameCache = new AndroidPackageNameCache (this);
			
			return packageNameCache.GetPackageName (pf.Name);
		}
		
		FilePath GetManifestFileName (MonoDroidProjectConfiguration conf)
		{
			if (conf != null && !conf.AndroidManifest.IsNullOrEmpty)
				return conf.AndroidManifest;
			return this.AndroidManifest;
		}
		
		public ProjectFile GetManifestFile (MonoDroidProjectConfiguration conf)
		{
			var manifestFile = GetManifestFileName (conf);
			if (manifestFile.IsNullOrEmpty)
				return null;
			
			// If a specified manifest is not in the project, add or create it
			// FIXME: do we really want to do this?
			var pf = Files.GetFile (manifestFile);
			if (pf != null)
				return pf;
			
			if (!File.Exists (manifestFile))
				AndroidAppManifest.Create (GetDefaultPackageName (), Name).WriteToFile (manifestFile);
			return AddFile (manifestFile);
		}
		
		public string GetPackageName (ConfigurationSelector conf)
		{
			return GetPackageName ((MonoDroidProjectConfiguration)GetConfiguration (conf));
		}
		
		string GetDefaultPackageName ()
		{
			string sanitized = SanitizeName (Name);
			if (sanitized.Length == 0)
				sanitized = "Application";
			return sanitized + "." + sanitized;
		}
		
		static string SanitizeName (string name)
		{
			var sb = new StringBuilder ();
			foreach (char c in name)
				if (char.IsLetterOrDigit (c))
					sb.Append (c);
			return sb.ToString ();
		}
		
		public override void Dispose ()
		{
			if (packageNameCache != null) {
				packageNameCache.Dispose ();
				packageNameCache = null;
			}
			
			base.Dispose ();
		}
	}
	
	static class MonoDroidBuildAction
	{
		public static readonly string AndroidResource = "AndroidResource";
	}
}
