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
using MonoDevelop.Tests.TestRunner.TestModel;

namespace MonoDevelop.Tests.TestRunner
{
	public class Runer: IApplication
	{
		public Task<int> Run (string[] arguments)
		{
			Func<List<string>, int> runTests = args => RunNUnit (args);

			var args = new List<string> (arguments);
			bool isPerformanceRun = false;
			string resultsXmlFile = null;

			foreach (var ar in args) {
				if (ar == "--performance") {
					isPerformanceRun = true;
					continue;
				}

				if (ar.StartsWith ("-xml=", StringComparison.OrdinalIgnoreCase)) {
					resultsXmlFile = ar.Substring ("-xml=".Length);
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

			string baselineXmlFile = null;
			if (isPerformanceRun) {
				if (resultsXmlFile == null) {
					Console.WriteLine ("Could not find the result xml file in the argument list (add -xml=TestResult_Assembly.dll.xml).");
					return Task.FromResult (1);
				}

				if (!TryGetBaseline (resultsXmlFile, out baselineXmlFile)) {
					Console.WriteLine ("Creating new baseline file at {0}", baselineXmlFile);
				} else
					Console.WriteLine ("Using baseline file '{0}'", baselineXmlFile);

				args.Remove ("--performance");
				args.Add ("--include=Performance");
			}

			// Make sure the updater is disabled while running tests
			Runtime.Preferences.EnableUpdaterForCurrentSession = false;

			var result = runTests (args);

			// run performance analysis if the test suite passed
			if (isPerformanceRun && result == 0) {
				result = GenerateResults (baselineXmlFile, resultsXmlFile, resultsXmlFile + "_Report.dll.xml");
			}

			return Task.FromResult (result);
		}

		static bool TryGetBaseline (string resultsXmlFile, out string baselineXmlFile)
		{
			baselineXmlFile = "Baseline" + resultsXmlFile.Substring (resultsXmlFile.IndexOf ('_'));
			if (File.Exists (baselineXmlFile)) {
				return true;
			}

			var index = baselineXmlFile.LastIndexOf (".dll.xml", StringComparison.OrdinalIgnoreCase);
			if (index != -1) {
				baselineXmlFile = baselineXmlFile.Remove (index, ".dll.xml".Length) + ".xml";
				return File.Exists (baselineXmlFile);
			}

			return false;
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

		static int GenerateResults (string baseFile, string inputFile, string resultsFile)
		{
			var baseTestSuite = new TestSuiteResult ();
			if (File.Exists (baseFile))
				baseTestSuite.Read (baseFile);

			var inputTestSuite = new TestSuiteResult ();
			inputTestSuite.Read (inputFile);

			inputTestSuite.RegisterPerformanceRegressions (baseTestSuite, out var regressions, out var improvements, out var newTests);
			inputTestSuite.Write (resultsFile);

			PrintTestCases ("Performance Regressions:", regressions);
			PrintTestCases ("Performance Improvements:", improvements);
			PrintTestCases ("New Performance Tests:", newTests);

			return inputTestSuite.HasErrors ? 1 : 0;
		}

		static void PrintTestCases (string header, List<TestCase> testCases)
		{
			if (testCases.Count <= 0)
				return;

			Console.WriteLine (header);
			for (int n = 0; n < testCases.Count; n++) {
				var imp = testCases [n];
				var number = (n + 1) + ") ";
				var messageToWrite = imp.Improvement?.Message ?? imp.Failure.Message;

				Console.WriteLine (number + imp.Name);
				Console.WriteLine (new string (' ', number.Length) + messageToWrite);
			}
			Console.WriteLine ();
		}
	}
}

