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
using System.Threading.Tasks;
using NUnit.Framework;

namespace MonoDevelop.AspNet.Tests.WebForms
{
	[TestFixture]
	class WebFormsCompletionTests : UnitTests.TestBase
	{
		[Test]
		public async Task DirectiveCompletion ()
		{
			var provider = await WebFormsTesting.CreateProvider (@"<%@ $ %>", ".aspx");
			Assert.IsNotNull (provider);
			Assert.AreEqual (9, provider.Count);
			Assert.IsNotNull (provider.Find ("Page"));
			Assert.IsNotNull (provider.Find ("Register"));
			
			provider = await WebFormsTesting.CreateProvider (@"<%@ $ %>", ".master");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			Assert.IsNotNull (provider.Find ("Master"));
			Assert.IsNotNull (provider.Find ("Register"));
			
			provider = await WebFormsTesting.CreateProvider (@"<%@ $ %>", ".ascx");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			Assert.IsNotNull (provider.Find ("Control"));
			Assert.IsNotNull (provider.Find ("Register"));
			
			provider = await WebFormsTesting.CreateProvider (@"<%@$ %>", ".aspx");
			Assert.IsNull (provider);
			
			provider = await WebFormsTesting.CreateProvider (@"<%@   $ %>", ".aspx");
			Assert.IsNull (provider);
		}
		
		[Test]
		public async Task DirectiveAttributeCompletion ()
		{
			var provider = await WebFormsTesting.CreateProvider (@"<%@ Page A$ %>", ".aspx");
			Assert.IsNotNull (provider);
			Assert.AreEqual (41, provider.Count);
			Assert.IsNotNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("Inherits"));
			
			provider = await WebFormsTesting.CreateProvider (@"<%@ Master A$ %>", ".master");
			Assert.IsNotNull (provider);
			Assert.AreEqual (18, provider.Count);
			Assert.IsNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("MasterPageFile"));
			Assert.IsNotNull (provider.Find ("Inherits"));
			
			provider = await WebFormsTesting.CreateProvider (@"<%@ Control A$ %>", ".ascx");
			Assert.IsNotNull (provider);
			Assert.AreEqual (17, provider.Count);
			Assert.IsNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("Inherits"));
		}
		
		[Test]
		[Ignore ("Not working")]
		public async Task DirectiveAttributeCtrlSpaceCompletion ()
		{
			var provider = await WebFormsTesting.CreateProvider (@"<%@ Page $ %>", ".aspx", true);
			Assert.IsNotNull (provider);
			Assert.AreEqual (41, provider.Count);
			Assert.IsNotNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("Inherits"));
			
			provider = await WebFormsTesting.CreateProvider (@"<%@ Master $ %>", ".master", true);
			Assert.IsNotNull (provider);
			Assert.AreEqual (18, provider.Count);
			Assert.IsNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("MasterPageFile"));
			Assert.IsNotNull (provider.Find ("Inherits"));
			
			provider = await WebFormsTesting.CreateProvider (@"<%@ Control $ %>", ".ascx", true);
			Assert.IsNotNull (provider);
			Assert.AreEqual (17, provider.Count);
			Assert.IsNull (provider.Find ("StyleSheetTheme"));
			Assert.IsNotNull (provider.Find ("Inherits"));
		}

		const string pageStart = @"<%@ Page Language=""C#"" %>
<!DOCTYPE html>
<html>
";
		async Task HeadBodyCompletion (bool ctrlSpace)
		{
			var provider = await WebFormsTesting.CreateProvider (pageStart + "<$", ".aspx", ctrlSpace);
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("head"));
			Assert.IsNotNull (provider.Find ("body"));
			Assert.IsNotNull (provider.Find ("/html>"));
			Assert.IsNull (provider.Find ("div"));
			Assert.IsNotNull (provider.Find ("asp:Button"));
		}

		[Test]
		public async Task HeadBodyCompletionAuto ()
		{
			await HeadBodyCompletion (false);
		}

		[Test]
		public async Task HeadBodyCompletionCtrlSpace ()
		{
			await HeadBodyCompletion (true);
		}

		[Test]
		public async Task TagPropertiesAuto ()
		{
			var provider = await WebFormsTesting.CreateProvider (pageStart + "<asp:Button r$", ".aspx");
			Assert.IsNotNull (provider.Find ("runat=\"server\""));
			Assert.IsNotNull (provider.Find ("BorderStyle"));
		}

		[Test]
		public async Task TagPropertiesCtrlSpace ()
		{
			var provider = await WebFormsTesting.CreateProvider (pageStart + "<asp:Button $", ".aspx", true);
			Assert.IsNotNull (provider.Find ("runat=\"server\""));
			Assert.IsNotNull (provider.Find ("id"));
			Assert.IsNotNull (provider.Find ("BorderStyle"));
			Assert.IsNotNull (provider.Find ("OnClick"));
		}
	}
}
