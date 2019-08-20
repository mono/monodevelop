//
// TestableTargetFrameworkNodeBuilder.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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

using System.Collections;
using System.Collections.Generic;
using MonoDevelop.DotNetCore.NodeBuilders;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.DotNetCore.Tests
{
	class TestableTargetFrameworkNodeBuilder : TargetFrameworkNodeBuilder
	{
		public List<object> ChildNodes = new List<object> ();

		public PackageDependenciesNode PackageDependencies;
		public SdkDependenciesNode SdkDependencies;
		public ProjectDependenciesNode ProjectDependencies;
		public AssemblyDependenciesNode AssemblyDependencies;

		void AddChild (ITreeBuilder treeBuilder, object dataObject)
		{
			ChildNodes.Add (dataObject);

			if (dataObject is AssemblyDependenciesNode assemblyDependencies) {
				AssemblyDependencies = assemblyDependencies;
			} else if (dataObject is PackageDependenciesNode packageDependencies) {
				PackageDependencies = packageDependencies;
			} else if (dataObject is ProjectDependenciesNode projectDependencies) {
				ProjectDependencies = projectDependencies;
			} else if (dataObject is SdkDependenciesNode sdkDependencies) {
				SdkDependencies = sdkDependencies;
			}
		}

		protected override void AddChildren (ITreeBuilder treeBuilder, IEnumerable dataObjects)
		{
			foreach (object dataObject in dataObjects) {
				AddChild (treeBuilder, dataObject);
			}
		}
	}
}
