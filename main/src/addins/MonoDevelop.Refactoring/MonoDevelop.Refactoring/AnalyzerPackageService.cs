//
// AnalyzerPackageService.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.CodeIssues;
using System.Reflection;
using System.IO;

namespace MonoDevelop.Refactoring
{
	public static class AnalyzerPackageService
	{
		public static void RemovePackageFiles (DotNetProject project, IEnumerable<string> files)
		{
			var loadedAnalyzers = project.Items.GetAll<AnalyzerProjectItem> ().ToList();
			var fileList = files.ToList ();
			bool save = false;

			foreach (var analyzer in loadedAnalyzers.Where (analyzer => fileList.Any (f => string.Equals (f, analyzer.FilePath, StringComparison.InvariantCultureIgnoreCase)))) {
				project.Items.Remove (analyzer);
				save = true;
			}
			if (save)
				project.SaveAsync (new Core.ProgressMonitor ());
		}

		public static void AddPackageFiles (DotNetProject project, IEnumerable<string> files)
		{
			var loader = new ShadowCopyAnalyzerAssemblyLoader ();
			bool save = false;
			foreach (var file in files) {
				if (!file.EndsWith (".dll", StringComparison.OrdinalIgnoreCase))
					continue;
				if (project.Items.GetAll<AnalyzerProjectItem> ().Any (analyzer => string.Equals (file, analyzer.FilePath, StringComparison.InvariantCultureIgnoreCase)))
					continue;
				try {
					var analyzers = new AnalyzersFromAssembly ();
					analyzers.AddAssembly (loader.LoadCore (file), true);
					if (analyzers.Analyzers.Count > 0) {
						var item = new AnalyzerProjectItem ();
						project.Items.Add (item);
						item.FilePath = file;
						save = true;
					}
				} catch (Exception) { 
				} finally {
				}
			}
			if (save)
				project.SaveAsync (new Core.ProgressMonitor ());
			
		}
	}
}

