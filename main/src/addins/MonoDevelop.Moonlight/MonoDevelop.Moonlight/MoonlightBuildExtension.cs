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
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using System.Xml;
using System.Text;

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightBuildExtension : ProjectServiceExtension
	{
		FilePath GetObjDir (MoonlightProject proj, DotNetProjectConfiguration conf)
		{
			return proj.BaseDirectory.Combine ("obj", conf.Id);
		}
		
		protected override BuildResult Compile (IProgressMonitor monitor, SolutionEntityItem item, BuildData buildData)
		{
			MoonlightProject proj = item as MoonlightProject;
			if (proj == null)
				return base.Compile (monitor, item, buildData);
			
			var objDir = GetObjDir (proj, buildData.Configuration);
			if (!Directory.Exists (objDir))
				Directory.CreateDirectory (objDir);
			
			var codeDomProvider = proj.LanguageBinding.GetCodeDomProvider ();
			string appName = proj.Name;
			
			var toResGen = new List<FilePath> ();
			List<BuildResult> results = new List<BuildResult> ();
			
			foreach (ProjectFile pf in proj.Files) {
				if (pf.BuildAction == BuildAction.Resource || pf.BuildAction == BuildAction.Page || pf.BuildAction == BuildAction.ApplicationDefinition)
					toResGen.Add (pf.FilePath);
				
				if (pf.FilePath.Extension == ".xaml" && pf.Generator == "MSBuild:MarkupCompilePass1") {
					var outFile = objDir.Combine (proj.LanguageBinding.GetFileName (pf.FilePath.FileName + ".g"));
					buildData.Items.Add (new ProjectFile (outFile, BuildAction.Compile));
					if (!File.Exists (outFile) || File.GetLastWriteTime (outFile) < File.GetLastWriteTime (pf.FilePath)) {
						string rel = pf.ProjectVirtualPath;
						monitor.Log.WriteLine ("Generating codebehind accessors for {0}...", rel);
						BuildResult result = XamlG.GenerateFile (codeDomProvider, appName, pf.FilePath, rel, outFile);
						if (result.Failed)
							return result;
						results.Add (result);
					}
				}
			}
			
			string resFile = objDir.Combine (appName + ".g.resources");
			if (toResGen.Count > 0) {
				DateTime lastMod = DateTime.MinValue;
				if (File.Exists (resFile))
					lastMod = File.GetLastWriteTime (resFile);
				foreach (string f in toResGen) {
					if (File.GetLastWriteTime (f) > lastMod) {
						BuildResult result = Respack (monitor, proj, toResGen, resFile);
						if (result.Failed)
							return result;
						results.Add (result);
						break;
					}
				}
				buildData.Items.Add (new ProjectFile (resFile, BuildAction.EmbeddedResource) {
					ResourceId = appName + ".g.resources"
				});
			} else {
				if (File.Exists (resFile))
					File.Delete (resFile);
			}
			
			return base.Compile (monitor, item, buildData).Append (results);
		}
		
		BuildResult Respack (IProgressMonitor monitor, MoonlightProject proj, List<FilePath> toResGen, FilePath outfile)
		{
			monitor.Log.WriteLine ("Packing resources...");
			
			var runtime = proj.TargetRuntime;
			BuildResult result = new BuildResult ();
			
			string respack = runtime.GetToolPath (proj.TargetFramework, "respack");
			if (String.IsNullOrEmpty (respack)) {
				result.AddError (null, 0, 0, null, "Could not find respack");
				result.FailedBuildCount++;
				return result;
			}
			
			var si = new System.Diagnostics.ProcessStartInfo ();
			var env = runtime.GetToolsExecutionEnvironment (proj.TargetFramework);
			env.MergeTo (si);
			
			si.FileName = respack.EndsWith (".exe")? "mono" : respack;
			si.WorkingDirectory = outfile.ParentDirectory;
			
			var sb = new System.Text.StringBuilder ();
			if (respack.EndsWith (".exe")) {
				sb.Append (respack);
				sb.Append (" ");
			}
			sb.Append (outfile);
			
			foreach (var infile in toResGen) {
				sb.AppendFormat (" \"{0}\",\"{1}\"", infile.FullPath, infile.ToRelative (proj.BaseDirectory));
			}
			si.Arguments = sb.ToString ();
			string err;
			int exit = ExecuteCommand (monitor, si, out err);
			if (exit != 0) {
				result.AddError (null, 0, 0, exit.ToString (), "respack failed: " + err);
				result.FailedBuildCount++;
			}
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
		
		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			MoonlightProject proj = item as MoonlightProject;
			DotNetProjectConfiguration conf = null;
			if (proj != null)
				conf = proj.GetConfiguration (configuration) as DotNetProjectConfiguration;
			
			base.Clean (monitor, item, configuration);
			
			if (conf == null)
				return;
			
			var objDir = GetObjDir (proj, conf);
			if (!Directory.Exists (objDir))
				return;
			
			foreach (ProjectFile pf in proj.Files) {
				if (pf.FilePath.Extension == ".xaml" && pf.Generator == "MSBuild:MarkupCompilePass1") {
					var outFile = objDir.Combine (proj.LanguageBinding.GetFileName (pf.FilePath.FileName + ".g"));
					if (File.Exists (outFile))
						File.Delete (outFile);
				}
			}
			
			if (proj.GenerateSilverlightManifest) {
				var manifest = conf.OutputDirectory.Combine ("AppManifest.xaml");
				if (File.Exists (manifest))
					File.Delete (manifest);
			}
			
			if (proj.CreateTestPage) {
				string testPageFile = GetTestPageFileName (proj, conf);
				if (File.Exists (testPageFile))
					File.Delete (testPageFile);
			}
			
			var resFile = objDir.Combine (proj.Name + ".g.resources");
			if (File.Exists (resFile))
				File.Delete (resFile);
			
			if (proj.XapOutputs) {
				var xapName = GetXapName (proj, conf);
				if (File.Exists (xapName))
					File.Delete (xapName);
			}
		}
		
		protected override bool GetNeedsBuilding (SolutionEntityItem item, ConfigurationSelector configuration)
		{
			MoonlightProject proj = item as MoonlightProject;
			DotNetProjectConfiguration conf = null;
			if (proj != null)
				conf = proj.GetConfiguration (configuration) as DotNetProjectConfiguration;
			if (conf == null)
				return base.GetNeedsBuilding (item, configuration);
			
			if (base.GetNeedsBuilding (item, configuration))
				return true;
			
			var objDir = GetObjDir (proj, conf);
			
			DateTime xapLastMod = DateTime.MaxValue;
			if (proj.XapOutputs) {
				var xapName = GetXapName (proj, conf);
				if (!File.Exists (xapName))
					return true;
				xapLastMod = File.GetLastWriteTime (xapName);
			}

			var manifest = conf.OutputDirectory.Combine ("AppManifest.xaml");
			if (proj.GenerateSilverlightManifest) {
				if (!File.Exists (manifest))
					return true;
				if (!String.IsNullOrEmpty (proj.SilverlightManifestTemplate)) {
					string template = proj.GetAbsoluteChildPath (proj.SilverlightManifestTemplate);
					if (File.Exists (template) && File.GetLastWriteTime (template) > File.GetLastWriteTime (manifest))
						return true;
				}
			}

			if (proj.CreateTestPage) {
				string testPageFile = GetTestPageFileName (proj, conf);
				if (!File.Exists (testPageFile))
					return true;
			}
			
			string appName = proj.Name;
			var resFile = objDir.Combine (appName + ".g.resources");
			DateTime resLastMod = DateTime.MinValue;
			if (File.Exists (resFile))
				resLastMod = File.GetLastWriteTime (resFile);
			
			foreach (ProjectFile pf in proj.Files) {
				if ((pf.BuildAction == BuildAction.Page || pf.BuildAction == BuildAction.ApplicationDefinition || pf.BuildAction == BuildAction.Resource)
				    && File.GetLastWriteTime (pf.FilePath) > resLastMod)
				{
					return true;
				}
				if (pf.FilePath.Extension == ".xaml" && pf.Generator == "MSBuild:MarkupCompilePass1") {
					var outFile = objDir.Combine (proj.LanguageBinding.GetFileName (pf.FilePath.FileName + ".g"));
					if (!File.Exists (outFile) || File.GetLastWriteTime (outFile) < File.GetLastWriteTime (pf.FilePath))
						return true;
				}
				if (pf.BuildAction == BuildAction.Content && File.GetLastWriteTime (pf.FilePath) > xapLastMod)
					return true;
			}
			return false;
		}		
		
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem item, ConfigurationSelector configuration)
		{
			MoonlightProject proj = item as MoonlightProject;
			DotNetProjectConfiguration conf = null;
			if (proj != null)
				conf = proj.GetConfiguration (configuration) as DotNetProjectConfiguration;
			if (conf == null)
				return base.Build (monitor, item, configuration);
			
			BuildResult result = base.Build (monitor, item, configuration);
			if (result.ErrorCount > 0 || monitor.IsCancelRequested)
				return result;

			if (proj.GenerateSilverlightManifest)
				if (result.Append (GenerateManifest (monitor, proj, conf, configuration)).Failed || monitor.IsCancelRequested)
					return result;
			
			if (proj.XapOutputs)
				if (result.Append (Zip (monitor, proj, conf, configuration)).Failed || monitor.IsCancelRequested)
					return result;

			if (proj.XapOutputs && proj.CreateTestPage)
				result.Append (CreateTestPage (monitor, proj, conf));
			
			return result;
		}

		BuildResult GenerateManifest (IProgressMonitor monitor, MoonlightProject proj, DotNetProjectConfiguration conf, ConfigurationSelector slnConf)
		{
			const string depNS = "http://schemas.microsoft.com/client/2007/deployment";
			
			monitor.Log.WriteLine ("Generating manifest...");
			
			var res = new BuildResult ();
			var manifest = conf.OutputDirectory.Combine ("AppManifest.xaml");

			string template = String.IsNullOrEmpty (proj.SilverlightManifestTemplate)?
				null : proj.GetAbsoluteChildPath (proj.SilverlightManifestTemplate);

			XmlDocument doc = new XmlDocument ();
			if (template != null) {
				if (!File.Exists (template)) {
					monitor.ReportError ("Could not find manifest template '" +  template + "'.", null);
					res.AddError ("Could not find manifest template '" +  template + "'.");
					res.FailedBuildCount++;
					return res;
				}
				try {
					doc.Load (template);
				} catch (XmlException ex) {
					monitor.ReportError ("Could not load manifest template '" +  template + "'.", null);
					res.AddError (template, ex.LineNumber, ex.LinePosition, null, "Error loading manifest template '" + ex.Source);
					res.FailedBuildCount++;
					return res;
				} catch (Exception ex) {
					monitor.ReportError ("Could not load manifest template '" +  template + "'.", ex);
					res.AddError ("Could not load manifest template '" +  template + "': " + ex.ToString ());
					res.FailedBuildCount++;
					return res;
				}

			} else {
				doc.LoadXml (@"<Deployment xmlns=""http://schemas.microsoft.com/client/2007/deployment"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""></Deployment>");
			}
			
			try {
				XmlNode deploymentNode = doc.DocumentElement;
				if (deploymentNode == null || deploymentNode.Name != "Deployment" || deploymentNode.NamespaceURI != depNS) {
					monitor.ReportError ("Missing or invalid root <Deployment> element in manifest template '" +  template + "'.", null);
					res.AddError ("Missing root <Deployment> element in manifest template '" +  template + "'.");
					res.FailedBuildCount++;
					return res;
				}
				if (deploymentNode.Attributes["EntryPointAssembly"] == null)
					deploymentNode.Attributes.Append (doc.CreateAttribute ("EntryPointAssembly")).Value = conf.CompiledOutputName.FileNameWithoutExtension;
				if (!String.IsNullOrEmpty (proj.SilverlightAppEntry) && deploymentNode.Attributes["EntryPointType"] == null)
					deploymentNode.Attributes.Append (doc.CreateAttribute ("EntryPointType")).Value = proj.SilverlightAppEntry;

				if (deploymentNode.Attributes["RuntimeVersion"] == null) {
					string runtimeVersion = null;
					string fxVersion = proj.TargetFramework.Id.Version;
					
					if (proj.TargetRuntime is MonoDevelop.Core.Assemblies.MonoTargetRuntime) {
						var package = proj.TargetRuntime.RuntimeAssemblyContext.GetPackage ("moonlight-web-" + fxVersion);
						if (package != null && package.IsFrameworkPackage) {
							runtimeVersion = package.Version;
						} else {
							LoggingService.LogWarning ("Moonlight core framework package not found, cannot determine " +
								"runtime version string. Falling back to default value.");
						}
					}
					
					if (runtimeVersion == null) {
						//FIXME how will we determine this for other runtimes?
						runtimeVersion = "2.0.31005.0";
					}
					
					deploymentNode.Attributes.Append (doc.CreateAttribute ("RuntimeVersion")).Value = runtimeVersion;
				}

				XmlNamespaceManager mgr = new XmlNamespaceManager (doc.NameTable);
				mgr.AddNamespace ("dep", depNS);
				XmlNode partsNode = deploymentNode.SelectSingleNode ("dep:Deployment.Parts", mgr);
				if (partsNode == null)
					partsNode = deploymentNode.AppendChild (doc.CreateElement ("Deployment.Parts", depNS));

				AddAssemblyPart (doc, partsNode, conf.CompiledOutputName);
				foreach (ProjectReference pr in proj.References) {
					if (pr.LocalCopy) {
						var pk = pr.Package;
						if (pk == null || !pk.IsFrameworkPackage || pk.Name.EndsWith ("-redist")) {
							foreach (string s in pr.GetReferencedFileNames (slnConf))
								AddAssemblyPart (doc, partsNode, s);
						}
					}
				}

			} catch (XmlException ex) {
				monitor.ReportError ("Error processing manifest template.", ex);
				res.AddError (template, ex.LineNumber, ex.LinePosition, null, "Error processing manifest template: '" + ex.Source);
				res.FailedBuildCount++;
				return res;
			}
			
			doc.Save (manifest);

			return res;
		}

		static void AddAssemblyPart (XmlDocument doc, XmlNode partsNode, FilePath assem)
		{
			XmlNode child = doc.CreateElement ("AssemblyPart", "http://schemas.microsoft.com/client/2007/deployment");
			child.Attributes.Append (doc.CreateAttribute ("Name", "http://schemas.microsoft.com/winfx/2006/xaml")).Value = assem.FileNameWithoutExtension;
			child.Attributes.Append (doc.CreateAttribute ("Source")).Value = Path.GetFileName (assem);
			partsNode.AppendChild (child);
		}

		BuildResult CreateTestPage (IProgressMonitor monitor, MoonlightProject proj, DotNetProjectConfiguration conf)
		{
			monitor.Log.WriteLine ("Creating test page...");
			
			string testPageFile = GetTestPageFileName (proj, conf);
			try {
				using (var sr = new StreamReader (System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceStream ("PreviewTemplate.html"))) {
					var sb = new StringBuilder (sr.ReadToEnd ());
					string xapName = String.IsNullOrEmpty (proj.XapFilename)? proj.Name + ".xap" : proj.XapFilename;
					sb.Replace ("@TITLE@", proj.Name);
					sb.Replace ("@XAP_FILE@", xapName);
					File.WriteAllText (testPageFile, sb.ToString ());
				}
			} catch (Exception ex) {
				monitor.ReportError ("Error generating test page '" + testPageFile + "'.", ex);
				BuildResult res = new BuildResult ();
				res.AddError ("Error generating test page '" + testPageFile + "':" + ex.ToString ());
				res.FailedBuildCount++;
				return res;
			}
			return null;
		}
		
		FilePath GetTestPageFileName (MoonlightProject proj, DotNetProjectConfiguration conf)
		{
			string testPage = proj.TestPageFileName;
			if (String.IsNullOrEmpty (testPage))
				testPage = "TestPage.html";
			return conf.OutputDirectory.Combine (testPage);
		}
		
		FilePath GetXapName (MoonlightProject proj, DotNetProjectConfiguration conf)
		{
			string xapName = proj.XapFilename;
			if (String.IsNullOrEmpty (xapName))
				xapName = proj.Name + ".xap";
			return conf.OutputDirectory.Combine (xapName);
		}
		
		BuildResult Zip (IProgressMonitor monitor, MoonlightProject proj, DotNetProjectConfiguration conf, ConfigurationSelector slnConf)
		{
			var xapName = GetXapName (proj, conf);
			
			var src = new List<string> ();
			var targ = new List<string> ();
			
			src.Add (conf.CompiledOutputName);
			targ.Add (conf.CompiledOutputName.FileName);
			
			// FIXME: this is a hack for the Mono Soft Debugger. In future the mdb files should be *beside* the xap,
			// when sdb supports that model. Note that there's no point doing this for pdb files, because the debuggers 
			// that read pdb files don't expect them to be in the xap.
			var doSdbCopy = conf.DebugMode && proj.TargetRuntime is MonoDevelop.Core.Assemblies.MonoTargetRuntime;
			
			if (doSdbCopy) {
				FilePath mdb = conf.CompiledOutputName + ".mdb";
				if (File.Exists (mdb)) {
					src.Add (mdb);
					targ.Add (mdb.FileName);
				}
			}

			if (proj.GenerateSilverlightManifest) {
				src.Add (conf.OutputDirectory.Combine ("AppManifest.xaml"));
				targ.Add ("AppManifest.xaml");
			}

			foreach (ProjectFile pf in proj.Files) {
				if (pf.BuildAction == BuildAction.Content) {
					src.Add (pf.FilePath);
					targ.Add (pf.ProjectVirtualPath);
				}
			}
			
			BuildResult res = new BuildResult ();

			// The "copy to output" files don't seem to be included in xaps, so we can't use project.GetSupportFiles.
			// Instead we need to iterate over the refs and handle them manually.
			foreach (ProjectReference pr in proj.References) {
				if (pr.LocalCopy) {
					var pk = pr.Package;
					if (pk == null || !pk.IsFrameworkPackage || pk.Name.EndsWith ("-redist")) {
						string err = pr.ValidationErrorMessage;
						if (!String.IsNullOrEmpty (err)) {
							string msg = String.Format ("Could not add reference '{0}' to '{1}': {2}",
							                            pr.Reference, xapName.FileName, err);
							res.AddError (msg);
							monitor.Log.WriteLine (msg);
							continue;
						}
						foreach (string s in pr.GetReferencedFileNames (slnConf)) {
							src.Add (s);
							targ.Add (Path.GetFileName (s));
							
							if (doSdbCopy && s.EndsWith (".dll")) {
								FilePath mdb = s + ".mdb";
								if (File.Exists (mdb)) {
									src.Add (mdb);
									targ.Add (mdb.FileName);
								}
							}
						}
					}
				}
			}
			
			if (res.ErrorCount > 0) {
				res.FailedBuildCount++;
				return res;
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
			
			monitor.Log.WriteLine ("Compressing XAP file...");
			
			try {
				using (FileStream fs = new FileStream (xapName, FileMode.Create)) {
					var zipfile = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream (fs);
					zipfile.SetLevel (9);
					
					byte[] buffer = new byte[4096];
					
					for (int i = 0; i < src.Count && !monitor.IsCancelRequested; i++) {
						zipfile.PutNextEntry (new ICSharpCode.SharpZipLib.Zip.ZipEntry (targ[i]));
						using (FileStream inStream = File.OpenRead (src[i])) {
							int readCount;
							do {
								readCount = inStream.Read (buffer, 0, buffer.Length);
								zipfile.Write (buffer, 0, readCount);
							} while (readCount > 0);
						}
					}
					if (!monitor.IsCancelRequested) {
						zipfile.Finish ();
						zipfile.Close ();
					}
				}
			} catch (IOException ex) {
				monitor.ReportError ("Error writing xap file.", ex);
				res.AddError ("Error writing xap file:" + ex.ToString ());
				res.FailedBuildCount++;
				
				try {
					if (File.Exists (xapName))                                                               
						File.Delete (xapName);
				} catch {}
				
				return res;
			}
			
			if (monitor.IsCancelRequested) {
				try {
					if (File.Exists (xapName))                                                               
						File.Delete (xapName);
				} catch {}
			}
			
			return res;
		}
	}
}
