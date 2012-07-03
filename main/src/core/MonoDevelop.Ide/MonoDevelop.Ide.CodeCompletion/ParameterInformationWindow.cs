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

namespace MonoDevelop.Ide.CodeCompletion
{
	class ParameterInformationWindow : TooltipWindow
	{
		Gtk.Label heading;
		DescriptionLabel desc;
		Gtk.Label count;
		Gtk.Arrow goPrev;
		Gtk.Arrow goNext;
		
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
		
		public ParameterInformationWindow ()
		{
			heading = new Gtk.Label ("");
			heading.Xalign = 0;
			heading.Wrap = false;
			
			desc = new DescriptionLabel ();
			count = new Gtk.Label ("");

			var mainBox = new HBox (false, 2);
			
			HBox arrowHBox = new HBox ();

			goPrev = new Gtk.Arrow (Gtk.ArrowType.Up, ShadowType.None);
			arrowHBox.PackStart (goPrev, false, false, 0);
			arrowHBox.PackStart (count, false, false, 0);
			goNext = new Gtk.Arrow (Gtk.ArrowType.Down, ShadowType.None);
			arrowHBox.PackStart (goNext, false, false, 0);

			VBox vBox = new VBox ();
			vBox.PackStart (arrowHBox, false, false, 0);
			
			mainBox.PackStart (vBox, false, false, 0);
			mainBox.PackStart (heading, true, true, 0);
			
			var vBox2 = new VBox ();
			vBox2.BorderWidth = 3;
			vBox2.PackStart (mainBox, false, false, 0);
			vBox2.PackStart (desc, true, true, 4);
			Add (vBox2);
			EnableTransparencyControl = true;
			ShowAll ();
		}
		Dictionary<int, bool> doBreakParameters = new Dictionary<int, bool> ();

		int lastParam = -2;
		public void ShowParameterInfo (IParameterDataProvider provider, int overload, int _currentParam, int maxSize)
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

			string[] paramText = new string[numParams];
			for (int i = 0; i < numParams; i++) {
				string txt = provider.GetParameterDescription (overload, i);
				if (i == currentParam)
					txt = "<u><span foreground='" + LinkColor + "'>" + txt + "</span></u>";
				paramText [i] = txt;
			}
			string text = provider.GetHeading (overload, paramText, currentParam);
			heading.Markup = text;
			desc.Markup = provider.GetDescription (overload, currentParam);
			if (provider.Count > 1) {
				count.Show ();
				goPrev.Show ();
				goNext.Show ();
				count.Text = GettextCatalog.GetString ("{0} of {1}", overload + 1, provider.Count);
			} else {
				count.Hide ();
				goPrev.Hide ();
				goNext.Hide ();
			}
			var req = this.SizeRequest ();

			if (doBreakParameters.ContainsKey (overload) || req.Width > maxSize) {
				for (int i = 1; i < numParams; i++) {
					paramText [i] = Environment.NewLine + "\t" + paramText [i];
				}
				text = provider.GetHeading (overload, paramText, currentParam);
				heading.Markup = text;
				doBreakParameters [overload] = true;
			}
			QueueResize ();
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

		// The wrapping of Gtk.Label wasn't exactly what's needed in that case
		class DescriptionLabel : Gtk.DrawingArea
		{
			string markup;
			bool resizeRequested = true;
			
			public string Markup {
				get {
					return markup ?? "";
				}
				set {
					markup = value;
					Visible = !string.IsNullOrWhiteSpace (value);
					resizeRequested = true;
					QueueResize ();
				}
			}
		
			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				using (var layout = new Pango.Layout (this.PangoContext)) {
					layout.Width = (int)(Allocation.Width * Pango.Scale.PangoScale);
					layout.Wrap = Pango.WrapMode.Word;
					layout.Alignment = Pango.Alignment.Left;
					layout.SetMarkup (Markup);
					evnt.Window.DrawLayout (Style.TextGC (StateType.Normal), 0, 0, layout);
				}
				return true;
			}
		
			protected override void OnSizeAllocated (Gdk.Rectangle allocation)
			{
				base.OnSizeAllocated (allocation);
				if (!resizeRequested)
					return;
				using (var layout = new Pango.Layout (this.PangoContext)) {
					layout.Width = (int)(allocation.Width * Pango.Scale.PangoScale);
					layout.Wrap = Pango.WrapMode.Word;
					layout.Alignment = Pango.Alignment.Left;
					layout.SetMarkup (Markup);
					int w, h;
					layout.GetPixelSize (out w, out h);
					if (h > 0 && h != allocation.Height) {
						HeightRequest = h;
						resizeRequested = false;
						QueueResize ();
					}
				}
			
			}

		}
	}
}
