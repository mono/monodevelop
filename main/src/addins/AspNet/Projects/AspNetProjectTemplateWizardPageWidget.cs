//
// AspNetProjectTemplateWizardPageWidget.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using Gdk;

namespace MonoDevelop.AspNet.Projects
{
	class AspNetProjectTemplateWizardPageWidget : HBox
	{
		readonly AspNetProjectTemplateWizardPage page;

		public AspNetProjectTemplateWizardPageWidget (AspNetProjectTemplateWizardPage page)
		{
			this.page = page;

			Build ();

			includeTestProjectCheck.Active = page.IncludeTestProject;
		}

		CheckButton includeTestProjectCheck;
		CheckButton includeMvcCheck;
		CheckButton includeWebFormsCheck;
		CheckButton includeWebApiCheck;

		void Build ()
		{
			var backgroundColor = new Color (225, 228, 232);

			var box = new VBox { BorderWidth = 12, Spacing = 12 };
			var eb = new EventBox { Child = box };
			eb.ModifyBg (StateType.Normal, backgroundColor);
			var infoEB = new EventBox { WidthRequest = 280 };
			infoEB.ModifyBg (StateType.Normal, new Color (255, 255, 255));
			PackStart (eb, true, true, 0);
			PackStart (infoEB, false, false, 0);

			var frameworkLabel = new Label ("Include references and folders for:") { Xalign = 0 };
			box.PackStart (frameworkLabel, false, false, 0);
			var frameworkBox = new VBox { Spacing = 6 };
			box.PackStart (new Alignment (0, 0, 0, 0) { LeftPadding = 24, Child = frameworkBox } , false, false, 0);

			if (page.AspNetMvcMutable || page.AspNetMvcEnabled) {
				includeMvcCheck = CreateFancyCheckButton (
					"MVC",
					"Modern programming model. Unit testable, choice of templating languages."
				);
				includeMvcCheck.Active = page.AspNetMvcEnabled;
				includeMvcCheck.Toggled += (sender, e) => {
					page.AspNetMvcEnabled = includeMvcCheck.Active;
				};
				includeMvcCheck.Sensitive = page.AspNetMvcMutable;
				frameworkBox.PackStart (includeMvcCheck, false, false, 0);
			}

			if (page.AspNetWebFormsMutable || page.AspNetWebFormsEnabled) {
				includeWebFormsCheck = CreateFancyCheckButton (
					"Web Forms",
					"Stateful programming model similar to desktop applications."
				);
				includeWebFormsCheck.Active = page.AspNetWebFormsEnabled;
				includeWebFormsCheck.Toggled += (sender, e) => {
					page.AspNetWebFormsEnabled = includeWebFormsCheck.Active;
				};
				includeWebFormsCheck.Sensitive = page.AspNetWebFormsMutable;
				frameworkBox.PackStart (includeWebFormsCheck, false, false, 0);
			}

			if (page.AspNetWebApiMutable || page.AspNetWebApiEnabled) {
				includeWebApiCheck = CreateFancyCheckButton (
					"Web API",
					"Framework for creating HTTP web services."
				);
				includeWebApiCheck.Active = page.AspNetWebApiEnabled;
				includeWebApiCheck.Toggled += (sender, e) => {
					page.AspNetWebApiEnabled = includeWebApiCheck.Active;
				};
				includeWebApiCheck.Sensitive = page.AspNetWebApiMutable;
				frameworkBox.PackStart (includeWebApiCheck, false, false, 0);
			}

			includeTestProjectCheck = CreateFancyCheckButton (
				"Include Unit Test Project",
				"Add a Unit Test Project for testing the Web Project using NUnit."
			);
			includeTestProjectCheck.Toggled += (sender, e) => {
				page.IncludeTestProject = includeTestProjectCheck.Active;
			};

			box.PackStart (includeTestProjectCheck, false, false, 0);

			ShowAll ();
		}

		static CheckButton CreateFancyCheckButton (string title, string detail)
		{
			var button = new CheckButton ();
			var box = new VBox { Spacing = 4 };
			var titleLabel = new Label {
				Markup = "<b>" + GLib.Markup.EscapeText (title) + "</b>",
				Xalign = 0
			};
			box.PackStart (titleLabel, false, false, 0);
			var detailLabel = new Label {
				Text = detail,
				Xalign = 0
			};
			box.PackStart (detailLabel, false, false, 0);
			button.Child = new Alignment (0, 0,  0, 0)  { LeftPadding = 6, Child =  box };
			return button;
		}
	}
}
