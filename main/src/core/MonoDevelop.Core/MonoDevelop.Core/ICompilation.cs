//
// ICompilation.cs
//
// Author:
//       jason <jaimison@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using MonoDevelop.Projects;

namespace MonoDevelop.Core
{
	public interface IRoslynCompilation
	{
		INamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName);
		IEnumerable<MetadataReference> References { get; }
		ISymbol GetAssemblyOrModuleSymbol(MetadataReference reference);
		IAssemblySymbol Assembly { get; }
	}

	public abstract class RoslynCompilationProvider
	{
		public abstract IRoslynCompilation GetFromProject(Projects.Project project);
		public abstract string LanguageName { get; }
	}

	public class CompilationWrapper : IRoslynCompilation
	{
		private readonly Compilation compilation;

		public CompilationWrapper (Compilation compilation)
		{
			this.compilation = compilation ?? throw new System.ArgumentNullException (nameof (compilation));
		}
		public IEnumerable<MetadataReference> References => compilation.References;

		public IAssemblySymbol Assembly => compilation.Assembly;

		public ISymbol GetAssemblyOrModuleSymbol (MetadataReference reference) =>
			compilation.GetAssemblyOrModuleSymbol(reference);

		public INamedTypeSymbol GetTypeByMetadataName (string fullyQualifiedMetadataName) =>
			compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
	}
}
