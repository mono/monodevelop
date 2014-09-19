//
// WizardPage.cs
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
//

namespace MonoDevelop.Ide.Templates
{
	public abstract class WizardPage// : Gtk.Bin
	{
		// Enables/disables the Next button. 
		//public bool CanMoveToNextPage { get; }

		//public event CanMoveToNextPageChanged;

		public string Title { get; protected set; }

		public bool HasNextPage { get; protected set; }

		// Called when a page is made active. This allows the wizard to update its UI 
		// if the user moved from another page which may have changed some values.
		public virtual void Activated ()
		{
		}

		// Called when another page is displayed just before the next page is displayed.
		public virtual void Deactivated ()
		{
		}

		// Disposed when the dialog is disposed.
//		public override void Dispose ()
//		{
//		}
	}
}

