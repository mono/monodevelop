//
// PackageReferenceNode.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Serialization;
using NuGet;
using System.Runtime.Versioning;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	public class PackageReferenceNode
	{
		public PackageReferenceNode (PackageReference packageReference, bool installed, bool pending = false)
		{
			PackageReference = packageReference;
			Installed = installed;
			IsInstallPending = pending;
		}

		public PackageReference PackageReference { get; private set; }
		public bool Installed { get; private set; }
		public bool IsInstallPending { get; private set; }

		public string Name {
			get { return PackageReference.Id; }
		}

		public string Id {
			get { return PackageReference.Id; }
		}

		public SemanticVersion Version {
			get { return PackageReference.Version; }
		}

		public bool IsDevelopmentDependency {
			get { return PackageReference.IsDevelopmentDependency; }
		}

		public FrameworkName TargetFramework {
			get { return PackageReference.TargetFramework; }
		}

		public IVersionSpec VersionConstraint {
			get { return PackageReference.VersionConstraint; }
		}

		public string GetPackageVersionLabel ()
		{
			return GettextCatalog.GetString ("Version {0}", Version);
		}
	}
}

