//
// MyClass.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.IO;
using System.Reflection;
using MonoDevelop.Core;
using System.Collections.Generic;
using Mono.Addins;
using System.Linq;
using Mono.Addins.Description;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Tests.TestRunner
{
	public class Runer: IApplication
	{
		public Task<int> Run (string[] arguments)
		{
			Func<List<string>, int> runTests = args => RunNUnit (args);

			var args = new List<string> (arguments);
			var argsToAdd = new List<string> ();

			foreach (var ar in args) {
				if (ar == "--performance") {
					argsToAdd.Add ("-include=Performance");
					continue;
				}

				if ((ar.EndsWith (".dll", StringComparison.OrdinalIgnoreCase) || ar.EndsWith (".exe", StringComparison.OrdinalIgnoreCase)) && File.Exists (ar)) {
					try {
						var path = Path.GetFullPath (ar);
						var asm = Assembly.LoadFrom (path);
						var ids = new HashSet<string> ();
						foreach (var aname in asm.GetReferencedAssemblies ()) {
							if (aname.Name == "GuiUnit") {
								var guiUnitAsm = Assembly.LoadFile (Path.Combine (Path.GetDirectoryName (path), "GuiUnit.exe"));
								runTests = args => RunGuiUnit (args, guiUnitAsm);
								continue;
							}
							ids.UnionWith (GetAddinsFromReferences (aname));
						}

						foreach (var id in ids)
							AddinManager.LoadAddin (new ConsoleProgressStatus (false), id);

					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
				}
			}

			args.AddRange (argsToAdd);

			// Make sure the updater is disabled while running tests
			Runtime.Preferences.EnableUpdaterForCurrentSession = false;

			var result = runTests (args);

			// run performance analysis

			return Task.FromResult (result);
		}

		static int RunGuiUnit (List<string> args, Assembly guiUnitAsm)
		{
			Xwt.XwtSynchronizationContext.AutoInstall = false;
			SynchronizationContext.SetSynchronizationContext (new Xwt.XwtSynchronizationContext ());
			Runtime.MainSynchronizationContext = SynchronizationContext.Current;

			var method = guiUnitAsm.EntryPoint;
			return (int)method.Invoke (null, new [] { args.ToArray () });
		}

		static int RunNUnit (List<string> args)
		{
			args.RemoveAll (a => a.StartsWith ("-port=", StringComparison.Ordinal));
			args.Add ("-domain=None");
			return NUnit.ConsoleRunner.Runner.Main (args.ToArray ());
		}

		static IEnumerable<string> GetAddinsFromReferences (AssemblyName aname)
		{
			foreach (var adn in AddinManager.Registry.GetAddins ().Union (AddinManager.Registry.GetAddinRoots ())) {
				foreach (ModuleDescription m in adn.Description.AllModules) {
					bool found = false;
					foreach (var sname in m.Assemblies) {
						if (Path.GetFileNameWithoutExtension (sname) == aname.Name) {
							found = true;
							break;
						}
					}
					if (found) {
						yield return Addin.GetIdName (adn.Id);
						break;
					}
				}
			}
		}
	}
}

