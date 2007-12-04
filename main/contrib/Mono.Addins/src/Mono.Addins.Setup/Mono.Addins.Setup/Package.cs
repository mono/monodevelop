//
// Package.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using Mono.Addins.Description;

namespace Mono.Addins.Setup
{
	public abstract class Package
	{
		internal Package ()
		{
		}
		
		public abstract string Name { get; }
		
		// Returns true if the package will be installed in the shared directory,
		// false if it will be installed in the user directory.
		public virtual bool SharedInstall {
			get { return false; }
		}
		
		public static Package FromRepository (AddinRepositoryEntry repAddin)
		{
			return AddinPackage.PackageFromRepository (repAddin);
		}
		
		public static Package FromFile (string file)
		{
			return AddinPackage.PackageFromFile (file);
		}
		
		
		internal abstract void Resolve (IProgressMonitor monitor, AddinStore service, PackageCollection toInstall, PackageCollection toUninstall, PackageCollection required, DependencyCollection unresolved);
		
		internal abstract void PrepareInstall (IProgressMonitor monitor, AddinStore service);
		internal abstract void CommitInstall (IProgressMonitor monitor, AddinStore service);
		internal abstract void RollbackInstall (IProgressMonitor monitor, AddinStore service);
		
		internal abstract void PrepareUninstall (IProgressMonitor monitor, AddinStore service);
		internal abstract void CommitUninstall (IProgressMonitor monitor, AddinStore service);
		internal abstract void RollbackUninstall (IProgressMonitor monitor, AddinStore service);
		
		internal abstract bool IsUpgradeOf (Package p);
		
		internal virtual void EndInstall (IProgressMonitor monitor, AddinStore service)
		{
		}
		
		internal virtual void EndUninstall (IProgressMonitor monitor, AddinStore service)
		{
		}
		
		internal string CreateTempFolder ()
		{
			string bname = Path.Combine (Path.GetTempPath (), "mdtmp");
			string tempFolder = bname;
			int n = 0;
			while (Directory.Exists (tempFolder))
				tempFolder = bname + (++n);
			return tempFolder;
		}
	}
}
