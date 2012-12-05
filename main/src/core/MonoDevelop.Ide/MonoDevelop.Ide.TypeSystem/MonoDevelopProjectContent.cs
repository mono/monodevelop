//
// MonoDevelopProjectContent.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Collections.Generic;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.TypeSystem
{
	[Serializable]
	class MonoDevelopProjectContent : CSharpProjectContent
	{
		[NonSerialized]
		Project project;

		public Project Project {
			get {
				return project;
			}
			internal set {
				project = value;
			}
		}

		public MonoDevelopProjectContent (Project project)
		{
			this.project = project;
		}

		MonoDevelopProjectContent (MonoDevelopProjectContent pc) : base (pc)
		{
			this.project = pc.project;
		}

		public override ICompilation CreateCompilation()
		{
			var solutionSnapshot = new DefaultSolutionSnapshot();
			ICompilation compilation = new MonoDevelopCompilation(solutionSnapshot, this, AssemblyReferences);
			solutionSnapshot.AddCompilation(this, compilation);
			return compilation;
		}

		protected override CSharpProjectContent Clone()
		{
			return new MonoDevelopProjectContent(this);
		}

		public override ICompilation CreateCompilation(ISolutionSnapshot solutionSnapshot)
		{
			return new MonoDevelopCompilation(solutionSnapshot, this, AssemblyReferences);
		}
	}

	class MonoDevelopCompilation : SimpleCompilation
	{
		readonly MonoDevelopProjectContent content;

		public MonoDevelopCompilation (ISolutionSnapshot solutionSnapshot, MonoDevelopProjectContent content, IEnumerable<IAssemblyReference> assemblyReferences) : base (solutionSnapshot, content, assemblyReferences)
		{
			this.content = content;
		}

		public override INamespace GetNamespaceForExternAlias (string alias)
		{
			var netProject = content.Project as DotNetProject;
			if (netProject == null)
				return null;
			foreach (var r in netProject.References) {
				if (r.Aliases == alias) {
					foreach (var refAsm in ReferencedAssemblies) {
						if (refAsm.FullAssemblyName == r.StoredReference) {
							return refAsm.RootNamespace;
						}
						
					}
				}
			}
			

			return null;
		}
	}
}

