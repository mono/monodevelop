//
// OnTheFlyFormattingPanel.cs
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

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor;
using Xwt;
using MonoDevelop.Ide.Composition;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis;
using System;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	class OnTheFlyFormattingPanel : OptionsPanel
	{
		OnTheFlyFormattingPanelWidget widget;

		public override void ApplyChanges ()
		{
			widget?.ApplyChanges ();
		}

		public override Control CreatePanelWidget ()
		{
			widget = new OnTheFlyFormattingPanelWidget ();
			if (Xwt.Toolkit.CurrentEngine.Type != Xwt.ToolkitType.Gtk) {
				LoggingService.LogError ("OnTheFlyFormattingPanel: Xwt.Toolkit.CurrentEngine.Type != Xwt.ToolkitType.Gtk - currently unsupported");
				return null;
			}
			return (Gtk.Widget)Xwt.Toolkit.CurrentEngine.GetNativeWidget (widget);
		}

		class OnTheFlyFormattingPanelWidget : VBox
		{
			readonly IOptionService OptionService;

			readonly CheckBox formatOnTypeCheckBox;
			readonly CheckBox formatOnSemicolonCheckBox;
			readonly CheckBox formatOnCloseBraceCheckBox;
			readonly CheckBox formatOnReturnCheckBox;
			readonly CheckBox formatOnPasteCheckBox;
			readonly Ide.RoslynServices.Options.RoslynPreferences.PerLanguagePreferences preferences;

			public OnTheFlyFormattingPanelWidget ()
			{
				preferences = Ide.IdeApp.Preferences.Roslyn.CSharp;

				OptionService = TypeSystemService.Workspace.Services.GetService<IOptionService> ();
				formatOnTypeCheckBox = new CheckBox (GettextCatalog.GetString ("Automatically format when typing"));
				formatOnTypeCheckBox.Active = preferences.AutoFormattingOnTyping;
				formatOnTypeCheckBox.Toggled += FormatOnTypeCheckBox_Toggled;
				PackStart (formatOnTypeCheckBox);

				formatOnSemicolonCheckBox = new CheckBox (GettextCatalog.GetString ("Automatically format statement on ;"));
				formatOnSemicolonCheckBox.Active = preferences.AutoFormattingOnSemicolon;
				formatOnSemicolonCheckBox.MarginLeft = 18;
				PackStart (formatOnSemicolonCheckBox);

				formatOnCloseBraceCheckBox = new CheckBox (GettextCatalog.GetString ("Automatically format block on }"));
				formatOnCloseBraceCheckBox.Active = preferences.AutoFormattingOnCloseBrace;
				formatOnCloseBraceCheckBox.MarginLeft = 18;
				PackStart (formatOnCloseBraceCheckBox);

				formatOnReturnCheckBox = new CheckBox (GettextCatalog.GetString ("Automatically format on return"));
				formatOnReturnCheckBox.Active = preferences.AutoFormattingOnReturn;
				PackStart (formatOnReturnCheckBox);

				formatOnPasteCheckBox = new CheckBox (GettextCatalog.GetString ("Automatically format on paste"));
				formatOnPasteCheckBox.Active = preferences.FormatOnPaste;
				PackStart (formatOnPasteCheckBox);
				FormatOnTypeCheckBox_Toggled (this, EventArgs.Empty);
			}

			void FormatOnTypeCheckBox_Toggled (object sender, System.EventArgs e)
			{
				formatOnSemicolonCheckBox.Sensitive = formatOnCloseBraceCheckBox.Sensitive = formatOnTypeCheckBox.Active;
			}


			public void ApplyChanges ()
			{
				preferences.AutoFormattingOnTyping.Value = formatOnTypeCheckBox.Active;
				preferences.AutoFormattingOnSemicolon.Value = formatOnSemicolonCheckBox.Active;
				preferences.AutoFormattingOnCloseBrace.Value = formatOnCloseBraceCheckBox.Active;
				preferences.AutoFormattingOnReturn.Value = formatOnReturnCheckBox.Active;
				preferences.FormatOnPaste.Value = formatOnPasteCheckBox.Active;
				PropertyService.SaveProperties ();

			}
		}
	}
}
