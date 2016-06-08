//
// AggregatePackageSourceErrorMessage.cs
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
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	class AggregatePackageSourceErrorMessage
	{
		int totalPackageSources;
		int failedPackageSourcesCount;
		StringBuilder errorBuilder = new StringBuilder ();

		public AggregatePackageSourceErrorMessage (int totalPackageSources)
		{
			this.totalPackageSources = totalPackageSources;
			ErrorMessage = String.Empty;
		}

		public string ErrorMessage { get; private set; }

		public void AddError (string message)
		{
			failedPackageSourcesCount++;

			AppendErrorMessage (message);

			if (!MultiplePackageSources) {
				ErrorMessage = errorBuilder.ToString ();
			} else if (AllFailed) {
				ErrorMessage = GetAllPackageSourcesCouldNotBeReachedErrorMessage ();
			} else {
				ErrorMessage = GetSomePackageSourcesCouldNotBeReachedErrorMessage ();
			}
		}

		bool MultiplePackageSources {
			get { return totalPackageSources > 1; }
		}

		bool AllFailed {
			get { return failedPackageSourcesCount >= totalPackageSources; }
		}

		void AppendErrorMessage (string message)
		{
			if (errorBuilder.Length == 0) {
				errorBuilder.Append (message);
			} else {
				errorBuilder.AppendLine ();
				errorBuilder.AppendLine ();
				errorBuilder.Append (message);
			}
		}

		string GetAllPackageSourcesCouldNotBeReachedErrorMessage ()
		{
			return GettextCatalog.GetString ("All package sources could not be reached.") +
				Environment.NewLine +
				errorBuilder.ToString ();
		}

		string GetSomePackageSourcesCouldNotBeReachedErrorMessage ()
		{
			return GettextCatalog.GetString ("Some package sources could not be reached.") +
				Environment.NewLine +
				errorBuilder.ToString ();
		}
	}
}

