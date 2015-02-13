//
// MainToolbarController.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Components.MainToolbar
{
	class MainToolbarController : ICommandBar
	{
		internal IMainToolbarView ToolbarView {
			get;
			private set;
		}

		internal StatusBar StatusBar {
			get { return ToolbarView.StatusBar; }
		}

		public MainToolbarController (IMainToolbarView toolbarView)
		{
			ToolbarView = toolbarView;
			toolbarView.RunButtonClicked += HandleStartButtonClicked;
			IdeApp.CommandService.RegisterCommandBar (this);
		}

		public void ShowCommandBar (string barId)
		{
			ToolbarView.ShowCommandBar (barId);
		}

		public void HideCommandBar (string barId)
		{
			ToolbarView.HideCommandBar (barId);
		}

		static void HandleStartButtonClicked (object sender, EventArgs e)
		{
			OperationIcon operation;
			var ci = GetStartButtonCommandInfo (out operation);
			if (ci.Enabled)
				IdeApp.CommandService.DispatchCommand (ci.Command.Id);
		}

		static CommandInfo GetStartButtonCommandInfo (out OperationIcon operation)
		{
			if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted || !IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted) {
				operation = OperationIcon.Stop;
				return IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.ProjectCommands.Stop);
			}
			else {
				operation = OperationIcon.Run;
				var ci = IdeApp.CommandService.GetCommandInfo ("MonoDevelop.Debugger.DebugCommands.Debug");
				if (!ci.Enabled || !ci.Visible) {
					// If debug is not enabled, try Run
					ci = IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.ProjectCommands.Run);
					if (!ci.Enabled || !ci.Visible) {
						// Running is not possible, then allow building
						var bci = IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.ProjectCommands.BuildSolution);
						if (bci.Enabled && bci.Visible) {
							operation = OperationIcon.Build;
							ci = bci;
						}
					}
				}
				return ci;
			}
		}

		#region ICommandBar implementation
		bool toolbarEnabled = true;

		void ICommandBar.Update (object activeTarget)
		{
			if (!toolbarEnabled)
				return;
			OperationIcon operation;
			var ci = GetStartButtonCommandInfo (out operation);
			if (ci.Enabled != ToolbarView.RunButtonSensitivity)
				ToolbarView.RunButtonSensitivity = ci.Enabled;

			ToolbarView.RunButtonIcon = operation;
			var stopped = operation != OperationIcon.Stop;
			if (ToolbarView.ConfigurationPlatformSensitivity != stopped)
				ToolbarView.ConfigurationPlatformSensitivity = stopped;
		}

		void ICommandBar.SetEnabled (bool enabled)
		{
			toolbarEnabled = enabled;
			ToolbarView.RunButtonSensitivity = enabled;
			ToolbarView.SearchSensivitity = enabled;
		}
		#endregion
	}
}

