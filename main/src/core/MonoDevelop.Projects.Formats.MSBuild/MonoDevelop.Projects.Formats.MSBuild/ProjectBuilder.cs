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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class ProjectBuilder: MarshalByRefObject, IProjectBuilder
	{
		Project project;
		Engine engine;
		string file;
		ILogWriter currentLogWriter;
		MDConsoleLogger consoleLogger;

		AutoResetEvent wordDoneEvent = new AutoResetEvent (false);
		ThreadStart workDelegate;
		object workLock = new object ();
		Thread workThread;
		Exception workError;

		public ProjectBuilder (string file, string binDir)
		{
			this.file = file;
			RunSTA (delegate
			{
				engine = new Engine (binDir);
				engine.GlobalProperties.SetProperty ("BuildingInsideVisualStudio", "true");

				consoleLogger = new MDConsoleLogger (LoggerVerbosity.Normal, LogWriteLine, null, null);
				engine.RegisterLogger (consoleLogger);
			});
			
			Refresh ();
		}
		
		public void Refresh ()
		{
			RunSTA (delegate
			{
				project = new Project (engine);
				project.Load (file);
			});
		}
		
		void LogWriteLine (string txt)
		{
			if (currentLogWriter != null)
				currentLogWriter.WriteLine (txt);
		}
		
		public MSBuildResult[] RunTarget (string target, string configuration, string platform, ILogWriter logWriter,
			MSBuildVerbosity verbosity)
		{
			MSBuildResult[] result = null;
			RunSTA (delegate
			{
				try {
					SetupEngine (configuration, platform);
					currentLogWriter = logWriter;

					LocalLogger logger = new LocalLogger (Path.GetDirectoryName (file));
					engine.RegisterLogger (logger);

					consoleLogger.Verbosity = GetVerbosity (verbosity);
					project.Build (target);
					result = logger.BuildResult.ToArray ();

				}
				finally {
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

		public string[] GetAssemblyReferences (string configuration, string platform)
		{
			string[] refsArray = null;

			RunSTA (delegate
			{
				SetupEngine (configuration, platform);

				project.Build ("ResolveAssemblyReferences");
				BuildItemGroup grp = project.GetEvaluatedItemsByName ("ReferencePath");
				List<string> refs = new List<string> ();
				foreach (BuildItem item in grp)
					refs.Add (item.Include);
				refsArray = refs.ToArray ();
			});
			return refsArray;
		}
		
		void SetupEngine (string configuration, string platform)
		{
			Environment.CurrentDirectory = Path.GetDirectoryName (file);
			engine.GlobalProperties.SetProperty ("Configuration", configuration);
			if (!string.IsNullOrEmpty (platform))
				engine.GlobalProperties.SetProperty ("Platform", platform);
			else
				engine.GlobalProperties.RemoveProperty ("Platform");
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}

		void RunSTA (ThreadStart ts)
		{
			lock (workLock) {
				lock (threadLock) {
					workDelegate = ts;
					workError = null;
					if (workThread == null) {
						workThread = new Thread (STARunner);
						workThread.SetApartmentState (ApartmentState.STA);
						workThread.IsBackground = true;
						workThread.Start ();
					}
					else
						// Awaken the existing thread
						Monitor.Pulse (threadLock);
				}
				wordDoneEvent.WaitOne ();
			}
			if (workError != null)
				throw new Exception ("MSBuild operation failed", workError);
		}

		object threadLock = new object ();

		void STARunner ()
		{
			lock (threadLock) {
				do {
					try {
						workDelegate ();
					}
					catch (Exception ex) {
						workError = ex;
					}
					wordDoneEvent.Set ();
				}
				while (Monitor.Wait (threadLock, 60000));

				workThread = null;
			}
		}
	}
}
