// 
// ReducedPackageOperations.cs
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
using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class ReducedPackageOperations
	{
		IPackageOperationResolver resolver;
		IList<PackageOperation> operations;
		IEnumerable<IPackage> packages;
		
		public ReducedPackageOperations(IPackageOperationResolver resolver, IEnumerable<IPackage> packages)
		{
			this.resolver = resolver;
			this.packages = packages;
			this.operations = new List<PackageOperation>();
		}
		
		public IEnumerable<PackageOperation> Operations {
			get { return operations; }
		}
		
		public void Reduce()
		{
			foreach (IPackage package in packages) {
				if (!InstallOperationExists(package)) {
					operations.AddRange(resolver.ResolveOperations(package));
				}
			}
			
			operations = operations.Reduce();
		}
		
		bool InstallOperationExists(IPackage package)
		{
			var installOperation = new PackageOperation(package, PackageAction.Install);
			return operations.Any(operation => IsMatch(installOperation, operation));
		}
		
		bool IsMatch(PackageOperation x, PackageOperation y)
		{
			return (x.Package.Id == y.Package.Id) &&
				(x.Package.Version == y.Package.Version) &&
				(x.Action == y.Action);
		}
	}
}