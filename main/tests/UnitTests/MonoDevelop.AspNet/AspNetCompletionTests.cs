// 
// AspNetCompletion.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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

namespace MonoDevelop.AspNet.Tests
{

	[TestFixture]
	public class AspNetCompletionTests : UnitTests.TestBase
	{
		[Test]
		public void DirectiveCompletion ()
		{
			var provider = AspNetTesting.CreateProvider (@"<%@ $ %>", ".aspx", false);
			Assert.IsNotNull (provider);
			Assert.AreEqual (9, provider.Count);
			Assert.IsNotNull (provider.Find ("Page"));
			Assert.IsNotNull (provider.Find ("Register"));
			
			provider = AspNetTesting.CreateProvider (@"<%@ $ %>", ".master", false);
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			Assert.IsNotNull (provider.Find ("Master"));
			Assert.IsNotNull (provider.Find ("Register"));
			
			provider = AspNetTesting.CreateProvider (@"<%@ $ %>", ".ascx", false);
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			Assert.IsNotNull (provider.Find ("Control"));
			Assert.IsNotNull (provider.Find ("Register"));
			
			provider = AspNetTesting.CreateProvider (@"<%@$ %>", ".aspx", false);
			Assert.IsNull (provider);
			
			provider = AspNetTesting.CreateProvider (@"<%@   $ %>", ".aspx", false);
			Assert.IsNull (provider);
		}
		
		[Test]
		public void DirectiveAttributeCompletion ()
		{
			var provider = AspNetTesting.CreateProvider (@"<%@ Page A$ %>", ".aspx", false);
			Assert.IsNotNull (provider);
			Assert.AreEqual (41, provider.Count);
			Assert.IsNotNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("Inherits"));
			
			provider = AspNetTesting.CreateProvider (@"<%@ Master A$ %>", ".master", false);
			Assert.IsNotNull (provider);
			Assert.AreEqual (18, provider.Count);
			Assert.IsNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("MasterPageFile"));
			Assert.IsNotNull (provider.Find ("Inherits"));
			
			provider = AspNetTesting.CreateProvider (@"<%@ Control A$ %>", ".ascx", false);
			Assert.IsNotNull (provider);
			Assert.AreEqual (17, provider.Count);
			Assert.IsNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("Inherits"));
		}
		
		[Test]
		[Ignore ("Not working")]
		public void DirectiveAttributeCtrlSpaceCompletion ()
		{
			var provider = AspNetTesting.CreateProvider (@"<%@ Page $ %>", ".aspx", true);
			Assert.IsNotNull (provider);
			Assert.AreEqual (41, provider.Count);
			Assert.IsNotNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("Inherits"));
			
			provider = AspNetTesting.CreateProvider (@"<%@ Master $ %>", ".master", true);
			Assert.IsNotNull (provider);
			Assert.AreEqual (18, provider.Count);
			Assert.IsNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("MasterPageFile"));
			Assert.IsNotNull (provider.Find ("Inherits"));
			
			provider = AspNetTesting.CreateProvider (@"<%@ Control $ %>", ".ascx", true);
			Assert.IsNotNull (provider);
			Assert.AreEqual (17, provider.Count);
			Assert.IsNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("Inherits"));
		}
	}
}
