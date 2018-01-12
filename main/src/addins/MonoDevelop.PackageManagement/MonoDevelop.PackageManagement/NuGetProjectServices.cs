//
// NuGetProjectServices.cs
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

using System;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	class NuGetProjectServices : INuGetProjectServices, IProjectSystemService, IProjectScriptHostService
	{
		IProjectSystemReferencesReader referencesReader;

		public NuGetProjectServices (DotNetProject project)
		{
			referencesReader = new ProjectSystemReferencesReader (project);
		}

		public IProjectBuildProperties BuildProperties => throw new NotImplementedException ();

		public IProjectSystemCapabilities Capabilities => throw new NotImplementedException ();

		public IProjectSystemReferencesReader ReferencesReader {
			get { return referencesReader; }
		}

		public IProjectSystemReferencesService References => throw new NotImplementedException ();

		public IProjectSystemService ProjectSystem {
			get { return this; }
		}

		public IProjectScriptHostService ScriptService {
			get { return this; }
		}

		public T GetGlobalService<T> () where T : class
		{
			return null;
		}

		public Task SaveProjectAsync (CancellationToken token)
		{
			return Task.FromResult (0);
		}

		public Task<bool> ExecutePackageInitScriptAsync (
			PackageIdentity packageIdentity,
			string packageInstallPath,
			INuGetProjectContext projectContext,
			bool throwOnFailure,
			CancellationToken token)
		{
			return Task.FromResult (false);
		}

		public Task ExecutePackageScriptAsync (
			PackageIdentity packageIdentity,
			string packageInstallPath,
			string scriptRelativePath,
			INuGetProjectContext projectContext,
			bool throwOnFailure,
			CancellationToken token)
		{
			return Task.FromResult (0);
		}
	}
}
