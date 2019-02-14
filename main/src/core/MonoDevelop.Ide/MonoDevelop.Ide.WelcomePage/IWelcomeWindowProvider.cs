//
// IWelcomeDialogProvider.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2018 Microsoft Inc. (http://microsoft.com)
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

using MonoDevelop.Components;
using Mono.Addins;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomeWindowShowOptions
	{
		public WelcomeWindowShowOptions (bool closeSolution, bool userInitated = false)
		{
			CloseSolution = closeSolution;
			UserInitiated = userInitated;
		}

		public bool CloseSolution { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the user initiated the
		/// operation (i.e. Window->Start Window menu)
		/// </summary>
		/// <value><c>true</c> if user initiated; otherwise, <c>false</c>.</value>
		public bool UserInitiated { get; set; }
	}

	[TypeExtensionPoint]
	public interface IWelcomeWindowProvider
	{
		Window CreateWindow ();
		void ShowWindow (Window window, WelcomeWindowShowOptions options);
		void HideWindow (Window window);
	}
}
