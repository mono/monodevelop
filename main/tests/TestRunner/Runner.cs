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

namespace MonoDevelop.Tests.TestRunner
{
	public class Runer: IApplication
	{
		public int Run (string[] arguments)
		{
			var list = new List<string> (arguments);
			list.Add ("-domain=None");

			foreach (var ar in arguments) {
				if ((ar.EndsWith (".dll") || ar.EndsWith (".exe")) && File.Exists (ar)) {
					try {
						var asm = Assembly.LoadFrom (ar);
						HashSet<string> ids = new HashSet<string> ();
						foreach (var aname in asm.GetReferencedAssemblies ())
							ids.UnionWith (GetAddinsFromReferences (aname));

						foreach (var id in ids)
							AddinManager.LoadAddin (new Mono.Addins.ConsoleProgressStatus (false), id);

					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
				}
			}
			return NUnit.ConsoleRunner.Runner.Main (list.ToArray ());
		}

		IEnumerable<string> GetAddinsFromReferences (AssemblyName aname)
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

