using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.CodeDom.Compiler;
using System.Threading;

using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui.Components;
using MonoDevelop.Services;

namespace NemerleBinding
{
	public class NemerleBindingCompilerServices
	{
		class CompilerResultsParser : CompilerResults
		{
			public CompilerResultsParser() : base (new TempFileCollection ())
			{
			}
			
			bool SetErrorType(CompilerError error, string t)
			{
				switch(t)
				{
					case "error":
						error.IsWarning = false;
						return true;
					case "warning":
						error.IsWarning = true;
						return true;
					case "hint":
						error.IsWarning = true;
						error.ErrorNumber = "COMMENT";
						return true;
					default:
						return false;
				}
			}

			public void Parse(string l)
			{
				CompilerError error = new CompilerError();
				error.ErrorNumber = String.Empty;

				char [] delim = {':'};
				string [] s = l.Split(delim, 5);
				
				if (SetErrorType(error, s[0]))
				{
					error.ErrorText = l.Substring(l.IndexOf(s[0]+": ") + s[0].Length+2);
					error.FileName  = "";
					error.Line      = 0;
					error.Column    = 0;
				} else
				if ((s.Length >= 4)  && SetErrorType(error, s[3].Substring(1)))
				{
					error.ErrorText = l.Substring(l.IndexOf(s[3]+": ") + s[3].Length+2);
					error.FileName  = s[0];
					error.Line      = int.Parse(s[1]);
					error.Column    = int.Parse(s[2]);
				} else
				{
					error.ErrorText = l;
					error.FileName  = "";
					error.Line      = 0;
					error.Column    = 0;
					error.IsWarning = false;					
				}
				Errors.Add(error);
			}

			public ICompilerResult GetResult()
			{
				return new DefaultCompilerResult(this, "");
			} 
		}
	
		FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
		static string ncc = "ncc";

		private string GetOptionsString (DotNetProjectConfiguration configuration, NemerleParameters cp)
		{
			string options = " ";
			if (cp.Nostdmacros)
				options += " -no-stdmacros";
			if (cp.Nostdlib)
				options += " -no-stdlib";
			if (cp.Ot)
				options += " -Ot";
			if (cp.Obcm)
				options += " -Obcm";
			if (cp.Oocm)
				options += " -Oocm";
			if (cp.Oscm)
				options += " -Oscm";
			if (configuration.CompileTarget == CompileTarget.Library)
				options += " -tdll";
				
			return options;			
		}

		public bool CanCompile(string fileName)
		{
			return Path.GetExtension(fileName) == ".n";
		} 

		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection projectReferences, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			NemerleParameters cp = (NemerleParameters) configuration.CompilationParameters;
			if (cp == null) cp = new NemerleParameters ();
			
			string references = "";
			string files   = "";
			
			foreach (ProjectReference lib in projectReferences)
				references += " -r \"" + lib.GetReferencedFileName() + "\"";
			
			foreach (ProjectFile f in projectFiles)
				if (f.Subtype != Subtype.Directory)
					switch (f.BuildAction)
					{
						case BuildAction.Compile:
							files += " \"" + f.Name + "\"";
						break;
					}

			if (!Directory.Exists (configuration.OutputDirectory))
				Directory.CreateDirectory (configuration.OutputDirectory);
			
			string args = "-q -no-color " + GetOptionsString (configuration, cp) + references + files  + " -o " + configuration.CompiledOutputName;
			return DoCompilation (args);
		}
		
		// This enables check if we have output without blocking 
		class VProcess : Process
		{
			Thread t = null;
			public void thr()
			{
				while (StandardOutput.Peek() == -1){};
			}
			public void OutWatch()
			{
				t = new Thread(new ThreadStart(thr));
				t.Start();
			}
			public bool HasNoOut()
			{
				return t.IsAlive;
			} 
		}
		
		private ICompilerResult DoCompilation(string arguments)
		{
			string l;
			ProcessStartInfo si = new ProcessStartInfo(ncc, arguments);
			si.RedirectStandardOutput = true;
			si.RedirectStandardError = true;
			si.UseShellExecute = false;
			VProcess p = new VProcess();
			p.StartInfo = si;
			p.Start();

			p.OutWatch();
			while ((!p.HasExited) && p.HasNoOut())
//			while ((!p.HasExited) && (p.StandardOutput.Peek() == -1)) // this could eliminate VProcess outgrowth
			{
				System.Threading.Thread.Sleep (100);
			}
			
			CompilerResultsParser cr = new CompilerResultsParser();	
			while ((l = p.StandardOutput.ReadLine()) != null)
			{
				cr.Parse(l);
			}
			
			if  ((l = p.StandardError.ReadLine()) != null)
			{
				cr.Parse("error: " + ncc + " execution problem");
			}
			
			return cr.GetResult();
		}

