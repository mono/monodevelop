// 
// MonoDroidCommand.cs
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
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.MonoDroid
{
	
	public enum MonoDroidCommands
	{
		UploadToDevice,
		SelectDeviceTarget,
		ManageDevices,
		OpenAvdManager,
		CreateAndroidPackage,
		PublishApplication
	}
	
	class SelectDeviceTargetHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			if (proj == null || !MonoDroidFramework.HasAndroidJavaSdks)
				return;
			
			var conf = (MonoDroidProjectConfiguration) proj.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			var projSetting = proj.GetDeviceTarget (conf);
			
			foreach (var st in MonoDroidFramework.DeviceManager.Devices) {
				var i = info.Add (st.ID, st);
				if (projSetting != null && projSetting.Equals (st))
					i.Checked  = true;
			}
		}

		protected override void Run (object dataItem)
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			var conf = (MonoDroidProjectConfiguration) proj.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			proj.SetDeviceTarget (conf, ((AndroidDevice)dataItem).ID);
		}
	}
	
	class ManageDevicesHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			info.Visible = info.Enabled = proj != null;
		}
		
		protected override void Run ()
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			var conf = (MonoDroidProjectConfiguration) proj.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			var dlg = new MonoDevelop.MonoDroid.Gui.DeviceChooserDialog ();
			if (MessageService.ShowCustomDialog (dlg) == (int)Gtk.ResponseType.Ok)
				proj.SetDeviceTarget (conf, dlg.Device.ID);
		}
	}

	class ChangeDeviceTargetHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			info.Visible = info.Enabled = proj != null;
		}

		protected override void Run (object dataItem)
		{
			if (!MonoDroidFramework.EnsureSdksInstalled ())
				return;
			
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			var conf = (MonoDroidProjectConfiguration) proj.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			
			var device = MonoDroidUtility.ChooseDevice (null);
			if (device != null)
				proj.SetDeviceTarget (conf, device.ID);
		}
	}
	
	class DefaultUploadToDeviceHandler : CommandHandler
	{
		protected override void Update (MonoDevelop.Components.Commands.CommandInfo info)
		{
			var proj = GetActiveExecutableMonoDroidProject ();
			info.Visible = info.Enabled = proj != null;
		}
		
		protected override void Run ()
		{
			if (!MonoDroidFramework.EnsureSdksInstalled ())
				return;

			var configSel = IdeApp.Workspace.ActiveConfiguration;
			var proj = GetActiveExecutableMonoDroidProject ();
			
			OperationHandler upload = delegate {
				using (var monitor = new MonoDevelop.Ide.ProgressMonitoring.MessageDialogProgressMonitor ()) {
					AndroidDevice device = null;

					var conf = (MonoDroidProjectConfiguration) proj.GetConfiguration (configSel);
					var deviceId = proj.GetDeviceTarget (conf);
					if (deviceId != null)
						device = MonoDroidFramework.DeviceManager.GetDevice (deviceId);
					if (device == null)
						proj.SetDeviceTarget (conf, null);

					MonoDroidUtility.SignAndUpload (monitor, proj, configSel, true, ref device);
				}
			};
			
			if (proj.NeedsBuilding (configSel))
				IdeApp.ProjectOperations.Build (proj).Completed += upload;
			else
				upload (null);
		}
		
		public static MonoDroidProject GetActiveExecutableMonoDroidProject ()
		{
			var proj = IdeApp.ProjectOperations.CurrentSelectedProject as MonoDroidProject;
			if (proj != null && proj.IsAndroidApplication)
				return proj;
			var sln = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sln != null) {
				proj = sln.StartupItem as MonoDroidProject;
				if (proj != null && proj.IsAndroidApplication)
					return proj;
			}
			return null;
		}
	}
	
	class DefaultOpenAvdManagerHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			info.Visible = info.Enabled = proj != null;
		}
		
		protected override void Run ()
		{
			if (!MonoDroidFramework.EnsureSdksInstalled ())
				return;

			MonoDroidFramework.Toolbox.StartAvdManager ();
		}
	}

	class CreatePackageHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			info.Visible = info.Enabled = proj != null;
		}

		protected override void Run ()
		{
			if (!MonoDroidFramework.EnsureSdksInstalled ())
				return;
			
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			var configSel = IdeApp.Workspace.ActiveConfiguration;
			
			OperationHandler createApk = delegate {
				using (var monitor = new MonoDevelop.Ide.ProgressMonitoring.MessageDialogProgressMonitor ()) {
					MonoDroidUtility.SignAndCopy (monitor, proj, configSel);
				}
			};
			
			if (proj.NeedsBuilding (configSel))
				IdeApp.ProjectOperations.Build (proj).Completed += createApk;
			else
				createApk (null);
		}
	}
	
	class PublishApplicationHandler : CommandHandler 
	{
		protected override void Update (CommandInfo info)
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			info.Visible = info.Enabled = proj != null;
		}

		protected override void Run ()
		{
			if (!MonoDroidFramework.EnsureSdksInstalled ())
				return;
			
			// TODO: We may should check the current build profile and
			// show a warning if we are in a debug mode.
			var configSel = IdeApp.Workspace.ActiveConfiguration;
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			var conf = proj.GetConfiguration (configSel);

			OperationHandler signOp = delegate {
				using (var monitor = new MonoDevelop.Ide.ProgressMonitoring.MessageDialogProgressMonitor ()) {
					var dlg = new MonoDevelop.MonoDroid.Gui.MonoDroidPublishDialog () {
						ApkPath = conf.ApkPath,
						BaseDirectory = proj.BaseDirectory
					};

					if (MessageService.ShowCustomDialog (dlg) == (int)Gtk.ResponseType.Ok) {
						MonoDroidUtility.PublishPackage (monitor, proj, configSel, dlg.SigningOptions,
							conf.ApkPath, dlg.DestinationApkPath, dlg.CreateNewKey, dlg.DName, dlg.KeyValidity * 365);
					}
				};
			};

			if (proj.NeedsBuilding (configSel))
				IdeApp.ProjectOperations.Build (proj).Completed += signOp;
			else
				signOp (null);
		}
	}
}
