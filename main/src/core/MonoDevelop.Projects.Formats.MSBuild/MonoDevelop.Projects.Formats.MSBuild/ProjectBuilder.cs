// 
// ProjectBuilder.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class ProjectBuilder: MarshalByRefObject, IProjectBuilder
	{
		Engine engine;
		string file;
		ILogWriter currentLogWriter;
		MDConsoleLogger consoleLogger;
		BuildEngine buildEngine;

		public ProjectBuilder (BuildEngine buildEngine, Engine engine, string file)
		{
			this.file = file;
			this.engine = engine;
			this.buildEngine = buildEngine;
			consoleLogger = new MDConsoleLogger (LoggerVerbosity.Normal, LogWriteLine, null, null);
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

		void LogWriteLine (string txt)
		{
			if (currentLogWriter != null)
				currentLogWriter.WriteLine (txt);
		}
		
		public MSBuildResult[] RunTarget (string target, ProjectConfigurationInfo[] configurations, ILogWriter logWriter,
			MSBuildVerbosity verbosity)
		{
			MSBuildResult[] result = null;
			BuildEngine.RunSTA (delegate
			{
				try {
					var project = SetupProject (configurations);
					currentLogWriter = logWriter;

					LocalLogger logger = new LocalLogger (Path.GetDirectoryName (file));
					engine.UnregisterAllLoggers ();
					engine.RegisterLogger (logger);
					engine.RegisterLogger (consoleLogger);

					consoleLogger.Verbosity = GetVerbosity (verbosity);

					// We are using this BuildProject overload and the BuildSettings.None argument as a workaround to
					// an xbuild bug which causes references to not be resolved after the project has been built once.
					engine.BuildProject (project, new string[] { target }, new Hashtable (), BuildSettings.None);
					
					result = logger.BuildResult.ToArray ();
				} catch (InvalidProjectFileException ex) {
					result = new MSBuildResult[] { new MSBuildResult (false, ex.ProjectFile ?? file, ex.LineNumber, ex.ColumnNumber, ex.ErrorCode, ex.Message) };
				} finally {
					currentLogWriter = null;
				}
			});
			return result;
		}
		
		LoggerVerbosity GetVerbosity (MSBuildVerbosity verbosity)
		{
			switch (verbosity) {
			case MSBuildVerbosity.Quiet:
				return LoggerVerbosity.Quiet;
			case MSBuildVerbosity.Minimal:
				return LoggerVerbosity.Minimal;
			case MSBuildVerbosity.Normal:
			default:
				return LoggerVerbosity.Normal;
			case MSBuildVerbosity.Detailed:
				return LoggerVerbosity.Detailed;
			case MSBuildVerbosity.Diagnostic:
				return LoggerVerbosity.Diagnostic;
			}
		}

		public string[] GetAssemblyReferences (ProjectConfigurationInfo[] configurations)
		{
			string[] refsArray = null;

			BuildEngine.RunSTA (delegate
			{
				var project = SetupProject (configurations);
				
				// We are using this BuildProject overload and the BuildSettings.None argument as a workaround to
				// an xbuild bug which causes references to not be resolved after the project has been built once.
				engine.BuildProject (project, new string[] { "ResolveAssemblyReferences" }, new Hashtable (), BuildSettings.None);
				BuildItemGroup grp = project.GetEvaluatedItemsByName ("ReferencePath");
				List<string> refs = new List<string> ();
				foreach (BuildItem item in grp)
					refs.Add (UnescapeString (item.Include));
				refsArray = refs.ToArray ();
			});
			return refsArray;
		}
		
		Project SetupProject (ProjectConfigurationInfo[] configurations)
		{
			Project project = null;

			foreach (var pc in configurations) {
				var p = engine.GetLoadedProject (pc.ProjectFile);
				if (p == null) {
					p = new Project (engine);
					var content = buildEngine.GetUnsavedProjectContent (pc.ProjectFile);
					if (content == null)
						p.Load (pc.ProjectFile);
					else {
						p.FullFileName = pc.ProjectFile;

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
				p.GlobalProperties.SetProperty ("Configuration", pc.Configuration);
				if (!string.IsNullOrEmpty (pc.Platform))
					p.GlobalProperties.SetProperty ("Platform", pc.Platform);
				else
					p.GlobalProperties.RemoveProperty ("Platform");
				if (pc.ProjectFile == file)
					project = p;
			}

			Environment.CurrentDirectory = Path.GetDirectoryName (file);
			return project;
		}

		bool? hasXbuildFileBug;

		bool HasXbuildFileBug ()
		{
			if (hasXbuildFileBug == null) {
				Project p = new Project ();
				p.FullFileName = "foo";
				p.LoadXml ("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"/>");
				hasXbuildFileBug = p.FullFileName.Length == 0;
			}
			return hasXbuildFileBug.Value;
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}

		//from MSBuildProjectService
		static string UnescapeString (string str)
		{
			int i = str.IndexOf ('%');
			while (i != -1 && i < str.Length - 2) {
				int c;
				if (int.TryParse (str.Substring (i+1, 2), System.Globalization.NumberStyles.HexNumber, null, out c))
					str = str.Substring (0, i) + (char) c + str.Substring (i + 3);
				i = str.IndexOf ('%', i + 1);
			}
			return str;
		}
	}
}
