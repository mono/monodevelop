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
using System.Net.Configuration;

namespace MonoDevelop.CSharp.Navigation
{
	internal static class Counters
	{
		public static TimerCounter<NavigationMetadata> NavigateTo = InstrumentationService.CreateTimerCounter<NavigationMetadata> ("Navigate to", "Code Navigation", id: "CodeNavigation.NavigateTo");

		public static NavigationMetadata CreateNavigateToMetadata (string navigationType)
		{
			var metadata = new NavigationMetadata (navigationType);
			metadata.SetResult (false);
			return metadata;
		}

		public class NavigationMetadata: CounterMetadata
		{
			public NavigationMetadata ()
			{
			}

			public NavigationMetadata (string type)
			{
				Type = type;
				SetFailure ();// Will be updated when navigation finishes.
			}

			public string Type {
				get => ContainsProperty () ? GetProperty<string> () : null;
				set => SetProperty (value);
			}

			public void SetResult (bool result)
			{
				if (result) {
					SetSuccess ();
				} else {
					SetFailure ();
				}
			}

			public void UpdateUserCancellation (CancellationToken cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested)
					SetUserCancel ();
			}
		}
	}
}
