//
// WizardDialog.cs
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
using MonoDevelop.Core;
using Xwt;
using Xwt.Backends;

namespace MonoDevelop.Ide.Gui.Wizard
{
	public class WizardDialog : IDisposable
	{
		public static readonly int RightSideWidgetWidth = 240;

		Dialog Dialog;
		MonoDevelop.Components.ExtendedHeaderBox header;
		Button cancelButton, backButton, nextButton;
		IWizardDialogPage currentPage;
		Widget currentPageWidget;
		FrameBox currentPageFrame;
		FrameBox rightSideFrame;
		VBox container;
		Dictionary<IWizardDialogPage, Widget> pageWidgets = new Dictionary<IWizardDialogPage, Widget> ();

		public IWizardDialogPage CurrentPage {
			get {
				return currentPage;
			}

			private set {
				if (currentPage == value)
					return;

				if (value == null)
					throw new InvalidOperationException ("CurrentPage can not be set to null");

				if (currentPage != null)
					currentPage.PropertyChanged -= HandleCurrentPagePropertyChanged;

				currentPage = value;
				if (!pageWidgets.TryGetValue (currentPage, out currentPageWidget))
					currentPageWidget = pageWidgets [currentPage] = currentPage.GetControl ();

				header.Title = !string.IsNullOrEmpty (currentPage.PageTitle) ? currentPage.PageTitle : Controller.Title;
				header.Subtitle = currentPage.PageSubtitle;
				header.Image = currentPage.PageIcon ?? Controller.Icon;
				if (!string.IsNullOrEmpty (currentPage.NextButtonLabel))
					nextButton.Label = currentPage.NextButtonLabel;
				else
					nextButton.Label = Controller.CurrentPageIsLast ? GettextCatalog.GetString ("Finish") : GettextCatalog.GetString ("Next");
				nextButton.Sensitive = currentPage.CanGoNext;
				backButton.Visible = Controller.CanGoBack;
				backButton.Sensitive = currentPage.CanGoBack;

				currentPage.PropertyChanged += HandleCurrentPagePropertyChanged;
				currentPageFrame.Content = currentPageWidget;

				UpdateRightSideFrame ();

				if (Toolkit.CurrentEngine != MonoDevelop.Components.GtkUtil.GtkToolkit) {
					// HACK: force foreign Dialog to reallocate
					container.QueueForReallocate ();
					Dialog.Width += 1;
					Dialog.Width -= 1;
				}
			}
		}

		void UpdateRightSideFrame ()
		{
			var rightSideWidget = currentPage.GetRightSideWidget () ?? Controller.RightSideWidget;
			if (rightSideWidget != null) {
				rightSideFrame.Content = rightSideWidget;
				rightSideFrame.Visible = true;
				Dialog.Width = 900;
			} else {
				rightSideFrame.Visible = false;
				Dialog.Width = 900 - RightSideWidgetWidth;
			}
		}

		public IWizardDialogController Controller { get; private set; }

