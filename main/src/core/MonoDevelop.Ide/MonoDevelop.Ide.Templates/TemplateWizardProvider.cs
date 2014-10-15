//
// TemplateWizardProvider.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Templates
{
	public class TemplateWizardProvider
	{
		WizardPage currentWizardPage;

		public bool IsFirstPage { get; private set; }
		public bool IsLastPage { get; private set; }
		public int CurrentPageNumber { get; private set; }
		public TemplateWizard CurrentWizard { get; private set; }

		public WizardPage CurrentWizardPage {
			get { return currentWizardPage; }
			set {
				if (currentWizardPage != null) {
					currentWizardPage.CanMoveToNextPageChanged -= OnCanMoveToNextPageChanged;
				}
				currentWizardPage = value;
				if (currentWizardPage != null) {
					currentWizardPage.CanMoveToNextPageChanged += OnCanMoveToNextPageChanged;
				}
			}
		}

		public bool HasWizard {
			get { return CurrentWizard != null; }
		}

		public bool CanMoveToNextPage {
			get {
				if (currentWizardPage != null) {
					return currentWizardPage.CanMoveToNextPage;
				}
				return true;
			}
		}

		public event EventHandler CanMoveToNextPageChanged;

		void OnCanMoveToNextPageChanged (object sender, EventArgs e)
		{
			var handler = CanMoveToNextPageChanged;
			if (handler != null) {
				handler (sender, e);
			}
		}

		public bool MoveToFirstPage (SolutionTemplate template)
		{
			Reset ();

			if (!template.HasWizard) {
				return false;
			}

			CurrentWizard = IdeApp.Services.TemplatingService.GetWizard (template.Wizard);
			if (CurrentWizard == null) {
				LoggingService.LogError ("Unable to find project template wizard '{0}'.", template.Wizard);
				return false;
			}

			IsFirstPage = true;
			CurrentPageNumber++;
			CurrentWizardPage = CurrentWizard.GetPage (CurrentPageNumber);

			IsLastPage = CurrentWizard.TotalPages == 1;

			return true;
		}

		void Reset ()
		{
			CurrentWizard = null;
			CurrentPageNumber = 0;
			CurrentWizardPage = null;
			IsFirstPage = false;
			IsLastPage = false;
		}

		public bool MoveToNextPage ()
		{
			if (IsLastPage || !HasWizard) {
				return false;
			}

			CurrentPageNumber++;
			CurrentWizardPage = CurrentWizard.GetPage (CurrentPageNumber);

			IsFirstPage = false;
			IsLastPage = (CurrentPageNumber == CurrentWizard.TotalPages);

			return true;
		}

		public bool MoveToPreviousPage ()
		{
			if (IsFirstPage) {
				return false;
			}

			CurrentPageNumber--;
			CurrentWizardPage = CurrentWizard.GetPage (CurrentPageNumber);
			IsFirstPage = (CurrentPageNumber == 1);
			IsLastPage = false;

			return true;
		}

		public void Dispose ()
		{
			// Wizard pages should be cached and disposed.
			CurrentWizardPage = null;
		}
	}
}

