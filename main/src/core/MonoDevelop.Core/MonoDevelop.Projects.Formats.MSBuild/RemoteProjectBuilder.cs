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

		public IProjectBuilder LoadProject (string file, string binPath)
		{
			return engine.LoadProject (file, binPath);
		}
		
		public void UnloadProject (IProjectBuilder pb)
		{
			engine.UnloadProject (pb);
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
		
		internal RemoteProjectBuilder (string file, string binPath, RemoteBuildEngine engine)
		{
			this.engine = engine;
			builder = engine.LoadProject (file, binPath);
		}
		
		public MSBuildResult[] RunTarget (string target, ProjectConfigurationInfo[] configurations, ILogWriter logWriter,
			MSBuildVerbosity verbosity)
		{
			if (target == null)
				throw new ArgumentNullException ("target");

			return builder.RunTarget (target, configurations, logWriter, verbosity);
		}
		
		public string[] GetAssemblyReferences (ProjectConfigurationInfo[] configurations)
		{
			return builder.GetAssemblyReferences (configurations);
		}
		
		public void Refresh ()
		{
			builder.Refresh ();
		}
		
		public void RefreshWithContent (string projectContent)
		{
			builder.RefreshWithContent (projectContent);
		}

		public void Dispose ()
		{
			if (engine != null) {
				if (builder != null)
					engine.UnloadProject (builder);
				MSBuildProjectService.ReleaseProjectBuilder (engine);
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
