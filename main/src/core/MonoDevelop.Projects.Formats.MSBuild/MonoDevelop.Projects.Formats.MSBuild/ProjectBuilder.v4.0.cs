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
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Logging;
using Microsoft.Build.Execution;
using System.Xml;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public partial class ProjectBuilder: MarshalByRefObject, IProjectBuilder
	{
		readonly ProjectCollection engine;
		readonly string file;
		ILogWriter currentLogWriter;
		readonly ConsoleLogger consoleLogger;
		readonly BuildEngine buildEngine;

		public ProjectBuilder (BuildEngine buildEngine, ProjectCollection engine, string file)
		{
			this.file = file;
			this.engine = engine;
			this.buildEngine = buildEngine;
			consoleLogger = new ConsoleLogger (LoggerVerbosity.Normal, LogWriteLine, null, null);
			Refresh ();
		}

		public MSBuildResult Run (
			ProjectConfigurationInfo[] configurations, ILogWriter logWriter, MSBuildVerbosity verbosity,
			string[] runTargets, string[] evaluateItems, string[] evaluateProperties, Dictionary<string,string> globalProperties, int taskId)
		{
			if (runTargets == null || runTargets.Length == 0)
				throw new ArgumentException ("runTargets is empty");

			MSBuildResult result = null;
			BuildEngine.RunSTA (taskId, delegate {
				try {
					var project = SetupProject (configurations);
					currentLogWriter = logWriter;

					ILogger[] loggers;
					var logger = new LocalLogger (file);
					if (logWriter != null) {
						consoleLogger.Verbosity = GetVerbosity (verbosity);
						loggers = new ILogger[] { logger, consoleLogger };
					} else {
						loggers = new ILogger[] { logger };
					}

					//building the project will create items and alter properties, so we use a new instance
					var pi = project.CreateProjectInstance ();

					if (globalProperties != null)
						foreach (var p in globalProperties)
							pi.SetProperty (p.Key, p.Value);
					
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
							result.Items[name] = list;
						}
					}
				} catch (Microsoft.Build.Exceptions.InvalidProjectFileException ex) {
					var r = new MSBuildTargetResult (
						file, false, ex.ErrorSubcategory, ex.ErrorCode, ex.ProjectFile,
						ex.LineNumber, ex.ColumnNumber, ex.EndLineNumber, ex.EndColumnNumber,
						ex.BaseMessage, ex.HelpKeyword);
					if (logWriter != null)
						logWriter.WriteLine (r.ToString ());
					result = new MSBuildResult (new [] { r });
				} finally {
					currentLogWriter = null;
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
				var content = buildEngine.GetUnsavedProjectContent (file);
				if (content == null)
					p = engine.LoadProject (file);
				else {
					Environment.CurrentDirectory = Path.GetDirectoryName (file);
					p = engine.LoadProject (new XmlTextReader (new StringReader (content)));
					p.FullPath = file;
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
