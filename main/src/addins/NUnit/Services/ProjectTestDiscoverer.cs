//
// ProjectTestDiscoverer.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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
using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using MonoDevelop.Ide;
using System.Globalization;

namespace MonoDevelop.NUnit
{
	/// <summary>
	/// Handles most of the work required to extract files from projects,
	/// giving extending class a simple way to work with project's files. 
	/// </summary>
	public abstract class ProjectTestDiscoverer: ITestDiscoverer
	{
		/// <summary>
		/// Enumerates project's files filtering files
		/// based on data from the attribute
		/// </summary>
		class ProjectFilesFilter : IEnumerable<string>
		{
			readonly Project project;
			readonly bool useProjectOutput;
			readonly bool useProjectFiles;
			readonly string[] extensions;

			public ProjectFilesFilter (Project project, TestDiscoveryAttribute attr)
			{
				this.project = project;

				if (attr != null) {
					extensions = attr.Extensions;
					useProjectOutput = attr.Target.HasFlag (Target.ProjectOutput);
					useProjectFiles = attr.Target.HasFlag (Target.ProjectFiles);
				}
			}

			public IEnumerator<string> GetEnumerator ()
			{
				if (useProjectOutput) {
					foreach (var file in project.GetOutputFiles (IdeApp.Workspace.ActiveConfiguration)) {
						string path = file;
						if (MatchesExtensions (path))
							yield return path;
					}
				}
				if (useProjectFiles) {
					foreach (var file in project.Files) {
						string path = file.FilePath;
						if (MatchesExtensions (path))
							yield return path;
					}
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
			{
				return this.GetEnumerator ();
			}

			bool MatchesExtensions (string path)
			{
				// if no extensions were specified then true
				if (extensions == null || extensions.Length == 0)
					return true;

				foreach (var extension in extensions) {
					// if matches at least one of the extensions then true
					if (path.EndsWith ("." + extension, true, CultureInfo.InvariantCulture))
						return true;
				}

				return false;
			}
		}

		[Flags]
		public enum Target
		{
			ProjectOutput = 0x01,
			ProjectFiles = 0x02
		}

		public void Discover (IWorkspaceObject entry, ITestDiscoveryContext context, ITestDiscoverySink sink)
		{
			var project = entry as Project;
			if (project != null) {
				// extract discovery targets from abstract method's implementation
				MethodInfo method = this.GetType().GetMethod ("Discover",
					new Type[] { typeof(IEnumerable<string>), typeof(ITestDiscoveryContext), typeof(ITestDiscoverySink) });

				var attr = (TestDiscoveryAttribute) method.GetCustomAttributes (
					typeof(TestDiscoveryAttribute), false).SingleOrDefault ();

				Discover (new ProjectFilesFilter (project, attr), context, sink);
			}
		}

		public abstract void Discover (IEnumerable<string> files, ITestDiscoveryContext context,
			ITestDiscoverySink sink);
	}

	/// <summary>
	/// Attribute specifies filters used by discoverer
	/// </summary>
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
	public class TestDiscoveryAttribute : Attribute
	{
		public ProjectTestDiscoverer.Target Target { get; set; }
		public string[] Extensions { get; set; }

		public TestDiscoveryAttribute (ProjectTestDiscoverer.Target target, params string[] extensions)
		{
			Target = target;
			Extensions = extensions;
		}

		public TestDiscoveryAttribute (ProjectTestDiscoverer.Target target)
			: this (target, null)
		{
		}

		public TestDiscoveryAttribute (params string[] extensions)
			: this (0, extensions)
		{
		}

		public TestDiscoveryAttribute ()
		{
		}
	}
}

