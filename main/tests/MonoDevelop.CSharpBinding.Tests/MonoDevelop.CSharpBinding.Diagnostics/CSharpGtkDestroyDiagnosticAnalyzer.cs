//
// CSharpGtkDestroyDiagnosticAnalyzer.cs
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
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring.Tests;
using NUnit.Framework;

namespace MonoDevelop.CSharpBinding.Refactoring
{
	[TestFixture]
	public class CSharpGtkDestroyDiagnosticAnalyzer : ResultsEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;

		const string OneFromEach = @"using System;

namespace Gtk
{
	class Object
	{
		public virtual void Destroy() { }
	}
	class Widget : Object
	{
		public override void Destroy()
		{
			base.Destroy();
		}
	}
}

class MyWidget : Gtk.Widget
{
	public override void Destroy()
	{
		base.Destroy();
	}
}
class MyOtherWidget : MyWidget
{
	public override void Destroy()
	{
		base.Destroy();
	}
}
";
		const string message = "Override OnDestroyed rather than Destroy - the latter will not run from unmanaged destruction";
		static readonly ExpectedDiagnostic [] GtkDiagnostics = {
			new ExpectedDiagnostic (231, DiagnosticSeverity.Error,  message),
			new ExpectedDiagnostic (322, DiagnosticSeverity.Error, message),
		};

		[Test]
		public async Task DiagnosticsAreReported ()
		{
			await RunTest (3, OneFromEach, (remainingUpdates, doc) => {
				if (remainingUpdates == 0) {
					AssertExpectedDiagnostics (GtkDiagnostics, doc, x => x.Severity == DiagnosticSeverity.Error);
				}
				return Task.CompletedTask;
			});
		}
	}
}
