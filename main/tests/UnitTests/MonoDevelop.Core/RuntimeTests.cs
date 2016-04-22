//
// RuntimeTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using UnitTests;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class RuntimeTests : TestBase
	{
		/// <summary>
		/// This test works with the existing Task RunInMainThread (Func<Task> func).
		/// The func throws the exception so it is properly handled.
		/// </summary>
		[Test]
		public async Task RunInMainThreadWithFuncTask ()
		{
			await Task.Run (() => {
				var expectedException = new Exception ("Error");

				Task task = Runtime.RunInMainThread (() => ThrowException (expectedException));
				AggregateException ex = Assert.Throws<AggregateException> (() => task.Wait ());

				Assert.IsTrue (task.IsFaulted);
				Assert.AreEqual (expectedException, task.Exception.GetBaseException ());
				Assert.AreEqual (expectedException, ex.GetBaseException ());
			});
		}

		Task ThrowException (Exception ex)
		{
			throw ex;
		}

		/// <summary>
		/// This test was failing since using an await returns a faulted Task and the
		/// func does not directly throw an exception. The Task returned from the RunInMainThread
		/// method would never be faulted.
		/// 
		/// This test fails on the Mac with an InvalidProgramException: Invalid IL code in 
		/// MonoDevelop.Core.RuntimeTests/<RunInMainThreadWithAsyncFuncTask>
		/// </summary>
		[Test]
		[Ignore ("This test fails on Mac but works on Windows.")]
		public async Task RunInMainThreadWithAsyncFuncTask ()
		{
			await Task.Run (() => {
				var expectedException = new Exception ("Error");

				Task task = Runtime.RunInMainThread (async () => await ThrowExceptionAsync (expectedException));
				AggregateException ex = Assert.Throws<AggregateException> (() => task.Wait ());

				Assert.IsTrue (task.IsFaulted);
				Assert.AreEqual (expectedException, task.Exception.GetBaseException ());
				Assert.AreEqual (expectedException, ex.GetBaseException ());
			});
		}

		Task ThrowExceptionAsync (Exception ex)
		{
			throw ex;
		}

		/// <summary>
		/// This test was failing since using an await returns a faulted Task and the
		/// func does not directly throw an exception. The Task returned from the RunInMainThread
		/// method would never be faulted.
		/// </summary>
		[Test]
		public async Task RunInMainThreadWithFuncTaskWithAwaitInsideMethod ()
		{
			await Task.Run (() => {
				var expectedException = new Exception ("Error");

				Task task = Runtime.RunInMainThread (() => ThrowExceptionWithAwait (expectedException));
				AggregateException ex = Assert.Throws<AggregateException> (() => task.Wait ());

				Assert.IsTrue (task.IsFaulted);
				Assert.AreEqual (expectedException, task.Exception.GetBaseException ());
				Assert.AreEqual (expectedException, ex.GetBaseException ());
			});
		}

		async Task ThrowExceptionWithAwait (Exception ex)
		{
			await Task.Delay (1);
			throw ex;
		}
	}
}