		public void GenerateMakefile (Project project, Combine parentCombine)
		{
			StreamWriter stream = new StreamWriter (Path.Combine (project.BaseDirectory, "Makefile." + project.Name.Replace (" ", "")));

			DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) project.ActiveConfiguration;
			NemerleParameters cp = (NemerleParameters) configuration.CompilationParameters;
			
			string outputName = Path.GetFileName (configuration.CompiledOutputName);

			string relativeOutputDir = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, parentCombine.OutputDirectory);

			ArrayList compile_files = new ArrayList ();
			ArrayList pkg_references = new ArrayList ();
			ArrayList assembly_references = new ArrayList ();
			ArrayList project_references = new ArrayList ();
			ArrayList system_references = new ArrayList ();
			
			foreach (ProjectFile finfo in project.ProjectFiles) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
					case BuildAction.Compile:
						string rel_path = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, Path.GetDirectoryName (finfo.Name));
						if (CanCompile (finfo.Name));
						compile_files.Add (Path.Combine (rel_path, Path.GetFileName (finfo.Name)));
						break;
					}
				}
			}

			SystemAssemblyService sas = (SystemAssemblyService)ServiceManager.GetService (typeof (SystemAssemblyService));
			foreach (ProjectReference lib in project.ProjectReferences) {
				switch (lib.ReferenceType) {
				case ReferenceType.Gac:
					string pkg = sas.GetPackageFromFullName (lib.Reference);
					if (pkg == "MONO-SYSTEM") {
						system_references.Add (Path.GetFileName (lib.GetReferencedFileName ()));
					} else if (!pkg_references.Contains (pkg)) {
						pkg_references.Add (pkg);
					}
					break;
				case ReferenceType.Assembly:
					string assembly_fileName = lib.GetReferencedFileName ();
					string rel_path_to = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, Path.GetDirectoryName (assembly_fileName));
					assembly_references.Add (Path.Combine (rel_path_to, Path.GetFileName (assembly_fileName)));
					break;
				case ReferenceType.Project:
					IProjectService prjService = (IProjectService)ServiceManager.GetService (typeof (IProjectService));
					CombineEntryCollection allProjects = prjService.CurrentOpenCombine.GetAllProjects();
					
					foreach (Project projectEntry in allProjects) {
						if (projectEntry.Name == lib.Reference) {
							string project_base_dir = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, projectEntry.BaseDirectory);
							
							string project_output_fileName = projectEntry.GetOutputFileName ();
							project_references.Add (Path.Combine (project_base_dir, Path.GetFileName (project_output_fileName)));
						}
					}
					break;
				}
			}

			stream.WriteLine ("# This makefile is autogenerated by MonoDevelop");
			stream.WriteLine ("# Do not modify this file");
			stream.WriteLine ();
			stream.WriteLine ("SOURCES = \\");
			for (int i = 0; i < compile_files.Count; i++) {
				stream.Write (((string)compile_files[i]).Replace (" ", "\\ "));
				if (i != compile_files.Count - 1)
					stream.WriteLine (" \\");
				else
					stream.WriteLine ();
			}
			stream.WriteLine ();

			if (pkg_references.Count > 0) {
				stream.WriteLine ("PKG_REFERENCES = \\");
				for (int i = 0; i < pkg_references.Count; i++) {
					stream.Write (pkg_references[i]);
					if (i != pkg_references.Count - 1)
						stream.WriteLine (" \\");
					else
						stream.WriteLine ();
				}
				
				stream.WriteLine ();
				stream.WriteLine ("PKG_REFERENCES_BUILD = $(addprefix -pkg:, $(PKG_REFERENCES))");
				stream.WriteLine ();
				stream.WriteLine ("PKG_REFERENCES_CHECK = $(addsuffix .pkgcheck, $(PKG_REFERENCES))");
				stream.WriteLine ();
			}
			
			if (system_references.Count > 0) {
				stream.WriteLine ("SYSTEM_REFERENCES = \\");
				for (int i = 0; i < system_references.Count; i++) {
					stream.Write (system_references[i]);
					if (i != system_references.Count - 1)
						stream.WriteLine (" \\");
					else
						stream.WriteLine ();
				}
				stream.WriteLine ();
				stream.WriteLine ("SYSTEM_REFERENCES_BUILD = $(addprefix -r:, $(SYSTEM_REFERENCES))");
				stream.WriteLine ();
				stream.WriteLine ("SYSTEM_REFERENCES_CHECK = $(addsuffix .check, $(SYSTEM_REFERENCES))");
				stream.WriteLine ();
			}

			if (assembly_references.Count > 0) {
				stream.WriteLine ("ASSEMBLY_REFERENCES = \\");
				for (int i = 0; i < assembly_references.Count; i++) {
					stream.Write ("\"" + assembly_references[i] + "\"");
					if (i != assembly_references.Count - 1)
						stream.WriteLine (" \\");
					else
						stream.WriteLine ();
				}
				
				stream.WriteLine ();
				stream.WriteLine ("ASSEMBLY_REFERENCES_BUILD = $(addprefix -r:, $(ASSEMBLY_REFERENCES))");
				stream.WriteLine ();
			}

			if (project_references.Count > 0) {
				stream.WriteLine ("PROJECT_REFERENCES = \\");
				for (int i = 0; i < project_references.Count; i++) {
					stream.Write ("\"" + project_references[i] + "\"");
					if (i != project_references.Count - 1)
						stream.WriteLine (" \\");
					else
						stream.WriteLine ();
				}
				
				stream.WriteLine ();
				stream.WriteLine ("PROJECT_REFERENCES_BUILD = $(addprefix -r:, $(PROJECT_REFERENCES))");
				stream.WriteLine ();
			}

			stream.Write ("NCC_OPTIONS = " + GetOptionsString (configuration, cp));

			stream.WriteLine ();
			stream.WriteLine ();

			stream.WriteLine ("all: " + outputName);
			stream.WriteLine ();
			
			stream.WriteLine(outputName + ": $(SOURCES)");
			
			stream.Write ("\t{0} $(NCC_OPTIONS) -out:\"{1}\"", ncc, outputName);
			if (pkg_references.Count > 0) {
				stream.Write (" $(PKG_REFERENCES_BUILD)");
			}
			if (assembly_references.Count > 0) {
				stream.Write (" $(ASSEMBLY_REFERENCES_BUILD)");
			}
			if (project_references.Count > 0) {
				stream.Write (" $(PROJECT_REFERENCES_BUILD)");
			}
			if (system_references.Count > 0) {
				stream.Write (" $(SYSTEM_REFERENCES_BUILD)");
			}
			stream.WriteLine (" $(SOURCES) \\");
			stream.WriteLine ("\t&& cp \"{0}\" {1}/.", outputName, relativeOutputDir);
			
			stream.WriteLine ();
			stream.WriteLine ("clean:");
			stream.WriteLine ("\trm -f {0}", outputName);
			stream.WriteLine ();
			
			stream.Write ("depcheck: ");
			if (pkg_references.Count > 0) {
				stream.Write ("PKG_depcheck ");
			}
			if (system_references.Count > 0) {
				stream.Write ("SYSTEM_depcheck");
			}
			stream.WriteLine ();
			stream.WriteLine ();
			if (pkg_references.Count > 0) {
				stream.WriteLine ("PKG_depcheck: $(PKG_REFERENCES_CHECK)");
				stream.WriteLine ();
				stream.WriteLine ("%.pkgcheck:");
				stream.WriteLine ("\t@echo -n Checking for package $(subst .pkgcheck,,$@)...");
				stream.WriteLine ("\t@if pkg-config --libs $(subst .pkgcheck,,$@) &> /dev/null; then \\");
				stream.WriteLine ("\t\techo yes; \\");
				stream.WriteLine ("\telse \\");
				stream.WriteLine ("\t\techo no; \\");
				stream.WriteLine ("\t\texit 1; \\");
				stream.WriteLine ("\tfi");
				stream.WriteLine ();
			}

			if (system_references.Count > 0) {
				stream.WriteLine ("SYSTEM_depcheck: $(SYSTEM_REFERENCES_CHECK)");
				stream.WriteLine ();
				stream.WriteLine ("%.check:");
				stream.WriteLine ("\t@echo -n Checking for $(subst .check,,$@)...");
				stream.WriteLine ("\t@if [ ! -e `pkg-config --variable=libdir mono`/mono/1.0/$(subst .check,,$@) ]; then \\");
				stream.WriteLine ("\t\techo no; \\");
				stream.WriteLine ("\t\texit 1; \\");
				stream.WriteLine ("\telse \\");
				stream.WriteLine ("\t\techo yes; \\");
				stream.WriteLine ("\tfi");
			}
			
			stream.Flush ();
			stream.Close ();
		}		
	}
}
