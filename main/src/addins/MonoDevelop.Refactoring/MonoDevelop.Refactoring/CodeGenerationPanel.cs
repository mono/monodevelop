//
// CodeGenerationPanel.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using Xwt;

namespace MonoDevelop.Refactoring
{
	class CodeGenerationPanel : OptionsPanel
	{
		CodeGenerationPanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			widget = new CodeGenerationPanelWidget (LanguageNames.CSharp);

			return (Gtk.Widget)Toolkit.CurrentEngine.GetNativeWidget (widget);
		}

		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}

		class CodeGenerationPanelWidget : Xwt.VBox
		{
			string languageName;
			CheckBox placeSystemNamespaceFirst;
			CheckBox separateImportDirectiveGroups;

			ConfigurationProperty<bool> placeSystemNamespaceFirstProp;
			ConfigurationProperty<bool> separateImportDirectiveGroupsProp;

			public CodeGenerationPanelWidget (string languageName)
			{
				this.languageName = languageName;

				placeSystemNamespaceFirstProp = Ide.IdeApp.Preferences.Roslyn.For (languageName).PlaceSystemNamespaceFirst;
				separateImportDirectiveGroupsProp = Ide.IdeApp.Preferences.Roslyn.For (languageName).PlaceSystemNamespaceFirst;

				var hBox = new HBox ();
				hBox.PackStart (new Label { Markup = GettextCatalog.GetString ("Organize Usings") });
				hBox.PackStart (new HSeparator (), true, true);

				this.PackStart (hBox);
				this.PackStart (placeSystemNamespaceFirst = new CheckBox (GettextCatalog.GetString ("Place 'System' directives first when sorting usings")));
				this.PackStart (separateImportDirectiveGroups = new CheckBox (GettextCatalog.GetString ("Separate using groups when sorting")));
				placeSystemNamespaceFirst.Active = placeSystemNamespaceFirstProp.Value;
				separateImportDirectiveGroups.Active = separateImportDirectiveGroupsProp.Value;
			}

			public void ApplyChanges ()
			{
				placeSystemNamespaceFirstProp.Value = placeSystemNamespaceFirst.Active;
				separateImportDirectiveGroupsProp.Value = separateImportDirectiveGroups.Active;
			}
		}
	}
}
