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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	class TemplateWizardProvider
	{
		WizardPage currentWizardPage;

		List<WizardPage> cachedWizardPages = new List<WizardPage> ();

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

		public bool MoveToFirstPage (SolutionTemplate template, ProjectCreateParameters parameters)
		{
			Reset ();

			if (!template.HasWizard) {
				return false;
			}

			CurrentWizard = GetWizard (template.Wizard);
			if (CurrentWizard == null) {
				LoggingService.LogError ("Unable to find project template wizard '{0}'.", template.Wizard);
				return false;
			}

			CurrentWizard.Parameters = parameters;
			CurrentWizard.UpdateParameters (template);
			IsFirstPage = true;
			CurrentPageNumber = 1;

			if (CurrentWizard.TotalPages == 0) {
				IsLastPage = true;
				return false;
			}

			CurrentWizardPage = GetCurrentWizardPage ();

			IsLastPage = CurrentWizard.TotalPages == 1;

			return true;
		}

		WizardPage GetCurrentWizardPage ()
		{
			if (cachedWizardPages.Count >= CurrentPageNumber) {
				return cachedWizardPages [CurrentPageNumber - 1];
			}
			WizardPage page = CurrentWizard.GetPage (CurrentPageNumber);
			if (page != null) {
				cachedWizardPages.Add (page);
			}
			return page;
		}

		protected virtual TemplateWizard GetWizard (string id)
		{
			return IdeApp.Services.TemplatingService.GetWizard (id);
		}

		void Reset ()
		{
			CurrentWizard = null;
			CurrentPageNumber = 0;
			CurrentWizardPage = null;
			IsFirstPage = false;
			IsLastPage = false;

			DisposeWizardPages ();
		}

		void DisposeWizardPages ()
		{
			foreach (WizardPage page in cachedWizardPages) {
				page.Dispose ();
			}

			cachedWizardPages.Clear ();
		}

		public bool MoveToNextPage ()
		{
			if (IsLastPage || !HasWizard) {
				return false;
			}

			CurrentPageNumber++;
			CurrentWizardPage = GetCurrentWizardPage ();

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
			CurrentWizardPage = GetCurrentWizardPage ();
			IsFirstPage = (CurrentPageNumber == 1);
			IsLastPage = false;

			return true;
		}

		public void BeforeProjectIsCreated ()
		{
			CurrentWizard.ConfigureWizard ();
		}

		public void Dispose ()
		{
			Reset ();
		}
	}
}

