// ParameterInformationWindow.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//

using System;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.CodeCompletion
{
	class ParameterInformationWindow : XwtThemedPopup
	{
		CompletionTextEditorExtension ext;
		public CompletionTextEditorExtension Ext {
			get {
				return ext;
			}
			set {
				ext = value;
			}
		}

		ICompletionWidget widget;
		public ICompletionWidget Widget {
			get {
				return widget;
			}
			set {
				widget = value;
			}
		}

		VBox descriptionBox = new VBox (false, 0);
		VBox vb2 = new VBox (false, 0);
		Cairo.Color foreColor;
		MonoDevelop.Components.FixedWidthWrapLabel headlabel;

		public ParameterInformationWindow ()
		{
			WindowTransparencyDecorator.Attach (this);

			headlabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			headlabel.Indent = -20;
			
			headlabel.Wrap = Pango.WrapMode.WordChar;
			headlabel.BreakOnCamelCasing = false;
			headlabel.BreakOnPunctuation = false;
			descriptionBox.Spacing = 4;
			VBox vb = new VBox (false, 0);
			vb.PackStart (headlabel, true, true, 0);
			vb.PackStart (descriptionBox, true, true, 0);
			
			HBox hb = new HBox (false, 0);
			hb.PackStart (vb, true, true, 0);
			
			vb2.Spacing = 4;
			vb2.PackStart (hb, true, true, 0);
			Content = BackendHost.ToolkitEngine.WrapWidget (vb2, Xwt.NativeWidgetSizing.DefaultPreferredSize);

			UpdateStyle ();
			Styles.Changed += HandleThemeChanged;
			IdeApp.Preferences.ColorScheme.Changed += HandleThemeChanged;

			vb2.ShowAll ();
			//DesktopService.RemoveWindowShadow (this);
			Content.BoundsChanged += Content_BoundsChanged;
		}

		void Content_BoundsChanged (object sender, EventArgs e)
		{
			UpdateParameterInfoLocation ();
		}

		internal async void UpdateParameterInfoLocation ()
		{
			var isCompletionWindowVisible = (CompletionWindowManager.Wnd?.Visible ?? false);
			var ctx = Widget.CurrentCodeCompletionContext;
			var lineHeight = (int)Ext.Editor.LineHeight;
			var geometry = Xwt.MessageDialog.RootWindow.Screen.VisibleBounds;
			var cmg = ParameterInformationWindowManager.CurrentMethodGroup;

			int cparam = Ext != null ? await Ext.GetCurrentParameterIndex (cmg.MethodProvider.ApplicableSpan.Start) : 0;
			var lastW = (int)Width;
			var lastH = (int)Height;

			int X, Y;
			X = cmg.CompletionContext.TriggerXCoord;
			if (isCompletionWindowVisible) {
				// place above
				Y = ctx.TriggerYCoord - lineHeight - (int)lastH - 10;
			} else {
				// place below
				Y = ctx.TriggerYCoord;
			}

			if (X + lastW > geometry.Right)
				X = (int)geometry.Right - (int)lastW;
			if (Y < geometry.Top)
				Y = ctx.TriggerYCoord;
			if (Y + lastH > geometry.Bottom) {
				Y = Y - lineHeight - (int)lastH - 4;
			}

			if (isCompletionWindowVisible) {
				var completionWindow = new Xwt.Rectangle (CompletionWindowManager.X, CompletionWindowManager.Y - lineHeight, CompletionWindowManager.Wnd.Allocation.Width, CompletionWindowManager.Wnd.Allocation.Height + lineHeight * 2);
				if (completionWindow.IntersectsWith (new Xwt.Rectangle (X, Y, lastW, lastH))) {
					X = (int)completionWindow.X;
					Y = (int)completionWindow.Y - (int)lastH - 6;
					if (Y < 0) {
						Y = (int)completionWindow.Bottom + 6;
					}
				}
			}
			Location = new Xwt.Point (X, Y);
		}

		void UpdateStyle ()
		{
			var scheme = SyntaxHighlightingService.GetEditorTheme (IdeApp.Preferences.ColorScheme);
			if (!scheme.FitsIdeTheme (IdeApp.Preferences.UserInterfaceTheme))
				scheme = SyntaxHighlightingService.GetDefaultColorStyle (IdeApp.Preferences.UserInterfaceTheme);
			
			Theme.SetSchemeColors (scheme);
			Theme.Font = FontService.SansFont.CopyModified (Styles.FontScale11).ToXwtFont ();
			Theme.ShadowColor = Styles.PopoverWindow.ShadowColor;
			foreColor = Styles.PopoverWindow.DefaultTextColor.ToCairoColor ();

			headlabel.ModifyFg (StateType.Normal, foreColor.ToGdkColor ());
			headlabel.FontDescription = FontService.GetFontDescription ("Editor").CopyModified (Styles.FontScale11);

			//if (this.Visible)
			//	QueueDraw ();
		}

		void HandleThemeChanged (object sender, EventArgs e)
		{
			UpdateStyle ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				Styles.Changed -= HandleThemeChanged;
				IdeApp.Preferences.ColorScheme.Changed -= HandleThemeChanged;
			}
			base.Dispose (disposing);
		}

		int lastParam = -2;
		TooltipInformation currentTooltipInformation;
		ParameterHintingResult lastProvider;
		CancellationTokenSource cancellationTokenSource;

		public async void ShowParameterInfo (ParameterHintingResult provider, int overload, int _currentParam, int maxSize)
		{
			if (provider == null)
				throw new ArgumentNullException ("provider");
			int numParams = System.Math.Max (0, provider [overload].ParameterCount);
			var currentParam = System.Math.Min (_currentParam, numParams - 1);
			if (numParams > 0 && currentParam < 0)
				currentParam = 0;
			if (lastParam == currentParam && (currentTooltipInformation != null) && lastProvider == provider) {
				return;
			}
			lastProvider = provider;

			lastParam = currentParam;
			var parameterHintingData = (ParameterHintingData)provider [overload];
			ResetTooltipInformation ();
			if (ext == null) {
				// ext == null means HideParameterInfo was called aka. we are not in valid context to display tooltip anymore
				lastParam = -2;
				return;
			}
			var ct = new CancellationTokenSource ();
			try {
				cancellationTokenSource = ct;
				currentTooltipInformation = await parameterHintingData.CreateTooltipInformation (ext.Editor, ext.DocumentContext, currentParam, false, ct.Token);
			} catch (Exception ex) {
				if (!(ex is TaskCanceledException))
					LoggingService.LogError ("Error while getting tooltip information", ex);
				return;
			}

			if (ct.IsCancellationRequested)
				return;

			cancellationTokenSource = null;

			Theme.NumPages = provider.Count;
			Theme.CurrentPage = overload;

			if (provider.Count > 1) {
				Theme.DrawPager = true;
				Theme.PagerVertical = true;
			}

			ShowTooltipInfo ();
		}

		void ShowTooltipInfo ()
		{
			ClearDescriptions ();
			headlabel.Markup = currentTooltipInformation.SignatureMarkup;
			headlabel.Visible = true;
			if (Theme.DrawPager)
				headlabel.WidthRequest = headlabel.RealWidth + 70;
			
			foreach (var cat in currentTooltipInformation.Categories) {
				descriptionBox.PackStart (CreateCategory (TooltipInformationWindow.GetHeaderMarkup (cat.Item1), cat.Item2), true, true, 4);
			}
			
			if (!string.IsNullOrEmpty (currentTooltipInformation.SummaryMarkup)) {
				descriptionBox.PackStart (CreateCategory (TooltipInformationWindow.GetHeaderMarkup (GettextCatalog.GetString ("Summary")), currentTooltipInformation.SummaryMarkup), true, true, 4);
			}
			descriptionBox.ShowAll ();
			Content.QueueForReallocate ();
			Show ();
		}

		void CurrentTooltipInformation_Changed (object sender, EventArgs e)
		{
			ShowTooltipInfo ();
		}

		void ClearDescriptions ()
		{
			while (descriptionBox.Children.Length > 0) {
				var child = descriptionBox.Children [0];
				descriptionBox.Remove (child);
				child.Destroy ();
			}
		}

		void ResetTooltipInformation ()
		{
			if (cancellationTokenSource != null) {
				cancellationTokenSource.Cancel ();
				cancellationTokenSource = null;
			}
			currentTooltipInformation = null;
		}

		VBox CreateCategory (string categoryName, string categoryContentMarkup)
		{
			return TooltipInformationWindow.CreateCategory (categoryName, categoryContentMarkup, foreColor, Theme.Font.ToPangoFont ());
		}

		public void ChangeOverload ()
		{
			lastParam = -2;
			ResetTooltipInformation ();
		}

		protected override bool OnPagerLeftClicked ()
		{
			if (Ext != null && Widget != null)
				return ParameterInformationWindowManager.OverloadUp (Ext, Widget);
			return base.OnPagerRightClicked ();
		}

		protected override bool OnPagerRightClicked ()
		{
			if (Ext != null && Widget != null)
				return ParameterInformationWindowManager.OverloadDown (Ext, Widget);
			return base.OnPagerRightClicked ();
		}
		
		public void HideParameterInfo ()
		{
			ChangeOverload ();
			Hide ();
			Ext = null;
			Widget = null;
		}
	}
}
