//
// FakeProjectManager.cs
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
using ICSharpCode.PackageManagement;
using NuGet;
using System.Collections.Generic;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeProjectManager : ISharpDevelopProjectManager
	{
		public FakePackageRepository FakeLocalRepository {
			get { return LocalRepository as FakePackageRepository; }
			set { LocalRepository = value; }
		}

		public FakePackageRepository FakeSourceRepository {
			get { return SourceRepository as FakePackageRepository; }
			set { SourceRepository = value; }
		}

		public bool IsInstalledReturnValue;

		public FakeProjectManager ()
		{
			LocalRepository = new FakePackageRepository ();
			SourceRepository = new FakePackageRepository ();
		}

		public event EventHandler<PackageOperationEventArgs> PackageReferenceAdded;

		protected virtual void OnPackageReferenceAdded (IPackage package)
		{
			if (PackageReferenceAdded != null) {
				PackageReferenceAdded (this, new PackageOperationEventArgs (package, null, String.Empty));
			}
		}

		public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoved;

		protected virtual void OnPackageReferenceRemoved (PackageOperationEventArgs eventArgs)
		{
			if (PackageReferenceRemoved != null) {
				PackageReferenceRemoved (this, eventArgs);
			}
		}

		#pragma warning disable 67
		public event EventHandler<PackageOperationEventArgs> PackageReferenceAdding;
		public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoving;
		#pragma warning restore 67

		public IPackageRepository LocalRepository { get; set; }

		public ILogger Logger { get; set; }

		public IPackageRepository SourceRepository { get; set; }

		public IPackagePathResolver PathResolver { get; set; }

		public IProjectSystem Project {
			get { return FakeProjectSystem; }
			set { FakeProjectSystem = value as FakeProjectSystem; }
		}

		public FakeProjectSystem FakeProjectSystem = new FakeProjectSystem ();

		public void RemovePackageReference (string packageId, bool forceRemove, bool removeDependencies)
		{
			throw new NotImplementedException ();
		}

		public IPackage PackagePassedToIsInstalled;

		public bool IsInstalled (IPackage package)
		{
			PackagePassedToIsInstalled = package;
			return IsInstalledReturnValue;
		}

		public string PackageIdPassedToIsInstalled;

		public bool IsInstalled (string packageId)
		{
			PackageIdPassedToIsInstalled = packageId;
			return IsInstalledReturnValue;
		}

		public void FirePackageReferenceAdded (IPackage package)
		{
			OnPackageReferenceAdded (package);
		}

		public void FirePackageReferenceRemoved (IPackage package)
		{
			FirePackageReferenceRemoved (new PackageOperationEventArgs (package, null, String.Empty));
		}

		public void FirePackageReferenceRemoved (PackageOperationEventArgs eventArgs)
		{
			OnPackageReferenceRemoved (eventArgs);
		}

		public void AddPackageReference (IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			throw new NotImplementedException ();
		}

		public void RemovePackageReference (IPackage package, bool forceRemove, bool removeDependencies)
		{
			throw new NotImplementedException ();
		}

		public void AddPackageReference (string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			string key = packageId + version.ToString ();
			if (addPackageReferenceCallbacks.ContainsKey (key)) {
				Action callback = addPackageReferenceCallbacks [key];
				callback ();
			}
		}

		Dictionary<string, Action> addPackageReferenceCallbacks = new Dictionary<string, Action> ();

		public void WhenAddPackageReferenceCalled (string id, SemanticVersion version, Action callback)
		{
			string key = id + version.ToString ();
			addPackageReferenceCallbacks.Add (key, callback);
		}

		public void UpdatePackageReference (string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions)
		{
			string key = packageId + version.ToString ();
			if (updatePackageReferenceCallbacks.ContainsKey (key)) {
				Action callback = updatePackageReferenceCallbacks [key];
				callback ();
			}
		}

		Dictionary<string, Action> updatePackageReferenceCallbacks = new Dictionary<string, Action> ();

		public void WhenUpdatePackageReferenceCalled (string id, SemanticVersion version, Action callback)
		{
			string key = id + version.ToString ();
			updatePackageReferenceCallbacks.Add (key, callback);
		}

		public void UpdatePackageReference (string packageId, IVersionSpec versionSpec, bool updateDependencies, bool allowPrereleaseVersions)
		{
			throw new NotImplementedException ();
		}

		public IPackage PackagePassedToHasOlderPackageInstalled;
		public bool HasOlderPackageInstalledReturnValue;

		public bool HasOlderPackageInstalled (IPackage package)
		{
			PackagePassedToHasOlderPackageInstalled = package;
			return HasOlderPackageInstalledReturnValue;
		}

		public DependencyVersion DependencyVersion {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public bool WhatIf {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public void UpdatePackageReference (IPackage remotePackage, bool updateDependencies, bool allowPrereleaseVersions)
		{
			throw new NotImplementedException ();
		}

		public List<PackageReference> PackageReferences = new List<PackageReference> ();

		public PackageReference AddPackageReference (string packageId, string packageVersion)
		{
			var packageReference = new PackageReference (
				packageId,
				new SemanticVersion (packageVersion),
				null,
				null,
				false,
				false);

			PackageReferences.Add (packageReference);

			return packageReference;
		}

		public IEnumerable<PackageReference> GetPackageReferences ()
		{
			return PackageReferences;
		}
	}
}
