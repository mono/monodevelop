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
		
		[ItemProperty ("AndroidApplication")]
		string androidApplicationUnparsed;
		
		[ProjectPathItemProperty ("AndroidResgenFile")]
		string androidResgenFile;
		
		[ItemProperty ("AndroidResgenClass")]
		string androidResgenClass;
		
		[ProjectPathItemProperty ("AndroidManifest")]
		string androidManifest;
		
		[ItemProperty ("MonoAndroidResourcePrefix")]
		string monoDroidResourcePrefix;

		[ItemProperty ("MonoAndroidAssetsPrefix")]
		string monoDroidAssetsPrefix;
		
		public override string ProjectType {
			get { return "MonoDroid"; }
		}
		
		public override bool IsLibraryBasedProjectType {
			get { return true; }
		}
		
		bool isAndroidApplication;
			
		public bool IsAndroidApplication {
			get { return isAndroidApplication; }
			set {
				if (value == isAndroidApplication)
					return;
				isAndroidApplication = value;
				androidApplicationUnparsed = value.ToString ();
				NotifyModified ("IsAndroidApplication");
			}
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

		public string MonoDroidAssetsPrefix {
			get { return monoDroidAssetsPrefix; }
			set {
				if (value == "")
					value = null;
				if (value == monoDroidAssetsPrefix)
					return;
				monoDroidAssetsPrefix = value;
				NotifyModified ("MonoAndroidAssetsPrefix");
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
				NotifyModified ("MonoAndroidResourcePrefix");
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
			
			var androidApplicationAtt = projectOptions.Attributes ["AndroidApplication"];
			if (androidApplicationAtt != null) {
				this.IsAndroidApplication = bool.Parse (androidApplicationAtt.Value);
			}
			
			var androidManifestAtt = projectOptions.Attributes ["AndroidManifest"];
			if (androidManifestAtt != null) {
				this.IsAndroidApplication = true;
				this.AndroidManifest = MakePathNative (androidManifestAtt.Value);
			}
			
			monoDroidAssetsPrefix = "Assets";
			monoDroidResourcePrefix = "Resources";
		}
		
		string MakePathNative (string path)
		{
			char c = Path.DirectorySeparatorChar == '\\'? '/' : '\\'; 
			return path.Replace (c, Path.DirectorySeparatorChar);
		}
		
		void Init ()
		{
			MonoDroidFramework.DeviceManager.IncrementOpenProjectCount ();
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new MonoDroidProjectConfiguration (name);
			conf.CopyFrom (base.CreateConfiguration (name));

			if (conf.Name.IndexOf ("debug", StringComparison.OrdinalIgnoreCase) > -1) {
				conf.AndroidUseSharedRuntime = true;
				conf.MonoDroidLinkMode = MonoDroidLinkMode.None;
			} else {
				conf.AndroidUseSharedRuntime = false;
				conf.MonoDroidLinkMode = MonoDroidLinkMode.Full;
			}

			return conf;
		}
		
		public override bool SupportsFormat (FileFormat format)
		{
			return format.Id == "MSBuild10";
		}
		
		public override MonoDevelop.Core.Assemblies.TargetFrameworkMoniker GetDefaultTargetFrameworkId ()
		{
			return new MonoDevelop.Core.Assemblies.TargetFrameworkMoniker (FX_MONODROID, MonoDroidFramework.DefaultAndroidVersion.OSVersion);
		}
		
		public override bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			return framework.Id.Identifier == FX_MONODROID;
		}
		
		protected override void OnEndLoad ()
		{
			// Migration logic for AndroidManifest element is run if it exists and AndroidApplication is empty
			// In order to do this, we don't let the deserializer handle AndroidApplicatio, but parse it here
			if (!string.IsNullOrEmpty (androidApplicationUnparsed)) {
				isAndroidApplication = string.Equals (androidApplicationUnparsed, "true", StringComparison.OrdinalIgnoreCase);
			}
			else if (!string.IsNullOrEmpty (androidManifest)) {
				androidApplicationUnparsed = "True";
				isAndroidApplication = true;
				if (!File.Exists (androidManifest))
					androidManifest = null;
			}
			
			base.OnEndLoad ();
		}

		#endregion
		
		#region Execution
		
		/// <summary>
		/// User setting of device for running app in simulator. Null means use default.
		/// </summary>
		public string GetDeviceTarget (MonoDroidProjectConfiguration conf)
		{
			//FIXME: do we really want this to be per-project/per-configuration? or should it be a global MD setting?
			var device = UserProperties.GetValue<string> (GetDeviceTargetKey (conf));
			if (string.IsNullOrEmpty (device))
				return null;
			return device;
		}
		
		public void SetDeviceTarget (MonoDroidProjectConfiguration conf, string value)
		{
			UserProperties.SetValue<string> (GetDeviceTargetKey (conf), value);
		}
		
		string GetDeviceTargetKey (MonoDroidProjectConfiguration conf)
		{
			return "AndroidDeviceId-" + conf.Id;
		}
		
		// Disable this as we are not gonna use it for now.
		//bool HackGetUserAssemblyPaths = false;
		
		//HACK: base.GetUserAssemblyPaths depends on GetOutputFileName being an assembly
		new IList<string> GetUserAssemblyPaths (ConfigurationSelector configuration)
		{
			try {
				//HackGetUserAssemblyPaths = true;
				return base.GetUserAssemblyPaths (configuration);
			} finally {
				//HackGetUserAssemblyPaths = false;
			}
		}
		
		public override FilePath GetOutputFileName (ConfigurationSelector configuration)
		{
			if (!IsAndroidApplication)
				return base.GetOutputFileName (configuration);

			// Don't return the apk file here,
			// as it is produced in the sign step,
			// not in the build step anymore (for now).
			return base.GetOutputFileName (configuration);
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
			
			// Same as in GetOutputFileName: we removed the apk checks as the apk file
			// is now generated in the sign step, not in the build step.
			return false;
		}
		
		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel,
		                                                            DotNetProjectConfiguration configuration)
		{
			var conf = (MonoDroidProjectConfiguration) configuration;
			
			return new MonoDroidExecutionCommand (conf.PackageName,
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

		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configSel)
		{
			var conf = (MonoDroidProjectConfiguration) GetConfiguration (configSel);
			
			if (NeedsBuilding (configSel)) {
				monitor.ReportError (
					GettextCatalog.GetString ("MonoDroid projects must be built before uploading"), null);
				return;
			}
			
			IConsole console = null;
			var opMon = new AggregatedOperationMonitor (monitor);
			try {
				var handler = context.ExecutionHandler as MonoDroidExecutionHandler;
				bool useHandlerDevice = handler != null && handler.DeviceTarget != null;
				
				AndroidDevice device = null;
				
				if (useHandlerDevice) {
					device = handler.DeviceTarget;
				} else {
					var deviceId = GetDeviceTarget (conf);
					if (deviceId != null)
						device = MonoDroidFramework.DeviceManager.GetDevice (deviceId);
					if (device == null)
						SetDeviceTarget (conf, null);
				}
				
				var uploadOp = MonoDroidUtility.SignAndUpload (monitor, this, configSel, false, ref device);
				
				//user cancelled device selection
				if (device == null)
					return;
				
				opMon.AddOperation (uploadOp);
				uploadOp.WaitForCompleted ();
				
				if (!uploadOp.Success || monitor.IsCancelRequested)
					return;
				
				//get the activity name after signing produced the final manifest
				string activity;
				if (!GetActivityNameFromManifest (monitor, conf, out activity))
					return;

				//successful, persist the device choice
				if (!useHandlerDevice)
					SetDeviceTarget (conf, device.ID);
				
				var command = (MonoDroidExecutionCommand) CreateExecutionCommand (configSel, conf);
				command.Device = device;
				command.Activity = activity;
				
				//FIXME: would be nice to skip this if it's a debug handler, which will set another value later
				var propOp = MonoDroidFramework.Toolbox.SetProperty (device, "debug.mono.extra", string.Empty);
				opMon.AddOperation (propOp);
				propOp.WaitForCompleted ();
				if (!propOp.Success) {
					monitor.ReportError (GettextCatalog.GetString ("Count not clear debug settings on device"),
						propOp.Error);
					return;
				}
				
				console = context.ConsoleFactory.CreateConsole (false);
				var executeOp = context.ExecutionHandler.Execute (command, console);
				opMon.AddOperation (executeOp);
				executeOp.WaitForCompleted ();
				
			} finally {
				opMon.Dispose ();
				if (console != null)
					console.Dispose ();
			}
		}
		
		public bool PackageNeedsSigning (ConfigurationSelector configuration)
		{
			var conf = GetConfiguration (configuration);
			if (!File.Exists (conf.ApkSignedPath))
				return true;

			var apkBuildTime = File.GetLastWriteTime (conf.ApkSignedPath);
			if  (Files.Any (file => (file.BuildAction == MonoDroidBuildAction.AndroidResource || file.BuildAction == MonoDroidBuildAction.AndroidAsset)
					&& File.Exists (file.FilePath) && File.GetLastWriteTime (file.FilePath) > apkBuildTime))
				return true;
				
			var manifestFile = GetManifestFileName (conf);
			if (!manifestFile.IsNullOrEmpty && File.Exists (manifestFile)
				&& File.GetLastWriteTime (manifestFile) > apkBuildTime)
				return true;

			var outputFile = GetOutputFileName (configuration);
			return File.GetLastWriteTime (conf.ApkSignedPath) < File.GetLastWriteTime (outputFile);
		}
		
		public IAsyncOperation SignPackage (ConfigurationSelector configSel)
		{
			TaskService.Errors.ClearByOwner (this);
			
			var monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
			
			DispatchService.ThreadDispatch (delegate {
            	 SignPackageAsync (monitor, configSel);
            }, null);
			
			return monitor.AsyncOperation;
		}
		
		void SignPackageAsync (IProgressMonitor monitor, ConfigurationSelector configSel)
		{
			monitor.BeginTask ("Signing package", 0);
			
			BuildResult result = null;
			try {
				result = this.OnRunTarget (monitor, "SignAndroidPackage", configSel);
				if (result.ErrorCount > 0) {
					monitor.ReportError ("Signing failed", null);
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Signing failed."), ex);
			}
			DispatchService.GuiDispatch (delegate {
				SignPackageDone (monitor, result); // disposes the monitor
			});
		}
		
		void SignPackageDone (IProgressMonitor monitor, BuildResult result)
		{
			monitor.EndTask ();
			
			if (result != null && result.Errors.Count > 0) {
				var tasks = new Task [result.Errors.Count];
				for (int n = 0; n < tasks.Length; n++) {
					tasks [n] = new Task (result.Errors [n], this);
				}
				TaskService.Errors.AddRange (tasks);
				TaskService.ShowErrors ();
			}
			
			monitor.Dispose ();
		}

		static bool GetActivityNameFromManifest (IProgressMonitor monitor, MonoDroidProjectConfiguration conf, out string activity)
		{
			activity = null;

			var manifestFile = conf.ObjDir.Combine ("android", "AndroidManifest.xml");
			if (!File.Exists (manifestFile)) {
				monitor.ReportError ("Intermediate manifest file is missing", null);
				return false;
			}
			
			var manifest = AndroidAppManifest.Load (manifestFile);
			activity = manifest.GetLaunchableActivityName ();
			if (string.IsNullOrEmpty (activity)) {
				monitor.ReportError ("Application does not contain a launchable activity", null);
				return false;
			}

			activity = manifest.PackageName + "/" + activity;
			return true;
		}
		
		#endregion
		
		#region Resgen
		
		protected override void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			base.OnFileChangedInProject (e);
			if (Loading)
				return;
			
			if (e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
				QueueResgenUpdate ();
		}
		
		protected override void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			base.OnFileRemovedFromProject (e);
			if (Loading)
				return;
			
			if (e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
				QueueResgenUpdate ();
			//clear the manifest element if the file is removed
			else if (!AndroidManifest.IsNullOrEmpty && e.ProjectFile.FilePath == AndroidManifest)
				AndroidManifest = null;
		}
		
		protected override void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			base.OnFileRenamedInProject (e);
			if (Loading)
				return;
			
			if (e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
				QueueResgenUpdate ();
			//if renaming the file to "AndroidManifest.xml", and the manifest element is not in use, set it as a convenience
			else if (AndroidManifest.IsNullOrEmpty && e.NewName.ToRelative (BaseDirectory) == "AndroidManifest.xml")
				AndroidManifest = e.NewName;
			//track manifest file renames or things will break
			else if (AndroidManifest == e.OldName)
				AndroidManifest = e.NewName;
		}
		
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			base.OnFileAddedToProject (e);
			if (Loading)
				return;
			
			if (e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
				QueueResgenUpdate ();
			//if adding a file called AndroidManifest.xml, and the manifest element is not in use, set it as a convenience
			//TODO: is it worth coping with LogicalNames?
			else if (AndroidManifest.IsNullOrEmpty && e.ProjectFile.FilePath.ToRelative (BaseDirectory) == "AndroidManifest.xml")
				AndroidManifest = e.ProjectFile.FilePath;
		}
		
		protected override void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			base.OnFilePropertyChangedInProject (e);
			if (Loading)
				return;
			
			if (e.ProjectFile.BuildAction == MonoDroidBuildAction.AndroidResource)
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
				MonoDroidBuildAction.AndroidAsset,
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
			
			var parentDir = ((FilePath)fileName).ToRelative (BaseDirectory).ParentDirectory;
			if (!parentDir.IsNullOrEmpty) {
				var parentOfParentDir = parentDir.ParentDirectory;
				if (!parentOfParentDir.IsNullOrEmpty) {
					foreach (var prefix in MonoDroidResourcePrefixes)
						if (prefix == parentOfParentDir)
							return MonoDroidBuildAction.AndroidResource;

				}
			}

			if (((FilePath)fileName).IsChildPathOf (BaseDirectory.Combine (MonoDroidAssetsPrefix)))
				return MonoDroidBuildAction.AndroidAsset;
				
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
				if (split.Length != 2)
					continue;
				
				//check that the kind matches but ignore qualifiers
				if (!split[0].StartsWith (kind, StringComparison.OrdinalIgnoreCase))
					continue;
				if (split[0].Length != kind.Length && split[0][kind.Length] != '-')
					continue;
				
				//HACK: MonoDroid currently requires IDs in xml files to be lowercased
				id = Path.GetFileNameWithoutExtension (split[1]).ToLower ();
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
				if (resPrefixes != null)
					return resPrefixes;
				
				if (string.IsNullOrEmpty (MonoDroidResourcePrefix))
					return (resPrefixes = new FilePath[] { "Resources" });
				
				var split = MonoDroidResourcePrefix.Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				var list = new List<FilePath> ();
				for (int i = 0; i < split.Length; i++) {
					var s = split[i].Trim ();
					if (s.Length == 0)
						continue;
					list.Add (MakePathNative (s));
				}
				return (resPrefixes = list.ToArray ());
			}
		}
		
		AndroidPackageNameCache packageNameCache;
		
		public string GetPackageName (MonoDroidProjectConfiguration conf)
		{
			var f = GetManifestFileName (conf);
			
			if (!f.IsNullOrEmpty) {
				if (packageNameCache == null)
					packageNameCache = new AndroidPackageNameCache (this);
				string packageName = packageNameCache.GetPackageName (f);
				if (!string.IsNullOrEmpty (packageName))
					return packageName;
			}
			
			//no name in manifest, use same default package name as GetAndroidPackageName MSBuild task
			var name = conf.CompiledOutputName.FileNameWithoutExtension.Replace (" ", "");
			if (name.Contains ("."))
				return name;
			else
				return name + "." + name;
		}
		
		FilePath GetManifestFileName (MonoDroidProjectConfiguration conf)
		{
			if (conf != null && !conf.AndroidManifest.IsNullOrEmpty)
				return conf.AndroidManifest;

			// AndroidManifest property may have not been added to the solution,
			// yet it could exist in the default location.
			if (string.IsNullOrEmpty (AndroidManifest)) {
				var defManifestPath = GetDefaultManifestFileName ();
				if (File.Exists (defManifestPath)) {
					AddExistingManifest (defManifestPath);
					MonoDevelop.Ide.IdeApp.ProjectOperations.Save (this);
				}
			}

			return this.AndroidManifest;
		}

		string GetDefaultManifestFileName ()
		{
			return BaseDirectory.Combine ("Properties", "AndroidManifest.xml");
		}
		
		public AndroidAppManifest AddManifest ()
		{
			if (AndroidManifest.IsNullOrEmpty)
				AndroidManifest = GetDefaultManifestFileName ();
			if (!Directory.Exists (AndroidManifest.ParentDirectory))
				Directory.CreateDirectory (AndroidManifest.ParentDirectory);
			var manifest = AndroidAppManifest.Create (GetDefaultPackageName (), Name);
			manifest.WriteToFile (AndroidManifest);
			AddFile (AndroidManifest);
			return manifest;
		}

		// Add an existing manifest file.
		void AddExistingManifest (string manifestFile)
		{
			AndroidManifest = manifestFile;
			AddFile (AndroidManifest);
		}
		
		public string GetPackageName (ConfigurationSelector conf)
		{
			return GetPackageName ((MonoDroidProjectConfiguration)GetConfiguration (conf));
		}
		
		string GetDefaultPackageName ()
		{
			string sanitized = SanitizeName (Name);
			if (sanitized.Length == 0)
				sanitized = "application";
			return sanitized + "." + sanitized;
		}
		
		static string SanitizeName (string name)
		{
			var sb = new StringBuilder ();
			foreach (char c in name)
				if (char.IsLetterOrDigit (c))
					sb.Append (char.ToLowerInvariant (c));
			return sb.ToString ();
		}
		
		bool disposed = false;
		
		public override void Dispose ()
		{
			lock (this) {
				if (disposed)
					return;
				disposed = true;
			}
			
			MonoDroidFramework.DeviceManager.DecrementOpenProjectCount ();
			
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
		public static readonly string AndroidAsset = "AndroidAsset";
	}
}
