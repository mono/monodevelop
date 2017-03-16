﻿//
// MonoExecutionParameters.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class MonoExecutionParametersTests
	{
		[Test, TestCaseSource ("ExecutionParameters")]
		public void CheckWeTrimString (MonoExecutionParameters par)
		{
			string ops;
			par.GenerateOptions (new Dictionary<string, string> (), out ops);

			Assert.AreEqual (ops.Trim (), ops);
		}

		static MonoExecutionParameters [] ExecutionParameters = new[] {
			new MonoExecutionParameters (),
			new MonoExecutionParameters { MonoVerboseLevel = 3 },
			new MonoExecutionParameters { MonoVerboseLevel = 3, MonoDebugMode = true, },
			new MonoExecutionParameters { MonoDebugMode = true, MonoDebugCasts = true, },
			new MonoExecutionParameters { MonoDebugMode = true, MonoDebugCasts = true, MonoVerifyAll = true, MonoConfigFile = "test.config" },
		};
	}
}
