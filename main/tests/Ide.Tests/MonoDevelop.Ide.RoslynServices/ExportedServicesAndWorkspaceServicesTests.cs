//
// ExportedServicesAndWorkspaceServicesTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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

namespace MonoDevelop.Ide.RoslynServices.Options
{
	[TestFixture]
	public class ExportedServicesAndWorkspaceServicesTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;

		[Test]
		public async Task ServiceIsRegistered ()
		{
			// Initialize MEF
			await CompositionManager.InitializeAsync ();

			CompositionManager.Instance.AssertExportsContains<IOptionPersister, MonoDevelopGlobalOptionPersister> ();
		}

		[Test]
		public async Task WorkspaceServiceIsRegistered ()
		{
			using (var testCase = await SetupTestCase ("class MyClass {}")) {
				var doc = testCase.Document;

				AssertWorkspaceService<INotificationService, MonoDevelopNotificationServiceFactory.MonoDevelopNotificationService> ();

				void AssertWorkspaceService<TExport, TActual> () where TExport : IWorkspaceService
				{
					var actual = doc.RoslynWorkspace.Services.GetService<TExport> ();
					Assert.That (actual, Is.TypeOf<TActual> ());
				}
			}
		}
	}
}
