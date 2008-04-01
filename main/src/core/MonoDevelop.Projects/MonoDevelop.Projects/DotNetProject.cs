//  DotNetProject.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects.Ambience;

namespace MonoDevelop.Projects
{
	[DataInclude (typeof(DotNetProjectConfiguration))]
	public class DotNetProject : Project
	{
		[ItemProperty]
		string language;
		ClrVersion clrVersion = ClrVersion.Default;
		bool usePartialTypes = true;
		
		IDotNetLanguageBinding languageBinding;
		
		public override string ProjectType {
			get { return "DotNet"; }
		}
		
		public override MonoDevelop.Projects.Ambience.Ambience Ambience {
			get { return Services.Ambience.AmbienceFromName (LanguageName); }
		}
		
		public string LanguageName {
			get { return language; }
		}
		
		public override string [] SupportedLanguages {
			get {
				return new string [] { "", language };
			}
		}
		
		public IDotNetLanguageBinding LanguageBinding {
			get { return languageBinding; }
		}
		
		[ItemProperty ("clr-version")]
		public ClrVersion ClrVersion {
			get {
				if (clrVersion == ClrVersion.Default)
					ClrVersion = ClrVersion.Default;
				return clrVersion;
			}
			set {
				ClrVersion validValue = GetValidClrVersion (value);
				if (clrVersion == validValue || validValue == ClrVersion.Default)
					return;
				clrVersion = validValue;
				
				// Propagate the clr version to configurations. We don't support
				// per-project clr versions right now, but we might support it
				// in the future.
				foreach (DotNetProjectConfiguration conf in Configurations)
					conf.ClrVersion = clrVersion;

				UpdateSystemReferences ();
			}
		}
		
		//if possible, find a ClrVersion that the language binding can handle
		ClrVersion GetValidClrVersion (ClrVersion suggestion)
		{
			if (suggestion == ClrVersion.Default) {
				if (languageBinding == null)
					return ClrVersion.Default;
				else
					suggestion = ClrVersion.Net_2_0;
			}
			
			if (languageBinding != null) {
				ClrVersion[] versions = languageBinding.GetSupportedClrVersions ();
				if (versions != null && versions.Length > 0) {
					foreach (ClrVersion v in versions) {
						if (v == suggestion) {
							return suggestion;
						}
					}
					
					return versions[0];
				}
			}
			
			return suggestion;
		}
		
		[ItemProperty (DefaultValue=true)]
		public bool UsePartialTypes {
			get { return usePartialTypes; }
			set { usePartialTypes = value; }
		}
		
		public DotNetProject ()
		{
		}
		
		public DotNetProject (string languageName)
		{
			language = languageName;
			languageBinding = FindLanguage (language);
			this.usePartialTypes = SupportsPartialTypes;
		}
		
		public DotNetProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
		{
			string binPath;
			if (info != null) {
				Name = info.ProjectName;
				binPath = info.BinPath;
			} else {
				binPath = ".";
			}
			
			language = languageName;
			languageBinding = FindLanguage (language);
			
			if (languageBinding != null) {
				if (projectOptions != null)
					projectOptions.SetAttribute ("DefineDebug", "True");
				DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) CreateConfiguration ("Debug");
				configuration.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
				Configurations.Add (configuration);
				
				configuration = (DotNetProjectConfiguration) CreateConfiguration ("Release");
				configuration.DebugMode = false;
				if (projectOptions != null)
					projectOptions.SetAttribute ("DefineDebug", "False");
				configuration.CompilationParameters = languageBinding.CreateCompilationParameters (projectOptions);
				Configurations.Add (configuration);
			}
			
