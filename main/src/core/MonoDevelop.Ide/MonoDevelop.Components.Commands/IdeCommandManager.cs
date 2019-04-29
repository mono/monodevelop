//
// IdeCommandManager.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.Commands
{
	class IdeCommandManager : CommandManager
	{
		public IdeCommandManager ()
		{
		}

		protected override Task OnInitialize (ServiceProvider serviceProvider)
		{
			CommandTargetScanStarted += CommandServiceCommandTargetScanStarted;
			CommandTargetScanFinished += CommandServiceCommandTargetScanFinished;
			base.KeyBindingFailed += OnKeyBindingFailed;

			KeyBindingService.LoadBindingsFromExtensionPath ("/MonoDevelop/Ide/KeyBindingSchemes");
			KeyBindingService.LoadCurrentBindings ("MD2");

			CommandError += delegate (object sender, CommandErrorArgs args) {
				LoggingService.LogInternalError (args.ErrorMessage, args.Exception);
			};
			Counters.Initialization.Trace ("Loading Commands");
			LoadCommands ("/MonoDevelop/Ide/Commands");

			return base.OnInitialize (serviceProvider);
		}

		static void OnKeyBindingFailed (object sender, KeyBindingFailedEventArgs e)
		{
			IdeApp.Workbench.StatusBar.ShowWarning (e.Message);
		}

		static ITimeTracker commandTimeCounter;

		static void CommandServiceCommandTargetScanStarted (object sender, EventArgs e)
		{
			commandTimeCounter = Counters.CommandTargetScanTime.BeginTiming ();
		}

		static void CommandServiceCommandTargetScanFinished (object sender, EventArgs e)
		{
			commandTimeCounter?.End ();
		}
	}
}
