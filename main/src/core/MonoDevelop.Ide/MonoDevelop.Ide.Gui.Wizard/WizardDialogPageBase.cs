//
// WizardDialogPageBase.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using System.ComponentModel;
using MonoDevelop.Components;
using Xwt.Drawing;

namespace MonoDevelop.Ide.Gui.Wizard
{
	public abstract class WizardDialogPageBase : IWizardDialogPage
	{
		bool canGoBack, canGoNext;
		string pageTitle, pageSubtitle, nextButtonLabel;
		Image pageIcon;
		Control pageControl;
		Control rightSideWidget;

		public WizardDialogPageBase ()
		{
			canGoBack = canGoNext = true;
		}

		public bool CanGoBack {
			get { return canGoBack; }
			protected set {
				canGoBack = value;
				OnPropertyChanged (nameof (CanGoBack));
			}
		}

		public bool CanGoNext {
			get { return canGoNext; }
			protected set {
				canGoNext = value;
				OnPropertyChanged (nameof (CanGoNext));
			}
		}

		public string PageTitle {
			get { return pageTitle; }
			protected set {
				pageTitle = value;
				OnPropertyChanged (nameof (PageTitle));
			}
		}

		public string PageSubtitle {
			get { return pageSubtitle; }
			protected set {
				pageSubtitle = value;
				OnPropertyChanged (nameof (PageSubtitle));
			}
		}

		public string NextButtonLabel {
			get { return nextButtonLabel; }
			protected set {
				nextButtonLabel = value;
				OnPropertyChanged (nameof (NextButtonLabel));
			}
		}

		public Image PageIcon {
			get { return pageIcon; }
			protected set {
				pageIcon = value;
				OnPropertyChanged (nameof (PageIcon));
			}
		}

		public Control GetControl ()
		{
			if (pageControl == null)
				pageControl = CreateControl ();
			return pageControl;
		}

		public Control GetRightSideWidget ()
		{
			if (rightSideWidget == null)
				rightSideWidget = CreateRightSideWidget ();
			return rightSideWidget;
		}

		protected abstract Control CreateControl ();

		protected virtual Control CreateRightSideWidget ()
		{
			return null;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged (string propertyName)
		{
			PropertyChanged?.Invoke (this, new System.ComponentModel.PropertyChangedEventArgs (propertyName));
		}

		#region IDisposable Support
		private bool disposedValue = false;

		protected virtual void Dispose (bool disposing)
		{
			if (!disposedValue) {
				if (disposing) {
					if (pageControl != null)
						pageControl.Dispose ();
					if (rightSideWidget != null)
						rightSideWidget.Dispose ();
				}

				pageControl = null;
				rightSideWidget = null;

				disposedValue = true;
			}
		}

		public void Dispose ()
		{
			Dispose (true);
		}
		#endregion
	}
}
