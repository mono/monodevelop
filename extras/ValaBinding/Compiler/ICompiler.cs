//
// ICompiler.cs: interface that must be implemented by any class that wants
// to provide a compiler for the ValaBinding addin.
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
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

namespace MonoDevelop.ValaBinding
{
	[TypeExtensionPoint ("/ValaBinding/Compilers")]
	public interface ICompiler
	{
		string Name {
			get;
		}
		
		string CompilerCommand {
			get;
		}
		
		string GetCompilerFlags(ValaProjectConfiguration configuration);
		
		string GetDefineFlags(ValaProjectConfiguration configuration);
		
		BuildResult Compile(ValaProject project,
            ConfigurationSelector solutionConfiguration,
			IProgressMonitor monitor);
		
		void Clean(ProjectFileCollection projectFiles, ValaProjectConfiguration configuration, IProgressMonitor monitor);
	}
}
