//
// AutoTestToolbar.cs
//
// Author:
//       olgaboiarshynova <v-olboia@microsoft.com>
//
// Copyright (c) 2019 
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
using MonoDevelop.Components.AutoTest.Results;

namespace MonoDevelop.Components.AutoTest
{
	public class AutoTestToolbar : MarshalByRefObject
	{
		AutoTestSession session;
		readonly Func<AppQuery, AppQuery> selectorViewQuery = c => c.Marked ("MonoDevelop.MacIntegration.MainToolbar.SelectorView+PathSelectorView");

		public AutoTestToolbar (AutoTestSession session)
		{
			this.session = session;
		}

		public  AppResult SelectorViewControl {
			get {
				var c = session.CreateNewQuery ();
				c = selectorViewQuery (c);
				var queryResult = session.ExecuteQuery (c);

				return queryResult [0];
			}
		}

		public string GetActiveConfiguration ()
		{
			#if MAC
			return (SelectorViewControl as NSObjectResult).GetActiveConfiguration ()?.DisplayString;
			#else
			return null;
			#endif

		}

		public string GetActiveExecutionTarget ()
		{
			#if MAC
			return (SelectorViewControl as NSObjectResult).GetActiveRuntime ()?.DisplayString;
			#else
			return null;
			#endif
		}

		public string[] GetConfigurations ()
		{
			#if MAC
			return (SelectorViewControl as NSObjectResult).GetConfigurationModels ().Select (m => $"{m.DisplayString}").ToArray();
			#else
			return null;
			#endif
		}

		public string[] GetExecutionTargets ()
		{
			#if MAC
			return (SelectorViewControl as NSObjectResult).GetRuntimeModels ().Select (m => $"{m.FullDisplayString}").ToArray ();
			#else
			return null;
			#endif
		}

		public string[] GetStartupProjects ()
		{
			#if MAC
			return (SelectorViewControl as NSObjectResult).GetStartupProjectNames ();
			#else
			return null;
			#endif
		}

		public string GetStatusMessage ()
		{
			return (string)session.GetGlobalValue ("MonoDevelop.Ide.IdeApp.Workbench.RootWindow.StatusBar.text");
		}

		public string GetActiveStartupProject ()
		{
			#if MAC
			return (SelectorViewControl as NSObjectResult).GetActiveStartupProject ();
			#else
			return null;
			#endif
		}

		public Dictionary<ExecutionInfoKeys, string> GetExecutionInfo ()
		{
			var info = new Dictionary<ExecutionInfoKeys, string> (3);
			info.Add (ExecutionInfoKeys.StartupProject, GetActiveStartupProject ());
			info.Add (ExecutionInfoKeys.ActiveConfiguration, GetActiveConfiguration ());
			info.Add (ExecutionInfoKeys.ActiveExecitionTarget, GetActiveExecutionTarget ());
			return info;
		}

		public enum ExecutionInfoKeys
		{
			StartupProject,
			ActiveConfiguration,
			ActiveExecitionTarget
		}
	}
}
