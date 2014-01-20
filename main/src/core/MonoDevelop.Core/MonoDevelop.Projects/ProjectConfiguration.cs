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
using System.Xml.Linq;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects
{
	public class ProjectConfiguration : SolutionItemConfiguration, IMSBuildDataObject
	{

		public ProjectConfiguration ()
		{
		}

		public ProjectConfiguration (string name) : base(name)
		{
		}

		#region IProjectConfigurationData implementation

		void IMSBuildDataObject.Read (IMSBuildPropertySet pset, MSBuildFileFormat format)
		{
			Read (pset, format);
		}

		void IMSBuildDataObject.Write (IMSBuildPropertySet pset, MSBuildFileFormat format)
		{
			Write (pset, format);
		}

		#endregion

		internal protected virtual void Read (IMSBuildPropertySet pset, MSBuildFileFormat format)
		{
			intermediateOutputDirectory = pset.GetPathValue ("IntermediateOutputPath");
			outputDirectory = pset.GetPathValue ("OutputPath", defaultValue:"." + Path.DirectorySeparatorChar);
			debugMode = pset.GetValue<bool> ("DebugSymbols");
			pauseConsoleOutput = pset.GetValue ("ConsolePause", true);
			externalConsole = pset.GetValue<bool> ("ExternalConsole");
			commandLineParameters = pset.GetValue ("Commandlineparameters", "");
			runWithWarnings = pset.GetValue ("RunWithWarnings", true);

			var svars = pset.GetValue ("EnvironmentVariables");
			if (svars != null) {
				XElement vars = XElement.Parse (svars);
				foreach (var val in vars.Elements ("Variable")) {
					var name = (string)val.Attribute ("name");
					if (name != null)
						environmentVariables [name] = (string)val.Attribute ("value");
				}
			}
		}

		internal protected virtual void Write (IMSBuildPropertySet pset, MSBuildFileFormat format)
		{
			pset.SetValue ("IntermediateOutputPath", intermediateOutputDirectory, defaultValue:FilePath.Null);
			pset.SetValue ("DebugSymbols", debugMode, false);
			pset.SetValue ("OutputPath", outputDirectory);
			pset.SetValue ("ConsolePause", pauseConsoleOutput, true);
			pset.SetValue ("ExternalConsole", externalConsole, false);
			pset.SetValue ("Commandlineparameters", commandLineParameters, "");
			pset.SetValue ("RunWithWarnings", runWithWarnings, true);

			if (environmentVariables.Count > 0) {
				XElement e = new XElement ("EnvironmentVariables");
				foreach (var v in environmentVariables) {
					var val = new XElement ("Variable");
					val.SetAttributeValue ("name", v.Key);
					val.SetAttributeValue ("value", v.Value);
					e.Add (val);
				}
				pset.SetValue ("EnvironmentVariables", e.ToString ());
			}
		}

		FilePath intermediateOutputDirectory = FilePath.Empty;

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

		FilePath outputDirectory = "." + Path.DirectorySeparatorChar;
		public virtual FilePath OutputDirectory {
			get { return outputDirectory; }
			set { outputDirectory = value; }
		}

		bool debugMode = false;
		public bool DebugMode {
			get { return debugMode; }
			set { debugMode = value; }
		}

		bool pauseConsoleOutput = true;
		public bool PauseConsoleOutput {
			get { return pauseConsoleOutput; }
			set { pauseConsoleOutput = value; }
		}

		bool externalConsole = false;
		public bool ExternalConsole {
			get { return externalConsole; }
			set { externalConsole = value; }
		}

		string commandLineParameters = "";
		public string CommandLineParameters {
			get { return commandLineParameters; }
			set { commandLineParameters = value; }
		}

		Dictionary<string, string> environmentVariables = new Dictionary<string, string> ();
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

		bool runWithWarnings = true;
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
