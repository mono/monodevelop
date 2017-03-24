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
using System.Collections.Generic;
using MonoDevelop.Core.StringParsing;
using System.Xml.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Linq;

namespace MonoDevelop.Projects
{
	public class ProjectConfiguration : SolutionItemConfiguration
	{
		bool debugTypeWasNone;
		IPropertySet properties;
		MSBuildPropertyGroup mainPropertyGroup;

		public ProjectConfiguration (string id) : base(id)
		{
		}

		internal protected virtual void Read (IPropertySet pset)
		{
			properties = pset;

			intermediateOutputDirectory = pset.GetPathValue ("IntermediateOutputPath");
			outputDirectory = pset.GetPathValue ("OutputPath", defaultValue:"." + Path.DirectorySeparatorChar);
			debugMode = pset.GetValue<bool> ("DebugSymbols", false);
			pauseConsoleOutput = pset.GetValue ("ConsolePause", true);
			if (pset.HasProperty ("Externalconsole")) {//for backward compatiblity before version 6.0 it was lowercase
				writeExternalConsoleLowercase = true;
				externalConsole = pset.GetValue<bool> ("Externalconsole");
			} else {
				writeExternalConsoleLowercase = false;
				externalConsole = pset.GetValue<bool> ("ExternalConsole");
			}
			commandLineParameters = pset.GetValue ("Commandlineparameters", "");
			runWithWarnings = pset.GetValue ("RunWithWarnings", true);

			// Special case: when DebugType=none, xbuild returns an empty string
			debugType = pset.GetValue ("DebugType");
			if (string.IsNullOrEmpty (debugType)) {
				debugType = "none";
				debugTypeReadAsEmpty = true;
			}
			debugTypeWasNone = debugType == "none";

			var svars = pset.GetValue ("EnvironmentVariables");
			ParseEnvironmentVariables (svars, environmentVariables);

			// Kep a clone of the loaded env vars, so we can check if they have changed when saving
			loadedEnvironmentVariables = new Dictionary<string, string> (environmentVariables);
			
			pset.ReadObjectProperties (this, GetType (), true);
		}

		void ParseEnvironmentVariables (string xml, Dictionary<string, string> dict)
		{
			if (string.IsNullOrEmpty (xml)) {
				dict.Clear ();
				return;
			}
			var vars = XElement.Parse (xml);
			if (vars != null) {
				foreach (var val in vars.Elements (XName.Get ("Variable", GetProjectNamespace ()))) {
					var name = (string)val.Attribute ("name");
					if (name != null)
						dict [name] = (string)val.Attribute ("value");
				}
			}
		}

		string GetProjectNamespace ()
		{
			var msbuildProject = ParentItem?.MSBuildProject;
			if (msbuildProject == null) {
				var projectObject = properties as IMSBuildProjectObject;
				if (projectObject != null)
					msbuildProject = projectObject.ParentProject;
			}

			if (msbuildProject != null)
				return msbuildProject.Namespace;

			return MSBuildProject.Schema;
		}

		internal protected virtual void Write (IPropertySet pset)
		{
			pset.SetPropertyOrder ("DebugSymbols", "DebugType", "Optimize", "OutputPath", "DefineConstants", "ErrorReport");

			FilePath defaultImPath;
			if (!string.IsNullOrEmpty (Platform))
				defaultImPath = ParentItem.BaseIntermediateOutputPath.Combine (Platform, Name);
			else
				defaultImPath = ParentItem.BaseIntermediateOutputPath.Combine (Name);

			pset.SetValue ("IntermediateOutputPath", IntermediateOutputDirectory, defaultImPath);

			// xbuild returns 'false' for DebugSymbols if DebugType==none, no matter which value is defined
			// in the project file. Here we avoid overwriting the value if it has not changed.
			if (debugType != "none" || !debugTypeWasNone)
				pset.SetValue ("DebugSymbols", debugMode, false);
			
			pset.SetValue ("OutputPath", outputDirectory, defaultValue:new FilePath ("." + Path.DirectorySeparatorChar));
			pset.SetValue ("ConsolePause", pauseConsoleOutput, true);
			if (writeExternalConsoleLowercase)
				pset.SetValue ("Externalconsole", externalConsole, false);
			else
				pset.SetValue ("ExternalConsole", externalConsole, false);
			pset.SetValue ("Commandlineparameters", commandLineParameters, "");
			pset.SetValue ("RunWithWarnings", runWithWarnings, true);

			if (debugType != "none" || !debugTypeReadAsEmpty)
				pset.SetValue ("DebugType", debugType, "");

			// Save the env vars only if the dictionary has changed.

			if (loadedEnvironmentVariables == null || loadedEnvironmentVariables.Count != environmentVariables.Count || loadedEnvironmentVariables.Any (e => !environmentVariables.ContainsKey (e.Key) || environmentVariables[e.Key] != e.Value)) {
				if (environmentVariables.Count > 0) {
					string xmlns = GetProjectNamespace ();
					XElement e = new XElement (XName.Get ("EnvironmentVariables", xmlns));
					foreach (var v in environmentVariables) {
						var val = new XElement (XName.Get ("Variable", xmlns));
						val.SetAttributeValue ("name", v.Key);
						val.SetAttributeValue ("value", v.Value);
						e.Add (val);
					}
					pset.SetValue ("EnvironmentVariables", e.ToString (SaveOptions.DisableFormatting));
				} else
					pset.RemoveProperty ("EnvironmentVariables");
				loadedEnvironmentVariables = new Dictionary<string, string> (environmentVariables);
			}

			pset.WriteObjectProperties (this, GetType (), true);
		}