		public WizardDialog (IWizardDialogController controller)
		{
			Controller = controller;
			Dialog = new Dialog ();

			Dialog.Name = "wizard_dialog";
			Dialog.Width = 900;
			Dialog.Height = 630;
			Dialog.Resizable = false;
			Dialog.Padding = 0;

			if (string.IsNullOrEmpty (controller.Title))
				Dialog.Title = BrandingService.ApplicationName;
			else
				Dialog.Title = controller.Title;

			// FIXME: Gtk dialogs don't support ThemedImage
			//if (controller.Image != null)
			//	Dialog.Icon = controller.Image.WithSize (IconSize.Large);
			
			Dialog.ShowInTaskbar = false;

			container = new VBox ();
			container.Spacing = 0;

			header = new MonoDevelop.Components.ExtendedHeaderBox (controller.Title, null, controller.Icon);
			header.BackgroundColor = Styles.Wizard.BannerBackgroundColor;
			header.TitleColor = Styles.Wizard.BannerForegroundColor;
			header.SubtitleColor = Styles.Wizard.BannerSecondaryForegroundColor;
			header.BorderColor = Styles.Wizard.BannerShadowColor;

			var buttonBox = new HBox ();
			var buttonFrame = new FrameBox (buttonBox);
			buttonFrame.Padding = 10;

			cancelButton = new Button (GettextCatalog.GetString ("Cancel"));
			cancelButton.Clicked += (sender, e) => {
				Respond (false);
			};
			backButton = new Button (GettextCatalog.GetString ("Back"));
			backButton.Clicked += (sender, e) => Controller.GoBack ();
			nextButton = new Button (GettextCatalog.GetString ("Next"));
			nextButton.Clicked += (sender, e) => Controller.GoNext ();

			if (Toolkit.CurrentEngine.Type == ToolkitType.XamMac)
				cancelButton.MinWidth = backButton.MinWidth = nextButton.MinWidth = 77;

			buttonBox.PackStart (cancelButton, false, false);
			buttonBox.PackEnd (nextButton, false, false);
			buttonBox.PackEnd (backButton, false, false);

			container.PackStart (header);

			var contentHBox = new HBox ();
			contentHBox.Spacing = 0;

			currentPageFrame = new FrameBox ();
			currentPageFrame.BackgroundColor = Styles.Wizard.PageBackgroundColor;
			contentHBox.PackStart (currentPageFrame, true, true);

			rightSideFrame = new FrameBox () { Visible = false };
			rightSideFrame.BorderColor = Styles.Wizard.ContentSeparatorColor;
			rightSideFrame.BorderWidthLeft = 1;
			rightSideFrame.WidthRequest = RightSideWidgetWidth;
			rightSideFrame.BackgroundColor = Styles.Wizard.RightSideBackgroundColor;
			contentHBox.PackEnd (rightSideFrame, false, true);

			var contentFrame = new FrameBox (contentHBox);
			contentFrame.Padding = 0;
			contentFrame.BorderColor = Styles.Wizard.ContentShadowColor;
			contentFrame.BorderWidth = 0;
			contentFrame.BorderWidthBottom = 1;

			container.PackStart (contentFrame, true, true);
			container.PackEnd (buttonFrame);

			Dialog.Content = container;

			CurrentPage = controller.CurrentPage;

			controller.PropertyChanged += HandleControllerPropertyChanged;
			controller.Completed += HandleControllerCompleted;
		}

		void HandleControllerPropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (sender != Controller)
				throw new InvalidOperationException ();

			switch (e.PropertyName) {
				case nameof (Controller.Title): Dialog.Title = Controller.Title; break;
				// FIXME: Gtk dialogs don't support ThemedImage
				//case nameof (Controller.Icon): Dialog.Icon = Controller.Icon.WithSize (IconSize.Large); break;
				case nameof (Controller.CurrentPage): CurrentPage = Controller.CurrentPage; break;
				case nameof (Controller.RightSideWidget): UpdateRightSideFrame (); break;
			}
		}

		void HandleCurrentPagePropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (sender != currentPage)
				throw new InvalidOperationException ();

			switch (e.PropertyName) {
			case nameof (CurrentPage.PageTitle): header.Title = !string.IsNullOrEmpty (currentPage.PageTitle) ? currentPage.PageTitle : Controller.Title; break;
			case nameof (CurrentPage.PageSubtitle): header.Subtitle = currentPage.PageSubtitle; break;
			case nameof (CurrentPage.PageIcon): header.Image = currentPage.PageIcon ?? Controller.Icon; break;
			case nameof (CurrentPage.NextButtonLabel):
				if (!string.IsNullOrEmpty (currentPage.NextButtonLabel))
					nextButton.Label = currentPage.NextButtonLabel;
				else
					nextButton.Label = Controller.CurrentPageIsLast ? GettextCatalog.GetString ("Finish") : GettextCatalog.GetString ("Next");
				break;
			case nameof (CurrentPage.CanGoNext): nextButton.Sensitive = currentPage.CanGoNext; break;
			case nameof (CurrentPage.CanGoBack):
				backButton.Visible = Controller.CanGoBack;
				backButton.Sensitive = currentPage.CanGoBack;
				break;
			}
		}

		void HandleControllerCompleted (object sender, EventArgs e)
		{
			if (sender != Controller)
				throw new InvalidOperationException ();
			Respond (true);
		}

		void Respond (bool finished)
		{
			Dialog.Respond (finished ? Command.Ok : Command.Cancel);
			Dialog.Close ();
		}

		public bool Run ()
		{
			return Run (null);
		}

		public bool Run (WindowFrame parent)
		{
			var cmd = Dialog.Run (parent);
			return cmd != Command.Cancel;
		}

		bool disposed = false;
		public void Dispose ()
		{
			if (!disposed) {
				Controller.Completed -= HandleControllerCompleted;
				Controller.PropertyChanged -= HandleControllerPropertyChanged;
				if (CurrentPage != null)
					currentPage.PropertyChanged -= HandleCurrentPagePropertyChanged;
				disposed = true;
			}
			Dialog.Dispose ();
		}
	}
}
