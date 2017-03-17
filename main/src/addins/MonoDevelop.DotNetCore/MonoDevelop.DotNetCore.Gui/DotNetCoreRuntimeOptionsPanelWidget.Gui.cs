//
// DotNetCoreRuntimeOptionsPanelWidget.Gui.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.DotNetCore.Gui
{
	partial class DotNetCoreRuntimeOptionsPanelWidget : Bin 
	{
		VBox mainVBox;
		HBox mainHBox;
		Label targetFrameworkLabel;
		ComboBox runtimeVersionCombo;

		void Build ()
		{
			Stetic.Gui.Initialize (this);
			Stetic.BinContainer.Attach (this);

			Name = "MonoDevelop.Ide.Projects.DotNetCoreRuntimeOptionsPanelWidget";
			mainVBox = new VBox ();
			mainVBox.Name = "mainVBox";
			mainVBox.Spacing = 12;

			mainHBox = new HBox ();
			mainHBox.Name = "mainHBox";
			mainHBox.Spacing = 7;

			targetFrameworkLabel = new Label ();
			targetFrameworkLabel.Name = "targetFrameworkLabel";
			targetFrameworkLabel.Xalign = 0F;
			targetFrameworkLabel.LabelProp = GettextCatalog.GetString ("Target _framework:");
			targetFrameworkLabel.UseUnderline = true;
			mainHBox.PackStart (targetFrameworkLabel, false, true, 0);

			runtimeVersionCombo = ComboBox.NewText ();
			runtimeVersionCombo.Name = "runtimeVersionCombo";
			mainHBox.PackStart (runtimeVersionCombo, false, false, 0);
			mainVBox.PackStart (mainHBox, false, false, 0);

			Add (mainVBox);
			if ((Child != null)) {
				Child.ShowAll ();
			}
			Show ();
		}
	}
}
