// 
// PagedResultsWidget.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Gtk;
using ICSharpCode.PackageManagement;
using System.ComponentModel;

namespace MonoDevelop.PackageManagement
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PagedResultsWidget : Gtk.Bin
	{
		PackagesViewModel viewModel;
		List<Button> buttons;

		public PagedResultsWidget ()
		{
			this.Build ();
			AddButtons ();
		}

		void AddButtons ()
		{
			buttons = new List<Button> ();
			buttons.AddRange (new [] {
				firstButton,
				secondButton,
				thirdButton,
				fourthButton,
				fifthButton
			});
		}

		public void LoadPackagesViewModel (PackagesViewModel viewModel)
		{
			this.viewModel = viewModel;
			this.viewModel.PropertyChanged += ViewModelPropertyChanged;
		}

		void ViewModelPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			UpdatePages ();
		}

		void UpdatePages ()
		{
			if (viewModel.IsPaged) {
				this.backButton.Visible = viewModel.HasPreviousPage;
				this.forwardButton.Visible = viewModel.HasNextPage;
				UpdateButtonPageNumbers ();
			}
		}

		void UpdateButtonPageNumbers ()
		{
			for (int index = 0; index < buttons.Count; ++index) {
				Button button = buttons [index];
				if (index < viewModel.Pages.Count) {
					Page page = viewModel.Pages [index];
					SetButtonPageNumber (button, page);
				} else {
					button.Visible = false;
				}
			}
		}

		void SetButtonPageNumber (Button button, Page page)
		{
			if (page.IsSelected) {
				string markup = String.Format ("<b>{0}</b>", page.Number);
				SetButtonText (button, markup);
			} else {
				SetButtonText (button, page.Number.ToString ());
			}
		}

		void SetButtonText (Button button, string markup)
		{
			Label label = (Label) button.Child;
			label.Markup = markup;
		}

		protected void BackButtonClicked (object sender, EventArgs e)
		{
			viewModel.ShowPreviousPage ();
		}

		protected void ForwardButtonClicked (object sender, EventArgs e)
		{
			viewModel.ShowNextPage ();
		}

		protected void PageButtonClicked (object sender, EventArgs e)
		{
			Button button = (Button) sender;
			int index = buttons.IndexOf (button);
			viewModel.ShowPage (viewModel.Pages [index].Number);
		}
		
		protected override void OnDestroyed ()
		{
			if (viewModel != null) {
				viewModel.PropertyChanged -= ViewModelPropertyChanged;
			}
			base.OnDestroyed ();
		}
	}
}

