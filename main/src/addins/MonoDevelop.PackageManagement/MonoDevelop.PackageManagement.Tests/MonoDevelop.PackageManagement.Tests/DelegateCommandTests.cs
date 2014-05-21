//
// DelegateCommandTests.cs
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
using ICSharpCode.PackageManagement;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class DelegateCommandTests
	{
		[Test]
		public void CanExecute_NoCanExecuteDelegateDefined_ReturnsTrue ()
		{
			Action<object> execute = delegate { };
			var command = new DelegateCommand (execute);

			bool result = command.CanExecute (null);

			Assert.IsTrue (result);
		}

		[Test]
		public void CanExecute_CanExecuteDelegateDefinedToReturnFalse_ReturnsFalse ()
		{
			Action<object> execute = delegate { };
			Predicate<object> canExecute = delegate {
				return false;
			};
			var command = new DelegateCommand (execute, canExecute);

			bool result = command.CanExecute (null);

			Assert.IsFalse (result);
		}

		[Test]
		public void CanExecute_CanExecuteDelegateDefined_ParameterPassedToCanExecuteDelegate ()
		{
			Action<object> execute = delegate { };

			object parameterPassed = null;
			Predicate<object> canExecute = param => {
				parameterPassed = param;
				return true;
			};
			var command = new DelegateCommand (execute, canExecute);

			object expectedParameter = new object ();
			bool result = command.CanExecute (expectedParameter);

			Assert.AreEqual (expectedParameter, parameterPassed);
			Assert.IsTrue (result);
		}

		[Test]
		public void Execute_ObjectPassedAsParameter_ParameterPassedToExecuteDelegate ()
		{
			object parameterPassed = null;
			Action<object> execute = param => parameterPassed = param;
			var command = new DelegateCommand (execute);

			object expectedParameter = new object ();
			command.Execute (expectedParameter);

			Assert.AreEqual (expectedParameter, parameterPassed);
		}
	}
}

