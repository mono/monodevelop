//
// PackageManagementProgressMonitorFactory.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui.Pads;
using System;
using System.Linq;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementProgressMonitorFactory : IPackageManagementProgressMonitorFactory
	{
		public ProgressMonitor CreateProgressMonitor (string title)
		{
			return CreateProgressMonitor (title, clearConsole: true);
		}

		public ProgressMonitor CreateProgressMonitor (string title, bool clearConsole)
		{
			ConfigureConsoleClearing (clearConsole);

			OutputProgressMonitor consoleMonitor = CreatePackageConsoleOutputMonitor ();

			Pad pad = IdeApp.Workbench.ProgressMonitors.GetPadForMonitor (consoleMonitor);

			ProgressMonitor statusMonitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				title,
				Stock.StatusSolutionOperation,
				false,
				false,
				false,
				pad,
				true);

			return new PackageManagementProgressMonitor (consoleMonitor, statusMonitor);
		}

		OutputProgressMonitor CreatePackageConsoleOutputMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"PackageConsole",
				GettextCatalog.GetString ("Package Console"),
				Stock.Console,
				false,
				true);
		}

		void ConfigureConsoleClearing (bool clearConsole)
		{
			var workbench = (DefaultWorkbench)IdeApp.Workbench.RootWindow;
			var codon = workbench.PadContentCollection.FirstOrDefault (pad => pad.PadId.StartsWith ("OutputPad-PackageConsole-", StringComparison.Ordinal));
			if (codon != null) {
				var pad = codon.PadContent as DefaultMonitorPad;
				if (pad != null) {
					pad.ClearOnBeginProgress = clearConsole;
				}
			}
		}
	}
}

