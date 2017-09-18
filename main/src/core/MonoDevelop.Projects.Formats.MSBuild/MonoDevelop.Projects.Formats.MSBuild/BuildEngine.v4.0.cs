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
using System.Runtime.Remoting;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using System.Linq;
using System.Globalization;

namespace MonoDevelop.Projects.MSBuild
{
	partial class BuildEngine
	{
		static CultureInfo uiCulture;
		readonly Dictionary<string, string> unsavedProjects = new Dictionary<string, string> ();
		readonly ProjectCollection engine = new ProjectCollection { DefaultToolsVersion = MSBuildConsts.Version };

		public void SetCulture (CultureInfo uiCulture)
		{
			BuildEngine.uiCulture = uiCulture;
		}

		public void SetGlobalProperties (IDictionary<string, string> properties)
		{
			foreach (var p in properties)
				engine.SetGlobalProperty (p.Key, p.Value);
		}

		public ProjectBuilder LoadProject (string file)
		{
			return new ProjectBuilder (this, engine, file);
		}
		
		public void UnloadProject (ProjectBuilder pb)
		{
			pb.Dispose ();
		}

		internal void SetUnsavedProjectContent (string file, string content)
		{
			lock (unsavedProjects)
				unsavedProjects[file] = content;
		}

		internal string GetUnsavedProjectContent (string file)
		{
			lock (unsavedProjects) {
				string content;
				unsavedProjects.TryGetValue (file, out content);
				return content;
			}
		}

		internal void UnloadProject (string file)
		{
			RunSTA (delegate
			{
				// Unloading projects modifies the collection, so copy it
				var loadedProjects = engine.GetLoadedProjects(file).ToArray();

				if (loadedProjects.Length == 0)
					return;

				var rootElement = loadedProjects[0].Xml;

				foreach (var p in loadedProjects)
					engine.UnloadProject(p);

				// Try to unload the projects' XML from the cache
				// This could fail if something else is referencing the xml somehow.
				// But not a big deal, it's just a cache.

				engine.TryUnloadProject(rootElement);
			});
		}
	}
}