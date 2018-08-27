// 
// ProjectBuilder.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2009-2011 Novell, Inc (http://www.novell.com)
// Copyright (c) 2011-2015 Xamarin Inc. (http://www.xamarin.com)
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
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Logging;
using Microsoft.Build.Execution;
using System.Xml;

namespace MonoDevelop.Projects.MSBuild
{
	partial class ProjectBuilder
	{
		readonly ProjectCollection engine;
		readonly string file;
		readonly BuildEngine buildEngine;

		public ProjectBuilder (BuildEngine buildEngine, ProjectCollection engine, string file)
		{
			this.file = file;
			this.engine = engine;
			this.buildEngine = buildEngine;
			Refresh ();
		}

		public MSBuildResult Run (
			ProjectConfigurationInfo[] configurations, IEngineLogWriter logWriter, MSBuildVerbosity verbosity,
			string[] runTargets, string[] evaluateItems, string[] evaluateProperties, Dictionary<string,string> globalProperties, int taskId)
		{
			if (runTargets == null || runTargets.Length == 0)
				throw new ArgumentException ("runTargets is empty");

			MSBuildResult result = null;

			BuildEngine.RunSTA (taskId, delegate {
				Project project = null;
				Dictionary<string, string> originalGlobalProperties = null;

				MSBuildLoggerAdapter loggerAdapter;

				if (buildEngine.BuildOperationStarted) {
					loggerAdapter = buildEngine.StartProjectSessionBuild (logWriter);
				}
				else
					loggerAdapter = new MSBuildLoggerAdapter (logWriter, verbosity);

				try {
					project = SetupProject (configurations);

					if (globalProperties != null) {
						originalGlobalProperties = new Dictionary<string, string> ();
						foreach (var p in project.GlobalProperties)
							originalGlobalProperties [p.Key] = p.Value;
						if (globalProperties != null) {
							foreach (var p in globalProperties)
								project.SetGlobalProperty (p.Key, p.Value);
						}
					}

					// Building the project will create items and alter properties, so we use a new instance
					var pi = project.CreateProjectInstance ();

					Build (pi, runTargets, loggerAdapter.Loggers);

					result = new MSBuildResult (loggerAdapter.BuildResult.ToArray ());

					if (evaluateProperties != null) {
						foreach (var name in evaluateProperties) {
							var prop = pi.GetProperty (name);
							result.Properties [name] = prop != null? prop.EvaluatedValue : null;
						}
					}

					if (evaluateItems != null) {
						foreach (var name in evaluateItems) {
							var grp = pi.GetItems (name);
							var list = new List<MSBuildEvaluatedItem> ();
							foreach (var item in grp) {
								var evItem = new MSBuildEvaluatedItem (name, UnescapeString (item.EvaluatedInclude));
								foreach (var metadataName in item.MetadataNames) {
									evItem.Metadata [metadataName] = UnescapeString (item.GetMetadataValue (metadataName));
								}
								list.Add (evItem);
							}
							result.Items[name] = list.ToArray ();
						}
					}
				} catch (Microsoft.Build.Exceptions.InvalidProjectFileException ex) {
					var r = new MSBuildTargetResult (
						file, false, ex.ErrorSubcategory, ex.ErrorCode, ex.ProjectFile,
						ex.LineNumber, ex.ColumnNumber, ex.EndLineNumber, ex.EndColumnNumber,
						ex.BaseMessage, ex.HelpKeyword);
					loggerAdapter.LogWriteLine (r.ToString ());
					result = new MSBuildResult (new [] { r });
				} finally {
					if (buildEngine.BuildOperationStarted)
						buildEngine.EndProjectSessionBuild ();
					else
						loggerAdapter.Dispose ();
					
					if (project != null && globalProperties != null) {
						foreach (var p in globalProperties)
							project.RemoveGlobalProperty (p.Key);
						foreach (var p in originalGlobalProperties)
							project.SetGlobalProperty (p.Key, p.Value);
					}
				}
			});
			return result;
		}

		Project SetupProject (ProjectConfigurationInfo[] configurations)
		{
			Project project = null;

			var slnConfigContents = GenerateSolutionConfigurationContents (configurations);

			foreach (var pc in configurations) {
				var p = ConfigureProject (pc.ProjectFile, pc.Configuration, pc.Platform, slnConfigContents);
				if (pc.ProjectFile == file)
					project = p;
			}

			// Reload referenced projects if they have changed in disk. ProjectCollection doesn't do it automatically.

			foreach (var p in project.Imports) {
				if (File.Exists (p.ImportedProject.FullPath) && p.ImportedProject.LastWriteTimeWhenRead != File.GetLastWriteTime (p.ImportedProject.FullPath))
					p.ImportedProject.Reload (false);
			}

			var projectDir = Path.GetDirectoryName (file);
			if (!string.IsNullOrEmpty (projectDir) && Directory.Exists (projectDir))
				Environment.CurrentDirectory = projectDir;
			return project;
		}

