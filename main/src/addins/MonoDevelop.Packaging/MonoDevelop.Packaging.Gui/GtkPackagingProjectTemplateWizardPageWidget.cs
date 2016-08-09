//
// GtkPackagingProjectTemplateWizardPageWidget.cs
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

using System;
using Gtk;
using MonoDevelop.Packaging.Templating;

namespace MonoDevelop.Packaging.Gui
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class GtkPackagingProjectTemplateWizardPageWidget : Gtk.Bin
	{
		PackagingProjectTemplateWizardPage wizardPage;

		public GtkPackagingProjectTemplateWizardPageWidget ()
		{
			this.Build ();
		}

		internal GtkPackagingProjectTemplateWizardPageWidget (PackagingProjectTemplateWizardPage wizardPage)
			: this ()
		{
			this.wizardPage = wizardPage;

			packageDescriptionTextView.AcceptsTab = false;

			packageAuthorsTextBox.Text = wizardPage.Authors;
			packageVersionTextBox.Text = wizardPage.Version;

			packageIdTextBox.TextInserted += PackageIdTextInserted;
			packageIdTextBox.Changed += PackageIdTextBoxChanged;
			packageVersionTextBox.Changed += PackageVersionTextBoxChanged;
			packageAuthorsTextBox.Changed += PackageDescriptionTextChanged;
			packageDescriptionTextView.Buffer.Changed += PackageDescriptionTextChanged;

			packageIdTextBox.ActivatesDefault = true;
			packageVersionTextBox.ActivatesDefault = true;
			packageAuthorsTextBox.ActivatesDefault = true;

			packageIdTextBox.TruncateMultiline = true;
			packageVersionTextBox.TruncateMultiline = true;
			packageAuthorsTextBox.TruncateMultiline = true;
		}

		protected override void OnFocusGrabbed ()
		{
			packageIdTextBox.GrabFocus ();
		}

		void PackageIdTextInserted (object o, TextInsertedArgs args)
		{
			if (args.Text.IndexOf ('\r') >= 0) {
				var textBox = (Entry)o;
				textBox.Text = textBox.Text.Replace ("\r", string.Empty);
			}
		}

		void PackageIdTextBoxChanged (object sender, EventArgs e)
		{
			wizardPage.Id = packageIdTextBox.Text;
		}

		void PackageVersionTextBoxChanged (object sender, EventArgs e)
		{
			wizardPage.Version = packageVersionTextBox.Text;
		}

		void PackageAuthorsTextBoxChanged (object sender, EventArgs e)
		{
			wizardPage.Authors = packageAuthorsTextBox.Text;
		}

		void PackageDescriptionTextChanged (object sender, EventArgs e)
		{
			wizardPage.Description = packageDescriptionTextView.Buffer.Text;
		}
	}
}
