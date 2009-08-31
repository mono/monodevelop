// 
// RemoveBackingStoreTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using NUnit.Framework;
using MonoDevelop.Refactoring;
using MonoDevelop.Refactoring.ConvertPropery;
using System.Collections.Generic;

namespace MonoDevelop.Refactoring.Tests
{
	[TestFixture()]
	public class RemoveBackingStoreTests : UnitTests.TestBase
	{
		void TestBackingStore (string inputString, string outputString)
		{
			RemoveBackingStore refactoring = new RemoveBackingStore ();
			RefactoringOptions options = ExtractMethodTests.CreateRefactoringOptions (inputString);
			List<Change> changes = refactoring.PerformChanges (options, null);
			string output = ExtractMethodTests.GetOutput (options, changes);
			Assert.IsTrue (ExtractMethodTests.CompareSource (output, outputString), "Expected:" + Environment.NewLine + outputString + Environment.NewLine + "was:" + Environment.NewLine + output);
		}
		
		[Test()]
		public void RemoveBackingStoreTest ()
		{
			TestBackingStore (@"class TestClass
{
	int index;
	public int $Index {
		get {
			return index;
		}
		private set {
			index = value;
		}
	}
	
	void PrintIndex ()
	{
		Console.WriteLine (index);
	}
}
", @"class TestClass
{
	public int Index {
		get;
		private set;
	}
	void PrintIndex ()
	{
		Console.WriteLine (Index);
	}
}
");
		}
		
	}
}
