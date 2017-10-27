// 
// CSharpFormattingProfileDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Ide;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Content;
using Microsoft.CodeAnalysis.CSharp.Formatting;


namespace MonoDevelop.CSharp.Formatting
{
	partial class CSharpFormattingProfileDialog : Gtk.Dialog
	{
		readonly TextEditor texteditor = TextEditorFactory.CreateNewEditor ();
		readonly CSharpFormattingPolicy profile;
		TreeStore indentationOptions, newLineOptions, spacingOptions, styleOptions, wrappingOptions;
		static readonly Dictionary<Microsoft.CodeAnalysis.CSharp.Formatting.LabelPositionOptions, string> labelPositionOptionsTranslationDictionary = new Dictionary<Microsoft.CodeAnalysis.CSharp.Formatting.LabelPositionOptions, string> ();
		static readonly Dictionary<Microsoft.CodeAnalysis.CSharp.Formatting.BinaryOperatorSpacingOptions, string> binaryOperatorSpacingOptionsDictionary = new Dictionary<Microsoft.CodeAnalysis.CSharp.Formatting.BinaryOperatorSpacingOptions, string> ();


		static CSharpFormattingProfileDialog ()
		{
			labelPositionOptionsTranslationDictionary [Microsoft.CodeAnalysis.CSharp.Formatting.LabelPositionOptions.LeftMost] = GettextCatalog.GetString ("leftmost column");
			labelPositionOptionsTranslationDictionary [Microsoft.CodeAnalysis.CSharp.Formatting.LabelPositionOptions.NoIndent] = GettextCatalog.GetString ("normal placement");
			labelPositionOptionsTranslationDictionary [Microsoft.CodeAnalysis.CSharp.Formatting.LabelPositionOptions.OneLess] = GettextCatalog.GetString ("one indent less");

			binaryOperatorSpacingOptionsDictionary [Microsoft.CodeAnalysis.CSharp.Formatting.BinaryOperatorSpacingOptions.Ignore] = GettextCatalog.GetString ("ignore");
			binaryOperatorSpacingOptionsDictionary [Microsoft.CodeAnalysis.CSharp.Formatting.BinaryOperatorSpacingOptions.Remove] = GettextCatalog.GetString ("remove");
			binaryOperatorSpacingOptionsDictionary [Microsoft.CodeAnalysis.CSharp.Formatting.BinaryOperatorSpacingOptions.Single] = GettextCatalog.GetString ("single");
		}
		
		public static string TranslateValue (object value)
		{
			if (value is Microsoft.CodeAnalysis.CSharp.Formatting.LabelPositionOptions)
				return labelPositionOptionsTranslationDictionary [(Microsoft.CodeAnalysis.CSharp.Formatting.LabelPositionOptions)value];
			if (value is Microsoft.CodeAnalysis.CSharp.Formatting.BinaryOperatorSpacingOptions)
				return binaryOperatorSpacingOptionsDictionary [(Microsoft.CodeAnalysis.CSharp.Formatting.BinaryOperatorSpacingOptions)value];
			throw new Exception ("unknown property type: " + value);
		}

		public static object ConvertProperty (Type propertyType, string newText)
		{
			if (propertyType == typeof(Microsoft.CodeAnalysis.CSharp.Formatting.LabelPositionOptions))
				return labelPositionOptionsTranslationDictionary.First (p => p.Value == newText).Key;
			if (propertyType == typeof(Microsoft.CodeAnalysis.CSharp.Formatting.BinaryOperatorSpacingOptions))
				return binaryOperatorSpacingOptionsDictionary.First (p => p.Value == newText).Key;
			throw new Exception ("unknown property type: " + propertyType);
		}

