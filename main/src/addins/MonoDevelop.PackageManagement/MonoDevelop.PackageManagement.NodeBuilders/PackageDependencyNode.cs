//
// PackageDependencyNode.cs
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	class PackageDependencyNode
	{
		PackageDependenciesNode dependenciesNode;
		PackageDependency dependency;

		PackageDependencyNode (PackageDependenciesNode dependenciesNode, PackageDependency dependency)
		{
			this.dependenciesNode = dependenciesNode;
			this.dependency = dependency;
		}

		public static PackageDependencyNode Create (PackageDependenciesNode dependenciesNode, string dependencyName)
		{
			PackageDependency dependency = dependenciesNode.GetDependency (dependencyName);
			if (dependency != null)
				return new PackageDependencyNode (dependenciesNode, dependency);

			return null;
		}

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
			return new IconId ("md-package-dependency");
		}

		public bool HasDependencies ()
		{
			return dependency.Dependencies.Any ();
		}

		public IEnumerable<PackageDependencyNode> GetDependencyNodes ()
		{
			return GetDependencyNodes (dependenciesNode, dependency);
		}

		public static IEnumerable<PackageDependencyNode> GetDependencyNodes (
			PackageDependenciesNode dependenciesNode,
			PackageDependency dependency)
		{
			return dependency.Dependencies
				.Select (item => PackageDependencyNode.Create (dependenciesNode, item))
				.Where (item => item != null);
		}
	}
}
