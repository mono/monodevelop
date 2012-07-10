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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Collections;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class BuildEngine: MarshalByRefObject, IBuildEngine
	{
		static AutoResetEvent wordDoneEvent = new AutoResetEvent (false);
		static ThreadStart workDelegate;
		static object workLock = new object ();
		static Thread workThread;
		static Exception workError;

		ManualResetEvent doneEvent = new ManualResetEvent (false);
		Dictionary<string,ProjectCollection> engines = new Dictionary<string, ProjectCollection> ();

		public void Dispose ()
		{
			doneEvent.Set ();
		}
		
		internal WaitHandle WaitHandle {
			get { return doneEvent; }
		}

		public IProjectBuilder LoadProject (string file, string binDir)
		{
			return new ProjectBuilder (this, GetEngine (binDir), file);
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

		ProjectCollection GetEngine (string binDir)
		{
			ProjectCollection engine = null;
			RunSTA (delegate {
				if (!engines.TryGetValue (binDir, out engine)) {
					engine = new ProjectCollection ();
					engine.SetGlobalProperty ("BuildingInsideVisualStudio", "true");
					
					//we don't have host compilers in MD, and this is set to true by some of the MS targets
					//which causes it to always run the CoreCompile task if BuildingInsideVisualStudio is also
					//true, because the VS in-process compiler would take care of the deps tracking
					engine.SetGlobalProperty ("UseHostCompilerIfAvailable", "false");
					engines [binDir] = engine;
				}
			});
			return engine;
		}

		internal void UnloadProject (string file)
		{
			RunSTA (delegate {
				foreach (var engine in engines.Values) {
					ProjectRootElement xml = null;
					foreach (var p in engine.GetLoadedProjects (file)) {
						engine.UnloadProject (p);
						xml = p.Xml;
					}
					if (xml != null) {
						// All the projects from the same file share the same xml, so it has to be unloaded only once
						engine.UnloadProject (xml);
					}
				}
			});
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