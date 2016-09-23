//
// DotNetRunConfiguration.cs
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
using System.Collections.Generic;
using System.Xml.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Core.Serialization;
using System.Linq;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects
{
	public class AssemblyRunConfiguration: DotNetProjectRunConfiguration
	{
		MonoExecutionParameters monoParameters = new MonoExecutionParameters ();

		public AssemblyRunConfiguration (string name): base (name)
		{
		}

		internal protected override void Initialize (Project project)
		{
			base.Initialize (project);
			StartAction = StartActions.Project;
		}

		[ItemProperty (DefaultValue = "")]
		public string StartAction { get; set; }

		[ItemProperty (DefaultValue = "")]
		public FilePath StartProgram { get; set; } = "";

		public class StartActions
		{
			public const string Project = "Project";
			public const string Program = "Program";
		}

		public override string Summary {
			get {
				string envVars = null;
				if (EnvironmentVariables.Count > 0) {
					var v = EnvironmentVariables.First ();
					envVars = v.Key + "=" + v.Value;
					if (EnvironmentVariables.Count > 1)
						envVars += "...";
				}
				if (StartAction == StartActions.Project) {
					if (!string.IsNullOrEmpty (StartArguments) && envVars != null)
						return GettextCatalog.GetString ("Start the project with arguments '{0}' and environment variables '{1}'", StartArguments, envVars);
					else if (!string.IsNullOrEmpty (StartArguments))
						return GettextCatalog.GetString ("Start the project with arguments '{0}'", StartArguments);
					else if (envVars != null)
						return GettextCatalog.GetString ("Start the project with environment variables '{0}''", envVars);
					else
						return GettextCatalog.GetString ("Start the project");
				} else {
					if (StartProgram.IsNullOrEmpty)
						return GettextCatalog.GetString ("Selected startup program is not valid");
					var app = StartProgram.FileName;
					if (!string.IsNullOrEmpty (StartArguments) && EnvironmentVariables.Count > 0)
						return GettextCatalog.GetString ("Run {0} with arguments '{1}' and custom environment variables '{2}'", app, StartArguments, envVars);
					else if (!string.IsNullOrEmpty (StartArguments))
						return GettextCatalog.GetString ("Run {0} with arguments '{1}'", app, StartArguments);
					else if (envVars != null)
						return GettextCatalog.GetString ("Run {0} with environment variables '{1}'", app, envVars);
					else
						return GettextCatalog.GetString ("Run {0}", app);
				}
			}
		}

		public bool IsEmpty {
			get { return string.IsNullOrEmpty (StartArguments) && string.IsNullOrEmpty (StartWorkingDirectory) && StartAction == StartActions.Project && EnvironmentVariables.Count == 0 && string.IsNullOrEmpty (TargetRuntimeId); }
		}

		internal protected override void Read (IPropertySet pset)
		{
			base.Read (pset);
			pset.ReadObjectProperties (monoParameters, monoParameters.GetType (), false);
		}

		internal protected override void Write (IPropertySet pset)
		{
			pset.SetPropertyOrder ("StartAction", "StartProgram", "StartArguments", "StartWorkingDirectory", "ExternalConsole", "ConsolePause", "EnvironmentVariables");
			pset.WriteObjectProperties (monoParameters, monoParameters.GetType (), false);
			base.Write (pset);
		}

		public MonoExecutionParameters MonoParameters {
			get { return monoParameters; }
			set { monoParameters = value; }
		}

		[ItemProperty (DefaultValue = "")]
		public string TargetRuntimeId { get; set; } = "";

		public override bool CanRunLibrary {
			get {
				return StartAction == AssemblyRunConfiguration.StartActions.Program && !string.IsNullOrEmpty (StartProgram);
			}
		}

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (AssemblyRunConfiguration)config;

			StartProgram = other.StartProgram;
			StartAction = other.StartAction;
			monoParameters = other.monoParameters.Clone ();
			TargetRuntimeId = other.TargetRuntimeId;
		}
	}
}

