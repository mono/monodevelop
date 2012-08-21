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
using System.Text;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Components;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.Ide.Gui.Content;
using System.Collections.Generic;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Ide.CodeCompletion
{
	class ParameterInformationWindow : PopoverWindow
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
		Gdk.Color foreColor;
		MonoDevelop.Components.FixedWidthWrapLabel headlabel;
		HBox helpbox;
		DelecationViewPagerBubbles infoBubbles = new DelecationViewPagerBubbles ();

		public ParameterInformationWindow ()
		{
			this.AllowShrink = false;
			this.AllowGrow = false;
			
			headlabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			headlabel.Indent = -20;
			var des = FontService.GetFontDescription ("Editor");
			
			headlabel.FontDescription = des;
			
			headlabel.Wrap = Pango.WrapMode.WordChar;
			headlabel.BreakOnCamelCasing = true;
			headlabel.BreakOnPunctuation = true;
			descriptionBox.Spacing = 4;
			VBox vb = new VBox (false, 0);
			vb.PackStart (headlabel, true, true, 0);
			vb.PackStart (descriptionBox, true, true, 0);
			
			HBox hb = new HBox (false, 0);
			hb.PackStart (vb, true, true, 0);
			
			
			vb2.Spacing = 4;
			vb2.PackStart (hb, true, true, 0);
			this.Add (vb2);
			var scheme = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle (Style, PropertyService.Get<string> ("ColorScheme"));
			this.BackgroundColor = scheme.Tooltip.CairoBackgroundColor;
			
			foreColor = scheme.Default.Color;
			headlabel.ModifyFg (StateType.Normal, foreColor);
			ShowAll ();
		}

		int lastParam = -2;
		public void ShowParameterInfo (ParameterDataProvider provider, int overload, int _currentParam, int maxSize)
		{
			if (provider == null)
				throw new ArgumentNullException ("provider");
			int numParams = System.Math.Max (0, provider.GetParameterCount (overload));
			var currentParam = System.Math.Min (_currentParam, numParams - 1);
			if (numParams > 0 && currentParam < 0)
				currentParam = 0;
			if (lastParam == currentParam) {
				return;
			}
			lastParam = currentParam;
			ClearDescriptions ();

			var o = provider.CreateTooltipInformation (overload, _currentParam, false);
			headlabel.Markup = o.SignatureMarkup;
			headlabel.Visible = true;
			
			foreach (var cat in o.Categories) {
				descriptionBox.PackStart (CreateCategory (cat.Item1, cat.Item2), true, true, 4);
			}
			
			if (!string.IsNullOrEmpty (o.SummaryMarkup)) {
				descriptionBox.PackStart (CreateCategory (GettextCatalog.GetString ("Summary"), o.SummaryMarkup), true, true, 4);
			}
			descriptionBox.ShowAll ();
			infoBubbles.Bubbles = provider.Count;
			infoBubbles.ActiveBubble = overload;
			if (helpbox == null && provider.Count >= 2) {
				helpbox = new HBox (false, 0);
				var leftArrow = new DeclarationViewArrow (true);
				helpbox.PackStart (leftArrow, false, false, 0);
				helpbox.PackStart (infoBubbles, true, true, 0);
				var rightArrow = new DeclarationViewArrow (false);
				helpbox.PackEnd (rightArrow, false, false, 0);
				helpbox.BorderWidth = 0;
				vb2.PackStart (helpbox, false, true, 0);
				helpbox.ShowAll ();
			}
			QueueResize ();
		}

		void ClearDescriptions ()
		{
			while (descriptionBox.Children.Length > 0) {
				var child = descriptionBox.Children [0];
				descriptionBox.Remove (child);
				child.Destroy ();
			}
		}

		VBox CreateCategory (string categoryName, string categoryContentMarkup)
		{
			var vbox = new VBox ();
			
			vbox.Spacing = 2;
			
			var catLabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			catLabel.Text = categoryName;
			catLabel.ModifyFg (StateType.Normal, foreColor);
			
			vbox.PackStart (catLabel, false, true, 0);
			
			var contentLabel = new MonoDevelop.Components.FixedWidthWrapLabel ();
			contentLabel.Wrap = Pango.WrapMode.WordChar;
			contentLabel.BreakOnCamelCasing = true;
			contentLabel.BreakOnPunctuation = true;
			contentLabel.Markup = categoryContentMarkup.Trim ();
			contentLabel.ModifyFg (StateType.Normal, foreColor);
			
			vbox.PackStart (contentLabel, true, true, 0);
			
			return vbox;
		}

		public void ChangeOverload ()
		{
			lastParam = -2;
		}
		
		public void HideParameterInfo ()
		{
			ChangeOverload ();
			Hide ();
		}
	}
}
