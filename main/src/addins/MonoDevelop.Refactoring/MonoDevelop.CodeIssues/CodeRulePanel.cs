//
// CodeRulePanel.cs
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

using System;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;
using System.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;

namespace MonoDevelop.CodeIssues
{
	class CodeRulePanel : OptionsPanel
	{
		CodeRulePanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			widget = new CodeRulePanelWidget ();

			return widget.TextEditor;
		}

		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}

		class CodeRulePanelWidget
		{
			readonly Encoding encoding;
			readonly bool loadingError;

			public TextEditor TextEditor { get; private set; }

			public CodeRulePanelWidget ()
			{
				TextEditor = TextEditorFactory.CreateNewEditor ();
				TextEditor.MimeType = "application/xml";
				TextEditor.Options = DefaultSourceEditorOptions.PlainEditor;
				try {
					TextEditor.Text = TextFileUtility.GetText (TypeSystemService.RuleSetManager.GlobalRulesetFileName, out encoding);
				} catch (Exception e) {
					LoggingService.LogError ("Error while loading global rule set file " + TypeSystemService.RuleSetManager, e);
					loadingError = true;
				}
			}

			internal void ApplyChanges ()
			{
				if (loadingError)
					return;
				try {
					TextFileUtility.WriteText (TypeSystemService.RuleSetManager.GlobalRulesetFileName, TextEditor.Text, encoding);
				} catch (Exception e) {
					LoggingService.LogError ("Error while saving global rule set file " + TypeSystemService.RuleSetManager.GlobalRulesetFileName, e);
					MessageService.ShowError (GettextCatalog.GetString ("Error while saving global rule set file '{0}'.", TypeSystemService.RuleSetManager.GlobalRulesetFileName));
				}
			}
		}
	}
}
