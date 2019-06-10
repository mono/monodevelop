//
// MonoDevelopFrameworkAssemblyPathResolverFactoryTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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

using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.Composition;
using Microsoft.CodeAnalysis.Host;

namespace MonoDevelop.Ide.RoslynServices
{
	[TestFixture]
	public class MonoDevelopFrameworkAssemblyPathResolverFactoryTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;

		[Test]
		public async Task ServiceIsRegistered ()
		{
			using (var testCase = await SetupTestCase ("class MyTest {}")) {
				var doc = testCase.Document.DocumentContext;

				var service = doc.RoslynWorkspace.Services.GetService<IFrameworkAssemblyPathResolver> ();
				Assert.IsNotNull (service);
			}
		}

		[Test]
		public async Task TestSimpleCase ()
		{
			using (var testCase = await SetupTestCase ("class MyTest {}")) {
				var doc = testCase.Document.DocumentContext;

				var service = doc.RoslynWorkspace.Services.GetService<IFrameworkAssemblyPathResolver> ();
				string path = service.ResolveAssemblyPath (doc.AnalysisDocument.Project.Id, "System");
				Assert.IsNotNull (path);
			}
		}

	}
}