		/// <summary>
		/// Property set where the properties for this configuration are defined.
		/// </summary>
		public IPropertySet Properties {
			get {
				return properties ?? MainPropertyGroup;
			}
			internal set {
				properties = value;
			}
		}

		internal MSBuildPropertyGroup MainPropertyGroup {
			get {
				if (mainPropertyGroup == null) {
					if (ParentItem == null)
						mainPropertyGroup = new MSBuildPropertyGroup ();
					else
						mainPropertyGroup = ParentItem.MSBuildProject.CreatePropertyGroup ();
					mainPropertyGroup.IgnoreDefaultValues = true;
				}
				return mainPropertyGroup;
			}
			set {
				mainPropertyGroup = value;
				mainPropertyGroup.IgnoreDefaultValues = true;
			}
		}

		internal MSBuildProjectInstance ProjectInstance { get; set; }

		FilePath intermediateOutputDirectory = FilePath.Empty;

		public virtual FilePath IntermediateOutputDirectory {
			get {
				if (!intermediateOutputDirectory.IsNullOrEmpty)
					return intermediateOutputDirectory;
				return DefaultIntermediateOutputDirectory;
			}
			set {
				if (value.IsNullOrEmpty)
					value = FilePath.Null;
				if (intermediateOutputDirectory == value)
					return;
				intermediateOutputDirectory = value;
			}
		}

		string DefaultIntermediateOutputDirectory {
			get {
				if (ParentItem == null)
					return string.Empty;
				if (!string.IsNullOrEmpty (Platform))
					return ParentItem.BaseIntermediateOutputPath.Combine (Platform, Name);
				return ParentItem.BaseIntermediateOutputPath.Combine (Name);
			}
		}

		FilePath outputDirectory = "." + Path.DirectorySeparatorChar;
		public virtual FilePath OutputDirectory {
			get { return outputDirectory; }
			set { outputDirectory = value; }
		}

		bool debugMode = false;
		public bool DebugSymbols {
			get { return debugMode; }
			set { debugMode = value; }
		}

		bool pauseConsoleOutput = true;
		public bool PauseConsoleOutput {
			get { return pauseConsoleOutput; }
			set { pauseConsoleOutput = value; }
		}

		bool writeExternalConsoleLowercase = false;
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

		bool debugTypeReadAsEmpty;
		string debugType = "";
		public string DebugType {
			get { return debugType; }
			set { debugType = value; }
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

		protected override void OnCopyFrom (ItemConfiguration configuration, bool isRename)
		{
			base.OnCopyFrom (configuration, isRename);

			ProjectConfiguration projectConf = configuration as ProjectConfiguration;

			if (isRename && projectConf.IntermediateOutputDirectory == projectConf.DefaultIntermediateOutputDirectory)
				intermediateOutputDirectory = null;
			else
				intermediateOutputDirectory = projectConf.intermediateOutputDirectory;

			outputDirectory = projectConf.outputDirectory;

			if (isRename && outputDirectory != null) {
				var ps = outputDirectory.ToString ().Split (Path.DirectorySeparatorChar);
				int i = Array.IndexOf (ps, configuration.Name);
				if (i != -1) {
					ps [i] = Name;
					outputDirectory = string.Join (Path.DirectorySeparatorChar.ToString (), ps);
				}
			}

			debugMode = projectConf.debugMode;
			pauseConsoleOutput = projectConf.pauseConsoleOutput;
			externalConsole = projectConf.externalConsole;
			commandLineParameters = projectConf.commandLineParameters;
			debugType = projectConf.debugType;
			debugTypeWasNone = projectConf.debugTypeWasNone;
			debugTypeReadAsEmpty = projectConf.debugTypeReadAsEmpty;

			environmentVariables.Clear ();
			foreach (KeyValuePair<string, string> el in projectConf.environmentVariables) {
				environmentVariables.Add (el.Key, el.Value);
			}

			runWithWarnings = projectConf.runWithWarnings;

			MainPropertyGroup.CopyFrom (projectConf.MainPropertyGroup);
		}

		public new Project ParentItem {
			get { return (Project) base.ParentItem; }
		}
	}

}
