// 
// RemoteProjectBuilder.cs
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
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.IO;
using System.Linq;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class RemoteBuildEngine: IBuildEngine
	{
		IBuildEngine engine;
		Process proc;
		bool alive = true;
		
		public int ReferenceCount { get; set; }
		public DateTime ReleaseTime { get; set; }
		
		public RemoteBuildEngine (Process proc, IBuildEngine engine)
		{
			this.proc = proc;
			this.engine = engine;
		}

		public event EventHandler Disconnected;

		public IProjectBuilder LoadProject (string projectFile)
		{
			try {
				return engine.LoadProject (projectFile);
			} catch (Exception ex) {
				CheckDisconnected ();
				throw;
			}
		}
		
		public void UnloadProject (IProjectBuilder pb)
		{
			try {
				engine.UnloadProject (pb);
			} catch (Exception ex) {
				LoggingService.LogError ("Project unloading failed", ex);
				if (!CheckDisconnected ())
					throw;
			}
		}

		public void SetCulture (CultureInfo uiCulture)
		{
			try {
				engine.SetCulture (uiCulture);
			} catch (Exception ex) {
				CheckDisconnected ();
				throw;
			}
		}

		public void SetGlobalProperties (IDictionary<string, string> properties)
		{
			try {
				engine.SetGlobalProperties (properties);
			} catch (Exception ex) {
				CheckDisconnected ();
				throw;
			}
		}

		void IBuildEngine.Ping ()
		{
			engine.Ping ();
		}

		bool CheckAlive ()
		{
			if (!alive)
				return false;
			try {
				engine.Ping ();
				return true;
			} catch {
				alive = false;
				return false;
			}
		}

		internal bool CheckDisconnected ()
		{
			if (!CheckAlive ()) {
				if (Disconnected != null)
					Disconnected (this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		public void Dispose ()
		{
			try {
				alive = false;
				if (proc != null) {
					try {
						proc.Kill ();
					} catch {
					}
				}
				else
					engine.Dispose ();
			} catch {
				// Ignore
			}
		}
	}
	
	public class RemoteProjectBuilder: IDisposable
	{
		RemoteBuildEngine engine;
		IProjectBuilder builder;
		Dictionary<string,string[]> referenceCache;
		string file;

		internal RemoteProjectBuilder (string file, RemoteBuildEngine engine)
		{
			this.file = file;
			this.engine = engine;
			builder = engine.LoadProject (file);
			referenceCache = new Dictionary<string, string[]> ();
		}

		public event EventHandler Disconnected;

		void CheckDisconnected ()
		{
			if (engine != null && engine.CheckDisconnected ()) {
				if (Disconnected != null)
					Disconnected (this, EventArgs.Empty);
			}
		}

		public MSBuildResult Run (
			ProjectConfigurationInfo[] configurations,
			ILogWriter logWriter,
			MSBuildVerbosity verbosity,
			string[] runTargets,
			string[] evaluateItems,
			string[] evaluateProperties,
			Dictionary<string,string> globalProperties)
		{
			try {
				return builder.Run (configurations, logWriter, verbosity, runTargets, evaluateItems, evaluateProperties, globalProperties);
			} catch (Exception ex) {
				CheckDisconnected ();
				LoggingService.LogError ("RunTarget failed", ex);
				MSBuildTargetResult err = new MSBuildTargetResult (file, false, "", "", file, 1, 1, 1, 1, "Unknown MSBuild failure. Please try building the project again", "");
				MSBuildResult res = new MSBuildResult (new [] { err });
				return res;
			}
		}

		public string[] ResolveAssemblyReferences (ProjectConfigurationInfo[] configurations)
		{
			string[] refs = null;
			var id = configurations [0].Configuration + "|" + configurations [0].Platform;

			lock (referenceCache) {
				if (!referenceCache.TryGetValue (id, out refs)) {
					MSBuildResult result;
					try {
						result = builder.Run (
						            configurations, null, MSBuildVerbosity.Normal,
						            new[] { "ResolveAssemblyReferences" }, new [] { "ReferencePath" }, null, null
					            );
					} catch (Exception ex) {
						CheckDisconnected ();
						LoggingService.LogError ("ResolveAssemblyReferences failed", ex);
						return new string[0];
					}

					List<MSBuildEvaluatedItem> items;
					if (result.Items.TryGetValue ("ReferencePath", out items) && items != null) {
						refs = items.Select (i => i.ItemSpec).ToArray ();
					} else
						refs = new string[0];

					referenceCache [id] = refs;
				}
			}
			return refs;
		}

		public string[] GetSupportedTargets (ProjectConfigurationInfo[] configurations)
		{
			try {
				return builder.GetSupportedTargets (configurations);
			} catch (Exception ex) {
				CheckDisconnected ();
				LoggingService.LogError ("GetSupportedTargets failed", ex);
				return new string[0];
			}
		}

		public void Refresh ()
		{
			lock (referenceCache)
				referenceCache.Clear ();
			try {
				builder.Refresh ();
			} catch (Exception ex) {
				LoggingService.LogError ("MSBuild refresh failed", ex);
				CheckDisconnected ();
			}
		}
		
		public void RefreshWithContent (string projectContent)
		{
			lock (referenceCache)
				referenceCache.Clear ();
			try {
				builder.RefreshWithContent (projectContent);
			} catch (Exception ex) {
				LoggingService.LogError ("MSBuild refresh failed", ex);
				CheckDisconnected ();
			}
		}

		public void Dispose ()
		{
			if (!MSBuildProjectService.ShutDown && engine != null) {
				try {
					if (builder != null)
						engine.UnloadProject (builder);
					MSBuildProjectService.ReleaseProjectBuilder (engine);
				} catch {
					// Ignore
				}
				GC.SuppressFinalize (this);
				engine = null;
				builder = null;
			}
		}
		
		~RemoteProjectBuilder ()
		{
			Dispose ();
		}
	}
}
