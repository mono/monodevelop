//
// PerfTool.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Tests.TestRunner.TestModel;

namespace MonoDevelop.Tests.TestRunner
{
	sealed class PerfTool : IApplication
	{
		public Task<int> Run (string [] arguments)
			=> Task.FromResult (RunInternal (arguments));

		static int RunInternal (string[] args)
		{
			if (args.Length == 0 || args.Length > 2) {
				return PrintHelp ();
			}

			var baseFile = args [0];
			var inputFile = args [1];

			return UpdateBaseLine (baseFile, inputFile);
		}

		static int UpdateBaseLine (string baseFile, string inputFile)
		{
			var inputTestSuite = new TestSuiteResult ();
			inputTestSuite.Read (inputFile);

			if (File.Exists (baseFile)) {
				var baseTestSuite = new TestSuiteResult ();
				baseTestSuite.Read (baseFile);
				inputTestSuite.UpgradeToBaseline (baseTestSuite);
			}

			inputTestSuite.Write (baseFile);

			return 0;
		}

		static int PrintHelp ()
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("update-baseline <base-file> <input-file> [testcase]");
			Console.WriteLine ("    Updates the results in base-file that have improved in input-file");

			return 1;
		}
	}
}
