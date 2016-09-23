//
// ProcessRunConfiguration.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects
{
	public class ProcessRunConfiguration: ProjectRunConfiguration
	{
		public ProcessRunConfiguration (string name): base (name)
		{
		}

		[ItemProperty (DefaultValue = "")]
		public string StartArguments { get; set; } = "";

		[ItemProperty (DefaultValue = "")]
		public FilePath StartWorkingDirectory { get; set; } = "";

		[ItemProperty ("ConsolePause", DefaultValue = true)]
		public bool PauseConsoleOutput { get; set; } = true;

		[ItemProperty (DefaultValue = false)]
		public bool ExternalConsole { get; set; } = false;

		[ItemProperty (SkipEmpty = true, WrapObject = false)]
		public EnvironmentVariableCollection EnvironmentVariables { get; private set; } = new EnvironmentVariableCollection ();

		public override string Summary {
			get {
				string envVars = null;
				if (EnvironmentVariables.Count > 0) {
					var v = EnvironmentVariables.First ();
					envVars = v.Key + "=" + v.Value;
					if (EnvironmentVariables.Count > 1)
						envVars += "...";
				}
				if (!string.IsNullOrEmpty (StartArguments) && envVars != null)
					return GettextCatalog.GetString ("Run with arguments '{0}' and environment variables '{1}'", StartArguments, envVars);
				else if (!string.IsNullOrEmpty (StartArguments))
					return GettextCatalog.GetString ("Run with arguments '{0}'", StartArguments);
				else if (envVars != null)
					return GettextCatalog.GetString ("Run with environment variables '{0}''", envVars);
				else
					return GettextCatalog.GetString ("Run with no additional arguments");
			}
		}

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (ProcessRunConfiguration)config;

			StartArguments = other.StartArguments;
			StartWorkingDirectory = other.StartWorkingDirectory;
			EnvironmentVariables = new EnvironmentVariableCollection (other.EnvironmentVariables);
			ExternalConsole = other.ExternalConsole;
			PauseConsoleOutput = other.PauseConsoleOutput;
		}
	}
}