		Project ConfigureProject (string file, string configuration, string platform, string slnConfigContents)
		{			
			bool firstConfigure = false;
			var p = engine.GetLoadedProjects (file).FirstOrDefault ();
			if (p == null) {
				firstConfigure = true;
				var projectDir = Path.GetDirectoryName (file);

				// HACK: workaround to MSBuild bug #53019. We need to ensure that $(BaseIntermediateOutputPath) exists before
				// loading the project.
				if (!string.IsNullOrEmpty (projectDir))
					Directory.CreateDirectory (Path.Combine (projectDir, "obj"));

				var content = buildEngine.GetUnsavedProjectContent (file);
				if (content == null)
					p = engine.LoadProject (file);
				else {
					if (!string.IsNullOrEmpty (projectDir) && Directory.Exists (projectDir))
						Environment.CurrentDirectory = projectDir;

					var settings = new XmlReaderSettings {
						DtdProcessing = DtdProcessing.Ignore,
						IgnoreWhitespace = true,
						IgnoreComments = true,
					};
					var projectRootElement = ProjectRootElement.Create (XmlReader.Create (new StringReader (content), settings), engine);
					projectRootElement.FullPath = file;

					// Use the engine's default tools version to load the project. We want to build with the latest
					// tools version.
					p = new Project (projectRootElement, engine.GlobalProperties, MSBuildConsts.Version, engine);
				}
			}

			if (firstConfigure || p.GetPropertyValue ("Configuration") != configuration || (p.GetPropertyValue ("Platform") ?? "") != (platform ?? "")) {
				p.SetGlobalProperty ("Configuration", configuration);
				if (!string.IsNullOrEmpty (platform))
					p.SetGlobalProperty ("Platform", platform);
				else
					p.RemoveGlobalProperty ("Platform");
			}

			// The CurrentSolutionConfigurationContents property only needs to be set once
			// for the project actually being built
			// If a build session was started, that property is already set at engine level

			if (!buildEngine.BuildOperationStarted && this.file == file && p.GetPropertyValue ("CurrentSolutionConfigurationContents") != slnConfigContents) {
				p.SetGlobalProperty ("CurrentSolutionConfigurationContents", slnConfigContents);
			}

			return p;
		}


		/// <summary>
		/// Builds a list of targets with the specified loggers.
		/// </summary>
		internal void Build (ProjectInstance pi, string [] targets, IEnumerable<ILogger> loggers)
		{
			BuildResult results;

			if (!buildEngine.BuildOperationStarted) {
				BuildParameters parameters = new BuildParameters (engine);
				parameters.ResetCaches = false;
				parameters.EnableNodeReuse = true;
				BuildRequestData data = new BuildRequestData (pi, targets, parameters.HostServices);
				parameters.Loggers = loggers;
				results = BuildManager.DefaultBuildManager.Build (parameters, data);
			} else {
				BuildRequestData data = new BuildRequestData (pi, targets);
				results = BuildManager.DefaultBuildManager.BuildRequest (data);
			}
		}
		public void Dispose ()
		{
			buildEngine.UnloadProject (file);
		}

		public void Refresh ()
		{
			buildEngine.UnloadProject (file);
		}

		public void RefreshWithContent (string projectContent)
		{
			buildEngine.UnloadProject (file);
			buildEngine.SetUnsavedProjectContent (file, projectContent);
		}

		public static LoggerVerbosity GetVerbosity (MSBuildVerbosity verbosity)
		{
			switch (verbosity) {
			case MSBuildVerbosity.Quiet:
				return LoggerVerbosity.Quiet;
			case MSBuildVerbosity.Minimal:
				return LoggerVerbosity.Minimal;
			default:
				return LoggerVerbosity.Normal;
			case MSBuildVerbosity.Detailed:
				return LoggerVerbosity.Detailed;
			case MSBuildVerbosity.Diagnostic:
				return LoggerVerbosity.Diagnostic;
			}
		}

		//from MSBuildProjectService
		static string UnescapeString (string str)
		{
			int i = str.IndexOf ('%');
			while (i != -1 && i < str.Length - 2) {
				int c;
				if (int.TryParse (str.Substring (i + 1, 2), System.Globalization.NumberStyles.HexNumber, null, out c))
					str = str.Substring (0, i) + (char)c + str.Substring (i + 3);
				i = str.IndexOf ('%', i + 1);
			}
			return str;
		}

		internal static string GenerateSolutionConfigurationContents (ProjectConfigurationInfo [] configurations)
		{
			// can't use XDocument because of the 2.0 builder
			// and don't just build a string because things may need escaping

			var doc = new XmlDocument ();
			var root = doc.CreateElement ("SolutionConfiguration");
			doc.AppendChild (root);
			foreach (var config in configurations) {
				var el = doc.CreateElement ("ProjectConfiguration");
				root.AppendChild (el);
				el.SetAttribute ("Project", config.ProjectGuid);
				el.SetAttribute ("AbsolutePath", config.ProjectFile);
				el.SetAttribute ("BuildProjectInSolution", config.Enabled ? "True" : "False");
				el.InnerText = string.Format (config.Configuration + "|" + config.Platform);
			}

			//match MSBuild formatting
			var options = new XmlWriterSettings {
				Indent = true,
				IndentChars = "",
				OmitXmlDeclaration = true,
			};
			using (var sw = new StringWriter ())
			using (var xw = XmlWriter.Create (sw, options)) {
				doc.WriteTo (xw);
				xw.Flush ();
				return sw.ToString ();
			}
		}
	}
}
