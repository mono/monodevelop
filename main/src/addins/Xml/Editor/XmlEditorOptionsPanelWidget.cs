// 
// XmlEditorOptionsPanelWidget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Xml.Editor
{
	class XmlEditorOptionsPanelWidget : VBox
	{
		readonly CheckButton autoCompleteElementsCheck, autoAddPunctuationCheck, showSchemaAnnotationCheck, autoShowCodeCompletionCheck;

		public XmlEditorOptionsPanelWidget ()
		{
			Spacing = 6;

			autoCompleteElementsCheck = new CheckButton (GettextCatalog.GetString ("Automatically insert closing tags"));
			autoCompleteElementsCheck.SetCommonAccessibilityAttributes ("XmlOptionsPanel.autoComplete", "",
			                                                            GettextCatalog.GetString ("Check to enable automatic close tag insertion"));
			autoAddPunctuationCheck = new CheckButton (GettextCatalog.GetString ("Automatically insert punctuation (=\"\", />, etc.)"));
			autoAddPunctuationCheck.SetCommonAccessibilityAttributes ("XmlOptionsPanel.autoAdd", "",
			                                                          GettextCatalog.GetString ("Check to enable automatic punctuation insertion"));
			showSchemaAnnotationCheck = new CheckButton (GettextCatalog.GetString ("Show schema annotation"));

			autoShowCodeCompletionCheck = new CheckButton (GettextCatalog.GetString ("Automatically show code completion"));
			autoShowCodeCompletionCheck.SetCommonAccessibilityAttributes ("XmlOptionsPanel.autoShowCompletion", "",
																	  GettextCatalog.GetString ("Check to enable automatic showing of code completion"));

			PackStart (autoCompleteElementsCheck, false, false, 0);
			PackStart (autoAddPunctuationCheck, false, false, 0);
			//PackStart (showSchemaAnnotationCheck, false, false, 0);
			PackStart (autoShowCodeCompletionCheck, false, false, 0);

			ShowAll ();
		}

		public bool AutoCompleteElements {
			get { return autoCompleteElementsCheck.Active; }
			set { autoCompleteElementsCheck.Active = value; }
		}

		public bool ShowSchemaAnnotation {
			get { return showSchemaAnnotationCheck.Active; }
			set { showSchemaAnnotationCheck.Active = value; }
		}

		public bool AutoInsertFragments {
			get { return autoAddPunctuationCheck.Active; }
			set { autoAddPunctuationCheck.Active = value; }
		}

		public bool AutoShowCodeCompletion {
			get { return autoShowCodeCompletionCheck.Active; }
			set { autoShowCodeCompletionCheck.Active = value; }
		}
	}
}
