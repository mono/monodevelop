//
// BreakpointsAndSteppingTests.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

using NUnit.Framework;

using Mono.Debugging.Client;

using MonoDevelop.PerformanceTesting;

namespace Mono.Debugging.PerfTests
{
	public abstract class SteppingTests : PerfTestBase
	{
		protected SteppingTests (string de)
			: base (de)
		{
		}

		[OneTimeSetUp]
		public override void SetUp ()
		{
			base.SetUp ();
			Start ("BreakpointsAndStepping");
		}

		[Test]
		[Benchmark (Tolerance = 0.1)]
		public void OneLineProperty ()
		{
			InitializeTest ();
			AddBreakpoint ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
			StartTest ("OneLineProperty");
			CheckPosition ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");

			BenchmarkAction (() => {
				StepIn ("3722cad3-7da1-4c86-a398-bb2cf6cc65a9", "{");
				StepIn ("3722cad3-7da1-4c86-a398-bb2cf6cc65a9", "return");
				StepIn ("3722cad3-7da1-4c86-a398-bb2cf6cc65a9", "}");
				StepIn ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
				StepIn ("36c0a44a-44ac-4676-b99b-9a58b73bae9d");
			});
		}

		[Test]
		[Benchmark (Tolerance = 0.1)]
		public void StepOverPropertiesAndOperators1 ()
		{
			InitializeTest ();
			Session.Options.StepOverPropertiesAndOperators = true;
			AddBreakpoint ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
			StartTest ("OneLineProperty");
			CheckPosition ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");

			BenchmarkAction (() => {
				StepIn ("36c0a44a-44ac-4676-b99b-9a58b73bae9d");
			});
		}

		[Test]
		[Benchmark (Tolerance = 0.1)]
		public void StepOverPropertiesAndOperators2 ()
		{
			InitializeTest ();
			Session.Options.StepOverPropertiesAndOperators = true;
			AddBreakpoint ("6049ea77-e04a-43ba-907a-5d198727c448");
			StartTest ("TestOperators");
			CheckPosition ("6049ea77-e04a-43ba-907a-5d198727c448");

			BenchmarkAction (() => {
				StepIn ("49737db6-e62b-4c5e-8758-1a9d655be11a");
			});
		}

		[Test]
		[Benchmark (Tolerance = 0.1)]
		public void StaticConstructorStepping ()
		{
			InitializeTest ();
			AddBreakpoint ("6c42f31b-ca4f-4963-bca1-7d7c163087f1");
			StartTest ("StaticConstructorStepping");
			CheckPosition ("6c42f31b-ca4f-4963-bca1-7d7c163087f1");

			BenchmarkAction (() => {
				StepOver ("7e6862cd-bf31-486c-94fe-19933ae46094");
			});
		}
	}
}
