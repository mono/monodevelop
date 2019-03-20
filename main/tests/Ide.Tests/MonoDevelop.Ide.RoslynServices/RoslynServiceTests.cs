//
// RoslynService.cs
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
using System;
using NUnit.Framework;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using MonoDevelop.Core;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.RoslynServices
{
	[TestFixture]
	public class RoslynServiceTests : IdeTestBase
	{
		[TestCase (true)]
		[TestCase (false)]
		public async Task TestForegroundThread (bool inBg)
		{
			await CompositionManager.InitializeAsync ();

			var context = CompositionManager.GetExportedValue<IThreadingContext> ();
			var obj = new ForegroundThreadAffinitizedObject (context, false);

			var roslynContext = obj.ThreadingContext.JoinableTaskContext;
			Assert.AreSame (Runtime.MainThread, roslynContext.MainThread);
			Assert.IsTrue (obj.IsForeground ());

			await Task.Run (() => {
				Assert.IsFalse (obj.IsForeground ());
			});

			int x = 0;
			await obj.InvokeBelowInputPriorityAsync (() => {
				Assert.IsTrue (obj.IsForeground ());
				x++;
			});

			Assert.AreEqual (1, x);

			await Task.Run (() => obj.InvokeBelowInputPriorityAsync (() => {
				Assert.IsTrue (obj.IsForeground ());
				x++;
			}));

			Assert.AreEqual (2, x);
		}

		/// <summary>
		/// This verifies that the Reflection logic in <see cref="DefaultSourceEditorOptions.SetUseAsyncCompletion(bool)"/>
		/// is still compatible with the Roslyn build we're using
		/// </summary>
		[TestCase]
		public void TestAsyncCompletionServiceReflection ()
		{
			DefaultSourceEditorOptions.SetUseAsyncCompletion (true);
		}
	}
}
