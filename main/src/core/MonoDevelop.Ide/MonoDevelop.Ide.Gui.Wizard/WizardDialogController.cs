//
// WizardDialogController.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MonoDevelop.Components;
using MonoDevelop.Core;
using Xwt.Drawing;
namespace MonoDevelop.Ide.Gui.Wizard
{
	public class WizardDialogController : IWizardDialogController
	{
		string title;
		Image image;
		Control rightSideWidget;
		Xwt.Size defaultPageSize;
		IWizardDialogPage currentPage;
		ReadOnlyCollection<IWizardDialogPage> pages;

		public string Title {
			get { return title; }
			set {
				title = value;
				OnPropertyChanged (nameof (Title));
			}
		}

		public Image Icon {
			get { return image; }
			set {
				image = value;
				OnPropertyChanged (nameof (Icon));
			}
		}

		public Control RightSideWidget {
			get { return rightSideWidget; }
			set {
				rightSideWidget = value;
				OnPropertyChanged (nameof (RightSideWidget));
			}
		}

		public Xwt.Size DefaultPageSize {
			get { return defaultPageSize; }
			set {
				defaultPageSize = value;
				OnPropertyChanged (nameof (DefaultPageSize));
			}
		}

		public IReadOnlyCollection<IWizardDialogPage> Pages { get { return pages; } }

		public IWizardDialogPage CurrentPage {
			get {
				return currentPage;
			}
			private set {
				currentPage = value;
				OnPropertyChanged (nameof (CurrentPage));
			}
		}

		public bool CurrentPageIsLast {
			get { return Pages.IndexOf (CurrentPage) == Pages.Count - 1; }
		}

		public bool CanGoBack {
			get {
				return pages.Count > 1 && pages.IndexOf (CurrentPage) > 0;
			}
		}

		public WizardDialogController (string title, Image image,Control rightSideWidget, IWizardDialogPage page)
			: this (title, image, rightSideWidget, new IWizardDialogPage [] { page })
		{
		}

		public WizardDialogController (string title, Image icon, Control rightSideWidget, IEnumerable <IWizardDialogPage> pages)
		{
			this.pages = new ReadOnlyCollection<IWizardDialogPage> (pages.ToList ());
			if (this.pages.Count == 0)
				throw new ArgumentException ("pages must contain at least one page.", nameof (pages));
			Title = title;
			Icon = icon;
			CurrentPage = this.pages [0];
			RightSideWidget = rightSideWidget;
		}

		public void GoNext ()
		{
			if (CurrentPage.CanGoNext) {
				var currentIndex = Pages.IndexOf (CurrentPage);
				if (currentIndex == Pages.Count - 1)
					OnCompleted ();
				else
					CurrentPage = pages [currentIndex + 1];
			}
		}

		public void GoBack ()
		{
			if (CanGoBack) {
				var currentIndex = Pages.IndexOf (CurrentPage);
				CurrentPage = pages [currentIndex - 1];
			}
		}

		public bool RunWizard ()
		{
			var dialog = new WizardDialog (this);
			return dialog.Run (Xwt.MessageDialog.RootWindow);
		}

		public bool RunWizard (Xwt.WindowFrame parentWindow)
		{
			var dialog = new WizardDialog (this);
			return dialog.Run (parentWindow);
		}

		public event EventHandler Completed;

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnCompleted ()
		{
			Completed?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnPropertyChanged (string propertyName)
		{
			PropertyChanged?.Invoke (this, new System.ComponentModel.PropertyChangedEventArgs (propertyName));
		}

		public void Dispose ()
		{
		}
	}
}
