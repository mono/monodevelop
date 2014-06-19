//
// ProcessPackageActionTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ProcessPackageActionTests
	{
		TestableProcessPackageAction action;
		FakePackageManagementProject fakeProject;
		ExceptionThrowingProcessPackageAction exceptionThrowingAction;

		void CreateAction ()
		{
			action = new TestableProcessPackageAction ();
			fakeProject = action.FakeProject;
		}

		ILogger AddLoggerToAction ()
		{
			var logger = new NullLogger ();
			action.Logger = logger;
			return logger;
		}

		void CreateActionWithExceptionThrownInExecuteCore ()
		{
			exceptionThrowingAction = new ExceptionThrowingProcessPackageAction ();
		}

		[Test]
		public void Execute_LoggerIsNull_LoggerUsedByProjectIsPackageManagementLogger ()
		{
			CreateAction ();
			action.Execute ();

			ILogger actualLogger = fakeProject.Logger;

			Assert.IsInstanceOf<PackageManagementLogger> (actualLogger);
		}

		[Test]
		public void Execute_LoggerIsDefined_LoggerDefinedIsUsedByProjectManager ()
		{
			CreateAction ();
			ILogger expectedLogger = AddLoggerToAction ();
			action.Execute ();

			ILogger actualLogger = fakeProject.Logger;

			Assert.AreEqual (expectedLogger, actualLogger);
		}

		[Test]
		public void BeforeExecute_LoggerIsDefined_LoggerUsedByProjectIsConfiguredBeforeInstallPackageCalled ()
		{
			CreateAction ();
			ILogger expectedLogger = AddLoggerToAction ();
			action.CallBeforeExecute ();

			ILogger actualLogger = fakeProject.Logger;

			Assert.AreEqual (expectedLogger, actualLogger);
		}

		[Test]
		public void Execute_ExceptionThrownInExecuteCore_ExceptionThrownByExecuteMethod ()
		{
			CreateActionWithExceptionThrownInExecuteCore ();
			var expectedException = new Exception ("Error");
			exceptionThrowingAction.ExceptionToThrowInExecuteCore = expectedException;

			Exception exception = Assert.Throws<Exception> (() => exceptionThrowingAction.Execute ());

			Assert.AreEqual (expectedException, exception);
		}
	}
}

