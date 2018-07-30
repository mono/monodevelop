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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using Xwt;
using Xwt.Backends;

namespace MonoDevelop.Ide.Gui.Wizard
{
	public class WizardDialog : IDisposable
	{
		public static readonly int RightSideWidgetWidth = 240;

		readonly Dialog Dialog;
		readonly MonoDevelop.Components.ExtendedHeaderBox header;
		readonly Button cancelButton, backButton, nextButton;
		readonly ImageView statusImage;
		readonly HBox buttonBox;
		IWizardDialogPage currentPage;
		Widget currentPageWidget;
		readonly FrameBox currentPageFrame;
		readonly FrameBox rightSideFrame;
		readonly VBox container;
		Dictionary<IWizardDialogPage, Widget> pageWidgets = new Dictionary<IWizardDialogPage, Widget> ();

		Components.AnimatedIcon animatedStatusIcon;
		IDisposable statusIconAnimation;
		CancellationTokenSource cancelTransitionRequest;
		Task currentTransitionTask;

		public IWizardDialogPage CurrentPage {
			get {
				return currentPage;
			}

			private set {
				if (currentPage == value)
					return;

				if (value == null)
					throw new InvalidOperationException ("CurrentPage can not be set to null");

				if (currentPage != null) {
					currentPageWidget.Sensitive = true; // reset after page transition
					currentPage.PropertyChanged -= HandleCurrentPagePropertyChanged;
				}

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

				Reallocate ();
			}
		}

		void Reallocate ()
		{
			var contentWidth = (Controller.DefaultPageSize.Width > 0 ? Controller.DefaultPageSize.Width : 660);
			var pageRequest = currentPageWidget.Surface.GetPreferredSize (true);
			contentWidth = Math.Max (contentWidth, pageRequest.Width);
			pageRequest = currentPageWidget.Surface.GetPreferredSize (SizeConstraint.WithSize (contentWidth), SizeConstraint.Unconstrained, true);
			var contentHeight = pageRequest.Height;
			currentPageFrame.MinHeight = Math.Max (contentHeight, Controller.DefaultPageSize.Height); // force default page height for smaller content
			currentPageFrame.MinWidth = Math.Max (contentWidth, Controller.DefaultPageSize.Width); // force default page width for smaller content
			var rightSideWidget = currentPage.GetRightSideWidget () ?? Controller.RightSideWidget;
			if (rightSideWidget != null) {
				var widget = (Xwt.Widget)rightSideWidget;
				if (rightSideFrame.Content != widget) {
					rightSideFrame.Content = widget;
					rightSideFrame.Content.VerticalPlacement = rightSideFrame.Content.HorizontalPlacement = WidgetPlacement.Fill;
					rightSideFrame.Visible = true;
				}
				Dialog.Width = contentWidth + RightSideWidgetWidth;
			} else {
				rightSideFrame.Visible = false;
				Dialog.Width = contentWidth;
			}
			Dialog.Height = Math.Max (contentHeight, Controller.DefaultPageSize.Height) + buttonBox.Size.Height;
		}

		public IWizardDialogController Controller { get; private set; }

		public WizardDialog (IWizardDialogController controller)
		{
			Controller = controller;
			Dialog = new Dialog ();

			Dialog.Name = "wizard_dialog";
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
			Dialog.Shown += HandleDialogShown;
			Dialog.CloseRequested += HandleDialogCloseRequested;

			container = new VBox ();
			container.Spacing = 0;

			header = new MonoDevelop.Components.ExtendedHeaderBox (controller.Title, null, controller.Icon);
			header.BackgroundColor = Styles.Wizard.BannerBackgroundColor;
			header.TitleColor = Styles.Wizard.BannerForegroundColor;
			header.SubtitleColor = Styles.Wizard.BannerSecondaryForegroundColor;
			header.BorderColor = Styles.Wizard.BannerShadowColor;

			buttonBox = new HBox ();
			var buttonFrame = new FrameBox (buttonBox);
			buttonFrame.Padding = 20;
			buttonFrame.PaddingRight = 0;

			cancelButton = new Button (GettextCatalog.GetString ("Cancel"));
			cancelButton.Clicked += HandleCancelButtonClicked;
			backButton = new Button (GettextCatalog.GetString ("Back"));
			backButton.Clicked += HandleBackButtonClicked;
			nextButton = new Button (GettextCatalog.GetString ("Next"));
			nextButton.Clicked += HandleNextButtonClicked;
			statusImage = new ImageView (ImageService.GetIcon ("md-empty", Gtk.IconSize.Button));

			if (Toolkit.CurrentEngine.Type == ToolkitType.XamMac) {
				var s = cancelButton.Surface.GetPreferredSize ();
				cancelButton.MinWidth = Math.Max (s.Width + 16, 100);
				s = backButton.Surface.GetPreferredSize ();
				backButton.MinWidth = Math.Max (s.Width + 16, 100);
				s = nextButton.Surface.GetPreferredSize ();
				nextButton.MinWidth = Math.Max (s.Width + 16, 100);
				buttonBox.Spacing = 0;
				statusImage.MarginRight = 6;
				#if MAC
				var nativeNext = nextButton.Surface.NativeWidget as AppKit.NSButton;
				nativeNext.KeyEquivalent = "\r";
				#endif
			} else {
				cancelButton.MinWidth = 70;
				backButton.MinWidth = 70;
				nextButton.MinWidth = 70;
				statusImage.MarginRight = 3;
			}

			if (ImageService.IsAnimation ("md-spinner-18", Gtk.IconSize.Button)) {
				animatedStatusIcon = ImageService.GetAnimatedIcon ("md-spinner-18", Gtk.IconSize.Button);
			}

			buttonBox.PackStart (cancelButton, false, false);
			buttonBox.PackEnd (statusImage, false, false);
			buttonBox.PackEnd (nextButton, false, false);
			buttonBox.PackEnd (backButton, false, false);
			statusImage.VerticalPlacement = cancelButton.VerticalPlacement = nextButton.VerticalPlacement = backButton.VerticalPlacement = WidgetPlacement.Center;

			container.PackStart (header);

			var contentHBox = new HBox ();
			contentHBox.Spacing = 0;

			currentPageFrame = new FrameBox ();
			currentPageFrame.BackgroundColor = Styles.Wizard.PageBackgroundColor;
			contentHBox.PackStart (currentPageFrame, true, true);

			rightSideFrame = new FrameBox () { Visible = false };
			//rightSideFrame.BorderColor = Styles.Wizard.ContentSeparatorColor;
			//rightSideFrame.BorderWidthLeft = 1;
			rightSideFrame.WidthRequest = RightSideWidgetWidth;
			rightSideFrame.BackgroundColor = Styles.Wizard.RightSideBackgroundColor;
			contentHBox.PackEnd (rightSideFrame, false, true);
			rightSideFrame.VerticalPlacement = rightSideFrame.HorizontalPlacement = WidgetPlacement.Fill;

			var contentFrame = new FrameBox (contentHBox);
			contentFrame.Padding = 0;
			contentFrame.BorderColor = Styles.Wizard.ContentShadowColor;
			contentFrame.BorderWidth = 0;
			contentFrame.BorderWidthBottom = 1;

			container.PackStart (contentFrame, true, true);
			container.PackEnd (buttonFrame);

			Dialog.Content = container;

			if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk3) {
				var nativeNext = nextButton.Surface.NativeWidget as Gtk.Button;
				nativeNext.CanDefault = true;
				nativeNext.GrabDefault ();
			}

