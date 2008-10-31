// MD1DotNetProjectHandler.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Extensions;
using Microsoft.Build.BuildEngine;
	
namespace MonoDevelop.Projects.Formats.MD1
{
	internal class MD1DotNetProjectHandler: MD1SolutionEntityItemHandler
	{
		public MD1DotNetProjectHandler (DotNetProject entry): base (entry)
		{
		}
		
		DotNetProject Project {
			get { return (DotNetProject) Item; }
		}
		
		protected override BuildResult OnClean (IProgressMonitor monitor, string configuration)
		{
			DotNetProject project = Project;
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) project.GetConfiguration (configuration);
			
			// Delete the generated debug info
			string file = project.GetOutputFileName (configuration);
			if (file != null) {
				if (File.Exists (file + ".mdb"))
					FileService.DeleteFile (file + ".mdb");
			}

			List<string> cultures = new List<string> ();
			monitor.Log.WriteLine (GettextCatalog.GetString ("Removing all .resources files"));
			foreach (ProjectFile pfile in project.Files) {
				if (pfile.BuildAction == BuildAction.EmbeddedResource &&
					Path.GetExtension (pfile.Name) == ".resx") {
					string resFilename = Path.ChangeExtension (pfile.Name, ".resources");
					if (File.Exists (resFilename))
						FileService.DeleteFile (resFilename);
				}
				string culture = GetCulture (pfile.Name);
				if (culture != null)
					cultures.Add (culture);
			}

			if (cultures.Count > 0 && conf != null && project.DefaultNamespace != null) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Removing all satellite assemblies"));
				string outputDir = ((DotNetProjectConfiguration)conf).OutputDirectory;
				string satelliteAsmName = project.DefaultNamespace + ".resources.dll";