			foreach (DotNetProjectConfiguration parameter in Configurations) {
				parameter.OutputDirectory = Path.Combine (binPath, parameter.Name);
				parameter.OutputAssembly  = Name;
				
				if (projectOptions != null) {
					if (projectOptions.Attributes["Target"] != null) {
						parameter.CompileTarget = (CompileTarget)Enum.Parse(typeof(CompileTarget), projectOptions.Attributes["Target"].InnerText);
					}
					if (projectOptions.Attributes["PauseConsoleOutput"] != null) {
						parameter.PauseConsoleOutput = Boolean.Parse(projectOptions.Attributes["PauseConsoleOutput"].InnerText);
					}
				}
			}
			
			this.usePartialTypes = SupportsPartialTypes;
		}
		
		public virtual bool SupportsPartialTypes {
			get {
				if (languageBinding == null)
					return false;
				System.CodeDom.Compiler.CodeDomProvider provider = languageBinding.GetCodeDomProvider ();
				if (provider == null)
					return false;
				return provider.Supports ( System.CodeDom.Compiler.GeneratorSupport.PartialTypes);
			}
		}
		
		protected override DataCollection Serialize (ITypeSerializer handler)
		{
			//make sure clr version is sorted out before saving
			ClrVersion v = this.ClrVersion;
			
			return base.Serialize (handler);
		}
		
		protected override void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			base.Deserialize (handler, data);
			languageBinding = FindLanguage (language);
			
