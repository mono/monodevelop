using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.CodeDom.Compiler;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui.Components;

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
				switch(t.Trim ())
				{
					case "error":
						error.IsWarning = false;
						return true;
					case "warning":
						error.IsWarning = true;
						return true;
					case "hint":
						error.IsWarning = true;
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
				string [] s = l.Split(delim, 7);
				
				try
				{
				    SetErrorType (error, s[5]);
				    if (s[6].StartsWith ("N") && s[6].Contains (": "))
				    {
				        string[] e = s[6].Split (delim, 2);
				        error.ErrorNumber = s[0];
				        error.ErrorText = s[1].Trim ();
				    }
				    else
				        error.ErrorText = s[6].Trim ();
				    error.FileName = s[0];
				    error.Line = int.Parse(s[1]);
				    error.Column = int.Parse(s[2]);
				}
				catch
				{
				    SetErrorType (error, s[0]);
				    error.ErrorText = s[1].Trim ();
				    error.FileName = "";
				    error.Line = 0;
				    error.Column = 0;
				}
				
				/*if (SetErrorType(error, s[5]))
				{
					error.ErrorText = s[6]; // l.Substring(l.IndexOf(s[0]+": ") + s[0].Length+2);
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
				}*/
				Errors.Add(error);
			}

			public BuildResult GetResult()
			{
				return new BuildResult(this, "");
			} 
		}
	
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
			if (cp.Greedy)
				options += " -greedy";
			if (cp.Pedantic)
				options += " -pedantic-lexer";
			if (configuration.CompileTarget == CompileTarget.Library)
				options += " -tdll";
				
			return options;			
		}

		public bool CanCompile(string fileName)
		{
			return (System.IO.Path.GetExtension(fileName).ToLower() == ".n");
		} 

		public BuildResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection projectReferences, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			NemerleParameters cp = (NemerleParameters) configuration.CompilationParameters;
			if (cp == null) cp = new NemerleParameters ();
			
			string references = "";
			string files   = "";
			
			foreach (ProjectReference lib in projectReferences)
				foreach (string a in lib.GetReferencedFileNames())
					references += " -r \"" + a + "\"";
			
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
		
		private BuildResult DoCompilation(string arguments)
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
	}
}
