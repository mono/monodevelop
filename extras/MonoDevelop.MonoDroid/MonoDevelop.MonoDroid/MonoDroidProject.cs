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
		
		[ItemProperty ("MonoDroidResourcePrefix")]
		string monoDroidResourcePrefix;
		
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
				androidResgenFile = value;
				NotifyModified ("AndroidResgenFile");
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
				monoDroidResourcePrefixes = null;
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
				string val = MakePathNative (androidManifestAtt.Value);
				foreach (MonoDroidProjectConfiguration cfg in Configurations)
					cfg.AndroidManifest = val;
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
			if (Configurations.Count > 0)
				conf.AndroidManifest = ((MonoDroidProjectConfiguration)Configurations[0]).AndroidManifest;
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
			
			FilePath f = fileName;
			f = f.ToRelative (BaseDirectory);
			
			foreach (var prefix in MonoDroidResourcePrefixes)
				if (f.ToString ().StartsWith (prefix))
					return MonoDroidBuildAction.AndroidResource;
				
			return baseAction;
		}
		
		string[] monoDroidResourcePrefixes;
		
		string[] MonoDroidResourcePrefixes {
			get {
				if (monoDroidResourcePrefixes == null) {
					monoDroidResourcePrefixes =
						MonoDroidResourcePrefix.Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
						.Select (p => MakePathNative (p.Trim ())).ToArray ();
				}
				return monoDroidResourcePrefixes;
			}
		}
		
		AndroidAppManifestCache manifests;
		
		public bool IsAndroidApplication (ConfigurationSelector conf)
		{
			return !((MonoDroidProjectConfiguration)GetConfiguration (conf)).IsApplication;
		}
		
		public AndroidAppManifest GetAndroidManifest (ConfigurationSelector conf)
		{
			if (manifests == null)
				manifests = new AndroidAppManifestCache (this);
			
			var cfg = (MonoDroidProjectConfiguration)GetConfiguration (conf);
			if (cfg.AndroidManifest.IsNullOrEmpty)
				return null;
			
			// If a specified manifest is not in the project, add or create it
			// FIXME: do we really want to do this?
			var pf = Files.GetFile (cfg.AndroidManifest);
			if (pf == null) {
				if (!File.Exists (cfg.AndroidManifest))
					AndroidAppManifest.Create (GetDefaultPackageName ()).WriteToFile (cfg.AndroidManifest);
				pf = AddFile (cfg.AndroidManifest);
			}
			
			return manifests.Get (cfg.AndroidManifest);
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
			if (manifests != null) {
				manifests.Dispose ();
				manifests = null;
			}
			
			base.Dispose ();
		}
	}
	
	static class MonoDroidBuildAction
	{
		public static readonly string AndroidResource = "AndroidResource";
	}
}
