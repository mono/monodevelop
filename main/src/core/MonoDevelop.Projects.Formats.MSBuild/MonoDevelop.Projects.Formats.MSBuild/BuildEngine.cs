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
		static readonly AutoResetEvent workDoneEvent = new AutoResetEvent (false);
		static ThreadStart workDelegate;
		static readonly object workLock = new object ();
		static Thread workThread;
		static CultureInfo uiCulture;
		static Exception workError;

		readonly ManualResetEvent doneEvent = new ManualResetEvent (false);
		readonly Dictionary<string,string> unsavedProjects = new Dictionary<string, string> ();

		internal readonly Engine Engine = new Engine { DefaultToolsVersion = MSBuildConsts.Version };

		public void Dispose ()
		{
			doneEvent.Set ();
		}
		
		internal WaitHandle WaitHandle {
			get { return doneEvent; }
		}

		public void Ping ()
		{
		}

		public void SetCulture (CultureInfo uiCulture)
		{
			BuildEngine.uiCulture = uiCulture;

		}

		public void SetGlobalProperties (IDictionary<string, string> properties)
		{
			var gp = Engine.GlobalProperties;
			foreach (var p in properties)
				gp.SetProperty (p.Key, p.Value);
		}

		public IProjectBuilder LoadProject (string file)
		{
			return new ProjectBuilder (this, file);
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

		internal void UnloadProject (string file)
		{
			lock (unsavedProjects)
				unsavedProjects.Remove (file);

			RunSTA (delegate {
				var loadedProj = Engine.GetLoadedProject (file);
				if (loadedProj != null)
					Engine.UnloadProject (loadedProj);
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
				workDoneEvent.WaitOne ();
			}
			if (workError != null)
				throw new Exception ("MSBuild operation failed", workError);
		}

		static readonly object threadLock = new object ();
		
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
					workDoneEvent.Set ();
				}
				while (Monitor.Wait (threadLock, 60000));
				
				workThread = null;
			}
		}
	}
}