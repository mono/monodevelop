//
// PackageSourcesLoadErrorWidget.UI.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement.Gui
{
	partial class PackageSourcesLoadErrorWidget : Gtk.Bin
	{
		Label mainMessageLabel;
		Label secondaryMessageLabel;
		Label openNuGetConfigFileLabel;

		void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			global::Stetic.BinContainer.Attach (this);

			var vbox = new VBox ();
			vbox.Spacing = 6;

			mainMessageLabel = new Label ();
			mainMessageLabel.LineWrap = true;
			mainMessageLabel.LineWrapMode = Pango.WrapMode.Word;
			mainMessageLabel.Xalign = 0;
			mainMessageLabel.Yalign = 0;

			vbox.PackStart (mainMessageLabel, false, false, 6);

			secondaryMessageLabel = new Label ();
			secondaryMessageLabel.LineWrap = true;
			secondaryMessageLabel.LineWrapMode = Pango.WrapMode.Word;
			secondaryMessageLabel.Xalign = 0;
			secondaryMessageLabel.Yalign = 0;
			vbox.PackStart (secondaryMessageLabel, false, false, 6);

			openNuGetConfigFileLabel = new Label ();
			openNuGetConfigFileLabel.UseMarkup = true;
			openNuGetConfigFileLabel.Markup = GetOpenNuGetConfigFileLabel ();
			openNuGetConfigFileLabel.Xalign = 0;
			openNuGetConfigFileLabel.Yalign = 0;

			vbox.PackStart (openNuGetConfigFileLabel, true, true, 6);

			Add (vbox);

			ShowAll ();
		}

		string GetOpenNuGetConfigFileLabel ()
		{
			string text = GettextCatalog.GetString ("Open NuGet.Config file...");
			return "<a href=\"page://openfile\">" + text + "</a>";
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			secondaryMessageLabel.SetSizeRequest (allocation.Width - 20, -1);
		}
	}
}