			//older projects may not have this property but may not support partial types
			//so need to verify that the default attribute is OK
			if (UsePartialTypes && !SupportsPartialTypes) {
				LoggingService.LogWarning ("Project '{0}' has been set to use partial types but does not support them.", Name);
				UsePartialTypes = false;
			}
		}
		
		IDotNetLanguageBinding FindLanguage (string name)
		{
			IDotNetLanguageBinding binding = Services.Languages.GetBindingPerLanguageName (language) as IDotNetLanguageBinding;
			return binding;
		}

		public override IConfiguration CreateConfiguration (string name)
		{
			DotNetProjectConfiguration conf = new DotNetProjectConfiguration ();
			conf.Name = name;
			if (languageBinding != null)
				conf.CompilationParameters = languageBinding.CreateCompilationParameters (null);
			return conf;
		}
		
		protected override ICompilerResult DoBuild (IProgressMonitor monitor)
		{
			if (languageBinding == null) {
				DefaultCompilerResult langres = new DefaultCompilerResult ();
				string msg = GettextCatalog.GetString ("Unknown language '{0}'. You may need to install an additional add-in to support this language.", language);
				langres.AddError (msg);
				monitor.ReportError (msg, null);
				return langres;
			}

			DefaultCompilerResult refres = null;
			
			foreach (ProjectReference pr in ProjectReferences) {
				if (pr.ReferenceType == ReferenceType.Project) {
					// Ignore non-dotnet projects
					Project p = RootCombine != null ? RootCombine.FindProject (pr.Reference) : null;
					if (p != null && !(p is DotNetProject))
						continue;

					if (p == null || pr.GetReferencedFileNames ().Length == 0) {
						if (refres == null)
							refres = new DefaultCompilerResult ();
						string msg = GettextCatalog.GetString ("Referenced project '{0}' not found in the solution.", pr.Reference);
						monitor.ReportWarning (msg);
						refres.AddWarning (msg);
					}
				}
			}
			
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) ActiveConfiguration;
			conf.SourceDirectory = BaseDirectory;
			
			ProjectFileCollection files = ProjectFiles;
			ICompilerResult res = BuildResources (conf, ref files, monitor);
			if (res != null)
				return res;

			List<string> supportAssemblies = new List<string> ();
			CopySupportAssemblies (supportAssemblies);

			try {
				res = languageBinding.Compile (files, ProjectReferences, conf, monitor);
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
						File.Delete (s);
					} catch {
						// Ignore
					}
				}
			}
		}
		
		
		void CopySupportAssemblies (List<string> files)
		{
			foreach (ProjectReference projectReference in ProjectReferences) {
				if (projectReference.ReferenceType == ReferenceType.Project) {
					// It is a project reference. If this project depends
					// on other (non-gac) assemblies there may be a compilation problem because
					// the compiler won't be able to indirectly find them.
					// The solution is to copy them in the project directory, and delete
					// them after compilation.
					Project p = RootCombine.FindProject (projectReference.Reference);
					if (p == null)
						continue;
					
					string tdir = Path.GetDirectoryName (p.GetOutputFileName ());
					CopySupportAssemblies (p, tdir, files);
				}
			}
		}
		
		void CopySupportAssemblies (Project prj, string targetDir, List<string> files)
		{
			foreach (ProjectReference pref in prj.ProjectReferences) {
				if (pref.ReferenceType == ReferenceType.Gac)
					continue;
				foreach (string referenceFileName in pref.GetReferencedFileNames ()) {
					string asmName = Path.GetFileName (referenceFileName);
					asmName = Path.Combine (targetDir, asmName);
					if (!File.Exists (asmName)) {
						File.Copy (referenceFileName, asmName);
						files.Add (asmName);
					}
				}
				if (pref.ReferenceType == ReferenceType.Project) {
					Project sp = RootCombine.FindProject (pref.Reference);
					if (sp != null)
						CopySupportAssemblies (sp, targetDir, files);
				}
			}
		}

		// Builds the EmbedAsResource files. If any localized resources are found then builds the satellite assemblies
		// and sets @projectFiles to a cloned collection minus such resource files.
		private ICompilerResult BuildResources (DotNetProjectConfiguration configuration, ref ProjectFileCollection projectFiles, IProgressMonitor monitor)
		{
			string resgen = configuration.ClrVersion == ClrVersion.Net_2_0 ? "resgen2" : "resgen";
			bool cloned = false;
			Dictionary<string, string> resourcesByCulture = new Dictionary<string, string> ();
			foreach (ProjectFile finfo in projectFiles) {
				if (finfo.Subtype == Subtype.Directory || finfo.BuildAction != BuildAction.EmbedAsResource)
					continue;

				string fname = finfo.Name;
				string resourceId;
				CompilerError ce = GetResourceId (finfo, ref fname, resgen, out resourceId, monitor);
				if (ce != null) {
					CompilerResults cr = new CompilerResults (new TempFileCollection ());
					cr.Errors.Add (ce);

					return new DefaultCompilerResult (cr, String.Empty);
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
			CompilerError err = GenerateSatelliteAssemblies (resourcesByCulture, configuration.OutputDirectory, al, DefaultNamespace, monitor);
			if (err != null) {
				CompilerResults cr = new CompilerResults (new TempFileCollection ());
				cr.Errors.Add (err);

				return new DefaultCompilerResult (cr, String.Empty);
			}

			return null;
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

		public override string GetOutputFileName ()
		{
			if (ActiveConfiguration != null) {
				DotNetProjectConfiguration conf = (DotNetProjectConfiguration) ActiveConfiguration;
				return conf.CompiledOutputName;
			}
			else
				return null;
		}
		
		protected override void DoExecute (IProgressMonitor monitor, ExecutionContext context)
		{
			CopyReferencesToOutputPath (true);
			
			DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) ActiveConfiguration;
			monitor.Log.WriteLine ("Running " + configuration.CompiledOutputName + " ...");
			
			string platform = "Mono";
			
			switch (configuration.NetRuntime) {
				case NetRuntime.Mono:
					platform = "Mono";
					break;
				case NetRuntime.MonoInterpreter:
					platform = "Mint";
					break;
			}

			IConsole console;
			if (configuration.ExternalConsole)
				console = context.ExternalConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
			else
				console = context.ConsoleFactory.CreateConsole (!configuration.PauseConsoleOutput);
			
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor);
			
			try {
				IExecutionHandler handler = context.ExecutionHandlerFactory.CreateExecutionHandler (platform);
				if (handler == null) {
					monitor.ReportError ("Can not execute \"" + configuration.CompiledOutputName + "\". The selected execution mode is not supported in the " + platform + " platform.", null);
					return;
				}
			
				IProcessAsyncOperation op = handler.Execute (configuration.CompiledOutputName, configuration.CommandLineParameters, Path.GetDirectoryName (configuration.CompiledOutputName), null, console);
				
				operationMonitor.AddOperation (op);
				op.WaitForCompleted ();
				monitor.Log.WriteLine ("The application exited with code: {0}", op.ExitCode);
			} catch (Exception ex) {
				monitor.ReportError ("Can not execute " + "\"" + configuration.CompiledOutputName + "\"", ex);
			} finally {
				operationMonitor.Dispose ();
				console.Dispose ();
			}
		}
		
		public override bool IsCompileable(string fileName)
		{
			if (languageBinding == null)
				return false;
			return languageBinding.IsSourceCodeFile (fileName);
		}
		
		public virtual string GetDefaultNamespace (string fileName)
		{
			if (UseParentDirectoryAsNamespace) {
				try {
					DirectoryInfo directory = new DirectoryInfo (Path.GetDirectoryName (fileName));
					if (directory != null) 
						return directory.Name;
				} catch {
				}
			}
			
			if (!string.IsNullOrEmpty (DefaultNamespace))
				return DefaultNamespace;
			
			StringBuilder sb = new StringBuilder ();
			foreach (char c in Name) {
				if (char.IsLetterOrDigit (c) || c == '_' || c == '.')
					sb.Append (c);
			}
			if (sb.Length > 0)
				return sb.ToString ();
			else
				return "Application";
		}
		
		internal protected override void OnClean (IProgressMonitor monitor)
		{
			base.OnClean (monitor);
			
			// Delete the generated debug info
			string file = GetOutputFileName ();
			if (file != null) {
				if (File.Exists (file + ".mdb"))
					FileService.DeleteFile (file + ".mdb");
			}

			List<string> cultures = new List<string> ();
			monitor.Log.WriteLine (GettextCatalog.GetString ("Removing all .resources files"));
			foreach (ProjectFile pfile in ProjectFiles) {
				if (pfile.BuildAction == BuildAction.EmbedAsResource &&
					Path.GetExtension (pfile.Name) == ".resx") {
					string resFilename = Path.ChangeExtension (pfile.Name, ".resources");
					if (File.Exists (resFilename))
						File.Delete (resFilename);
				}
				string culture = GetCulture (pfile.Name);
				if (culture != null)
					cultures.Add (culture);
			}

			if (cultures.Count > 0 && ActiveConfiguration != null && DefaultNamespace != null) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Removing all satellite assemblies"));
				string outputDir = ((DotNetProjectConfiguration)ActiveConfiguration).OutputDirectory;
				string satelliteAsmName = DefaultNamespace + ".resources.dll";

				foreach (string culture in cultures) {
					string path = String.Format ("{0}{3}{1}{3}{2}", outputDir, culture, satelliteAsmName, Path.DirectorySeparatorChar);
					if (File.Exists (path))
						File.Delete (path);
				}
			}
		}

		// Make sure that the project references are valid for the target clr version.
		void UpdateSystemReferences ()
		{
			ArrayList toDelete = new ArrayList ();
			ArrayList toAdd = new ArrayList ();
			
			foreach (ProjectReference pref in ProjectReferences) {
				if (pref.ReferenceType == ReferenceType.Gac) {
					string newRef = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (pref.Reference, this.ClrVersion);
					if (newRef == null)
						toDelete.Add (pref);
					else if (newRef != pref.Reference) {
						toDelete.Add (pref);
						toAdd.Add (new ProjectReference (ReferenceType.Gac, newRef));
					}
				}
			}
			foreach (ProjectReference pref in toDelete) {
				ProjectReferences.Remove (pref);
			}
			foreach (ProjectReference pref in toAdd) {
				ProjectReferences.Add (pref);
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



	}
}
