//
// ICompiler.cs: interface that must be implemented by any class that wants
// to provide a compiler for the CBinding addin.
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace CBinding
{
	[TypeExtensionPoint ("/CBinding/Compilers")]
	public interface ICompiler
	{
		string Name {
			get;
		}
		
		Language Language {
			get;
		}
		
		string CompilerCommand {
			get;
		}
		
		bool SupportsCcache {
			get;
		}
		
		bool SupportsPrecompiledHeaders {
			get;
		}
		
		string GetCompilerFlags (CProjectConfiguration configuration);
		
		string GetDefineFlags (CProjectConfiguration configuration);
		
		BuildResult Compile (
			ProjectFileCollection projectFiles,
		    ProjectPackageCollection packages,
		    CProjectConfiguration configuration,
		    IProgressMonitor monitor);
		
		void Clean (ProjectFileCollection projectFiles, CProjectConfiguration configuration, IProgressMonitor monitor);
	}
}