		const int propertyColumn   = 0;
		const int displayTextColumn = 1;
		const int exampleTextColumn = 2;
		const int toggleVisibleColumn = 3;
		const int comboVisibleColumn = 4;
		protected ListStore ComboBoxStore = new ListStore (typeof (string), typeof (string));
		
		
		public CSharpFormattingProfileDialog (CSharpFormattingPolicy profile)
		{
			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			this.Build ();
			this.DefaultWidth = 1400;
			this.DefaultHeight = 600;
			this.hpaned1.Position = (int)(DefaultWidth * 0.618);

			this.profile = profile;
			this.Title = profile.IsBuiltIn ? GettextCatalog.GetString ("Show built-in profile") : GettextCatalog.GetString ("Edit Profile");
			
			notebookCategories.SwitchPage += delegate {
				TreeView treeView;
				switch (notebookCategories.Page) {
				case 0:
					treeView = treeviewIndentOptions;
					break;
				case 1:
					treeView = treeviewNewLines;
					break;
				case 2: // Blank lines
					treeView = treeviewSpacing;
					break;
				case 3:
					treeView = treeviewWrapping;
					break;
				case 4:
					treeView = treeviewStyle;
					break;
				default:
					return;
				}
				
				TreeModel model;
				TreeIter iter;
				if (treeView.Selection.GetSelected (out model, out iter))
					UpdateExample (model, iter);
			};
			notebookCategories.ShowTabs = false;
			comboboxCategories.AppendText (GettextCatalog.GetString ("Indentation"));
			comboboxCategories.AppendText (GettextCatalog.GetString ("New Lines"));
			comboboxCategories.AppendText (GettextCatalog.GetString ("Spacing"));
			comboboxCategories.AppendText (GettextCatalog.GetString ("Wrapping"));
			comboboxCategories.AppendText (GettextCatalog.GetString ("Style"));
			comboboxCategories.Changed += delegate {
				texteditor.Text = "";
				notebookCategories.Page = comboboxCategories.Active;
			};
			comboboxCategories.Active = 0;
			
			var options = DefaultSourceEditorOptions.Instance;
			texteditor.Options = DefaultSourceEditorOptions.PlainEditor;
			texteditor.IsReadOnly = true;
			texteditor.MimeType = CSharpFormatter.MimeType;
			scrolledwindow.AddWithViewport (texteditor);
			ShowAll ();
			
			#region Indent options
			indentationOptions = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool));
			
