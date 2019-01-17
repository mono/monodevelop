﻿//
// TestableProjectPackagesFolderNode.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.NodeBuilders;
using NuGet.Packaging;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class TestableProjectPackagesFolderNode : ProjectPackagesFolderNode
	{
		public TestableProjectPackagesFolderNode (
			IDotNetProject project,
			IUpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace)
			: base (project, updatedPackagesInWorkspace, false)
		{
		}

		public List<PackageReference> PackageReferences = new List<PackageReference> ();

		protected override IEnumerable<PackageReference> GetPackageReferences ()
		{
			return PackageReferences;
		}

		public List<PackageReference> PackageReferencesWithPackageInstalled = new List<PackageReference> ();

		public override bool IsPackageInstalled (PackageReference reference)
		{
			return PackageReferencesWithPackageInstalled.Contains (reference);
		}

		public TaskCompletionSource<bool> RefreshTaskCompletionSource;

		protected override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync (CancellationTokenSource tokenSource)
		{
			RefreshTaskCompletionSource = new TaskCompletionSource<bool> ();
			return Task.FromResult (PackageReferences.AsEnumerable ());
		}

		protected override void OnInstalledPackagesRead (Task<IEnumerable<PackageReference>> task, CancellationTokenSource tokenSource)
		{
			base.OnInstalledPackagesRead (task, tokenSource);
			RefreshTaskCompletionSource.SetResult (true);
		}
	}
}

