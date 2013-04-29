//
// InternalTestRunner.cs
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
using Mono.Addins;

namespace MonoDevelop.Core
{
	/// <summary>
	/// This runner is used to run the MonoDevelop tests from inside MonoDevelop.
	/// The class has to be referenced in the csproj of all NUnit test projects.
	/// </summary>
	class InternalTestRunner: NUnit.Core.SimpleTestRunner
	{
		public InternalTestRunner ()
		{
			Runtime.Initialize (true);
			AddinManager.LoadAddin (null, "MonoDevelop.TestRunner");
		}

		public override NUnit.Core.TestResult EndRun ()
		{
			var r = base.EndRun ();
			Runtime.Shutdown ();
			return r;
		}
	}
}

