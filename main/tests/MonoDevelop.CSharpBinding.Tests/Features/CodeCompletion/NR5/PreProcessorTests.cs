// 
// PreProcessorTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion
{
	public class PreProcessorTests: TestBase
	{
		[Test]
		public void TestPreProcessorContext ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$#$", provider => {
				Assert.IsNotNull (provider.Find ("if"), "directive 'if' not found.");
				Assert.IsNotNull (provider.Find ("region"), "directive 'region' not found.");
			});
		}
		
		[Test]
		public void TestPreProcessorContext2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"// $#$", provider => {
				Assert.IsNull (provider.Find ("if"), "directive 'if' not found.");
				Assert.IsNull (provider.Find ("region"), "directive 'region' not found.");
			});
		}
		
		
		[Test]
		public void TestIfContext ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$#if D$", provider => {
				Assert.IsNotNull (provider.Find ("DEBUG"), "define 'DEBUG' not found.");
			});
		}	

		[Test]
		public void TestIfInsideComment ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$// #if D$", provider => {
				Assert.IsNull (provider.Find ("DEBUG"), "define 'DEBUG' found.");
			});
		}

		/// <summary>
		/// Bug 10051 - Cannot type negate conditional
		/// </summary>
		[Test]
		public void TestBug10051 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$#if $");
			Assert.IsNotNull (provider.Find ("DEBUG"), "define 'DEBUG' not found.");

			provider = CodeCompletionBugTests.CreateProvider (@"$#if $", true);
			Assert.IsNotNull (provider.Find ("DEBUG"), "define 'DEBUG' not found.");
		}	

		/// <summary>
		///Bug 10079 - Cannot type && conditional
		/// </summary>
		[Test]
		public void TestBug10079 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$#if TRUE &$");
			Assert.IsTrue (provider == null || provider.Count == 0);
			
			provider = CodeCompletionBugTests.CreateProvider (@"$#if TRUE && $", true);
			Assert.IsNotNull (provider.Find ("DEBUG"), "define 'DEBUG' not found.");
		}	


		/// <summary>
		/// Bug 10294 - Comments in preprocessor directives are not handler correctly
		/// </summary>
		[Test]
		public void TestBug10294 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$#if TRUE // D$",true);
			Assert.IsTrue (provider == null || provider.Count == 0);
		}
	}
}
