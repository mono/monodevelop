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
		readonly string sdksPath;

		public ProjectBuilder (BuildEngine buildEngine, ProjectCollection engine, string file, string sdksPath)
		{
			this.file = file;
			this.sdksPath = sdksPath;
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
				try {
					if (sdksPath != null)
						Environment.SetEnvironmentVariable ("MSBuildSDKsPath", sdksPath);
					project = SetupProject (configurations);
					InitLogger (logWriter);

					ILogger[] loggers;
					var logger = new LocalLogger (file);
					if (logWriter != null) {
						var consoleLogger = new ConsoleLogger (GetVerbosity (verbosity), LogWrite, null, null);
						var eventLogger = new TargetLogger (logWriter.RequiredEvents, LogEvent);
						loggers = new ILogger[] { logger, consoleLogger, eventLogger };
					} else {
						loggers = new ILogger[] { logger };
					}

					if (globalProperties != null) {
						originalGlobalProperties = new Dictionary<string, string> ();
						foreach (var p in project.GlobalProperties)
							originalGlobalProperties [p.Key] = p.Value;
						foreach (var p in globalProperties)
							project.SetGlobalProperty (p.Key, p.Value);
						project.ReevaluateIfNecessary ();
					}

					//building the project will create items and alter properties, so we use a new instance
					var pi = project.CreateProjectInstance ();
					
					pi.Build (runTargets, loggers);

					result = new MSBuildResult (logger.BuildResult.ToArray ());

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
								foreach (var m in item.Metadata) {
									evItem.Metadata [m.Name] = UnescapeString (m.EvaluatedValue);
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
					LogWriteLine (r.ToString ());
					result = new MSBuildResult (new [] { r });
				} finally {
					DisposeLogger ();
					if (project != null && globalProperties != null) {
						foreach (var p in globalProperties)
							project.RemoveGlobalProperty (p.Key);
						foreach (var p in originalGlobalProperties)
							project.SetGlobalProperty (p.Key, p.Value);
						project.ReevaluateIfNecessary ();
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

			Environment.CurrentDirectory = Path.GetDirectoryName (file);
			return project;
		}

		Project ConfigureProject (string file, string configuration, string platform, string slnConfigContents)
		{			
			var p = engine.GetLoadedProjects (file).FirstOrDefault ();
			if (p == null) {
				
				// HACK: workaround to MSBuild bug #53019. We need to ensure that $(BaseIntermediateOutputPath) exists before
				// loading the project.
				Directory.CreateDirectory (Path.Combine (Path.GetDirectoryName (file), "obj"));

				var content = buildEngine.GetUnsavedProjectContent (file);
				if (content == null)
					p = engine.LoadProject (file);
				else {
					Environment.CurrentDirectory = Path.GetDirectoryName (file);
					var projectRootElement = ProjectRootElement.Create (new XmlTextReader (new StringReader (content)));
					projectRootElement.FullPath = file;

					// Use the engine's default tools version to load the project. We want to build with the latest
					// tools version.
					string toolsVersion = engine.DefaultToolsVersion;
					p = new Project (projectRootElement, engine.GlobalProperties, toolsVersion, engine);
				}
			}
			p.SetProperty ("CurrentSolutionConfigurationContents", slnConfigContents);
			p.SetProperty ("Configuration", configuration);
			if (!string.IsNullOrEmpty (platform))
				p.SetProperty ("Platform", platform);
			else
				p.SetProperty ("Platform", "");
			return p;
		}
	}
}
