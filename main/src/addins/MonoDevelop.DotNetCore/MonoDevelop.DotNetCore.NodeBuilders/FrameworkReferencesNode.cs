//
// FrameworkReferencesNode.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class FrameworkReferencesNode
	{
		public static readonly string NodeName = "FrameworkReferences";

		public FrameworkReferencesNode (DependenciesNode parentNode)
		{
			ParentNode = parentNode;
		}

		public FrameworkReferencesNode (TargetFrameworkNode frameworkNode)
		{
			FrameworkNode = frameworkNode;
			ParentNode = frameworkNode.DependenciesNode;
		}

		internal DotNetProject Project {
			get { return ParentNode.Project; }
		}

		internal TargetFrameworkNode FrameworkNode { get; private set; }

		internal DependenciesNode ParentNode { get; private set; }

		public string GetLabel ()
		{
			return GettextCatalog.GetString ("Frameworks");
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

		public bool LoadedReferences {
			get { return ParentNode.FrameworkReferencesCache.LoadedReferences; }
		}

		public bool HasChildNodes ()
		{
			return GetChildNodes ().Any ();
		}

		public IEnumerable<FrameworkReferenceNode> GetChildNodes ()
		{
			if (ParentNode.FrameworkReferencesCache.LoadedReferences) {
				return ParentNode.FrameworkReferencesCache.GetFrameworkReferenceNodes (GetTargetFrameworkMoniker ());
			} else {
				return GetDefaultNodes ();
			}
		}

		/// <summary>
		/// Returns a FrameworkReference node for the project whilst the framework references are still loading.
		/// </summary>
		public IEnumerable<FrameworkReferenceNode> GetDefaultNodes ()
		{
			return GetDefaultNodes (GetTargetFrameworkMoniker ());
		}

		TargetFrameworkMoniker GetTargetFrameworkMoniker ()
		{
			return FrameworkNode?.GetTargetFrameworkMoniker () ?? Project.TargetFramework.Id;
		}

		IEnumerable<FrameworkReferenceNode> GetDefaultNodes (TargetFrameworkMoniker targetFramework)
		{
			if (targetFramework.IsNetCoreApp ())
				yield return new FrameworkReferenceNode ("Microsoft.NETCore.App");
			else if (targetFramework.IsNetStandard ())
				yield return new FrameworkReferenceNode ("NETStandard.Library");
		}
	}
}