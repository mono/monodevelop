//
// SourceLinkDialog.UI.cs
//
// Author:
//       Jason Imison <jaimison@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp. (http://microsoft.com)
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
using MonoDevelop.Core;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Gui.Dialogs;

using Xwt;
using Xwt.Drawing;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Debugger
{
	class SourceLinkDialog : Dialog
	{
		readonly string frameName;
		readonly string uri;
		readonly string fileName;
		CheckBox checkBox;

		public static Command GetAndOpenCommand { get; private set; }

        public SourceLinkDialog (string frameName, string uri, string fileName)
		{
			this.frameName = frameName;
			this.uri = uri;
			this.fileName = fileName;
			Build ();
		}

		void Build ()
		{
			var t = Xwt.Toolkit.NativeEngine;
			Width = 500;
			Resizable = false;
			this.Decorated = false;
			var hbox = new HBox ();
			hbox.Accessible.Role = Xwt.Accessibility.Role.Filler;
			var icon = new ImageView ();
			icon.WidthRequest = 100;

			Image logo;

			var aboutFile = BrandingService.GetFile ("VisualStudio128.png");
			if (aboutFile != null)
				logo = Image.FromFile (aboutFile);
			else
				logo = Image.FromResource (AboutDialogImage.Name);

			logo = logo.Scale (0.8);
			icon.Image = logo;
			hbox.VerticalPlacement = WidgetPlacement.Start;
			hbox.PackStart (icon);

			var mainVBox = new VBox ();
			mainVBox.Accessible.Role = Xwt.Accessibility.Role.Filler;
			hbox.PackEnd (mainVBox);
			Content = hbox;

			var box = new HBox ();
			box.Accessible.Role = Xwt.Accessibility.Role.Filler;
			mainVBox.PackStart (box);

			var label1 = new Label ();
			label1.MarginTop = 10;
			label1.Font = IdeServices.FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale14).ToXwtFont (); 

			label1.Markup = $"<b>{GettextCatalog.GetString ("External source code available")}</b>";

			box.PackStart (label1);

			var box2 = new HBox ();
			box.Accessible.Role = Xwt.Accessibility.Role.Filler;
			mainVBox.PackStart (box2);

			var label2 = new Label ();
			label2.MarginTop = 10;
			var text = GettextCatalog.GetString ("is a call to external source code. Would you like to get and view it?");
			label2.Markup = $"<b>{ frameName }</b> {text}";
			label2.Wrap = WrapMode.Word;
			label2.WidthRequest = 350;
			box2.PackStart (label2);

			var expander = new Expander ();
			expander.Label = GettextCatalog.GetString("Details");
			expander.Expanded = true;

			mainVBox.PackStart (expander);

			var table = new Table ();
			table.MarginTop = 5;
			var fileLabel = new Label ();
			fileLabel.Text = GettextCatalog.GetString ("File:");
			fileLabel.TextAlignment = Alignment.End;

			table.Add (fileLabel, 0, 0);
			var fileNameLabel = new Label ();
			fileNameLabel.Text = fileName;

			table.Add (fileNameLabel, 1, 0);

			expander.Content = table;

			var addressLabel = new Label ();
			addressLabel.Text = GettextCatalog.GetString ("Address:");
			addressLabel.TextAlignment = Alignment.End;

			table.Add (addressLabel, 0, 1, vpos:WidgetPlacement.Start);
			var uriLabel = new LinkLabel ();
			uriLabel.Text = uri;
			uriLabel.Uri = new Uri (uri);
			uriLabel.Wrap = WrapMode.Character;
			uriLabel.SetCommonAccessibilityAttributes ("SourceLinkDialog.url", uriLabel,
				GettextCatalog.GetString ("The URL where the source code will be downloaded from."));
			uriLabel.WidthRequest = 280;

			table.Add (uriLabel, 1, 1);
			checkBox = new CheckBox (GettextCatalog.GetString ("Always get source code automatically"));
			checkBox.Active = PropertyService.Get ("SourceLink.OpenAutomatically", true);
			var checkHBox = new HBox ();
			checkHBox.PackStart (checkBox);
			mainVBox.PackStart (checkHBox);
			var cancelButton = new DialogButton (Command.Cancel);
			Buttons.Add (cancelButton);

			GetAndOpenCommand = new Command ("GetAndOpen", GettextCatalog.GetString ("Get and Open"));
			this.DefaultCommand = GetAndOpenCommand;

			CommandActivated += GetAndOpenButton_Clicked;
		}

		void GetAndOpenButton_Clicked (object sender, EventArgs e)
		{
			PropertyService.Set ("SourceLink.OpenAutomatically", checkBox.Active);
		}


		protected override void Dispose (bool disposing)
		{
			//CommandActivated.Clicked -= GetAndOpenButton_Clicked;
			base.Dispose (disposing);
		}
	}
}
