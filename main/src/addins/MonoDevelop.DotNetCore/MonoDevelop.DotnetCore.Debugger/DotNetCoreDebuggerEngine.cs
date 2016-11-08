//
// DotNetCoreDebuggerEngine.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.IO;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.Debugger;
using MonoDevelop.DotNetCore;

namespace MonoDevelop.DotnetCore.Debugger
{
	public class DotNetCoreDebuggerEngine : DebuggerEngineBackend
	{
		public override bool CanDebugCommand (ExecutionCommand cmd)
		{
			return cmd is DotNetCoreExecutionCommand;
		}

		public override bool IsDefaultDebugger (ExecutionCommand cmd)
		{
			return true;
		}

		public static bool CanDebugRuntime (TargetRuntime runtime)
		{
			return true;
		}

		public override DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand c)
		{
			var cmd = (DotNetCoreExecutionCommand)c;
			var dsi = new DebuggerStartInfo {
				Command = cmd.OutputPath,
				Arguments = cmd.DotNetArguments,
				WorkingDirectory = cmd.WorkingDirectory
			};

			foreach (var envVar in cmd.EnvironmentVariables)
				dsi.EnvironmentVariables [envVar.Key] = envVar.Value;

			return dsi;
		}

		public override DebuggerSession CreateSession ()
		{
			return new DotNetCoreDebuggerSession ();
		}
	}
}
