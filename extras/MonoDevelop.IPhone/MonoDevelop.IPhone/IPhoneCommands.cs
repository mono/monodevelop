// 
// IPhoneCommand.cs
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.IO;
 
using MonoDevelop.Projects;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide;

namespace MonoDevelop.IPhone
{
	
	public enum IPhoneCommands
	{
		UploadToDevice,
		ExportToXcode,
		SelectSimulatorTarget,
		ViewDeviceConsole
	}
	
	class SelectSimulatorTargetHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			var proj = IdeApp.ProjectOperations.CurrentSelectedProject as IPhoneProject;
			if (proj == null)
				return;
			
			var workspaceConfig = IdeApp.Workspace.ActiveConfigurationId;
			var conf = proj.GetConfiguration (new SolutionConfigurationSelector (workspaceConfig)) as IPhoneProjectConfiguration;
			if (conf == null || conf.Platform != IPhoneProject.PLAT_SIM)
				return;
			
			var projSetting = proj.GetSimulatorTarget (conf);
			
			var def = info.Add ("Default", null);
			if (projSetting == null)
				def.Checked  = true;
			
			foreach (var st in IPhoneFramework.GetSimulatorTargets (IPhoneSdkVersion.Parse (conf.MtouchMinimumOSVersion), proj.SupportedDevices)) {
				var i = info.Add (st.ToString (), st);
				if (projSetting != null && projSetting.Equals (st))
					i.Checked  = true;
			}
		}

		protected override void Run (object dataItem)
		{
			var proj = IdeApp.ProjectOperations.CurrentSelectedProject as IPhoneProject;
			if (proj != null) {
				var workspaceConfig = IdeApp.Workspace.ActiveConfigurationId;
				var conf = proj.GetConfiguration (new SolutionConfigurationSelector (workspaceConfig)) as IPhoneProjectConfiguration;
				if (conf != null && conf.Platform == IPhoneProject.PLAT_SIM)
					proj.SetSimulatorTarget (conf, (IPhoneSimulatorTarget) dataItem);
			}
		}
	}
	
	class DefaultUploadToDeviceHandler : CommandHandler
	{
		protected override void Update (MonoDevelop.Components.Commands.CommandInfo info)
		{
			var proj = GetActiveExecutableIPhoneProject ();
			info.Visible = proj != null;
			if (info.Visible && IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) {
					var conf = (IPhoneProjectConfiguration)proj.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
					info.Enabled = conf != null && conf.Platform == IPhoneProject.PLAT_IPHONE;
				} else {
				info.Enabled = false;
			}
		}
		
		protected override void Run ()
		{
			if (IPhoneFramework.SimOnly) {
				IPhoneFramework.ShowSimOnlyDialog ();
				return;
			}
			
			var proj = GetActiveExecutableIPhoneProject ();
			var conf = (IPhoneProjectConfiguration)proj.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			
			if (!IdeApp.Preferences.BuildBeforeExecuting) {
				IPhoneUtility.Upload (proj.TargetRuntime, proj.TargetFramework, conf.AppDirectory);
				return;
			}
			
			IdeApp.ProjectOperations.Build (proj).Completed += delegate (IAsyncOperation op) {
				if (!op.Success || (op.SuccessWithWarnings && !IdeApp.Preferences.RunWithWarnings))
					return;
				IPhoneUtility.Upload (proj.TargetRuntime, proj.TargetFramework, conf.AppDirectory);
			}; 
		}
		
		public static IPhoneProject GetActiveExecutableIPhoneProject ()
		{
			var proj = IdeApp.ProjectOperations.CurrentSelectedProject as IPhoneProject;
			if (proj != null && proj.CompileTarget == CompileTarget.Exe)
				return proj;
			var sln = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sln != null) {
				proj = sln.StartupItem as IPhoneProject;
				if (proj != null && proj.CompileTarget == CompileTarget.Exe)
					return proj;
			}
			return null;
		}
	}
	
	class ExportToXcodeCommandHandler : CommandHandler
	{
		protected override void Update (MonoDevelop.Components.Commands.CommandInfo info)
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableIPhoneProject ();
			info.Visible = proj != null;
			if (info.Visible) {
				var conf = (IPhoneProjectConfiguration)proj.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				info.Enabled = conf != null && IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted;
			}
		}
		
		protected override void Run ()
		{
			if (IPhoneFramework.SimOnly) {
				IPhoneFramework.ShowSimOnlyDialog ();
				return;
			}
			
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableIPhoneProject ();
			var slnConf = IdeApp.Workspace.ActiveConfiguration;
			var conf = (IPhoneProjectConfiguration)proj.GetConfiguration (slnConf);
			
			IdeApp.ProjectOperations.Build (proj).Completed += delegate (IAsyncOperation op) {
				if (!op.Success)
					return;
				GenerateXCodeProject (proj, conf, slnConf);
			};
		}
		
		void GenerateXCodeProject (IPhoneProject proj, IPhoneProjectConfiguration conf, ConfigurationSelector slnConf)
		{
			string mtouchPath = IPhoneUtility.GetMtouchPath (proj.TargetRuntime, proj.TargetFramework);
			
			var xcodeDir = conf.OutputDirectory.Combine ("XcodeProject");
			if (!Directory.Exists (xcodeDir)) {
				try {
					Directory.CreateDirectory (xcodeDir);
				} catch (IOException ex) {
					MessageService.ShowException (ex, "Failed to create directory '" + xcodeDir +"' for Xcode project");
					return;
				}
			}
			
			var args = new System.Text.StringBuilder ();
			args.AppendFormat ("-xcode=\"{0}\" -v", xcodeDir);
			foreach (ProjectFile pf in proj.Files) {
				if (pf.BuildAction == BuildAction.Content) {
					var rel = pf.ProjectVirtualPath;
					args.AppendFormat (" -res=\"{0}\",\"{1}\"", pf.FilePath, rel);
					
					//hack around mtouch 1.0 bug. create resource directories
					string subdir = rel.ParentDirectory;
					if (string.IsNullOrEmpty (subdir))
						continue;
					subdir = xcodeDir.Combine (subdir);
					try {
						if (!Directory.Exists (subdir))
							Directory.CreateDirectory (subdir);
					} catch (IOException ex) {
						MessageService.ShowException (ex, "Failed to create directory '" + subdir +"' for Xcode project");
						return;
					}
				} else if (pf.BuildAction == BuildAction.Page) {
					args.AppendFormat (" -res=\"{0}\"", pf.FilePath);
				}
			}
			
			args.AppendFormat (" -res=\"{0}\",\"Info.plist\"", conf.AppDirectory.Combine ("Info.plist"));
			
			foreach (string asm in proj.GetReferencedAssemblies (slnConf).Distinct ())
				args.AppendFormat (" -r=\"{0}\"", asm);
			
			var sdkVersion = IPhoneSdkVersion.Parse (conf.MtouchSdkVersion);
			if (!IPhoneFramework.SdkIsInstalled (sdkVersion))
				sdkVersion = IPhoneFramework.GetClosestInstalledSdk (sdkVersion);
			
			IPhoneBuildExtension.AppendExtrasMtouchArgs (args, sdkVersion, proj, conf);
			args.AppendFormat (" \"{0}\"", conf.CompiledOutputName);
			
			string argStr = args.ToString ();
			
			var console = (IConsole) IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString ("Generate Xcode project"), MonoDevelop.Ide.Gui.Stock.RunProgramIcon, true, true);
			console.Log.WriteLine (mtouchPath + " " + argStr);
			Runtime.ProcessService.StartConsoleProcess (mtouchPath, argStr, conf.OutputDirectory, console, null);
		}
	}
	
	class ViewDeviceConsoleHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
		}
		
		protected override void Run ()
		{
			IPhoneDeviceConsole.Run ();
		}
	}
	
	public static class IPhoneUtility
	{
		public static IProcessAsyncOperation Upload (TargetRuntime runtime, TargetFramework fx, FilePath appBundle)
		{
			string mtouchPath = GetMtouchPath (runtime, fx);
			var console = (IConsole) IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString ("Deploy to Device"), MonoDevelop.Ide.Gui.Stock.RunProgramIcon, true, true);
			console.Log.WriteLine (String.Format ("{0} -installdev=\"{1}\"", mtouchPath, appBundle));
			return Runtime.ProcessService.StartConsoleProcess (mtouchPath,
				String.Format ("-installdev=\"{0}\"", appBundle), appBundle.ParentDirectory, console, null);
		}

		public static string GetMtouchPath (TargetRuntime runtime, TargetFramework fx)
		{
			string mtouchPath = runtime.GetToolPath (fx, "mtouch");
			if (string.IsNullOrEmpty (mtouchPath))
				throw new InvalidOperationException ("Cannot upload iPhone application. mtouch tool is missing.");
			return mtouchPath;
		}
		
		public static void MakeSimulatorGrabFocus ()
		{
			System.Diagnostics.Process.Start ("osascript", "-e 'tell application \"iPhone Simulator\"' -e 'activate' -e 'end tell'");
		}
	}
}
