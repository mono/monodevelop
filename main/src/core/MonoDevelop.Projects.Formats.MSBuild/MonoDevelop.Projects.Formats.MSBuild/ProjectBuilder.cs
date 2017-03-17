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
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;

//this is the builder for the deprecated build engine API
using System.Linq;


#pragma warning disable 618

namespace MonoDevelop.Projects.MSBuild
{
	partial class ProjectBuilder: MarshalByRefObject
	{
		readonly string file;
		readonly MDConsoleLogger consoleLogger;
		readonly BuildEngine buildEngine;

		public ProjectBuilder (BuildEngine buildEngine, string file)
		{
			if (file == null) {
				throw new ArgumentNullException ("file");
			}
			this.file = file;
			this.buildEngine = buildEngine;
			consoleLogger = new MDConsoleLogger (LoggerVerbosity.Normal, LogWriteLine, null, null);
		}

		//HACK: Mono does not implement 3.5 CustomMetadataNames API
		FieldInfo evaluatedMetadataField = typeof(BuildItem).GetField ("evaluatedMetadata", BindingFlags.NonPublic | BindingFlags.Instance);

		public MSBuildResult Run (
			ProjectConfigurationInfo[] configurations, IEngineLogWriter logWriter, MSBuildVerbosity verbosity,
			string[] runTargets, string[] evaluateItems, string[] evaluateProperties, Dictionary<string,string> globalProperties, int taskId)
		{
			MSBuildResult result = null;
			BuildEngine.RunSTA (taskId, delegate {
				try {
					var project = SetupProject (configurations);
					InitLogger (logWriter);

					buildEngine.Engine.UnregisterAllLoggers ();

					var logger = new LocalLogger (file);
					buildEngine.Engine.RegisterLogger (logger);
					if (logWriter != null) {
						buildEngine.Engine.RegisterLogger (consoleLogger);
						consoleLogger.Verbosity = GetVerbosity (verbosity);
						buildEngine.Engine.RegisterLogger (new TargetLogger (logWriter.RequiredEvents, LogEvent));
					}

					if (runTargets != null && runTargets.Length > 0) {
						if (globalProperties != null) {
							foreach (var p in globalProperties)
								project.GlobalProperties.SetProperty (p.Key, p.Value);
                        }

						// We are using this BuildProject overload and the BuildSettings.None argument as a workaround to
						// an xbuild bug which causes references to not be resolved after the project has been built once.
						buildEngine.Engine.BuildProject (project, runTargets, new Hashtable (), BuildSettings.None);

						if (globalProperties != null) {
							foreach (var p in globalProperties.Keys) {
								project.GlobalProperties.RemoveProperty (p);
								buildEngine.Engine.GlobalProperties.RemoveProperty (p);
							}
						}
					}

					result = new MSBuildResult (logger.BuildResult.ToArray ());

					if (evaluateProperties != null) {
						foreach (var name in evaluateProperties)
							result.Properties [name] = project.GetEvaluatedProperty (name);
					}

					if (evaluateItems != null) {
						foreach (var name in evaluateItems) {
							BuildItemGroup grp = project.GetEvaluatedItemsByName (name);
							var list = new List<MSBuildEvaluatedItem> ();
							foreach (BuildItem item in grp) {
								var evItem = new MSBuildEvaluatedItem (name, UnescapeString (item.FinalItemSpec));
								foreach (DictionaryEntry de in (IDictionary) evaluatedMetadataField.GetValue (item)) {
									evItem.Metadata [(string)de.Key] = UnescapeString ((string)de.Value);
								}
								list.Add (evItem);
							}
							result.Items[name] = list.ToArray ();
						}
					}
				} catch (InvalidProjectFileException ex) {
					var r = new MSBuildTargetResult (
						file, false, ex.ErrorSubcategory, ex.ErrorCode, ex.ProjectFile,
						ex.LineNumber, ex.ColumnNumber, ex.EndLineNumber, ex.EndColumnNumber,
						ex.BaseMessage, ex.HelpKeyword);
					LogWriteLine (r.ToString ());
					result = new MSBuildResult (new [] { r });
				} finally {
					DisposeLogger ();
				}
			});
			return result;
		}

		Project SetupProject (ProjectConfigurationInfo[] configurations)
		{
			Project project = null;

			var slnConfigContents = GenerateSolutionConfigurationContents (configurations);

			foreach (var pc in configurations) {
				var p = buildEngine.Engine.GetLoadedProject (pc.ProjectFile);

				if (p != null && pc.ProjectFile == file) {
					// building the project may create new items and/or modify some properties,
					// so we always need to use a new instance of the project when building
					buildEngine.Engine.UnloadProject (p);
					p = null;
				}

				Environment.CurrentDirectory = Path.GetDirectoryName (Path.GetFullPath (file));

				if (p == null) {
					p = new Project (buildEngine.Engine);
					var content = buildEngine.GetUnsavedProjectContent (pc.ProjectFile);
					if (content == null) {
						p.Load (pc.ProjectFile);
					} else {
						p.FullFileName = Path.GetFullPath (pc.ProjectFile);

						if (HasXbuildFileBug ()) {
							// Workaround for Xamarin bug #14295: Project.Load incorrectly resets the FullFileName property
							var t = p.GetType ();
							t.InvokeMember ("PushThisFileProperty", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, p, new object[] { p.FullFileName });
							t.InvokeMember ("DoLoad", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, p, new object[] { new StringReader (content) });
						} else {
							p.Load (new StringReader (content));
						}
					}
				}

				p.GlobalProperties.SetProperty ("CurrentSolutionConfigurationContents", slnConfigContents);
				p.GlobalProperties.SetProperty ("Configuration", pc.Configuration);
				if (!string.IsNullOrEmpty (pc.Platform))
					p.GlobalProperties.SetProperty ("Platform", pc.Platform);
				else
					p.GlobalProperties.RemoveProperty ("Platform");
				if (pc.ProjectFile == file)
					project = p;
			}

			Environment.CurrentDirectory = Path.GetDirectoryName (Path.GetFullPath (file));
			return project;
		}

		bool? hasXbuildFileBug;

		bool HasXbuildFileBug ()
		{
			if (hasXbuildFileBug == null) {
				var p = new Project ();
				p.FullFileName = "foo";
				p.LoadXml ("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"/>");
				hasXbuildFileBug = p.FullFileName.Length == 0;
			}
			return hasXbuildFileBug.Value;
		}
	}
}
