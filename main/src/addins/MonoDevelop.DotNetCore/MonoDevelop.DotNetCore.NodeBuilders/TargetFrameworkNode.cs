//
// TargetFrameworkNode.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class TargetFrameworkNode
	{
		PackageDependencyInfo dependency;

		public TargetFrameworkNode (
			DependenciesNode dependenciesNode,
			PackageDependencyInfo dependency)
		{
			DependenciesNode = dependenciesNode;
			this.dependency = dependency;
		}

		internal DotNetProject Project {
			get { return DependenciesNode.Project; }
		}

		internal DependenciesNode DependenciesNode { get; }

		public string Name {
			get { return dependency.Name; }
		}

		public string GetLabel ()
		{
			return GLib.Markup.EscapeText (Name);
		}

		public string GetSecondaryLabel ()
		{
			return string.Format ("({0})", dependency.Version);
		}

		public IconId GetIconId ()
		{
			return new IconId ("md-framework-dependency");
		}

		public IEnumerable<PackageDependencyNode> GetDependencyNodes (bool sdkDependencies)
		{
			return PackageDependencyNode.GetDependencyNodes (
				DependenciesNode,
				dependency,
				sdkDependencies,
				topLevel: true);
		}

		public IEnumerable<object> GetChildNodes ()
		{
			return DependenciesNode.GetChildNodes (this);
		}
	}
}
