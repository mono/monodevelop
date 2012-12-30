// ProjectConfiguration.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//


using System;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.Core.Serialization;
using System.Collections.Generic;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Projects
{
	public class ProjectConfiguration : SolutionItemConfiguration
	{

		public ProjectConfiguration ()
		{
		}

		public ProjectConfiguration (string name) : base(name)
		{
		}

		[ProjectPathItemProperty("IntermediateOutputPath")]
		private FilePath intermediateOutputDirectory;

		public virtual FilePath IntermediateOutputDirectory {
			get {
				if (!intermediateOutputDirectory.IsNullOrEmpty)
					return intermediateOutputDirectory;
				if (!string.IsNullOrEmpty (Platform))
					return ParentItem.BaseIntermediateOutputPath.Combine (Platform, Name);
				return ParentItem.BaseIntermediateOutputPath.Combine (Name);
			}
			set {
				if (value.IsNullOrEmpty)
					value = FilePath.Null;
				if (intermediateOutputDirectory == value)
					return;
				intermediateOutputDirectory = value;
			}
		}

		[ProjectPathItemProperty("OutputPath")]
		private FilePath outputDirectory = "." + Path.DirectorySeparatorChar;
		public virtual FilePath OutputDirectory {
			get { return outputDirectory; }
			set { outputDirectory = value; }
		}

		[ItemProperty("DebugSymbols", DefaultValue = false)]
		private bool debugMode = false;
		public bool DebugMode {
			get { return debugMode; }
			set { debugMode = value; }
		}

		[ItemProperty("ConsolePause", DefaultValue = true)]
		private bool pauseConsoleOutput = true;
		public bool PauseConsoleOutput {
			get { return pauseConsoleOutput; }
			set { pauseConsoleOutput = value; }
		}

		[ItemProperty("Externalconsole", DefaultValue = false)]
		private bool externalConsole = false;
		public bool ExternalConsole {
			get { return externalConsole; }
			set { externalConsole = value; }
		}

		[ItemProperty("Commandlineparameters", DefaultValue = "")]
		private string commandLineParameters = "";
		public string CommandLineParameters {
			get { return commandLineParameters; }
			set { commandLineParameters = value; }
		}

		[ItemProperty("EnvironmentVariables", SkipEmpty = true)]
		[ItemProperty("Variable", Scope = "item")]
		[ItemProperty("name", Scope = "key")]
		[ItemProperty("value", Scope = "value")]
		private Dictionary<string, string> environmentVariables = new Dictionary<string, string> ();
		public Dictionary<string, string> EnvironmentVariables {
			get { return environmentVariables; }
		}
		
		public Dictionary<string, string> GetParsedEnvironmentVariables ()
		{
			if (ParentItem == null)
				return environmentVariables;

			StringTagModel tagSource = ParentItem.GetStringTagModel (Selector);
			Dictionary<string, string> vars = new Dictionary<string, string> ();
			foreach (var v in environmentVariables)
				vars [v.Key] = StringParserService.Parse (v.Value, tagSource);
			return vars;
		}

		[ItemProperty("RunWithWarnings", DefaultValue = true)]
		private bool runWithWarnings = true;
		public virtual bool RunWithWarnings {
			get { return runWithWarnings; }
			set { runWithWarnings = value; }
		}

		public override void CopyFrom (ItemConfiguration conf)
		{
			base.CopyFrom (conf);

			ProjectConfiguration projectConf = conf as ProjectConfiguration;

			intermediateOutputDirectory = projectConf.intermediateOutputDirectory;
			outputDirectory = projectConf.outputDirectory;
			debugMode = projectConf.debugMode;
			pauseConsoleOutput = projectConf.pauseConsoleOutput;
			externalConsole = projectConf.externalConsole;
			commandLineParameters = projectConf.commandLineParameters;

			environmentVariables.Clear ();
			foreach (KeyValuePair<string, string> el in projectConf.environmentVariables) {
				environmentVariables.Add (el.Key, el.Value);
			}

			runWithWarnings = projectConf.runWithWarnings;
		}

		public new Project ParentItem {
			get { return (Project) base.ParentItem; }
		}
	}

}
