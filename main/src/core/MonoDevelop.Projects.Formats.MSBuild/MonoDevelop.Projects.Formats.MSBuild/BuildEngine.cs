// 
// ProjectBuilder.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Runtime.Remoting;
using System.Collections.Generic;
using Microsoft.Build.BuildEngine;
using System.Globalization;
using System.IO;

//this is the builder for the deprecated build engine API
#pragma warning disable 618

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class BuildEngine: MarshalByRefObject, IBuildEngine
	{
		static AutoResetEvent wordDoneEvent = new AutoResetEvent (false);
		static ThreadStart workDelegate;
		static object workLock = new object ();
		static Thread workThread;
		static CultureInfo uiCulture;
		static Exception workError;

		ManualResetEvent doneEvent = new ManualResetEvent (false);
		Dictionary<string,string> unsavedProjects = new Dictionary<string, string> ();
		Engine engine;

		public void Dispose ()
		{
			doneEvent.Set ();
		}
		
		internal WaitHandle WaitHandle {
			get { return doneEvent; }
		}

		public void Initialize (string slnFile, CultureInfo uiCulture)
		{
			BuildEngine.uiCulture = uiCulture;
			engine = InitializeEngine (slnFile);
		}

		public IProjectBuilder LoadProject (string file)
		{
			return new ProjectBuilder (this, engine, file);
		}
		
		public void UnloadProject (IProjectBuilder pb)
		{
			((ProjectBuilder)pb).Dispose ();
			RemotingServices.Disconnect ((MarshalByRefObject) pb);
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}

		static Engine InitializeEngine (string slnFile)
		{
			var engine = new Engine ();
			engine.DefaultToolsVersion = MSBuildConsts.Version;
			var gp = engine.GlobalProperties;

			//this causes build targets to behave how they should inside an IDE, instead of in a command-line process
			gp.SetProperty ("BuildingInsideVisualStudio", "true");

			//we don't have host compilers in MD, and this is set to true by some of the MS targets
			//which causes it to always run the CoreCompile task if BuildingInsideVisualStudio is also
			//true, because the VS in-process compiler would take care of the deps tracking
			gp.SetProperty ("UseHostCompilerIfAvailable", "false");

			if (string.IsNullOrEmpty (slnFile))
				return engine;

			gp.SetProperty ("SolutionPath", Path.GetFullPath (slnFile));
			gp.SetProperty ("SolutionName", Path.GetFileNameWithoutExtension (slnFile));
			gp.SetProperty ("SolutionFilename", Path.GetFileName (slnFile));
			gp.SetProperty ("SolutionDir", Path.GetDirectoryName (slnFile) + Path.DirectorySeparatorChar);

			return engine;
		}

		internal void UnloadProject (Engine engine, string file, bool releaseEngine)
		{
			lock (unsavedProjects)
				unsavedProjects.Remove (file);

			RunSTA (delegate {
				var loadedProj = engine.GetLoadedProject (file);
				if (loadedProj != null)
					engine.UnloadProject (loadedProj);
			});
		}

		internal void SetUnsavedProjectContent (string file, string content)
		{
			lock (unsavedProjects)
				unsavedProjects [file] = content;
		}

		internal string GetUnsavedProjectContent (string file)
		{
			lock (unsavedProjects) {
				string content;
				unsavedProjects.TryGetValue (file, out content);
				return content;
			}
		}

		internal static void RunSTA (ThreadStart ts)
		{
			lock (workLock) {
				lock (threadLock) {
					workDelegate = ts;
					workError = null;
					if (workThread == null) {
						workThread = new Thread (STARunner);
						workThread.SetApartmentState (ApartmentState.STA);
						workThread.IsBackground = true;
						workThread.CurrentUICulture = uiCulture;
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

		static object threadLock = new object ();
		
		static void STARunner ()
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