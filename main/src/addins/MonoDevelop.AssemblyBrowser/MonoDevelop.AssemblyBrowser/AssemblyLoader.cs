// 
// AssemblyWrapper.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using Mono.Cecil;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.IO;
using System.Threading;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyLoader : IAssemblyResolver, IDisposable
	{
		readonly CancellationTokenSource src = new CancellationTokenSource ();
		readonly AssemblyBrowserWidget widget;
		
		public string FileName {
			get;
			private set;
		}
		
		Task<AssemblyDefinition> assemblyLoaderTask;

		public Task<AssemblyDefinition> LoadingTask {
			get {
				return assemblyLoaderTask;
			}
		}

		public AssemblyDefinition Assembly {
			get {
				return assemblyLoaderTask.Result;
			}
		}
		
		readonly Lazy<IUnresolvedAssembly> unresolvedAssembly;
		public IUnresolvedAssembly UnresolvedAssembly {
			get {
				return unresolvedAssembly.Value;
			}
		}
		
		public AssemblyLoader (AssemblyBrowserWidget widget, string fileName)
		{
			if (widget == null)
				throw new ArgumentNullException ("widget");
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			this.widget = widget;
			this.FileName = fileName;
			if (!File.Exists (fileName))
				throw new ArgumentException ("File doesn't exist.", "fileName");
			this.assemblyLoaderTask = Task.Factory.StartNew<AssemblyDefinition> (() => {
				return AssemblyDefinition.ReadAssembly (FileName, new ReaderParameters () {
					AssemblyResolver = this
				});
			}, src.Token);
			
			this.unresolvedAssembly = new Lazy<IUnresolvedAssembly> (delegate {
				try {
					return widget.CecilLoader.LoadAssembly (Assembly);
				} catch (Exception e) {
					LoggingService.LogError ("Error while loading assembly", e);
					return new ICSharpCode.NRefactory.TypeSystem.Implementation.DefaultUnresolvedAssembly (FileName);
				}
			});
		}
		
		#region IAssemblyResolver implementation
		AssemblyDefinition IAssemblyResolver.Resolve (AssemblyNameReference name)
		{
			var loader = widget.AddReferenceByAssemblyName (name, false);
			return loader != null ? loader.Assembly : null;
		}
		
		AssemblyDefinition IAssemblyResolver.Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			var loader = widget.AddReferenceByAssemblyName (name, false);
			return loader != null ? loader.Assembly : null;
		}
		
		AssemblyDefinition IAssemblyResolver.Resolve (string fullName)
		{
			var loader = widget.AddReferenceByAssemblyName (fullName, false);
			return loader != null ? loader.Assembly : null;
		}
		
		AssemblyDefinition IAssemblyResolver.Resolve (string fullName, ReaderParameters parameters)
		{
			var loader = widget.AddReferenceByAssemblyName (fullName, false);
			return loader != null ? loader.Assembly : null;
		}
		#endregion
		
		public string LookupAssembly (string fullAssemblyName)
		{
			var assemblyFile = Runtime.SystemAssemblyService.DefaultAssemblyContext.GetAssemblyLocation (fullAssemblyName, null);
			if (assemblyFile != null && System.IO.File.Exists (assemblyFile))
				return assemblyFile;
			
			var name = AssemblyNameReference.Parse (fullAssemblyName);
			var path = Path.GetDirectoryName (FileName);
			
			var dll = Path.Combine (path, name.Name + ".dll");
			if (File.Exists (dll))
				return dll;
			var exe = Path.Combine (path, name.Name + ".exe");
			if (File.Exists (exe))
				return exe;
			return null;
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			if (assemblyLoaderTask == null)
				return;
			src.Cancel ();
			src.Dispose ();
			assemblyLoaderTask = null;
		}
		#endregion
	}
}