			var column = new TreeViewColumn ();
			// pixbuf column
			var pixbufCellRenderer = new CellRendererImage ();
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);
			
			// text column
			var cellRendererText = new CellRendererText ();
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);
			 
			treeviewIndentOptions.Model = indentationOptions;
			treeviewIndentOptions.SearchColumn = -1; // disable the interactive search
			treeviewIndentOptions.HeadersVisible = false;
			treeviewIndentOptions.Selection.Changed += TreeSelectionChanged;

			var cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = ComboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;

			cellRendererCombo.Edited += new ComboboxEditedHandler (this, indentationOptions).ComboboxEdited;
			
			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			var cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, treeviewIndentOptions, indentationOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);
			treeviewIndentOptions.AppendColumn (column);


			AddOption (indentationOptions, "IndentBlock", GettextCatalog.GetString ("Indent block contents"), @"namespace Test
{
	class AClass
	{
		void Method ()
		{
			int x;
			int y;
		}
	}
}");
			AddOption (indentationOptions, "IndentBraces", GettextCatalog.GetString ("Indent open and close braces"), @"class AClass
{
	int aField;

	void AMethod()
	{
	}
}");
			AddOption (indentationOptions, "IndentSwitchSection", GettextCatalog.GetString ("Indent switch sections"), @"class AClass
{
	void Method(int x)
	{
		switch (x)
		{
			case 1:
			break;
		}
	}
}");
			AddOption (indentationOptions, "IndentSwitchCaseSection", GettextCatalog.GetString ("Indent case sections"), @"class AClass
{
	void Method(int x)
	{
		switch (x)
		{
			case 1:
			break;
		}
	}
}");
			AddOption (indentationOptions, "LabelPositioning", GettextCatalog.GetString ("Label indentation"), @"class Test
{
	void Method()
	{
	label:
		Console.WriteLine (""Hello World"");
	}

}");
			treeviewIndentOptions.ExpandAll ();
			#endregion
			
			#region New line options
			newLineOptions = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool));
			
			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);
			
			// text column
			cellRendererText = new CellRendererText ();
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);
			
			treeviewNewLines.Model = newLineOptions;
			treeviewNewLines.SearchColumn = -1; // disable the interactive search
			treeviewNewLines.HeadersVisible = false;
			treeviewNewLines.Selection.Changed += TreeSelectionChanged;

			cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = ComboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;
			cellRendererCombo.Edited += new ComboboxEditedHandler (this, newLineOptions).ComboboxEdited;

			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, treeviewNewLines, newLineOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);
			
			treeviewNewLines.AppendColumn (column);

			var category = AddOption (newLineOptions, null, GettextCatalog.GetString ("New line options for braces"), null);
			AddOption (newLineOptions, category, "NewLinesForBracesInTypes", GettextCatalog.GetString ("Place open brace on new line for types"), @"class Example
{
}");
			AddOption (newLineOptions, category, "NewLinesForBracesInMethods", GettextCatalog.GetString ("Place open brace on new line for methods"), @"void Example()
{
}");
			AddOption (newLineOptions, category, "NewLinesForBracesInProperties", GettextCatalog.GetString ("Place open brace on new line for properties"), @"int Example { 
	get  { 
		return 1;
	}
	set {
		// nothing
	}
}
"
);

			AddOption (newLineOptions, category, "NewLinesForBracesInAccessors", GettextCatalog.GetString ("Place open brace on new line for property accessors"), @"int Example { 
	get  { 
		return 1;
	}
	set {
		// nothing
	}
}
"
);


			AddOption (newLineOptions, category, "NewLinesForBracesInAnonymousMethods", GettextCatalog.GetString ("Place open brace on new line for anonymous methods"), @"void Example()
{
	var del = new delegate (int i, int j) {
	};
}");
			AddOption (newLineOptions, category, "NewLinesForBracesInControlBlocks", GettextCatalog.GetString ("Place open brace on new line for control blocks"), @"void Example()
{
	if (true)
	{
	}
}");
			AddOption (newLineOptions, category, "NewLinesForBracesInAnonymousTypes", GettextCatalog.GetString ("Place open brace on new line for anonymous types"), @"void Example()
{
	var c = new
	{
		A = 1,
		B = 2
	};
}");
			AddOption (newLineOptions, category, "NewLinesForBracesInObjectCollectionArrayInitializers", GettextCatalog.GetString ("Place open brace on new line for object initializers"), @"void Example()
{
	new MyObject
	{
		A = 1,
		B = 2 
	};
}");
			AddOption (newLineOptions, category, "NewLinesForBracesInLambdaExpressionBody", GettextCatalog.GetString ("Place open brace on new line for lambda expression"), @"void Example()
{
	Action act = () =>
	{
	};
}");

			category = AddOption (newLineOptions, null, GettextCatalog.GetString ("New line options for keywords"), null);
			AddOption (newLineOptions, category, "NewLineForElse", GettextCatalog.GetString ("Place \"else\" on new line"), @"void Example()
{
	if (true) {
		// ...
	} else {
		// ...
	}
}");
			AddOption (newLineOptions, category, "NewLineForCatch", GettextCatalog.GetString ("Place \"catch\" on new line"), @"void Example()
{
	try {
	} catch {
	} finally {
	}
}");
			AddOption (newLineOptions, category, "NewLineForFinally", GettextCatalog.GetString ("Place \"finally\" on new line"), @"void Example()
{
	try {
	} catch {
	} finally {
	}
}");

			category = AddOption (newLineOptions, null, GettextCatalog.GetString ("New line options for expressions"), null);
			AddOption (newLineOptions, category, "NewLineForMembersInObjectInit", GettextCatalog.GetString ("Place members in object initializers on new line"), @"void Example()
{
	new MyObject {
		A = 1, B = 2
	};
}");
			AddOption (newLineOptions, category, "NewLineForMembersInAnonymousTypes", GettextCatalog.GetString ("Place members in anonymous types on new line"), @"void Example()
{
	var c = new
	{
		A = 1, B = 2
	};
}");
			AddOption (newLineOptions, category, "NewLineForClausesInQuery", GettextCatalog.GetString ("Place query expression clauses on new line"), @"void Example()
{
    var q = from a in e
            from b in e select a * b;
}");
			treeviewNewLines.ExpandAll ();
			#endregion
			
			#region Spacing options
			spacingOptions = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool));

			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);

			// text column
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);

			treeviewSpacing.Model = spacingOptions;
			treeviewSpacing.SearchColumn = -1; // disable the interactive search
			treeviewSpacing.HeadersVisible = false;
			treeviewSpacing.Selection.Changed += TreeSelectionChanged;

			cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = ComboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;
			cellRendererCombo.Edited += new ComboboxEditedHandler (this, spacingOptions).ComboboxEdited;

			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);

			cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, treeviewSpacing, spacingOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);

			treeviewSpacing.AppendColumn (column);

			category = AddOption (spacingOptions, null, GettextCatalog.GetString ("Set spacing for method declarations"), null);
			AddOption (spacingOptions, category, "SpacingAfterMethodDeclarationName", GettextCatalog.GetString ("Insert space between method name and its opening parenthesis"), 
				@"void Example()
{
}");

			AddOption (spacingOptions, category, "SpaceWithinMethodDeclarationParenthesis", GettextCatalog.GetString ("Insert space within argument list parentheses"), 
				@"void Example(int i, int j)
{
}");
			AddOption (spacingOptions, category, "SpaceBetweenEmptyMethodDeclarationParentheses", GettextCatalog.GetString ("Insert space within empty argument list parentheses"), @"void Example()
{
}");

			category = AddOption (spacingOptions, null, GettextCatalog.GetString ("Set spacing for method calls"), null);
			AddOption (spacingOptions, category, "SpaceAfterMethodCallName", GettextCatalog.GetString ("Insert space between method name and its opening parenthesis"), @"void Example()
{
	Test();
}");
			AddOption (spacingOptions, category, "SpaceWithinMethodCallParentheses", GettextCatalog.GetString ("Insert space within argument list parentheses"), @"void Example()
{
	Test(1, 2);
}");
			AddOption (spacingOptions, category, "SpaceBetweenEmptyMethodCallParentheses", GettextCatalog.GetString ("Insert space within empty argument list parentheses"), @"void Example()
{
	Test();
}");

			category = AddOption (spacingOptions, null, GettextCatalog.GetString ("Set other spacing options"), null);
			AddOption (spacingOptions, category, "SpaceAfterControlFlowStatementKeyword", GettextCatalog.GetString ("Insert space after keywords in control flow statements"), @"void Example()
{
	if (condition)
	{
	}
}");

			AddOption (spacingOptions, category, "SpaceWithinExpressionParentheses", GettextCatalog.GetString ("Insert space within parentheses of expressions"), @"void Example()
{
	i = (5 + 3) * 2;
}");
			AddOption (spacingOptions, category, "SpaceWithinCastParentheses", GettextCatalog.GetString ("Insert space within parentheses of type casts"), @"void Example()
{
	test = (ITest)o;
}");
			AddOption (spacingOptions, category, "SpaceWithinOtherParentheses", GettextCatalog.GetString ("Insert space within parentheses of control flow statements"), @"void Example()
{
	if (condition)
	{
	}
}");

			AddOption (spacingOptions, category, "SpaceAfterCast", GettextCatalog.GetString ("Insert space after casts"), @"void Example()
{
	test = (ITest)o;
}");
			AddOption (spacingOptions, category, "SpacesIgnoreAroundVariableDeclaration", GettextCatalog.GetString ("Ignore spaces in declaration statements"), @"void Example()
{
	int x=5;
}");

			category = AddOption (spacingOptions, null, GettextCatalog.GetString ("Set spacing for brackets"), null);
			AddOption (spacingOptions, category, "SpaceBeforeOpenSquareBracket", GettextCatalog.GetString ("Insert space before open square bracket"), @"void Example()
{
	i[5] = 3;
}");
			AddOption (spacingOptions, category, "SpaceBetweenEmptySquareBrackets", GettextCatalog.GetString ("Insert space within empty square brackets"), @"void Example()
{
	new int[] {1, 2};
}");
			AddOption (spacingOptions, category, "SpaceWithinSquareBrackets", GettextCatalog.GetString ("Insert space within square brackets"), @"void Example()
{
	i[5] = 3;
}");

			category = AddOption (spacingOptions, null, GettextCatalog.GetString ("Other"), null);
			AddOption (spacingOptions, category, "SpaceAfterColonInBaseTypeDeclaration", GettextCatalog.GetString ("Insert space after colon for base or interface in type declaration"), @"class Foo : Bar
{
}");
			AddOption (spacingOptions, category, "SpaceAfterComma", GettextCatalog.GetString ("Insert space after comma"), @"void Example()
{
	var array = { 1,2,3,4 };
}");
			AddOption (spacingOptions, category, "SpaceAfterDot", GettextCatalog.GetString ("Insert space after dot"), @"void Example()
{
	Foo.Bar.Test();
}");
			AddOption (spacingOptions, category, "SpaceAfterSemicolonsInForStatement", GettextCatalog.GetString ("Insert space after semicolon in \"for\" statement"), @"void Example()
{
	for (int i = 0; i< 10; i++)
	{
	}
}");
			AddOption (spacingOptions, category, "SpaceBeforeColonInBaseTypeDeclaration", GettextCatalog.GetString ("Insert space before colon for base or interface in type declaration"), @"class Foo : Bar
{
}");
			AddOption (spacingOptions, category, "SpaceBeforeComma", GettextCatalog.GetString ("Insert space before comma"), @"void Example()
{
	var array = { 1,2,3,4 };
}");
			AddOption (spacingOptions, category, "SpaceBeforeDot", GettextCatalog.GetString ("Insert space before dot"), @"void Example()
{
	Foo.Bar.Test();
}");
			AddOption (spacingOptions, category, "SpaceBeforeSemicolonsInForStatement", GettextCatalog.GetString ("Insert space before semicolon in \"for\" statement"), @"void Example()
{
	for (int i = 0; i< 10; i++)
	{
	}
}");

			AddOption (spacingOptions, category, "SpacingAroundBinaryOperator", GettextCatalog.GetString ("Set spacing for operators"), @"void Example()
{
	i = (5 + 3) * 2;
}");

			treeviewSpacing.ExpandAll ();
			#endregion

			#region Style options
			styleOptions = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool));

			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);

			// text column
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);


			treeviewStyle.Model = styleOptions;
			treeviewStyle.SearchColumn = -1; // disable the interactive search
			treeviewStyle.HeadersVisible = false;
			treeviewStyle.Selection.Changed += TreeSelectionChanged;

			cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = ComboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;
			cellRendererCombo.Edited += new ComboboxEditedHandler (this, styleOptions).ComboboxEdited;

			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);

			cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, treeviewSpacing, styleOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);

			treeviewStyle.AppendColumn (column);

			AddOption (styleOptions, "PlaceSystemDirectiveFirst", GettextCatalog.GetString ("Place System directives first when sorting usings"), "");

			// AddOption (styleOptions, category, null, GettextCatalog.GetString ("Qualify member access with 'this'"), null);
			// AddOption (styleOptions, category, null, GettextCatalog.GetString ("Use 'var' when generating locals"), null);

			treeviewStyle.ExpandAll ();
			#endregion

			#region Wrapping options
			wrappingOptions = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool));

			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);

			// text column
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);

			treeviewWrapping.Model = wrappingOptions;
			treeviewWrapping.SearchColumn = -1; // disable the interactive search
			treeviewWrapping.HeadersVisible = false;
			treeviewWrapping.Selection.Changed += TreeSelectionChanged;

			cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = ComboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;
			cellRendererCombo.Edited += new ComboboxEditedHandler (this, wrappingOptions).ComboboxEdited;

			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);

			cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, treeviewSpacing, wrappingOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);

			treeviewWrapping.AppendColumn (column);

			AddOption (wrappingOptions, "WrappingPreserveSingleLine", GettextCatalog.GetString ("Leave block on single line"), "");
			AddOption (wrappingOptions, "WrappingKeepStatementsOnSingleLine", GettextCatalog.GetString ("Leave statements and member declarations on the same line"), "");

			treeviewWrapping.ExpandAll ();
			#endregion
		}
		
		static int SetFlag (Entry entry, int oldValue)
		{
			int newValue;
			return int.TryParse (entry.Text, out newValue) ? newValue : oldValue;
		}

		static PropertyInfo GetPropertyByName (string name)
		{
			PropertyInfo info = typeof(CSharpFormattingPolicy).GetProperty (name);
			if (info == null)
				throw new Exception (name + " property not found");
			return info;
		}

		static TreeIter AddOption (TreeStore model, string propertyName, string displayName, string example)
		{
			bool isBool = false;
			if (!string.IsNullOrEmpty (propertyName)) {
				PropertyInfo info = GetPropertyByName (propertyName);
				isBool = info.PropertyType == typeof (bool);
			}
			
			return model.AppendValues (propertyName, displayName, example, !string.IsNullOrEmpty (propertyName) && isBool, !string.IsNullOrEmpty (propertyName) && !isBool);
		}
		
		static TreeIter AddOption (TreeStore model, TreeIter parent, string propertyName, string displayName, string example)
		{
			bool isBool = false;
			if (!string.IsNullOrEmpty (propertyName)) {
				PropertyInfo info = GetPropertyByName (propertyName);
				isBool = info.PropertyType == typeof (bool);
			}
			
			return model.AppendValues (parent, propertyName, displayName, example, !string.IsNullOrEmpty (propertyName) && isBool, !string.IsNullOrEmpty (propertyName) && !isBool);
		}
		
		void TreeSelectionChanged (object sender, EventArgs e)
		{
			var treeSelection = (TreeSelection)sender;
			TreeModel model;
			TreeIter iter;
			if (treeSelection.GetSelected (out model, out iter)) {
				var info = GetProperty (model, iter);
				if (info != null && info.PropertyType != typeof (bool)) {
					ComboBoxStore.Clear ();
					foreach (var v in Enum.GetValues (info.PropertyType)) {
						ComboBoxStore.AppendValues (v.ToString (),  TranslateValue (v));
					}
				}
				
				UpdateExample (model, iter);
			}
		}
		
			
		void UpdateExample (TreeModel model, TreeIter iter)
		{
			string example = (string)model.GetValue (iter, exampleTextColumn);
			UpdateExample (example);
		}
		
		void UpdateExample (string example)
		{
			string text;
			if (!string.IsNullOrEmpty (example)) {
				text = Environment.NewLine != "\n" ? example.Replace ("\n", Environment.NewLine) : example;
			} else {
				text = "";
			}

			var types = DesktopService.GetMimeTypeInheritanceChain (MonoDevelop.CSharp.Formatting.CSharpFormatter.MimeType);
			var textPolicy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> (types);
			texteditor.Text = CSharpFormatter.FormatText (profile, textPolicy, text, 0, text.Length);
		}
		
		static PropertyInfo GetProperty (TreeModel model, TreeIter iter)
		{
			string propertyName = (string)model.GetValue (iter, propertyColumn);
			if (string.IsNullOrEmpty (propertyName))
				return null;
			return GetPropertyByName (propertyName);
		}
		
		object GetValue (string propertyName)
		{
			var info = GetPropertyByName (propertyName);
			return info.GetValue (profile, null);
		}
		
		static void RenderIcon (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var pixbufCellRenderer = (CellRendererImage)cell;
			if (model.IterHasChild (iter)) {
				pixbufCellRenderer.Image = ImageService.GetIcon (((TreeView)col.TreeView).GetRowExpanded (model.GetPath (iter)) ? MonoDevelop.Ide.Gui.Stock.OpenFolder : MonoDevelop.Ide.Gui.Stock.ClosedFolder, IconSize.Menu);
			} else {
				pixbufCellRenderer.Image = ImageService.GetIcon (MonoDevelop.Ide.Gui.Stock.Property, IconSize.Menu);
			}
		}
		
		static void ComboboxDataFunc (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var cellRenderer = (CellRendererCombo)cell;
			var info = GetProperty (model, iter);
			if (info == null) {
				cellRenderer.Text = "<invalid>";
				return;
			}

			var profile = ((CSharpFormattingProfileDialog)col.TreeView.Toplevel).profile;
			object value = info.GetValue (profile, null);
			
			cellRenderer.Text = value is Enum ? TranslateValue (value) : value.ToString ();
		}
		
		static void ToggleDataFunc (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var cellRenderer = (CellRendererToggle)cell;
			var info = GetProperty (model, iter);
			if (info == null || info.PropertyType != typeof(bool)) 
				return;

			var profile = ((CSharpFormattingProfileDialog)col.TreeView.Toplevel).profile;
			bool value = (bool)info.GetValue (profile, null);
			cellRenderer.Active = value;
		}
		
		class CellRendererToggledHandler
		{
			readonly CSharpFormattingProfileDialog dialog;
			readonly TreeStore model;
			readonly TreeView treeView;
			
			public CellRendererToggledHandler (CSharpFormattingProfileDialog dialog, TreeView treeView, TreeStore model)
			{
				this.dialog = dialog;
				this.model = model;
				this.treeView = treeView;
			}
			
			public void Toggled (object o, ToggledArgs args)
			{
				TreeIter iter;
				if (model.GetIterFromString (out iter, args.Path)) {
					var info = GetProperty (model, iter);
					if (info == null || info.PropertyType != typeof(bool))
						return;
					bool value = (bool)info.GetValue (dialog.profile, null);
					info.SetValue (dialog.profile, !value, null);
					dialog.UpdateExample (model, iter);
					// When toggeling with the keyboard the tree view doesn't update automatically
					// see 'Bug 188 - Pressing space to select does not update checkbox'
					treeView.QueueDraw ();
				}
			}
		}
		
		class ComboboxEditedHandler
		{
			readonly CSharpFormattingProfileDialog dialog;
			readonly TreeStore model;
			
			public ComboboxEditedHandler (CSharpFormattingProfileDialog dialog, TreeStore model)
			{
				this.dialog = dialog;
				this.model = model;
			}

			public void ComboboxEdited (object o, EditedArgs args)
			{
				TreeIter iter;
				if (model.GetIterFromString (out iter, args.Path)) {
					var info = GetProperty (model, iter);
					if (info == null)
						return;
					var value = ConvertProperty (info.PropertyType, args.NewText);
					info.SetValue (dialog.profile, value, null);
					dialog.UpdateExample (model, iter);
				}
			}
		}
	}
}
