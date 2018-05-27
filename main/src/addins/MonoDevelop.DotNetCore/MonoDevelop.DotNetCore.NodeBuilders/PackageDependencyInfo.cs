//
// PackageDependencyInfo.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
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

using System.Collections.Generic;
using System.Collections.Immutable;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class PackageDependencyInfo
	{
		ImmutableArray<PackageDependencyInfo> dependencies = ImmutableArray<PackageDependencyInfo>.Empty;
		PackageDependency dependency;

		public PackageDependencyInfo (PackageDependency dependency)
		{
			this.dependency = dependency;
		}

		public bool HasChildDiagnostic { get; set; }
		public bool IsBuilt { get; set; }

		public string Name => dependency.Name;
		public string Version => dependency.Version;

		public string DiagnosticCode => dependency.DiagnosticCode;
		public string DiagnosticMessage => dependency.DiagnosticMessage;
		public bool IsDiagnostic => dependency.IsDiagnostic;

		public IEnumerable<string> DependencyNames => dependency.Dependencies;

		public IEnumerable<PackageDependencyInfo> Dependencies {
			get { return dependencies; }
		}

		public void AddChild (PackageDependencyInfo dependency)
		{
			dependencies = dependencies.Add (dependency);

			if (dependency.HasChildDiagnostic || dependency.IsDiagnostic)
				HasChildDiagnostic = true;
		}

		public override string ToString ()
		{
			return Name;
		}
	}
}
