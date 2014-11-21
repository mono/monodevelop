// 
// IPackageRepositoryExtensions.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public static class IPackageRepositoryExtensions
	{
		public static IDisposable StartInstallOperation(this IPackageRepository repository, string mainPackageId = null, string mainPackageVersion = null)
		{
			return repository.StartOperation(RepositoryOperationNames.Install, mainPackageId, mainPackageVersion);
		}
		
		public static IDisposable StartUpdateOperation(this IPackageRepository repository, string mainPackageId = null, string mainPackageVersion = null)
		{
			return repository.StartOperation(RepositoryOperationNames.Update, mainPackageId, mainPackageVersion);
		}

		public static IDisposable StartRestoreOperation (this IPackageRepository repository, string mainPackageId = null, string mainPackageVersion = null)
		{
			return repository.StartOperation (RepositoryOperationNames.Restore, mainPackageId, mainPackageVersion);
		}
	}
}
