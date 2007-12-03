using System;
using System.IO;
using System.Diagnostics;
using Mono.Addins;
using TextEditor.CompilerService;

[assembly:Addin]
[assembly:AddinDependency ("TextEditor.CompilerService", "1.0")]

namespace TextEditor.CompilerService.CSharp
{
	[Extension]
	public class CSharpCompiler: ICompiler
	{
		public bool CanCompile (string file)
		{
			return Path.GetExtension (file) == ".cs";
		}
		
		public string Compile (string file, string outFile) 
		{
			string messages = "";
			
			ProcessStartInfo ps = new ProcessStartInfo ();
			ps.FileName = "mcs";
			ps.Arguments = "file";
			ps.UseShellExecute = false;
			ps.RedirectStandardOutput = true;
			Process p = Process.Start (ps);
			
			string line = null;
			while ((line = p.StandardOutput.ReadLine ()) != null) {
				messages += line + "\n";
			}
			return messages;
		}
	}
}