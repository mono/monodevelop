//
// GtkProjectNuGetBuildOptions.UI.cs
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

namespace MonoDevelop.Packaging.Gui
{
	partial class GtkProjectNuGetBuildOptionsPanelWidget : Gtk.Bin
	{
		CheckButton packOnBuildButton;
		Label missingMetadataLabel;

		void Build ()
		{
#pragma warning disable 436
			Stetic.Gui.Initialize (this);
			Stetic.BinContainer.Attach (this);
#pragma warning restore 436

			var vbox = new VBox ();
			vbox.Spacing = 6;

			packOnBuildButton = new CheckButton ();
			packOnBuildButton.Label = GettextCatalog.GetString ("Create a NuGet Package when building the project.");

			vbox.PackStart (packOnBuildButton, false, false, 10);

			missingMetadataLabel = new Label ();
			missingMetadataLabel.LineWrapMode = Pango.WrapMode.Word;
			missingMetadataLabel.Wrap = true;
			missingMetadataLabel.Xalign = 0;
			missingMetadataLabel.Yalign = 0;
			missingMetadataLabel.Xpad = 20;
			missingMetadataLabel.WidthRequest = 600;
			missingMetadataLabel.Text = GettextCatalog.GetString ("The project does not have NuGet package metadata so a NuGet package will not be created. NuGet package metadata can be specified in the Metadata section in Project Options");

			vbox.PackStart (missingMetadataLabel);

			Add (vbox);

			ShowAll ();
		}
	}
}
