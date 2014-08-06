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
		
		public int ReferenceCount { get; set; }
		public DateTime ReleaseTime { get; set; }
		
		public RemoteBuildEngine (Process proc, IBuildEngine engine)
		{
			this.proc = proc;
			this.engine = engine;
		}

		public IProjectBuilder LoadProject (string projectFile)
		{
			return engine.LoadProject (projectFile);
		}
		
		public void UnloadProject (IProjectBuilder pb)
		{
			engine.UnloadProject (pb);
		}

		public void SetCulture (CultureInfo uiCulture)
		{
			engine.SetCulture (uiCulture);
		}

		public void SetGlobalProperties (IDictionary<string, string> properties)
		{
			engine.SetGlobalProperties (properties);
		}
		
		public void Dispose ()
		{
			if (proc != null) {
				try {
					proc.Kill ();
				} catch {
				}
			}
			else
				engine.Dispose ();
		}
	}
	
	public class RemoteProjectBuilder: IDisposable
	{
		RemoteBuildEngine engine;
		IProjectBuilder builder;
		Dictionary<string,string[]> referenceCache;

		internal RemoteProjectBuilder (string file, RemoteBuildEngine engine)
		{
			this.engine = engine;
			builder = engine.LoadProject (file);
			referenceCache = new Dictionary<string, string[]> ();
		}

		public MSBuildResult Run (
			ProjectConfigurationInfo[] configurations,
			ILogWriter logWriter,
			MSBuildVerbosity verbosity,
			string[] runTargets,
			string[] evaluateItems,
			string[] evaluateProperties)
		{
			return builder.Run (configurations, logWriter, verbosity, runTargets, evaluateItems, evaluateProperties);
		}

		public string[] ResolveAssemblyReferences (ProjectConfigurationInfo[] configurations)
		{
			string[] refs = null;
			var id = configurations [0].Configuration + "|" + configurations [0].Platform;

			lock (referenceCache) {
				if (!referenceCache.TryGetValue (id, out refs)) {
					var result = Run (
						            configurations, null, MSBuildVerbosity.Normal,
						            new[] { "ResolveAssemblyReferences" }, new [] { "ReferencePath" }, null
					            );

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

		public void Refresh ()
		{
			lock (referenceCache)
				referenceCache.Clear ();
			builder.Refresh ();
		}
		
		public void RefreshWithContent (string projectContent)
		{
			lock (referenceCache)
				referenceCache.Clear ();
			builder.RefreshWithContent (projectContent);
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
