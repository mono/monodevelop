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
	public class DotNetRunConfiguration: ProjectRunConfiguration
	{
		MonoExecutionParameters monoParameters = new MonoExecutionParameters ();

		public DotNetRunConfiguration (string name): base (name)
		{
		}

		[ItemProperty (DefaultValue = "")]
		public string StartArguments { get; set; } = "";

		[ItemProperty (DefaultValue = "")]
		public FilePath StartWorkingDirectory { get; set; } = "";

		[ItemProperty (DefaultValue = "")]
		public string StartAction { get; set; } = StartActions.Project;

		[ItemProperty (DefaultValue = "")]
		public FilePath StartProgram { get; set; } = "";

		public class StartActions
		{
			public const string Project = "Project";
			public const string Program = "Program";
		}

		internal protected override void Read (IPropertySet pset)
		{
			base.Read (pset);

			var svars = pset.GetValue ("EnvironmentVariables");
			ParseEnvironmentVariables (svars, environmentVariables);

			// Kep a clone of the loaded env vars, so we can check if they have changed when saving
			loadedEnvironmentVariables = new Dictionary<string, string> (environmentVariables);

			pset.ReadObjectProperties (monoParameters, monoParameters.GetType (), false);
		}

		void ParseEnvironmentVariables (string xml, Dictionary<string, string> dict)
		{
			if (string.IsNullOrEmpty (xml)) {
				dict.Clear ();
				return;
			}
			var vars = XElement.Parse (xml);
			if (vars != null) {
				foreach (var val in vars.Elements (XName.Get ("Variable", MSBuildProject.Schema))) {
					var name = (string)val.Attribute ("name");
					if (name != null)
						dict [name] = (string)val.Attribute ("value");
				}
			}
		}

		internal protected override void Write (IPropertySet pset)
		{
			pset.WriteObjectProperties (monoParameters, monoParameters.GetType (), false);

			// Save the env vars only if the dictionary has changed.

			if (loadedEnvironmentVariables == null || loadedEnvironmentVariables.Count != environmentVariables.Count || loadedEnvironmentVariables.Any (e => !environmentVariables.ContainsKey (e.Key) || environmentVariables [e.Key] != e.Value)) {
				if (environmentVariables.Count > 0) {
					XElement e = new XElement (XName.Get ("EnvironmentVariables", MSBuildProject.Schema));
					foreach (var v in environmentVariables) {
						var val = new XElement (XName.Get ("Variable", MSBuildProject.Schema));
						val.SetAttributeValue ("name", v.Key);
						val.SetAttributeValue ("value", v.Value);
						e.Add (val);
					}
					pset.SetValue ("EnvironmentVariables", e.ToString (SaveOptions.DisableFormatting));
				} else
					pset.RemoveProperty ("EnvironmentVariables");
				loadedEnvironmentVariables = new Dictionary<string, string> (environmentVariables);
			}

			base.Write (pset);
		}

		Dictionary<string, string> loadedEnvironmentVariables;
		Dictionary<string, string> environmentVariables = new Dictionary<string, string> ();
		public Dictionary<string, string> EnvironmentVariables {
			get { return environmentVariables; }
		}

		public Dictionary<string, string> GetParsedEnvironmentVariables ()
		{
			if (ParentItem == null)
				return environmentVariables;

			StringTagModel tagSource = ParentItem.GetStringTagModel (ParentItem.DefaultConfiguration.Selector);
			Dictionary<string, string> vars = new Dictionary<string, string> ();
			foreach (var v in environmentVariables)
				vars [v.Key] = StringParserService.Parse (v.Value, tagSource);
			return vars;
		}

		public MonoExecutionParameters MonoParameters {
			get { return monoParameters; }
		}

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (DotNetRunConfiguration)config;

			StartArguments = other.StartArguments;
			StartWorkingDirectory = other.StartWorkingDirectory;
			StartProgram = other.StartProgram;
			StartAction = other.StartAction;
			environmentVariables = new Dictionary<string, string> (other.environmentVariables);
			monoParameters = other.monoParameters.Clone ();
		}

		protected override ExecutionCommand OnConfigureCommand (ExecutionCommand command)
		{
			base.OnConfigureCommand (command);
			var pcmd = (ProcessExecutionCommand)command;

			if (StartAction == DotNetRunConfiguration.StartActions.Program)
				pcmd = Runtime.ProcessService.CreateCommand (StartProgram);
			else {
				var cmd = (DotNetExecutionCommand)command;
				string monoOptions;
				monoParameters.GenerateOptions (cmd.EnvironmentVariables, out monoOptions);
				cmd.RuntimeArguments = monoOptions;
			}
			
			pcmd.Arguments = StartArguments;
			pcmd.WorkingDirectory = StartWorkingDirectory;

			foreach (var env in environmentVariables)
				pcmd.EnvironmentVariables [env.Key] = env.Value;

			return pcmd;
		}
	}
}

