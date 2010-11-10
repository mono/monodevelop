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
		ExportToXcode,
		SelectSimulatorTarget,
		ViewDeviceConsole,
		OpenAvdManager,
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
			
			var def = info.Add ("Default", null);
			if (projSetting == null)
				def.Checked  = true;
			
			foreach (var st in MonoDroidFramework.Devices) {
				var i = info.Add (st.ToString (), st);
				if (projSetting != null && projSetting.Equals (st))
					i.Checked  = true;
			}
		}

		protected override void Run (object dataItem)
		{
			var proj = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			if (proj == null)
				return;
			
			throw new NotImplementedException ();
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
			var configSel = IdeApp.Workspace.ActiveConfiguration;
			var proj = GetActiveExecutableMonoDroidProject ();
			
			OperationHandler upload = delegate {
				using (var monitor = new MonoDevelop.Ide.ProgressMonitoring.MessageDialogProgressMonitor ()) {
					AndroidDevice device;
					MonoDroidUtility.SignAndUpload (monitor, proj, configSel, true, out device);
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
		}
		
		protected override void Run ()
		{
			if (!MonoDroidFramework.EnsureSdksInstalled ())
				return;

			MonoDroidFramework.Toolbox.StartAvdManager ();
		}
	}
	
	class ViewDeviceConsoleHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
		}
		
		protected override void Run ()
		{
			if (!MonoDroidFramework.EnsureSdksInstalled ())
				return;

			MonoDroidDeviceConsole.Run ();
		}
	}
}
