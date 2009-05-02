// 
// MoonlightBuildExtension.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Projects;

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightBuildExtension : ProjectServiceExtension
	{
		string GetObjDir (MoonlightProject proj, DotNetProjectConfiguration conf)
		{
			return Path.Combine (Path.Combine (proj.BaseDirectory, "obj"), conf.Id);
		}
		
		protected override BuildResult Compile (MonoDevelop.Core.IProgressMonitor monitor, MonoDevelop.Projects.SolutionEntityItem item, MonoDevelop.Projects.BuildData buildData)
		{
			MoonlightProject proj = item as MoonlightProject;
			if (proj == null)
				return base.Compile (monitor, item, buildData);
			
			string objDir = GetObjDir (proj, buildData.Configuration);
			if (!Directory.Exists (objDir))
				Directory.CreateDirectory (objDir);
			
			var codeDomProvider = proj.LanguageBinding.GetCodeDomProvider ();
			string appName = proj.Name;
			
			List<string> toResGen = new List<string> ();
			List<BuildResult> results = new List<BuildResult> ();
			
			foreach (ProjectFile pf in proj.Files) {
				if (pf.FilePath.EndsWith (".xaml") && pf.Generator == "MSBuild:MarkupCompilePass1") {
					string outFile = Path.Combine (objDir, proj.LanguageBinding.GetFileName (Path.GetFileName (pf.FilePath) + ".g"));
					buildData.Items.Add (new ProjectFile (outFile, BuildAction.Compile));
					if (File.Exists (outFile) || File.GetLastWriteTime (outFile) > File.GetLastWriteTime (pf.FilePath))
						continue;
					BuildResult result = XamlG.GenerateFile (codeDomProvider, appName, pf.FilePath, pf.RelativePath, outFile);
					if (result.Failed)
						return result;
					results.Add (result);
				} else if (pf.BuildAction == BuildAction.Resource) {
					toResGen.Add (pf.FilePath);
				}
			}
			
			string resFile = Path.Combine (objDir, appName + ".g.resources");
			if (toResGen.Count > 0) {
				DateTime lastMod = DateTime.MinValue;
				if (File.Exists (resFile))
					lastMod = File.GetLastWriteTime (resFile);
				foreach (string f in toResGen) {
					if (File.GetLastWriteTime (f) > lastMod) {
						BuildResult result = ResGen (toResGen, resFile);
						if (result.Failed)
							return result;
						results.Add (result);
						break;
					}
				}
				buildData.Items.Add (new ProjectFile (resFile, BuildAction.EmbeddedResource));
			} else {
				if (File.Exists (resFile))
					File.Delete (resFile);
			}
			
			BuildResult baseResult = base.Compile (monitor, item, buildData);
			
			foreach (BuildResult result in results) {
				foreach (BuildError b in result.Errors) {
					if (b.IsWarning)
						baseResult.AddWarning (b.FileName, b.Line, b.Column, b.ErrorNumber, b.ErrorText);
					else
						baseResult.AddError (b.FileName, b.Line, b.Column, b.ErrorNumber, b.ErrorText);
				}
			}
			
			return baseResult;
		}
		
		BuildResult ResGen (List<string> toResGen, string outfile)
		{
			return new BuildResult ();
		}
		
		protected override void Clean (MonoDevelop.Core.IProgressMonitor monitor, MonoDevelop.Projects.SolutionEntityItem item, string configuration)
		{
			MoonlightProject proj = item as MoonlightProject;
			if (proj == null) {
				base.Clean (monitor, item, configuration);
				return;
			}
			
			DotNetProjectConfiguration conf = proj.GetActiveConfiguration (configuration) as DotNetProjectConfiguration;
			if (conf == null) {
				base.Clean (monitor, item, configuration);
				return;
			}
			
			string objDir = GetObjDir (proj, conf);
			if (!Directory.Exists (objDir)) {
				base.Clean (monitor, item, configuration);
				return;
			}
			
			foreach (ProjectFile pf in proj.Files) {
				if (pf.FilePath.EndsWith (".xaml") && pf.Generator == "MSBuild:MarkupCompilePass1") {
					string outFile = Path.Combine (objDir, proj.LanguageBinding.GetFileName (Path.GetFileName (pf.FilePath) + ".g"));
					if (File.Exists (outFile))
						File.Delete (outFile);
				}
			}
			
			string resFile = Path.Combine (objDir, proj.Name + ".g.resources");
			if (File.Exists (resFile))
				File.Delete (resFile);
			
			base.Clean (monitor, item, configuration);
		}
	}
}
