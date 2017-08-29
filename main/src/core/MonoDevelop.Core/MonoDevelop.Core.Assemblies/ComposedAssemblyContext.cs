// 
// ComposedAssemblyContext.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MonoDevelop.Core.Assemblies
{
	public class ComposedAssemblyContext: IAssemblyContext
	{
		ImmutableList<IAssemblyContext> sources = ImmutableList<IAssemblyContext>.Empty;
		
		public void Add (IAssemblyContext ctx)
		{
			sources = sources.Add (ctx);
			ctx.Changed += CtxChanged;
			CtxChanged (null, null);
		}
		
		public void Remove (IAssemblyContext ctx)
		{
			var prev = sources;
			sources = sources.Remove (ctx);
			if (sources != prev) {
				ctx.Changed -= CtxChanged;
				CtxChanged (null, null);
			}
		}
		
		public void Replace (IAssemblyContext oldCtx, IAssemblyContext newCtx)
		{
			int i = sources.IndexOf (oldCtx);
			if (i != -1) {
				sources = sources.SetItem (i, newCtx);
				CtxChanged (null, null);
			}
		}
		
		public void Dispose ()
		{
			foreach (IAssemblyContext ctx in sources)
				ctx.Changed -= CtxChanged;
		}

		void CtxChanged (object sender, EventArgs e)
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public event EventHandler Changed;

		[Obsolete("Avoid use of SystemPackage")]
		public IEnumerable<SystemPackage> GetPackages ()
		{
			foreach (IAssemblyContext ctx in sources) {
				foreach (SystemPackage p in ctx.GetPackages ())
					yield return p;
			}
		}

		[Obsolete("Avoid use of SystemPackage")]
		public IEnumerable<SystemPackage> GetPackages (TargetFramework fx)
		{
			foreach (IAssemblyContext ctx in sources) {
				foreach (SystemPackage p in ctx.GetPackages (fx))
					yield return p;
			}
		}
		
		public SystemAssembly[] GetAssembliesFromFullName (string fullname)
		{
			List<SystemAssembly> list = new List<SystemAssembly> ();
			foreach (IAssemblyContext ctx in sources) {
				foreach (SystemAssembly sa in ctx.GetAssembliesFromFullName (fullname))
					list.Add (sa);
			}
			return list.ToArray ();
		}
		
		public IEnumerable<SystemAssembly> GetAssemblies ()
		{
			foreach (IAssemblyContext ctx in sources) {
				foreach (SystemAssembly sa in ctx.GetAssemblies ())
					yield return sa;
			}
		}
		
		public IEnumerable<SystemAssembly> GetAssemblies (TargetFramework fx)
		{
			foreach (IAssemblyContext ctx in sources) {
				foreach (SystemAssembly sa in ctx.GetAssemblies (fx))
					yield return sa;
			}
		}
		
		public SystemAssembly GetAssemblyFromFullName (string fullname, string package, TargetFramework fx)
		{
			foreach (IAssemblyContext ctx in sources) {
				SystemAssembly sa = ctx.GetAssemblyFromFullName (fullname, package, fx);
				if (sa != null)
					return sa;
			}
			return null;
		}
		
		public SystemPackage[] GetPackagesFromFullName (string fullname)
		{
			List<SystemPackage> list = new List<SystemPackage> ();
			foreach (IAssemblyContext ctx in sources) {
				foreach (SystemPackage p in ctx.GetPackagesFromFullName (fullname))
					list.Add (p);
			}
			return list.ToArray ();
		}
		
		public SystemPackage GetPackage (string name)
		{
			foreach (IAssemblyContext ctx in sources) {
				SystemPackage sp = ctx.GetPackage (name);
				if (sp != null)
					return sp;
			}
			return null;
		}
		
		public SystemPackage GetPackage (string name, string version)
		{
			foreach (IAssemblyContext ctx in sources) {
				SystemPackage sp = ctx.GetPackage (name, version);
				if (sp != null)
					return sp;
			}
			return null;
		}
		
		public SystemPackage GetPackageFromPath (string path)
		{
			foreach (IAssemblyContext ctx in sources) {
				SystemPackage sp = ctx.GetPackageFromPath (path);
				if (sp != null)
					return sp;
			}
			return null;
		}
		
		public string FindInstalledAssembly (string fullname, string package, TargetFramework fx)
		{
			foreach (IAssemblyContext ctx in sources) {
				string asm = ctx.FindInstalledAssembly (fullname, package, fx);
				if (asm != null)
					return asm;
			}
			return null;
		}
		
		public string GetAssemblyLocation (string assemblyName, TargetFramework fx)
		{
			return GetAssemblyLocation (assemblyName, null, fx);
		}
		
		public string GetAssemblyLocation (string assemblyName, string package, TargetFramework fx)
		{
			foreach (IAssemblyContext ctx in sources) {
				string asm = ctx.GetAssemblyLocation (assemblyName, package, fx);
				if (asm != null)
					return asm;
			}
			return null;
		}
		
		public bool AssemblyIsInGac (string aname)
		{
			foreach (IAssemblyContext ctx in sources) {
				if (ctx.AssemblyIsInGac (aname))
					return true;
			}
			return false;
		}
		
		public string GetAssemblyNameForVersion (string fullName, TargetFramework fx)
		{
			return GetAssemblyNameForVersion (fullName, null, fx);
		}
		
		public string GetAssemblyNameForVersion (string fullName, string packageName, TargetFramework fx)
		{
			foreach (IAssemblyContext ctx in sources) {
				string asm = ctx.GetAssemblyNameForVersion (fullName, packageName, fx);
				if (asm != null)
					return asm;
			}
			return null;
		}
		
		public SystemAssembly GetAssemblyForVersion (string fullName, string packageName, TargetFramework fx)
		{
			foreach (IAssemblyContext ctx in sources) {
				SystemAssembly asm = ctx.GetAssemblyForVersion (fullName, packageName, fx);
				if (asm != null)
					return asm;
			}
			return null;
		}
		
		public string GetAssemblyFullName (string assemblyName, TargetFramework fx)
		{
			foreach (IAssemblyContext ctx in sources) {
				string asm = ctx.GetAssemblyFullName (assemblyName, fx);
				if (asm != null)
					return asm;
			}
			return null;
		}
	}
}
