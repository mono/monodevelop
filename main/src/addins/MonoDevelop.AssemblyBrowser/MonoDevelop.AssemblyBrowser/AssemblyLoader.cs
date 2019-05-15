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
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using ICSharpCode.Decompiler.Metadata;
using System.Reflection.Metadata;
using System.Linq;
using ICSharpCode.Decompiler.TypeSystem.Implementation;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyLoader : IDisposable
	{
		readonly CancellationTokenSource src = new CancellationTokenSource ();
		readonly AssemblyBrowserWidget widget;

		public string FileName {
			get;
			private set;
		}

		Task<PEFile> assemblyLoaderTask;
		TaskCompletionSource<PEFile> assemblyDefinitionTaskSource;

		public Task<PEFile> LoadingTask {
			get {
				return assemblyLoaderTask;
			}
			set {
				assemblyLoaderTask = value;
			}
		}

		public PEFile Assembly => AssemblyTask.Result;
		public Task<PEFile> AssemblyTask => assemblyDefinitionTaskSource.Task;

		public MetadataReader ModuleDefinition {
			get {
				return assemblyLoaderTask.Result.Metadata;
			}
		}

		CSharpDecompiler csharpDecompiler;

		public CSharpDecompiler CSharpDecompiler {
			get {
				if (csharpDecompiler == null) {
					csharpDecompiler = new CSharpDecompiler (DecompilerTypeSystem, new ICSharpCode.Decompiler.DecompilerSettings ());
				}

				return csharpDecompiler;
			}
		}

		DecompilerTypeSystem decompilerTypeSystem;
		public DecompilerTypeSystem DecompilerTypeSystem { 
			get {
				LoadTypeSystem (Assembly);
				return decompilerTypeSystem;
			}
		}

		void LoadTypeSystem (PEFile peFile)
		{
			decompilerTypeSystem = new DecompilerTypeSystem (peFile, new AssemblyResolver (Assembly, widget));
		}

		public Error Error { get; internal set; }

		public bool IsLoaded { get; private set; }

		public AssemblyLoader (AssemblyBrowserWidget widget, string fileName)
		{
			if (widget == null)
				throw new ArgumentNullException (nameof (widget));
			if (fileName == null)
				throw new ArgumentNullException (nameof (fileName));
			this.widget = widget;
			FileName = fileName;
			if (!File.Exists (fileName))
				throw new ArgumentException ("File doesn't exist.", nameof (fileName));

			assemblyDefinitionTaskSource = new TaskCompletionSource<PEFile> ();

			assemblyLoaderTask = Task.Run (() => {
				try {
					var peFile = new PEFile (FileName, System.Reflection.PortableExecutable.PEStreamOptions.PrefetchEntireImage);
					assemblyDefinitionTaskSource.SetResult (peFile);
					return peFile;
				} catch (Exception e) {
					LoggingService.LogError ("Error while reading assembly " + FileName, e);
					Error = new Error(e.Message);
					assemblyDefinitionTaskSource.SetResult (null);
					return null;
				} finally { IsLoaded = true; }
			});
		}

		ICompilation typeSystem;

		public ICompilation GetMinimalTypeSystem ()
		{
			if (typeSystem != null)
				return typeSystem;
			var assembly = Assembly;
			if (assembly == null)
				return null;
			return typeSystem = new SimpleCompilation (assembly.WithOptions (TypeSystemOptions.Default | TypeSystemOptions.Uncached | TypeSystemOptions.KeepModifiers), MinimalCorlib.Instance);
		}

		class MyUniversalAssemblyResolver : UniversalAssemblyResolver
		{
			public MyUniversalAssemblyResolver (string mainAssemblyFileName, bool throwOnError, string targetFramework) : base (mainAssemblyFileName, throwOnError, targetFramework)
			{
			}
		}
		class AssemblyResolver : IAssemblyResolver
		{
			readonly PEFile assembly;
			readonly AssemblyBrowserWidget widget;

			public AssemblyResolver (PEFile assembly, AssemblyBrowserWidget widget)
			{
				this.assembly = assembly;
				this.widget = widget;
			}

			public PEFile Resolve (IAssemblyReference reference)
			{
				try {
					var targetFramework = assembly.Reader.DetectTargetFrameworkId () ?? "";
					var resolver = new MyUniversalAssemblyResolver (assembly.FileName, false, targetFramework);
					var fileName = resolver.FindAssemblyFile (reference);
					if (fileName != null && File.Exists (fileName))
						return widget.AddReferenceByFileName (fileName)?.Assembly;
				} catch (Exception e) {
					LoggingService.LogInternalError ($"Error while resolving assembly {reference.FullName} for {assembly.FileName}.", e);
				}

				return widget.AddReferenceByAssemblyName (reference.FullName)?.Assembly;
			}

			public PEFile ResolveModule (PEFile mainModule, string moduleName)
			{
				return widget.AddReferenceByFileName (mainModule.FileName)?.Assembly;
			}
		}

		class FastNonInterningProvider : InterningProvider
		{
			Dictionary<string, string> stringDict = new Dictionary<string, string> ();

			public override string Intern (string text)
			{
				if (text == null)
					return null;

				string output;
				if (stringDict.TryGetValue (text, out output))
					return output;
				stringDict [text] = text;
				return text;
			}

			public override ISupportsInterning Intern (ISupportsInterning obj)
			{
				return obj;
			}

			public override IList<T> InternList<T> (IList<T> list)
			{
				return list;
			}

			public override object InternValue (object obj)
			{
				return obj;
			}
		}

		public string LookupAssembly (string fullAssemblyName)
		{
			var assemblyFile = Runtime.SystemAssemblyService.DefaultAssemblyContext.GetAssemblyLocation (fullAssemblyName, null);
			if (assemblyFile != null && File.Exists (assemblyFile))
				return assemblyFile;

			var name = AssemblyNameReference.Parse (fullAssemblyName);
			var path = Path.GetDirectoryName (FileName);

			var dll = Path.Combine (path, name.Name + ".dll");
			if (File.Exists (dll))
				return dll;
			var exe = Path.Combine (path, name.Name + ".exe");
			if (File.Exists (exe))
				return exe;

			foreach (var asm in Runtime.SystemAssemblyService.DefaultAssemblyContext.GetAssemblies ()) {
				if (string.Equals (asm.Name, fullAssemblyName, StringComparison.OrdinalIgnoreCase))
					return asm.Location;
			}

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
