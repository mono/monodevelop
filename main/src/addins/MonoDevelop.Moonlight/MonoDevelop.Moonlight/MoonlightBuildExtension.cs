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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightBuildExtension : ProjectServiceExtension
	{
		string GetObjDir (MoonlightProject proj, DotNetProjectConfiguration conf)
		{
			return Path.Combine (Path.Combine (proj.BaseDirectory, "obj"), conf.Id);
		}
		
		protected override BuildResult Compile (IProgressMonitor monitor, MonoDevelop.Projects.SolutionEntityItem item, MonoDevelop.Projects.BuildData buildData)
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
					if (File.Exists (outFile) && File.GetLastWriteTime (outFile) > File.GetLastWriteTime (pf.FilePath))
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
						BuildResult result = Respack (monitor, proj.TargetRuntime, toResGen, resFile);
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
			
			return MergeResults (base.Compile (monitor, item, buildData), results);
		}
		
		BuildResult MergeResults (BuildResult mergeInto, IEnumerable<BuildResult> mergeFrom)
		{
			foreach (BuildResult from in mergeFrom)
				MergeResults (mergeInto, from);
			return mergeInto;
		}
		
		BuildResult MergeResults (BuildResult mergeInto, BuildResult mergeFrom)
		{
			if (mergeFrom != null) {
				foreach (BuildError b in mergeFrom.Errors) {
					if (b.IsWarning)
						mergeInto.AddWarning (b.FileName, b.Line, b.Column, b.ErrorNumber, b.ErrorText);
					else
						mergeInto.AddError (b.FileName, b.Line, b.Column, b.ErrorNumber, b.ErrorText);
				}
			}
			return mergeInto;
		}
		
		BuildResult Respack (IProgressMonitor monitor, MonoDevelop.Core.Assemblies.TargetRuntime runtime, List<string> toResGen, string outfile)
		{
			BuildResult result = new BuildResult ();
			
			string respack = runtime.GetToolPath ("respack");
			if (string.IsNullOrEmpty (respack)) {
				result.AddError (null, 0, 0, null, "Could not find respack");
				return result;
			}
			
			var si = new System.Diagnostics.ProcessStartInfo ();
			foreach (KeyValuePair<string,string> env in runtime.GetToolsEnvironmentVariables ()) {
				if (env.Value == null) {
					if (si.EnvironmentVariables.ContainsKey (env.Key))
						si.EnvironmentVariables.Remove (env.Key);
				} else {
					si.EnvironmentVariables[env.Key] = env.Value;
				}
			}
			si.FileName = respack;
			si.WorkingDirectory = Path.GetDirectoryName (outfile);
			
			var sb = new System.Text.StringBuilder (outfile);
			foreach (string infile in toResGen) {
				sb.Append (" ");
				sb.Append (infile);
			}
			si.Arguments = sb.ToString ();
			
			string err;
			int exit = ExecuteCommand (monitor, si, out err);
			if (exit != 0)
				result.AddError (null, 0, 0, exit.ToString (), "respack failed: " + err);
			
			return result;
		}
		
		int ExecuteCommand (IProgressMonitor monitor, System.Diagnostics.ProcessStartInfo startInfo, out string errorOutput)
		{
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			
			errorOutput = string.Empty;
			int exitCode = -1;
			
			var swError = new StringWriter ();
			var chainedError = new LogTextWriter ();
			chainedError.ChainWriter (monitor.Log);
			chainedError.ChainWriter (swError);
			
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor);
			
			try {
				var p = Runtime.ProcessService.StartProcess (startInfo, monitor.Log, chainedError, null);
				operationMonitor.AddOperation (p); //handles cancellation
				
				p.WaitForOutput ();
				errorOutput = swError.ToString ();
				exitCode = p.ExitCode;
				p.Dispose ();
				
				if (monitor.IsCancelRequested) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Build cancelled"));
					monitor.ReportError (GettextCatalog.GetString ("Build cancelled"), null);
					if (exitCode == 0)
						exitCode = -1;
				}
			} finally {
				chainedError.Close ();
				swError.Close ();
				operationMonitor.Dispose ();
			}
			
			return exitCode;
		}
		
		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem item, string configuration)
		{
			MoonlightProject proj = item as MoonlightProject;
			DotNetProjectConfiguration conf = null;
			if (proj != null)
				conf = proj.GetActiveConfiguration (configuration) as DotNetProjectConfiguration;
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
		
		
		protected override BuildResult Build (MonoDevelop.Core.IProgressMonitor monitor, MonoDevelop.Projects.SolutionEntityItem item, string configuration)
		{
			MoonlightProject proj = item as MoonlightProject;
			DotNetProjectConfiguration conf = null;
			if (proj != null)
				conf = proj.GetActiveConfiguration (configuration) as DotNetProjectConfiguration;
			if (conf == null)
				return base.Build (monitor, item, configuration);
			
			BuildResult result = base.Build (monitor, item, configuration);
			if (result.Failed)
				return result;
			
			if (proj.XapOutputs)
				if (MergeResults (result, Zip (monitor, proj, conf)).Failed)
					return result;
			
			return result;
		}
		
		BuildResult Zip (IProgressMonitor monitor, MoonlightProject proj, DotNetProjectConfiguration conf)
		{
			string xapName = proj.XapFilename;
			if (String.IsNullOrEmpty (xapName))
				xapName = proj.Name + ".xap";
			xapName = Path.Combine (conf.OutputDirectory, xapName);
			
			string manifestName = xapName = Path.Combine (conf.OutputDirectory, xapName);
			
			var src = new List<string> ();
			var targ = new List<string> ();
			
			src.Add (conf.OutputAssembly);
			targ.Add (Path.GetFileName (conf.OutputAssembly));
			
			foreach (ProjectFile pf in proj.Files) {
				if (pf.BuildAction == BuildAction.Content) {
					src.Add (pf.FilePath);
					targ.Add (pf.RelativePath);
				}
			}
			
			if (File.Exists (xapName)) {
				DateTime lastMod = File.GetLastWriteTime (xapName);
				bool needsWrite = false;
				foreach (string file in src) {
					if (File.GetLastWriteTime (file) > lastMod) {
						needsWrite = true;
						break;
					}
				}
				if (!needsWrite)
					return null;
			}
			
			monitor.BeginStepTask ("Compressing " + xapName + "...", src.Count, 1);
			try {
				using (FileStream fs = new FileStream (xapName, FileMode.Create)) {
					var zipfile = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream (fs);
					zipfile.SetLevel (9);
					
					byte[] buffer = new byte[4096];
					
					for (int i = 0; i < src.Count; i++) {
						monitor.Step (1);
						zipfile.PutNextEntry (new ICSharpCode.SharpZipLib.Zip.ZipEntry (targ[i]));
						using (FileStream inStream = File.OpenRead (src[i])) {
							int readCount;
							do {
								readCount = inStream.Read (buffer, 0, buffer.Length);
								zipfile.Write (buffer, 0, readCount);
							} while (readCount > 0);
						}
					}
					zipfile.Finish ();
					zipfile.Close ();
				}
			} catch (IOException ex) {
				monitor.ReportError ("Error writing xap file.", ex);
				BuildResult res = new BuildResult ();
				res.AddError ("Error writing xap file:" + ex.ToString ());
				
				try {
					if (File.Exists (xapName))                                                               
						File.Delete (xapName);
				} catch {}
				
				return res;
			} finally {
				monitor.EndTask ();
			}
			
			return null;
		}
	}
}
