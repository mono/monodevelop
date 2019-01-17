﻿//
// PackageReferenceNodeDescriptor.cs
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

using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	internal class PackageReferenceNodeDescriptor : CustomDescriptor
	{
		PackageReferenceNode packageReferenceNode;

		public PackageReferenceNodeDescriptor (PackageReferenceNode packageReferenceNode)
		{
			this.packageReferenceNode = packageReferenceNode;
		}

		[LocalizedCategory ("Package")]
		[LocalizedDisplayName ("Id")]
		[LocalizedDescription ("Package Id.")]
		public string Id {
			get { return packageReferenceNode.Id; }
		}

		[LocalizedCategory ("Package")]
		[LocalizedDisplayName ("Version")]
		[LocalizedDescription ("Package version.")]
		public string Version {
			get { return packageReferenceNode.GetPackageDisplayVersion (); }
		}

		[LocalizedCategory ("Package")]
		[LocalizedDisplayName ("Development Dependency")]
		[LocalizedDescription ("Package is a development dependency.")]
		public bool IsDevelopmentDependency {
			get { return packageReferenceNode.IsDevelopmentDependency; }
		}

		[LocalizedCategory ("Package")]
		[LocalizedDisplayName ("Target Framework")]
		[LocalizedDescription ("Target framework for the Package.")]
		public NuGetFramework TargetFramework {
			get { return packageReferenceNode.TargetFramework; }
		}

		[LocalizedCategory ("Package")]
		[LocalizedDisplayName ("Version Constraint")]
		[LocalizedDescription ("Version constraint for the Package.")]
		public VersionRange VersionConstraint {
			get { return packageReferenceNode.VersionConstraint; }
		}

	}
}

