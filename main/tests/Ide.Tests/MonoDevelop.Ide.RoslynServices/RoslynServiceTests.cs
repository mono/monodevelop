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

namespace MonoDevelop.Ide.RoslynServices
{
	[TestFixture]
	public class RoslynServiceTests : IdeTestBase
	{
		[TestCase (false, false)]
		[TestCase (true, false)]
		[TestCase (true, true)]
		public async Task TestForegroundThread (bool initAgain, bool inBg)
		{
			if (initAgain) {
				var current = ForegroundThreadAffinitizedObject.CurrentForegroundThreadData;
				if (inBg) {
					await Task.Run (() => { RoslynService.Initialize (); });
				} else
					RoslynService.Initialize ();
				Assert.AreSame (current, ForegroundThreadAffinitizedObject.CurrentForegroundThreadData);
			}

			var obj = new ForegroundThreadAffinitizedObject (false);

			// FIXME: Roslyn does not about Xwt Synchronization context.
			//Assert.AreEqual (ForegroundThreadDataKind.MonoDevelopGtk, obj.ForegroundKind);
			Assert.AreEqual (Runtime.MainTaskScheduler, obj.ForegroundTaskScheduler);
			Assert.IsTrue (obj.IsForeground ());

			await Task.Run (() => {
				Assert.IsFalse (obj.IsForeground ());
			});

			int x = 0;
			await obj.InvokeBelowInputPriority (() => {
				Assert.IsTrue (obj.IsForeground ());
				x++;
			});

			Assert.AreEqual (1, x);

			await Task.Run (() => obj.InvokeBelowInputPriority (() => {
				Assert.IsTrue (obj.IsForeground ());
				x++;
			}));

			Assert.AreEqual (2, x);
		}
	}
}
