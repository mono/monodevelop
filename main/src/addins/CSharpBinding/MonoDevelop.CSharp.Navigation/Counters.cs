//
// Counters.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.CSharp.Navigation
{
	internal static class Counters
	{
		public static TimerCounter NavigateTo = InstrumentationService.CreateTimerCounter ("Navigate to", "Code Navigation", id: "CodeNavigation.NavigateTo");

		public static IDictionary<string, string> CreateNavigateToMetadata (string navigationType)
		{
			var metadata = new Dictionary<string, string> ();
			metadata ["Type"] = navigationType;
			metadata ["Result"] = "Failure"; // Will be updated when navigation finishes.
			return metadata;
		}

		public static void UpdateUserCancellation (IDictionary<string, string> metadata, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested) {
				metadata ["Result"] = "UserCancel";
			}
		}

		public static void UpdateNavigateResult (IDictionary<string, string> metadata, bool result)
		{
			metadata ["Result"] = result ? "Success" : "Failure";
		}

		/// <summary>
		/// Distinguish between IDE errors and a failure due to the user selecting an invalid item.
		/// Some navigation menus are enabled even if they are not valid. For example, right clicking
		/// on a method and selecting Navigate - Extension Methods will find no class at the caret and
		/// will not attempt to find any extension methods. Note that the CommandInfo is disabled for
		/// the menu but this does not seem to have any affect presumably because the method is async.
		/// </summary>
		public static void UpdateUserFault (IDictionary<string, string> metadata)
		{
			metadata ["Result"] = "UserFault";
		}
	}
}
