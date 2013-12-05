// 
// RecyclableAppDomain.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com_
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

namespace Mono.TextTemplating
{
	public class TemplatingAppDomainRecycler
	{
		const int DEFAULT_TIMEOUT_MS = 2 * 60 * 1000;
		const int DEFAULT_MAX_USES = 20;
		
		readonly string name;
		readonly object lockObj = new object ();
		
		RecyclableAppDomain domain;
		
		public TemplatingAppDomainRecycler (string name)
		{
			this.name = name;
		}
		
		public TemplatingAppDomainRecycler.Handle GetHandle ()
		{
			lock (lockObj) {
				if (domain == null || domain.UnusedHandles == 0) {
					domain = new RecyclableAppDomain (name);
				}
				return domain.GetHandle ();
			}
		}
		
		internal class RecyclableAppDomain
		{
			//TODO: implement timeout based recycling
			//DateTime lastUsed;
			
			AppDomain domain;
            DomainAssemblyLoader assemblyMap;
			
			int liveHandles;
			int unusedHandles = DEFAULT_MAX_USES;
			
			public RecyclableAppDomain (string name)
			{
				var info = new AppDomainSetup () {
					//appbase needs to allow loading this assembly, for remoting
					ApplicationBase = System.IO.Path.GetDirectoryName (typeof (TemplatingAppDomainRecycler).Assembly.Location),
					DisallowBindingRedirects = false,
					DisallowCodeDownload = true,
                    DisallowApplicationBaseProbing = false,
					ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
				};
				domain = AppDomain.CreateDomain (name, null, info);
                var t = typeof(DomainAssemblyLoader);
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                assemblyMap = (DomainAssemblyLoader) domain.CreateInstanceFromAndUnwrap(t.Assembly.Location, t.FullName);
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                domain.AssemblyResolve += assemblyMap.Resolve;// new DomainAssemblyLoader(assemblyMap).Resolve;
			}

            System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            {
                var a = typeof(RecyclableAppDomain).Assembly;
                if (args.Name == a.FullName)
                    return a;
                return null;
            }
			
			public int UnusedHandles { get { return unusedHandles; } }
			public int LiveHandles { get { return liveHandles; } }
			public AppDomain Domain { get { return domain; } }
			
			public void AddAssembly (System.Reflection.Assembly assembly)
			{
				assemblyMap.Add (assembly.FullName, assembly.Location);
			}
			
			public Handle GetHandle ()
			{
				lock (this) {
					if (unusedHandles <= 0) {
						throw new InvalidOperationException ("No handles left");
					}
					unusedHandles--;
					liveHandles++;
				}
				return new Handle (this);
			}
			
			public void ReleaseHandle ()
			{
				int lh;
				lock (this) {
					liveHandles--;
					lh = liveHandles;
				}
				if (unusedHandles == 0 && lh == 0) {
					UnloadDomain ();
				}
			}
			
			void UnloadDomain ()
			{
				AppDomain.Unload (domain);
				domain = null;
				assemblyMap = null;
				GC.SuppressFinalize (this);
			}
			
			~RecyclableAppDomain ()
			{
				if (liveHandles != 0)
					Console.WriteLine ("WARNING: recyclable AppDomain's handles were not all disposed");
			}
		}
		
		public class Handle : IDisposable
		{
			RecyclableAppDomain parent;
			
			internal Handle (RecyclableAppDomain parent)
			{
				this.parent = parent;
			}
			
			public AppDomain Domain {
				get { return parent.Domain; }
			}
			
			public void Dispose ()
			{
				if (parent == null)
					return;
				var p = parent;
				lock (this) {
					if (parent == null)
						return;
					parent = null;
				}
				p.ReleaseHandle ();
			}
			
			public void AddAssembly (System.Reflection.Assembly assembly)
			{
				parent.AddAssembly (assembly);
			}
		}
		
		[Serializable]
        class DomainAssemblyLoader : MarshalByRefObject
		{
            readonly Dictionary<string, string> map = new Dictionary<string, string>();
			
			public DomainAssemblyLoader ()
			{
			}
			
			public System.Reflection.Assembly Resolve (object sender, ResolveEventArgs args)
			{
				var assemblyFile = ResolveAssembly (args.Name);
				if (assemblyFile != null)
					return System.Reflection.Assembly.LoadFrom (assemblyFile);
				return null;
			}

            public string ResolveAssembly(string name)
            {
                string result;
                if (map.TryGetValue(name, out result))
                    return result;
                return null;
            }

            public void Add(string name, string location)
            {
                map[name] = location;
            }
		}
	}
}