			CurrentPage = controller.CurrentPage;

			controller.PropertyChanged += HandleControllerPropertyChanged;
			controller.Completed += HandleControllerCompleted;
		}

		async void HandleNextButtonClicked (object sender, EventArgs e)
		{
			nextButton.Sensitive = backButton.Sensitive = currentPageWidget.Sensitive = false;
			using (cancelTransitionRequest = new CancellationTokenSource ()) {
				currentTransitionTask = Controller.GoNext (cancelTransitionRequest.Token);
				if (await Task.WhenAny (currentTransitionTask, Task.Delay (200, cancelTransitionRequest.Token)).ConfigureAwait (false) != currentTransitionTask) {
					StartWorkingSpinner ();
				}
				await currentTransitionTask.ConfigureAwait (false);
				StopWorkingSpinner ();
			}
			cancelTransitionRequest = null;
		}

		async void HandleBackButtonClicked (object sender, EventArgs e)
		{
			nextButton.Sensitive = backButton.Sensitive = currentPageWidget.Sensitive = false;
			using (cancelTransitionRequest = new CancellationTokenSource ()) {
				currentTransitionTask = Controller.GoBack (cancelTransitionRequest.Token);
				if (await Task.WhenAny (currentTransitionTask, Task.Delay (200, cancelTransitionRequest.Token)).ConfigureAwait (false) != currentTransitionTask) {
					StartWorkingSpinner ();
				}
				await currentTransitionTask.ConfigureAwait (false);
				StopWorkingSpinner ();
			}
			cancelTransitionRequest = null;
		}

		void HandleCancelButtonClicked (object sender, EventArgs e)
		{
			if (cancelTransitionRequest != null && cancelTransitionRequest.IsCancellationRequested != true)
				cancelTransitionRequest.Cancel ();
			Respond (false);
		}

		void HandleDialogCloseRequested (object sender, CloseRequestedEventArgs args)
		{
			if (cancelTransitionRequest != null && cancelTransitionRequest.IsCancellationRequested != true)
				cancelTransitionRequest.Cancel ();
		}

		void StartWorkingSpinner ()
		{
			Runtime.RunInMainThread (() => {
				if (statusIconAnimation != null)
					StopWorkingSpinner ();

				if (animatedStatusIcon != null) {
					statusImage.Image = animatedStatusIcon.FirstFrame.WithAlpha (0.4).ToBitmap ();
					statusIconAnimation = animatedStatusIcon.StartAnimation (p => {
						statusImage.Image = p.WithAlpha (0.4).ToBitmap ();
					});
				} else {
					statusImage.Image = ImageService.GetIcon ("md-spinner-Button", Gtk.IconSize.Button);
				}
			});
		}

		void StopWorkingSpinner ()
		{
			Runtime.RunInMainThread (() => {
				if (statusIconAnimation != null) {
					statusIconAnimation.Dispose ();
					statusIconAnimation = null;
				}
				statusImage.Image = ImageService.GetIcon ("md-empty", Gtk.IconSize.Button);
			});
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
				case nameof (Controller.CanGoBack): backButton.Visible = Controller.CanGoBack; break;
				case nameof (Controller.RightSideWidget): Reallocate (); break;
				case nameof (Controller.DefaultPageSize): Reallocate (); break;
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
			case nameof (CurrentPage.CanGoBack): backButton.Sensitive = currentPage.CanGoBack; break;
			}
		}

		void HandleControllerCompleted (object sender, EventArgs e)
		{
			if (sender != Controller)
				throw new InvalidOperationException ();
			Runtime.RunInMainThread (() => {
				Respond (true);
			});
		}

		void HandleDialogShown (object sender, EventArgs e)
		{
			Reallocate ();
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
			return cmd == Command.Ok;
		}

		bool disposed = false;
		public void Dispose ()
		{
			if (!disposed) {
				Dialog.Shown -= HandleDialogShown;
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
