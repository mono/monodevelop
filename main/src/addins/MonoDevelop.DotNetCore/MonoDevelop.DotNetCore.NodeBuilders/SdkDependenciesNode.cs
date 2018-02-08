//
// SdkDependenciesNode.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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


using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class SdkDependenciesNode
	{
		public static readonly string NodeName = "SdkDependencies";

		public SdkDependenciesNode (DependenciesNode parentNode)
		{
			ParentNode = parentNode;
		}

		internal DotNetProject Project {
			get { return ParentNode.Project; }
		}

		internal DependenciesNode ParentNode { get; private set; }

		public string GetLabel ()
		{
			return GettextCatalog.GetString ("SDK");
		}

		public string GetSecondaryLabel ()
		{
			return string.Empty;
		}

		public IconId Icon {
			get { return Stock.OpenReferenceFolder; }
		}

		public IconId ClosedIcon {
			get { return Stock.ClosedReferenceFolder; }
		}

		public bool LoadedDependencies {
			get { return ParentNode.PackageDependencyCache.LoadedDependencies; }
		}

		public IEnumerable<TargetFrameworkNode> GetTargetFrameworkNodes ()
		{
			return ParentNode.GetTargetFrameworkNodes (sdkDependencies: true);
		}

		public PackageDependency GetDependency (string dependency)
		{
			return ParentNode.PackageDependencyCache.GetDependency (dependency);
		}

		/// <summary>
		/// Returns a SDK node for the project whilst the package dependencies are still loading.
		/// </summary>
		public IEnumerable<PackageDependencyNode> GetDefaultNodes ()
		{
			if (Project.TargetFramework.IsNetCoreApp ())
				yield return new PackageDependencyNode (ParentNode, "Microsoft.NETCore.App", true, true);
			else if (Project.TargetFramework.IsNetStandard ())
				yield return new PackageDependencyNode (ParentNode, "NETStandard.Library", true, true);
		}
	}
}