				foreach (string culture in cultures) {
					string path = String.Format ("{0}{3}{1}{3}{2}", outputDir, culture, satelliteAsmName, Path.DirectorySeparatorChar);
					if (File.Exists (path))
						FileService.DeleteFile (path);
				}
			}
			return null;
		}

		
		protected override BuildResult OnBuild (IProgressMonitor monitor, string configuration)
		{
			DotNetProject project = Project;
			
			bool hasBuildableFiles = false;
			foreach (ProjectFile pf in project.Files) {
				if (pf.BuildAction == BuildAction.Compile || pf.BuildAction == BuildAction.EmbeddedResource) {
					hasBuildableFiles = true;
					break;
				}
			}
			if (!hasBuildableFiles)
				return new BuildResult ();
			
			if (project.LanguageBinding == null) {
				BuildResult langres = new BuildResult ();
				string msg = GettextCatalog.GetString ("Unknown language '{0}'. You may need to install an additional add-in to support this language.", project.LanguageName);
				langres.AddError (msg);
				monitor.ReportError (msg, null);
				return langres;
			}

			BuildResult refres = null;
			
			foreach (ProjectReference pr in project.References) {
				if (pr.ReferenceType == ReferenceType.Project) {
					// Ignore non-dotnet projects
					Project p = project.ParentSolution != null ? project.ParentSolution.FindProjectByName (pr.Reference) : null;
					if (p != null && !(p is DotNetProject))
						continue;

					if (p == null || pr.GetReferencedFileNames (configuration).Length == 0) {
						if (refres == null)
							refres = new BuildResult ();
						string msg = GettextCatalog.GetString ("Referenced project '{0}' not found in the solution.", pr.Reference);
						monitor.ReportWarning (msg);
						refres.AddWarning (msg);
					}
				} else if (pr.StoredReference != pr.Reference && !pr.Reference.StartsWith (pr.StoredReference + ",")) {
					if (refres == null)
						refres = new BuildResult ();
					string msg = GettextCatalog.GetString ("Reference '{0}' not found on system. Using '{1} instead.", pr.StoredReference, pr.Reference);
					monitor.ReportWarning (msg);
					refres.AddWarning (msg);
				}
			}
			
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) project.GetConfiguration (configuration);
			conf.SourceDirectory = project.BaseDirectory;

			// Create a copy of the data needed to compile the project.
			// This data can be modified by extensions.
			// Also filter out items whose condition evaluates to false
			
			BuildData buildData = new BuildData ();
			ProjectParserContext ctx = new ProjectParserContext (project, conf);
		
			buildData.Files = new ProjectFileCollection ();
			foreach (ProjectFile file in project.Files) {
				if (ConditionParser.ParseAndEvaluate (file.Condition, ctx))
					buildData.Files.Add (file);
			}
			buildData.References = new ProjectReferenceCollection ();
			foreach (ProjectReference pref in project.References) {
				if (ConditionParser.ParseAndEvaluate (pref.Condition, ctx))
					buildData.References.Add (pref);
			}
			buildData.Configuration = (DotNetProjectConfiguration) conf.Clone ();
			buildData.Configuration.SetParentItem (project);

			return ProjectExtensionUtil.Compile (monitor, project, buildData, delegate {
				ProjectFileCollection files = buildData.Files;
				BuildResult res = BuildResources (buildData.Configuration, ref files, monitor);
				if (res != null)
					return res;
				
				List<string> supportAssemblies = new List<string> ();
				CopySupportAssemblies (supportAssemblies, configuration);
	
				try {
					res = project.LanguageBinding.Compile (files, buildData.References, buildData.Configuration, monitor);
					if (refres != null) {
						refres.Append (res);
						return refres;
					}
					else
						return res;
				}
				finally {
					// Delete support assemblies
					foreach (string s in supportAssemblies) {
						try {
							FileService.DeleteFile (s);
						} catch {
							// Ignore
						}
					}
				}		
			});
		}		

		// Builds the EmbedAsResource files. If any localized resources are found then builds the satellite assemblies
		// and sets @projectFiles to a cloned collection minus such resource files.
		private BuildResult BuildResources (DotNetProjectConfiguration configuration, ref ProjectFileCollection projectFiles, IProgressMonitor monitor)
		{
			DotNetProject project = Project;
			
			string resgen = "resgen";
			if (System.Environment.Version.Major >= 2) {
				switch (configuration.ClrVersion) {
					case ClrVersion.Net_2_0: resgen = "resgen2"; break;
					case ClrVersion.Net_1_1: resgen = "resgen1"; break;
				}
			}
			bool cloned = false;
			Dictionary<string, string> resourcesByCulture = new Dictionary<string, string> ();
			foreach (ProjectFile finfo in projectFiles) {
				if (finfo.Subtype == Subtype.Directory || finfo.BuildAction != BuildAction.EmbeddedResource)
					continue;

				string fname = finfo.Name;
				string resourceId;
				CompilerError ce = GetResourceId (finfo, ref fname, resgen, out resourceId, monitor);
				if (ce != null) {
					CompilerResults cr = new CompilerResults (new TempFileCollection ());
					cr.Errors.Add (ce);

					return new BuildResult (cr, String.Empty);
				}
				string culture = GetCulture (finfo.Name);
				if (culture == null)
					continue;

				string cmd = String.Empty;
				if (resourcesByCulture.ContainsKey (culture))
					cmd = resourcesByCulture [culture];

				cmd = String.Format ("{0} \"/embed:{1},{2}\"", cmd, fname, resourceId);
				resourcesByCulture [culture] = cmd;
				if (!cloned) {
					// Clone only if required
					ProjectFileCollection files = new ProjectFileCollection ();
					files.AddRange (projectFiles);
					projectFiles = files;
					cloned = true;
				}
				projectFiles.Remove (finfo);
			}

			string al = configuration.ClrVersion == ClrVersion.Net_2_0 ? "al2" : "al";
			CompilerError err = GenerateSatelliteAssemblies (resourcesByCulture, configuration.OutputDirectory, al, project.DefaultNamespace, monitor);
			if (err != null) {
				CompilerResults cr = new CompilerResults (new TempFileCollection ());
				cr.Errors.Add (err);

				return new BuildResult (cr, String.Empty);
			}

			return null;
		}
		
		void CopySupportAssemblies (List<string> files, string configuration)
		{
			foreach (ProjectReference projectReference in Project.References) {
				if (projectReference.ReferenceType == ReferenceType.Project) {
					// It is a project reference. If this project depends
					// on other (non-gac) assemblies there may be a compilation problem because
					// the compiler won't be able to indirectly find them.
					// The solution is to copy them in the project directory, and delete
					// them after compilation.
					DotNetProject p = Project.ParentSolution.FindProjectByName (projectReference.Reference) as DotNetProject;
					if (p == null)
						continue;
					
					string tdir = Path.GetDirectoryName (p.GetOutputFileName (configuration));
					CopySupportAssemblies (p, tdir, files, configuration);
				}
			}
		}
		
		void CopySupportAssemblies (DotNetProject prj, string targetDir, List<string> files, string configuration)
		{
			foreach (ProjectReference pref in prj.References) {
				if (pref.ReferenceType == ReferenceType.Gac)
					continue;
				foreach (string referenceFileName in pref.GetReferencedFileNames (configuration)) {
					string asmName = Path.GetFileName (referenceFileName);
					asmName = Path.Combine (targetDir, asmName);
					if (!File.Exists (asmName)) {
						File.Copy (referenceFileName, asmName);
						files.Add (asmName);
					}
				}
				if (pref.ReferenceType == ReferenceType.Project) {
					DotNetProject sp = Project.ParentSolution.FindProjectByName (pref.Reference) as DotNetProject;
					if (sp != null)
						CopySupportAssemblies (sp, targetDir, files, configuration);
				}
			}
		}
		
		CompilerError GetResourceId (ProjectFile finfo, ref string fname, string resgen, out string resourceId, IProgressMonitor monitor)
		{
			resourceId = finfo.ResourceId;
			if (resourceId == null) {
				LoggingService.LogDebug (GettextCatalog.GetString ("Error: Unable to build ResourceId for {0}.", fname));
				monitor.Log.WriteLine (GettextCatalog.GetString ("Error: Unable to build ResourceId for {0}.", fname));

				return new CompilerError (fname, 0, 0, String.Empty,
						GettextCatalog.GetString ("Unable to build ResourceId for {0}.", fname));
			}

			if (String.Compare (Path.GetExtension (fname), ".resx", true) != 0)
				return null;

			//Check whether resgen required
			FileInfo finfo_resx = new FileInfo (fname);
			FileInfo finfo_resources = new FileInfo (Path.ChangeExtension (fname, ".resources"));
			if (finfo_resx.LastWriteTime < finfo_resources.LastWriteTime) {
				fname = Path.ChangeExtension (fname, ".resources");
				return null;
			}

			using (StringWriter sw = new StringWriter ()) {
				LoggingService.LogDebug ("Compiling resources\n{0}$ {1} /compile {2}", Path.GetDirectoryName (fname), resgen, fname);
				monitor.Log.WriteLine (GettextCatalog.GetString (
					"Compiling resource {0} with {1}", fname, resgen));
				ProcessWrapper pw = null;
				try {
					ProcessStartInfo info = Runtime.ProcessService.CreateProcessStartInfo (
									resgen, String.Format ("/compile \"{0}\"", fname),
									Path.GetDirectoryName (fname), false);

					if (PlatformID.Unix == Environment.OSVersion.Platform)
						info.EnvironmentVariables ["MONO_IOMAP"] = "drive";

					pw = Runtime.ProcessService.StartProcess (info, sw, sw, null);
				} catch (System.ComponentModel.Win32Exception ex) {
					LoggingService.LogDebug (GettextCatalog.GetString (
						"Error while trying to invoke '{0}' to compile resource '{1}' :\n {2}", resgen, fname, ex.ToString ()));
					monitor.Log.WriteLine (GettextCatalog.GetString (
						"Error while trying to invoke '{0}' to compile resource '{1}' :\n {2}", resgen, fname, ex.Message));

					return new CompilerError (fname, 0, 0, String.Empty, ex.Message);
				}

				//FIXME: Handle exceptions
				pw.WaitForOutput ();

				if (pw.ExitCode == 0) {
					fname = Path.ChangeExtension (fname, ".resources");
				} else {
					string output = sw.ToString ();
					LoggingService.LogDebug (GettextCatalog.GetString (
						"Unable to compile ({0}) {1} to .resources. \nReason: \n{2}\n",
						resgen, fname, output));
					monitor.Log.WriteLine (GettextCatalog.GetString (
						"Unable to compile ({0}) {1} to .resources. \nReason: \n{2}\n",
						resgen, fname, output));

					//Try to get the line/pos
					int line = 0;
					int pos = 0;
					Match match = RegexErrorLinePos.Match (output);
					if (match.Success && match.Groups.Count == 3) {
						try {
							line = int.Parse (match.Groups [1].Value);
						} catch (FormatException){
						}

						try {
							pos = int.Parse (match.Groups [2].Value);
						} catch (FormatException){
						}
					}

					return new CompilerError (fname, line, pos, String.Empty, output);
				}
			}

			return null;
		}

		CompilerError GenerateSatelliteAssemblies (Dictionary<string, string> resourcesByCulture, string outputDir, string al, string defaultns, IProgressMonitor monitor)
		{
			foreach (KeyValuePair<string, string> pair in resourcesByCulture) {
				string culture = pair.Key;
				string satDir = Path.Combine (outputDir, culture);
				string outputFile = defaultns + ".resources.dll";

				//FIXME: don't regen if not required,
				//for that we'll need name of the .resources that these depend on..

				//create target dir
				Directory.CreateDirectory (satDir);

				using (StringWriter sw = new StringWriter ()) {
					//generate assembly
					string args = String.Format ("/t:lib {0} \"/out:{1}\" /culture:{2}", pair.Value, outputFile, culture);

					LoggingService.LogDebug ("Generating satellite assembly for '{0}' culture.\n{1}$ {2} {3}", culture, satDir, al, args);
					monitor.Log.WriteLine (GettextCatalog.GetString (
						"Generating satellite assembly for '{0}' culture with {1}", culture, al));
					ProcessWrapper pw = null;
					try {
						ProcessStartInfo info = Runtime.ProcessService.CreateProcessStartInfo (
										al, args,
										satDir, false);

						pw = Runtime.ProcessService.StartProcess (info, sw, sw, null);
					} catch (System.ComponentModel.Win32Exception ex) {
						LoggingService.LogDebug (GettextCatalog.GetString (
							"Error while trying to invoke '{0}' to generate satellite assembly for '{1}' culture:\n {2}", al, culture, ex.ToString ()));
						monitor.Log.WriteLine (GettextCatalog.GetString (
							"Error while trying to invoke '{0}' to generate satellite assembly for '{1}' culture:\n {2}", al, culture, ex.Message));

						return new CompilerError ("", 0, 0, String.Empty, ex.Message);
					}

					//FIXME: Handle exceptions
					pw.WaitForOutput ();

					if (pw.ExitCode != 0) {
						string output = sw.ToString ();
						LoggingService.LogDebug (GettextCatalog.GetString (
							"Unable to generate satellite assemblies for '{0}' culture with {1}.\nReason: \n{2}\n",
							culture, al, output));
						monitor.Log.WriteLine (GettextCatalog.GetString (
							"Unable to generate satellite assemblies for '{0}' culture with {1}.\nReason: \n{2}\n",
							culture, al, output));

						return new CompilerError (String.Empty, 0, 0, String.Empty, output);
					}
				}
			}

			return null;
		}
		
		//Given a filename like foo.it.resx, get 'it', if its
		//a valid culture
		//Note: hand-written as this can get called lotsa times
		//Note: code duplicated in prj2make/Utils.cs as TrySplitResourceName
		string GetCulture (string fname)
		{
			int last_dot = -1;
			int culture_dot = -1;
			int i = fname.Length - 1;
			while (i >= 0) {
				if (fname [i] == '.') {
					last_dot = i;
					break;
				}
				i --;
			}
			if (i < 0)
				return null;

			i--;
			while (i >= 0) {
				if (fname [i] == '.') {
					culture_dot = i;
					break;
				}
				i --;
			}
			if (culture_dot < 0)
				return null;

			string culture = fname.Substring (culture_dot + 1, last_dot - culture_dot - 1);
			if (!CultureNamesTable.ContainsKey (culture))
				return null;

			return culture;
		}

		// Used for parsing "Line 123, position 5" errors from tools
		// like resgen, xamlg
		static Regex regexErrorLinePos;
		static Regex RegexErrorLinePos {
			get {
				if (regexErrorLinePos == null)
					regexErrorLinePos = new Regex (@"Line (\d*), position (\d*)");
				return regexErrorLinePos;
			}
		}

		static Dictionary<string, string> cultureNamesTable;
		static Dictionary<string, string> CultureNamesTable {
			get {
				if (cultureNamesTable == null) {
					cultureNamesTable = new Dictionary<string, string> ();
					foreach (CultureInfo ci in CultureInfo.GetCultures (CultureTypes.AllCultures))
						cultureNamesTable [ci.Name] = ci.Name;
				}

				return cultureNamesTable;
			}
		}
	}

	class ProjectParserContext: IExpressionContext
	{
		Project project;
		DotNetProjectConfiguration config;
		
		public ProjectParserContext (Project project, DotNetProjectConfiguration config)
		{
			this.project = project;
			this.config = config;
		}
		
		public string FullFileName {
			get {
				return project.FileName;
			}
		}
		
		public string EvaluateString (string value)
		{
			string val = value.Replace ("$(Configuration)", config.Name);
			return val;
		}
	}
}
