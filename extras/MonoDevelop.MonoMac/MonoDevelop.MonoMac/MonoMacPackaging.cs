// 
// MacPackagingSettingsWidget.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.MacDev;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Diagnostics;
using System.IO;
using MonoDevelop.Core.Serialization;
using MonoDevelop.MacDev.Plist;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MonoMac.Gui
{
	public static class MonoMacPackaging
	{
		public static IAsyncOperation Package (MonoMacProject project, ConfigurationSelector configSel,
			MonoMacPackagingSettings settings, FilePath target)
		{
			IProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString ("Packaging Output"),
				MonoDevelop.Ide.Gui.Stock.RunProgramIcon, true, true);
			
			 var t = new System.Threading.Thread (() => {
				try {
					using (mon) {
						BuildPackage (mon, project, configSel, settings, target);
					}	
				} catch (Exception ex) {
					mon.ReportError ("Unhandled error in packaging", null);
					LoggingService.LogError ("Unhandled exception in packaging", ex);
				} finally {
					mon.Dispose ();
				}
			}) {
				IsBackground = true,
				Name = "Mac Packaging",
			};
			t.Start ();
			
			return mon.AsyncOperation;
		}
		
		public static bool BuildPackage (IProgressMonitor monitor, MonoMacProject project,
			ConfigurationSelector conf, MonoMacPackagingSettings settings, FilePath target)
		{
			string bundleKey = settings.BundleSigningKey;
			string packageKey = settings.PackageSigningKey;
			
			if (settings.SignBundle || (settings.CreatePackage && settings.SignPackage)) {
				var identities = Keychain.GetAllSigningIdentities ();
				
				if (string.IsNullOrEmpty (bundleKey)) {
					bundleKey = identities.FirstOrDefault (k => k.StartsWith (MonoMacPackagingSettingsWidget.APPLICATION_PREFIX));
					if (string.IsNullOrEmpty (bundleKey)) {
						monitor.ReportError ("Did not find default app signing key", null);
						return false;
					} else if (!identities.Any (k => k == bundleKey)) {
						monitor.ReportError ("Did not find app signing key in keychain", null);
						return false;
					}
				}
				
				if (string.IsNullOrEmpty (packageKey)) {
					packageKey = identities.FirstOrDefault (k => k.StartsWith (MonoMacPackagingSettingsWidget.INSTALLER_PREFIX));
					if (string.IsNullOrEmpty (packageKey)) {
						monitor.ReportError ("Did not find default package signing key", null);
						return false;
					} else if (!identities.Any (k => k == packageKey)) {
						monitor.ReportError ("Did not find package signing key in keychain", null);
						return false;
					}
				}
			}
			
			if (project.NeedsBuilding (conf)) {
				BuildResult res = project.Build (monitor, conf);
				if (res.ErrorCount > 0) {
					foreach (BuildError e in res.Errors)
						monitor.ReportError (e.ToString (), null);
					monitor.ReportError (GettextCatalog.GetString ("The project failed to build."), null);
					return false;
				}
			}
			
			var cfg = (MonoMacProjectConfiguration) project.GetConfiguration (conf);
			
			FilePath tempDir = "/tmp/monomac-build-" + DateTime.Now.Ticks;
			FilePath workingApp = tempDir.Combine (cfg.AppDirectory.FileName);
			
			try {
				//user will have agreed to overwrite when they picked the target
				if (Directory.Exists (target))
					Directory.Delete (target);
				else if (File.Exists (target))
					File.Delete (target);
				
				monitor.BeginTask (GettextCatalog.GetString ("Creating app bundle"), 0);
				var files = Directory.GetFiles (cfg.AppDirectory, "*", SearchOption.AllDirectories);
				HashSet<string> createdDirs = new HashSet<string> ();
				foreach (FilePath f in files) {
					var rel = f.ToRelative (cfg.AppDirectory);
					var parentDir = rel.ParentDirectory;
					if (settings.IncludeMono) {
						if (parentDir.IsNullOrEmpty || parentDir == "." || parentDir == "Contents/MacOS")
							continue;
						var ext = rel.Extension;
						if (ext == ".mdb" || ext == ".exe" || ext == ".dll")
							continue;
					}
					if (monitor.IsCancelRequested)
						return false;
					if (createdDirs.Add (parentDir))
						Directory.CreateDirectory (workingApp.Combine (parentDir));
					monitor.Log.WriteLine (rel);
					File.Copy (f, workingApp.Combine (rel));
				}
				monitor.EndTask ();
				
				if (settings.IncludeMono) {
					monitor.BeginTask (GettextCatalog.GetString ("Merging Mono into app bundle"), 0);
					
					var args = new ProcessArgumentBuilder ();
					switch (settings.LinkerMode){
					case MonoMacLinkerMode.LinkNone:
						args.Add ("--nolink");
						break;
						
					case MonoMacLinkerMode.LinkFramework:
						args.Add ("--linksdkonly");
						break;
						
					case MonoMacLinkerMode.LinkAll:
						// nothing
						break;
					}
					
					args.Add ("-o");
					args.AddQuoted (tempDir);
					args.Add ("-n");
					args.AddQuoted (cfg.AppName);
					
					var assemblies = project.GetReferencedAssemblies (conf, true);
					foreach (var a in assemblies) {
						args.Add ("-a");
						args.AddQuoted (a);
					}
					args.AddQuoted (cfg.CompiledOutputName);
					
					string mmpPath = Mono.Addins.AddinManager.CurrentAddin.GetFilePath ("mmp");
					
					//FIXME: workaround for Mono.Addins losing the executable bit during packaging
					var mmpInfo = new Mono.Unix.UnixFileInfo (mmpPath);
					if ((mmpInfo.FileAccessPermissions & Mono.Unix.FileAccessPermissions.UserExecute) == 0)
						mmpInfo.FileAccessPermissions |=  Mono.Unix.FileAccessPermissions.UserExecute;
					
					var psi = new ProcessStartInfo (mmpPath, args.ToString ());
					if (MacBuildUtilities.ExecuteBuildCommand (monitor, psi) != 0) {
						monitor.ReportError ("Merging Mono failed", null);
						return false;
					}
					
					var plistFile = workingApp.Combine ("Contents", "Info.plist");
					var plistDoc = new PlistDocument ();
					plistDoc.LoadFromXmlFile (plistFile);
					((PlistDictionary)plistDoc.Root)["MonoBundleExecutable"] = cfg.CompiledOutputName.FileName;
					plistDoc.WriteToFile (plistFile);
					
					monitor.EndTask ();
				}
				
				//TODO: verify bundle details if for app store?
					
				if (settings.SignBundle) {
					monitor.BeginTask (GettextCatalog.GetString ("Signing app bundle"), 0);
					
					var args = new ProcessArgumentBuilder ();
					args.Add ("-v", "-f", "-s");
					args.AddQuoted (bundleKey, workingApp);
					
					var psi = new ProcessStartInfo ("codesign", args.ToString ());
					if (MacBuildUtilities.ExecuteBuildCommand (monitor, psi) != 0) {
						monitor.ReportError ("Signing failed", null);
						return false;
					}
					
					monitor.EndTask ();
				}
				
				if (settings.CreatePackage) {
					monitor.BeginTask (GettextCatalog.GetString ("Creating installer"), 0);
					
					var args = new ProcessArgumentBuilder ();
					args.Add ("--component");
					args.AddQuoted (workingApp);
					args.Add ("/Applications");
					if (settings.SignPackage) {
						args.Add ("--sign");
						args.AddQuoted (packageKey);
					}
					if (!settings.ProductDefinition.IsNullOrEmpty) {
						args.Add ("--product");
						args.AddQuoted (settings.ProductDefinition);
					}
					args.AddQuoted (target);
					
					var psi = new ProcessStartInfo ("productbuild", args.ToString ());
					try {
						if (MacBuildUtilities.ExecuteBuildCommand (monitor, psi) != 0) {
							monitor.ReportError ("Package creation failed", null);
							return false;
						}
					} catch (System.ComponentModel.Win32Exception) {
						monitor.ReportError ("productbuild not found", null);
						return false;
					}
					monitor.EndTask ();
				} else {
					Directory.Move (workingApp, target);
				}
			} finally {
				try {
					if (Directory.Exists (tempDir))
						Directory.Delete (tempDir, true);
				} catch (Exception ex) {
					LoggingService.LogError ("Error removing temp directory", ex);
				}
			}
			
			return true;
		}
		
		static void CopyDirectory (IProgressMonitor monitor, FilePath src, FilePath target)
		{
			CopyDirectoryRec (monitor, src, target, target);
		}
		
		static void CopyDirectoryRec (IProgressMonitor monitor, FilePath src, FilePath target, FilePath targetRoot)
		{
			Directory.CreateDirectory (target);
			foreach (FilePath file in Directory.GetFiles (src)) {
				if (monitor.IsCancelRequested)
					return;
				var t = target.Combine (file.FileName);
				monitor.Log.WriteLine (t.ToRelative (targetRoot));
				File.Copy (file, t);
			}
			foreach (FilePath dir in Directory.GetDirectories (src)) {
				CopyDirectoryRec (monitor, dir, target.Combine (dir.FileName), targetRoot);
			}
		}
	}
	
	public class MonoMacPackagingSettings
	{
		[ItemProperty]
		public bool IncludeMono { get; set; }
		
		[ItemProperty]
		public bool SignBundle { get; set; }
		
		[ItemProperty]
		public string BundleSigningKey { get; set; }
		
		[ItemProperty]
		public MonoMacLinkerMode LinkerMode { get; set; }
		
		[ItemProperty]
		public bool CreatePackage { get; set; }
		
		[ItemProperty]
		public bool SignPackage { get; set; }
		
		[ItemProperty]
		public string PackageSigningKey { get; set; }
		
		[ItemProperty]
		public FilePath ProductDefinition { get; set; }
		
		public static MonoMacPackagingSettings GetAppStoreDefault ()
		{
			return new MonoMacPackagingSettings () {
				IncludeMono = true,
				SignBundle = true,
				LinkerMode = MonoMacLinkerMode.LinkAll,
				CreatePackage = true,
				SignPackage = true,
			};
		}
	}
	
	public enum MonoMacLinkerMode
	{
		LinkNone,
		LinkFramework,
		LinkAll
	}
}
