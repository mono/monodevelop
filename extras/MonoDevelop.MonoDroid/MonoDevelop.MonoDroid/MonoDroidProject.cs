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

namespace MonoDevelop.MonoDroid
{

	public class MonoDroidProject : DotNetProject
	{
		internal const string FX_MONODROID = "MonoDroid";
		
		#region Properties
		
		[ProjectPathItemProperty ("AndroidResgenFile")]
		string androidResgenFile;
		
		[ProjectPathItemProperty ("AndroidManifest")]
		string androidManifest;
		
		[ItemProperty ("MonoDroidResourcePrefix")]
		string monoDroidResourcePrefix;
		
		public override string ProjectType {
			get { return "MonoDroid"; }
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
			var devTarget = GetDeviceTarget (conf);
			
			return new MonoDroidExecutionCommand (conf.ApkSignedPath, TargetRuntime, TargetFramework, conf.DebugMode) {
				UserAssemblyPaths = GetUserAssemblyPaths (configSel)
			};
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configSel)
		{
			var conf = (MonoDroidProjectConfiguration) GetConfiguration (configSel);
			
			//sign, upload
			
			throw new NotImplementedException ();
			
			base.OnExecute (monitor, context, configSel);
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
		
		public ProjectFile GetManifestFile (MonoDroidProjectConfiguration conf)
		{
			if (AndroidManifest.IsNullOrEmpty)
				return null;
			
			FilePath manifestFile;
			if (conf == null || (manifestFile = conf.AndroidManifest).IsNullOrEmpty)
				manifestFile = this.AndroidManifest;
			
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
