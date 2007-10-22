
using System;
using Mono.Addins;

namespace TextEditor.CompilerService
{
	[TypeExtensionPoint]
	public interface ICompiler
	{
		bool CanCompile (string file);
		string Compile (string file, string outFile);
	}
}
