//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace MonoDevelop.TextEditor.Wpf.Find
{
	public partial class FindUI : UserControl
	{
		private IWpfTextView currentTextView; // The text view on which the UI is being displayed at any given time

		/// <summary>
		/// Used for resizing of the control
		/// </summary>
		double _resizingBaselineWidth;
		double _resizingBaselinePoint;

		public event Action<string> SearchTextChanged;
		public event Action<string> ReplaceTextChanged;
		public event Action<FindOptions> FindOptionsChanged;
		public event Action FindPrevious;
		public event Action FindNext;
		public event Action Replace;
		public event Action ReplaceAll;
		public event Action CloseRequested;

		public const string FindUIAdornmentLayer = nameof (FindUIAdornmentLayer);

		public FindUI ()
		{
			InitializeComponent ();

			Visibility = Visibility.Collapsed;

			SearchControl.TextChanged += OnSearchTextChanged;
			ReplaceControl.TextChanged += OnReplaceTextChanged;

			FindReplaceToggleButton.Checked += (s, e) => IsInReplaceMode = true;
			FindReplaceToggleButton.Unchecked += (s, e) => IsInReplaceMode = false;
			MatchCaseToggleButton.Checked += RaiseFindOptionsChanged;
			MatchCaseToggleButton.Unchecked += RaiseFindOptionsChanged;
			MatchWholeWordToggleButton.Checked += RaiseFindOptionsChanged;
			MatchWholeWordToggleButton.Unchecked += RaiseFindOptionsChanged;
			RegularExpressionToggleButton.Checked += RaiseFindOptionsChanged;
			RegularExpressionToggleButton.Unchecked += RaiseFindOptionsChanged;

			UpdateReplaceVisibility (false);
		}

		public void ShowAdornment (IWpfTextView view)
		{
			IAdornmentLayer adornmentLayer = view.GetAdornmentLayer (FindUIAdornmentLayer);

			// Make sure anti-aliasing doesn't cause trouble
			((UIElement)adornmentLayer).SnapsToDevicePixels = true;

			currentTextView = view;

			if (adornmentLayer.Elements.Count == 0) {
				adornmentLayer.AddAdornment (AdornmentPositioningBehavior.OwnerControlled, null, this, this, null);
			}

			Position ();

			Visibility = Visibility.Visible;

			view.VisualElement.SizeChanged += OnViewSizeChanged;
			view.Closed += OnViewClosed;

			FocusFindBox ();
		}

		public void HideAdornment ()
		{
			IWpfTextView textView = currentTextView;

			// Send the focus back to the editor.
			if (textView != null && !textView.IsClosed) {
				textView.VisualElement.Focus ();
			}

			// Hide the adornment (Note that this calls DetachFromView on us)
			// This has to be done after the focus transfer because the FindAdornmentManager
			// needs to respond to the focus transfer before it disconnects itself from the view.
			Visibility = Visibility.Collapsed;

			if (textView != null) {
				if (this.IsKeyboardFocusWithin) {
					// This method will remove the Find UI adornment from the visual tree but that on its own
					// won't move the focus from the Find UI. WPF would later get around to restoring a valid focus.
					// There is a problematic case when the adornment is being hidden while doing a FindNext to
					// a document in a different top-level floating window frame because when we do try to focus
					// the other top-level window frame, WPF will detect that focus is still "disconnected"
					// and will restore focus to this editor instead of moving it as we request.
					// By manually moving focus here, it will not get into this "disconnected" state.
					textView.VisualElement.Focus ();
				}

				currentTextView = null;

				textView.GetAdornmentLayer (FindUIAdornmentLayer).RemoveAllAdornments ();
				textView.VisualElement.SizeChanged -= OnViewSizeChanged;
				textView.Closed -= OnViewClosed;
				textView = null;
			}
		}

		private void RaiseFindOptionsChanged (object sender, EventArgs args)
		{
			FindOptionsChanged?.Invoke (FindOptions);
		}

		public FindOptions FindOptions {
			get {
				var options = FindOptions.None;
				if (MatchCaseToggleButton.IsChecked == true) {
					options |= FindOptions.MatchCase;
				}

				if (MatchWholeWordToggleButton.IsChecked == true) {
					options |= FindOptions.WholeWord;
				}

				if (RegularExpressionToggleButton.IsChecked == true) {
					options |= FindOptions.UseRegularExpressions;
				}

				return options;
			}
			set {
				MatchCaseToggleButton.IsChecked = (value & FindOptions.MatchCase) != 0;
				MatchWholeWordToggleButton.IsChecked = (value & FindOptions.WholeWord) != 0;
				RegularExpressionToggleButton.IsChecked = (value & FindOptions.UseRegularExpressions) != 0;
			}
		}

		private bool isInReplaceMode;
		public bool IsInReplaceMode {
			get => isInReplaceMode;
			set {
				if (isInReplaceMode == value) {
					return;
				}

				isInReplaceMode = value;
				FindReplaceToggleButton.IsChecked = value;
				UpdateReplaceVisibility (value);
			}
		}

		private void UpdateReplaceVisibility (bool value)
		{
			var visibility = value ? Visibility.Visible : Visibility.Collapsed;
			ReplaceControlGroup.Visibility = visibility;
			ReplaceButtons.Visibility = visibility;
		}

		private void OnSearchTextChanged (object sender, TextChangedEventArgs e)
		{
			SearchTextChanged?.Invoke (SearchControl.Text);
		}

		private void OnReplaceTextChanged (object sender, TextChangedEventArgs e)
		{
			ReplaceTextChanged?.Invoke (ReplaceControl.Text);
		}

		private void OnFindPreviousClick (object sender, RoutedEventArgs args)
		{
			FindPrevious?.Invoke ();
		}

		private void OnFindNextClick (object sender, RoutedEventArgs args)
		{
			FindNext?.Invoke ();
		}

		private void OnReplaceNext (object sender, RoutedEventArgs e)
		{
			Replace?.Invoke ();
			e.Handled = true;
		}

		private void OnReplaceAll (object sender, RoutedEventArgs e)
		{
			ReplaceAll?.Invoke ();
			e.Handled = true;
		}

		private void OnMouseDown (object sender, MouseButtonEventArgs e)
		{
			// We don't want to allow the mouse click to go through to the editor.
			e.Handled = true;
		}

		private void OnMouseUp (object sender, MouseButtonEventArgs e)
		{
			// We don't want to allow the mouse click to go through to the editor.
			e.Handled = true;
		}

		private void OnGotKeyboardFocus (object sender, KeyboardFocusChangedEventArgs e)
		{
			// Don't let the GotKeyboardFocus event propagate to the parent. The text view, upon receipt of this event,
			// does some processing (among which is IME) and causes issues.
			e.Handled = true;
		}

		private void OnLostKeyboardFocus (object sender, KeyboardFocusChangedEventArgs e)
		{
			// Don't let the LostKeyboardFocus event propagate to the parent. The text view, upon receipt of this event,
			// does some processing (among which is IME) and causes issues.
			e.Handled = true;
		}

		private void OnResizerMouseLeftButtonDown (object sender, MouseButtonEventArgs e)
		{
			if (!this.Resizer.IsMouseCaptured) {
				_resizingBaselinePoint = this.PointToScreen (e.GetPosition (this)).X;
				_resizingBaselineWidth = this.ActualWidth;

				// While in the CaptureMouse call we can reentrantly get the MouseMove event so we
				// have to have initialized the point and width fields above before the call.
				if (!this.Resizer.CaptureMouse ()) {
					_resizingBaselinePoint = .0;
					_resizingBaselineWidth = .0;
				}

				e.Handled = true;
			}
		}

		private void OnResizerMouseLeftButtonUp (object sender, MouseButtonEventArgs e)
		{
			if (this.Resizer.IsMouseCaptured) {
				this.Resizer.ReleaseMouseCapture ();
				_resizingBaselinePoint = .0;
				_resizingBaselineWidth = .0;
				e.Handled = true;
			}
		}

		private void OnResizerMouseMove (object sender, MouseEventArgs e)
		{
			if (this.Resizer.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) {
				Point mouseLocation = e.GetPosition (this);
				Point anchorPoint = this.PointFromScreen (new Point (_resizingBaselinePoint, .0));
				double delta = anchorPoint.X > mouseLocation.X ? anchorPoint.X - mouseLocation.X : -(mouseLocation.X - anchorPoint.X);
				this.Width = Math.Min (Math.Max (this.MinWidth, _resizingBaselineWidth + delta), this.MaxWidth);

				Canvas.SetTop (this, 0.0);
				Canvas.SetLeft (this, currentTextView.ViewportWidth - this.Width);

				e.Handled = true;
			}
		}

		private void OnHide (object sender, RoutedEventArgs e)
		{
			CloseRequested?.Invoke ();
			e.Handled = true;
		}

		private void OnViewClosed (object sender, EventArgs args)
		{
			this.HideAdornment ();
		}

		private void OnViewSizeChanged (object sender, SizeChangedEventArgs e)
		{
			if (e.WidthChanged) {
				Position ();
			}
		}

		public void Position ()
		{
			if (currentTextView != null) {
				this.MaxWidth = currentTextView.ViewportWidth;
				this.Width = Math.Min (Math.Max (this.Width, this.MinWidth), this.MaxWidth);

				Canvas.SetTop (this, 0.0);
				Canvas.SetLeft (this, currentTextView.ViewportWidth - this.Width);
			}
		}

		protected override void OnPreviewKeyDown (KeyEventArgs e)
		{
			if (e == null || e.Handled) {
				return;
			}

			// We handle the Escape key on preview so that we have the chance of handling it before any of the controls inside
			// the find adornment since we want to always dismiss the UI when escape is pressed irrespective of the state of
			// the child controls.
			base.OnPreviewKeyDown (e);

			if (e.Handled) {
				return;
			}

			if (e.Key == Key.Escape) {
				// If the search or replace controls have their pop up open, we want to let Escape take its normal
				// route so that it dismisses the popups. Also check whether a dropdown button has its context menu open.
				if (!InputManager.Current.IsInMenuMode) {
					CloseRequested?.Invoke ();
					e.Handled = true;
				}
			} else if (e.Key == Key.F3) {
				if (e.KeyboardDevice.Modifiers == ModifierKeys.None) {
					FindNext?.Invoke ();
					e.Handled = true;
				} else if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift) {
					FindPrevious?.Invoke ();
					e.Handled = true;
				}
			} else if (e.Key == Key.A && e.KeyboardDevice.Modifiers == ModifierKeys.Alt) {
				ReplaceAll?.Invoke ();
				e.Handled = true;
			} else if (e.Key == Key.R && e.KeyboardDevice.Modifiers == ModifierKeys.Alt) {
				Replace?.Invoke ();
				e.Handled = true;
			}
		}

		private void OnIsVisibleChanged (object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue) {
				this.Position ();
			}
		}

		public void FocusFindBox ()
		{
			Dispatcher.BeginInvoke (new Action (() => this.SearchControl.Focus ()), DispatcherPriority.Input);
		}
	}
}

