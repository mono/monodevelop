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
namespace MonoDevelop.CSharp.Formatting
{
	public partial class CSharpFormattingProfileDialog : Gtk.Dialog
	{
		Mono.TextEditor.TextEditor texteditor = new Mono.TextEditor.TextEditor ();
		CSharpFormattingPolicy profile;
		Gtk.TreeStore indentOptions, bacePositionOptions, newLineOptions;
		
		Gtk.TreeStore whiteSpaceCategory;
		ListStore whiteSpaceOptions; 
		
		#region Examples
		const string methodSpaceExample = @"class ClassDeclaration { 
		public static void Main (string[] args)
		{
			Console.WriteLine (""Hello World!"");
		}
	}";
		
		const string propertyExample = @"class ClassDeclaration { 
		int myProperty;
		int MyProperty { 
			get { return myProperty;} 
			set { myProperty = value;} 
		}
		
		int myOtherProperty;
		int MyOtherProperty { 
			get { 
				Console.WriteLine (""get myOtherProperty"");
				return myOtherProperty;
			} 
			set { 
				if (myOtherProperty != value)
					myOtherProperty = value;
			} 
		}
	}";
		
		const string spaceExample = @"class ClassDeclaration { 
		public void TestMethod ()
		{
			try {
				TestMethod ("""");
			} catch (Exception e) {
				// Do something
			} finally {
				// Do something
			}
		}
		
		public void TestMethod (string test)
		{
			lock (this) {
				switch (test) {
					case ""A"":
						Console.WriteLine (""was A"");
						break;
					case ""B"":
						Console.WriteLine (""was B"");
						break;
				}
			}
		}
		
		public void Calculate (int a, int b)
		{
			if (a < b) {
				for (int i = a; i < b; i++) {
					Console.WriteLine (i);
				}
			} else {
				using (object o = new object ()) {
					while (b < a) {
						ConentryNamesole.WriteLine (b++);
					}
				}
			}
		}
	}";
		const string eventExample = @"class ClassDeclaration { 
		EventHandler<EventArgs> onAction;
		public event EventHandler<EventArgs> Action {
			add { onAction = (EventHandler<EventArgs>)Delegate.Combine(onAction, value); }
			remove { onAction = (EventHandler<EventArgs>)Delegate.Remove(onAction, value);}
		}
		EventHandler<EventArgs> onAnotherAction;
		public event EventHandler<EventArgs> AnotherAction {
			add { if (value != null) 
					onAnotherAction = (EventHandler<EventArgs>)Delegate.Combine(onAnotherAction, value); }
			remove { if (value != null) 
					onAnotherAction = (EventHandler<EventArgs>)Delegate.Remove(onAnotherAction, value);}
		}
	}";
		
		const string simpleUsingStatement = @"class ClassDeclaration { 
		public void Test ()
		{
			using (object o = new object ()) {
				Console.WriteLine (""Hello World!"");
			}
		}
	}";
		
		const string simpleFixedStatement = @"class ClassDeclaration { 
		public void Test (Point pt)
		{
			fixed (int* p = &pt.x) {
				*p = 10; 
			}
		}
	}";
		
		const string simpleIf = @"class ClassDeclaration { 
		public void Test (int i)
		{
			if (i == 5) {
				Console.WriteLine (""== 5"");
			} else if (i > 0) {
				Console.WriteLine ("">0"");
			} else if (i < 0) {
				Console.WriteLine (""<0"");
			} else {
				Console.WriteLine (""== 0"");
			}
		}
	}";
		
		const string simpleWhile = @"class ClassDeclaration { 
		public void Test ()
		{
			while (true) {
				Console.WriteLine (""Hello World!"");
			}
		}
	}";
		const string simpleCatch = @"class ClassDeclaration { 
		public void Test ()
		{
			try {
				Console.WriteLine (""Hello World!"");
			} catch (Exception) {
				Console.WriteLine (""Got exception!!"");
			} finally {
				Console.WriteLine (""finally done."");
			}
		}
	}";
		
		const string simpleDoWhile = @"class ClassDeclaration { 
		public void Test ()
		{
			int i = 0;
			do {
				Console.WriteLine (""Hello World!"");
			} while (i++ < 10);
		}
	}";
		
		const string simpleArrayInitializer = @"class ClassDeclaration { 
		public void Test (object o)
		{
			int[] i = new int[] { 1, 3, 3, 7 };
		}
	}";
		const string condOpExample = @"class ClassDeclaration { 
		public string GetSign (int i)
		{
			return i < 0 ? ""-"" : ""+"";
		}
	}";
		const string switchExample = @"class ClassDeclaration { 
		public void Test (int i)
		{
			switch (i) {
				case 0:
					Console.WriteLine (""was zero"");
					break;
				case 1:
					Console.WriteLine (""was one"");
					break;
				default:
					Console.WriteLine (""was "" + i);
					break;
			}
		}
	}";
		const string simpleFor = @"class ClassDeclaration { 
		public void Test ()
		{
			for (int i = 0; i < 10; i++) {
				Console.WriteLine (""Hello World!"");
			}
		}
	}";
		const string simpleForeach = @"class ClassDeclaration : ArrayList { 
		public void Test ()
		{
			foreach (object o in this) {
				Console.WriteLine (""Hello World!"");
			}
		}
	}";
		const string simpleLock = @"class ClassDeclaration { 
		public void Test ()
		{
			lock (this) {
				Console.WriteLine (""Hello World!"");
			}
		}
	}";
		const string operatorExample = @"class ClassDeclaration { 
		public void TestMethod ()
		{
			int a = 5 << 5;
			int b = (a + 5 - 3) * 6 / 2;
			a += b;
			a = a & ~255;
			if (a == b || b < a >> 1) {
				b -= a;
			}
		}
	}";
		const string blankLineExample = @"// Example
using System;
using System.Collections;
namespace TestSpace {
	using MyNamespace;
	class Test
	{
		int a;
		string b;
		public Test (int a, string b)
		{
			this.a = a;
			this.b = b;
		}
		void Print ()
		{
			Console.WriteLine (""a: {0} b : {1}"", a, b);
		}
	}
	class MyTest 
	{
	}
}
";
		#endregion
		
		const int propertyColumn   = 0;
		const int displayTextColumn = 1;
		const int exampleTextColumn = 2;
		const int toggleVisibleColumn = 3;
		const int comboVisibleColumn = 4;
		protected ListStore comboBoxStore = new ListStore (typeof (string), typeof (string));
		
		
		public CSharpFormattingProfileDialog (CSharpFormattingPolicy profile)
		{
			this.Build ();
			this.profile = profile;
			this.Title = profile.IsBuiltIn ? GettextCatalog.GetString ("Show built-in profile") : GettextCatalog.GetString ("Edit Profile");
			entryName.Text = profile.Name;
			entryName.Sensitive = !profile.IsBuiltIn;
			entryName.Changed += delegate {
				profile.Name = entryName.Text;
			};
			notebookCategories.SwitchPage += delegate {
				TreeView treeView;
				Console.WriteLine (notebookCategories.Page);
				switch (notebookCategories.Page) {
				case 0:
					treeView = treeviewIndentOptions;
					break;
				case 1:
					treeView = treeviewBracePositions;
					break;
				case 2: // Blank lines
					UpdateExample (blankLineExample);
					return;
				case 3: // white spaces
					WhitespaceCategoryChanged (treeviewInsertWhiteSpaceCategory.Selection, EventArgs.Empty);
					return;
				case 4:
					treeView = treeviewNewLines;
					break;
				default:
					return;
				}
				
				var model = treeView.Model;
				Gtk.TreeIter iter;
				if (treeView.Selection.GetSelected (out model, out iter))
					UpdateExample (model, iter);
			};
			notebookCategories.ShowTabs = false;
			comboboxCategories.AppendText (GettextCatalog.GetString ("Indentation"));
			comboboxCategories.AppendText (GettextCatalog.GetString ("Braces"));
			comboboxCategories.AppendText (GettextCatalog.GetString ("Blank lines"));
			comboboxCategories.AppendText (GettextCatalog.GetString ("White Space"));
			comboboxCategories.AppendText (GettextCatalog.GetString ("New Lines"));
			comboboxCategories.Changed += delegate(object sender, EventArgs e) {
				texteditor.Text = "";
				notebookCategories.Page = comboboxCategories.Active;
			};
			comboboxCategories.Active = 0;
			
			var options = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance;
			texteditor.Options.FontName = options.FontName;
			texteditor.Options.ColorScheme = options.ColorScheme;
			texteditor.Options.ShowFoldMargin = false;
			texteditor.Options.ShowIconMargin = false;
			texteditor.Options.ShowLineNumberMargin = false;
			texteditor.Options.ShowInvalidLines = false;
			texteditor.Document.ReadOnly = true;
			texteditor.Document.MimeType = CSharpFormatter.MimeType;
			scrolledwindow.Child = texteditor;
			ShowAll ();
			
			#region Indent options
			indentOptions = new Gtk.TreeStore (typeof (string), typeof (string), typeof (string), typeof(bool), typeof(bool));
			
			TreeViewColumn column = new TreeViewColumn ();
			// pixbuf column
			var pixbufCellRenderer = new CellRendererPixbuf ();
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);
			
			// text column
			CellRendererText cellRendererText = new CellRendererText ();
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);
			 
			treeviewIndentOptions.Model = indentOptions;
			treeviewIndentOptions.HeadersVisible = false;
			treeviewIndentOptions.Selection.Changed += TreeSelectionChanged;
			treeviewIndentOptions.AppendColumn (column);
			
			column = new TreeViewColumn ();
			CellRendererCombo cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = comboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;

			cellRendererCombo.Edited +=  new ComboboxEditedHandler (this, indentOptions).ComboboxEdited;
			
			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			CellRendererToggle cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, indentOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);
			
			treeviewIndentOptions.AppendColumn (column);
			var category = AddOption (indentOptions, null, GettextCatalog.GetString ("Declarations"), null);
			AddOption (indentOptions, category, "IndentNamespaceBody", GettextCatalog.GetString ("within namespaces"), "namespace Test { class AClass {} }");
			
			AddOption (indentOptions, category, "IndentClassBody", GettextCatalog.GetString ("within classes"), "class AClass { int aField; void AMethod () {}}");
			AddOption (indentOptions, category, "IndentInterfaceBody", GettextCatalog.GetString ("within interfaces"), "interface IAInterfaces { int AProperty {get;set;} void AMethod ();}");
			AddOption (indentOptions, category, "IndentStructBody", GettextCatalog.GetString ("within structs"), "struct AStruct { int aField; void AMethod () {}}");
			AddOption (indentOptions, category, "IndentEnumBody", GettextCatalog.GetString ("within enums"), "enum AEnum { A, B, C }");
			
			AddOption (indentOptions, category, "IndentMethodBody", GettextCatalog.GetString ("within methods"), methodSpaceExample);
			AddOption (indentOptions, category, "IndentPropertyBody", GettextCatalog.GetString ("within properties"), propertyExample);
			AddOption (indentOptions, category, "IndentEventBody", GettextCatalog.GetString ("within events"), eventExample);
			
			category = AddOption (indentOptions, null, GettextCatalog.GetString ("Statements"), null);
			AddOption (indentOptions, category, "IndentBlocks", GettextCatalog.GetString ("within blocks"), spaceExample);
			AddOption (indentOptions, category, "IndentSwitchBody", GettextCatalog.GetString ("Indent 'switch' body"), spaceExample);
			AddOption (indentOptions, category, "IndentCaseBody", GettextCatalog.GetString ("Indent 'case' body"), spaceExample);
			AddOption (indentOptions, category, "IndentBreakStatements", GettextCatalog.GetString ("Indent 'break' statements"), spaceExample);
			
			AddOption (indentOptions, category, "AlignEmbeddedIfStatements", GettextCatalog.GetString ("Align embedded 'if' statements"), "class AClass { void AMethod () { if (a) if (b) { int c; } } } ");
			AddOption (indentOptions, category, "AlignEmbeddedUsingStatements", GettextCatalog.GetString ("Align embedded 'using' statements"), "class AClass { void AMethod () {using (IDisposable a = null) using (IDisposable b = null) { int c; } } }");
			treeviewIndentOptions.ExpandAll ();
			#endregion
			
			#region Brace options
			bacePositionOptions = new Gtk.TreeStore (typeof (string), typeof (string), typeof (string), typeof(bool), typeof(bool));
			
			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);
			
			// text column
			cellRendererText = new CellRendererText ();
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);
			
			treeviewBracePositions.Model = bacePositionOptions;
			treeviewBracePositions.HeadersVisible = false;
			treeviewBracePositions.Selection.Changed += TreeSelectionChanged;
			treeviewBracePositions.AppendColumn (column);
			
			column = new TreeViewColumn ();
			cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = comboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;
			cellRendererCombo.Edited += new ComboboxEditedHandler (this, bacePositionOptions).ComboboxEdited;

			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, bacePositionOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);
			
			treeviewBracePositions.AppendColumn (column);
			
			AddOption (bacePositionOptions, "NamespaceBraceStyle", GettextCatalog.GetString ("Namespace declaration"), "namespace TestNameSpace {}");
			
			AddOption (bacePositionOptions, "ClassBraceStyle", GettextCatalog.GetString ("Class declaration"), "class ClassDeclaration {}");
			AddOption (bacePositionOptions, "InterfaceBraceStyle", GettextCatalog.GetString ("Interface declaration"), "interface InterfaceDeclaraction {}");
			AddOption (bacePositionOptions, "StructBraceStyle", GettextCatalog.GetString ("Struct declaration"), "struct StructDeclaration {}");
			AddOption (bacePositionOptions, "EnumBraceStyle", GettextCatalog.GetString ("Enum declaration"), "enum EnumDeclaration { A, B, C}");
			
			AddOption (bacePositionOptions, "MethodBraceStyle", GettextCatalog.GetString ("Method declaration"), "class ClassDeclaration { void MyMethod () {} }");
			AddOption (bacePositionOptions, "AnonymousMethodBraceStyle", GettextCatalog.GetString ("Anonymous methods"), "class ClassDeclaration { void MyMethod () { MyEvent += delegate (object sender, EventArgs e) { if (true) Console.WriteLine (\"Hello World\"); }; } }");
			AddOption (bacePositionOptions, "ConstructorBraceStyle", GettextCatalog.GetString ("Constructor declaration"), "class ClassDeclaration { public ClassDeclaration () {} }");
			AddOption (bacePositionOptions, "DestructorBraceStyle", GettextCatalog.GetString ("Destructor declaration"), "class ClassDeclaration { ~ClassDeclaration () {} }");
			
			AddOption (bacePositionOptions, "StatementBraceStyle", GettextCatalog.GetString ("Statements"), spaceExample);
			
			category = AddOption (bacePositionOptions, "PropertyBraceStyle", GettextCatalog.GetString ("Property declaration"), propertyExample);
			AddOption (bacePositionOptions, category, "PropertyGetBraceStyle", GettextCatalog.GetString ("Get declaration"), propertyExample);
			AddOption (bacePositionOptions, category, "AllowPropertyGetBlockInline", GettextCatalog.GetString ("Allow one line get"), propertyExample);
			AddOption (bacePositionOptions, category, "PropertySetBraceStyle", GettextCatalog.GetString ("Set declaration"), propertyExample);
			AddOption (bacePositionOptions, category, "AllowPropertySetBlockInline", GettextCatalog.GetString ("Allow one line set"), propertyExample);
			
			
			category = AddOption (bacePositionOptions, "EventBraceStyle", GettextCatalog.GetString ("Event declaration"), eventExample);
			AddOption (bacePositionOptions, category, "EventAddBraceStyle", GettextCatalog.GetString ("Add declaration"), eventExample);
			AddOption (bacePositionOptions, category, "AllowEventAddBlockInline", GettextCatalog.GetString ("Allow one line add"), eventExample);
			AddOption (bacePositionOptions, category, "EventRemoveBraceStyle", GettextCatalog.GetString ("Remove declaration"), eventExample);
			AddOption (bacePositionOptions, category, "AllowEventRemoveBlockInline", GettextCatalog.GetString ("Allow one line remove"), eventExample);
			
			category = AddOption (bacePositionOptions, null, GettextCatalog.GetString ("Brace forcement"), null);
			AddOption (bacePositionOptions, category, "IfElseBraceForcement", GettextCatalog.GetString ("'if...else' statement"), @"class ClassDeclaration { 
	public void Test ()
		{
			if (true) {
				Console.WriteLine (""Hello World!"");
			}
			if (true)
				Console.WriteLine (""Hello World!"");
		}
	}");
			AddOption (bacePositionOptions, category, "ForBraceForcement", GettextCatalog.GetString ("'for' statement"), @"class ClassDeclaration { 
		public void Test ()
		{
			for (int i = 0; i < 10; i++) {
				Console.WriteLine (""Hello World "" + i);
			}
			for (int i = 0; i < 10; i++)
				Console.WriteLine (""Hello World "" + i);
		}
	}");
			AddOption (bacePositionOptions, category, "WhileBraceForcement", GettextCatalog.GetString ("'while' statement"), @"class ClassDeclaration { 
		public void Test ()
		{
			int i = 0;
			while (i++ < 10) {
				Console.WriteLine (""Hello World "" + i);
			}
			while (i++ < 20)
				Console.WriteLine (""Hello World "" + i);
		}
	}");
			AddOption (bacePositionOptions, category, "UsingBraceForcement", GettextCatalog.GetString ("'using' statement"), simpleUsingStatement);
			AddOption (bacePositionOptions, category, "FixedBraceForcement", GettextCatalog.GetString ("'fixed' statement"), simpleFixedStatement);
			treeviewBracePositions.ExpandAll ();
			#endregion
			
			#region New line options
			newLineOptions = new Gtk.TreeStore (typeof (string), typeof (string), typeof (string), typeof(bool), typeof(bool));
			
			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);
			
			// text column
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);
			
			treeviewNewLines.Model = newLineOptions;
			treeviewNewLines.HeadersVisible = false;
			treeviewNewLines.Selection.Changed += TreeSelectionChanged;
			treeviewNewLines.AppendColumn (column);
			
			column = new TreeViewColumn ();
			cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = comboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;
			cellRendererCombo.Edited += new ComboboxEditedHandler (this, newLineOptions).ComboboxEdited;

			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, newLineOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);
			
			treeviewNewLines.AppendColumn (column);
			
			AddOption (newLineOptions, "PlaceElseOnNewLine", GettextCatalog.GetString ("Place 'else' on new line"), simpleIf);
			AddOption (newLineOptions, "PlaceElseIfOnNewLine", GettextCatalog.GetString ("Place 'else if' on new line"), simpleIf);
			AddOption (newLineOptions, "PlaceCatchOnNewLine", GettextCatalog.GetString ("Place 'catch' on new line"), simpleCatch);
			AddOption (newLineOptions, "PlaceFinallyOnNewLine", GettextCatalog.GetString ("Place 'finally' on new line"), simpleCatch);
			AddOption (newLineOptions, "PlaceWhileOnNewLine", GettextCatalog.GetString ("Place 'while' on new line"), simpleDoWhile);
			AddOption (newLineOptions, "PlaceArrayInitializersOnNewLine", GettextCatalog.GetString ("Place array initializers on new line"), simpleArrayInitializer);
			treeviewNewLines.ExpandAll ();
			#endregion
			
			#region White space options
			whiteSpaceCategory = new TreeStore (typeof (string), typeof (Category));

			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);
			
			// text column
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 0);
			
			treeviewInsertWhiteSpaceCategory.AppendColumn (column);
			
			treeviewInsertWhiteSpaceCategory.Model = whiteSpaceCategory;
			treeviewInsertWhiteSpaceCategory.HeadersVisible = false;
			treeviewInsertWhiteSpaceCategory.Selection.Changed += WhitespaceCategoryChanged;
			
			treeviewInsertWhiteSpaceOptions.Model = whiteSpaceOptions;
			
			category = whiteSpaceCategory.AppendValues (GettextCatalog.GetString ("Declarations"), null);
			string example = @"class Example {
		void Test ()
		{
		}
		
		void Test (int a, int b, int c)
		{
		}
}";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("Methods"), new Category (example,
				new Option ("BeforeMethodDeclarationParentheses", GettextCatalog.GetString ("before opening parenthesis")),
				new Option ("WithinMethodDeclarationParentheses", GettextCatalog.GetString ("within parenthesis")),
				new Option ("BetweenEmptyMethodDeclarationParentheses", GettextCatalog.GetString ("between empty parenthesis")),
				new Option ("BeforeMethodDeclarationParameterComma", GettextCatalog.GetString ("before comma in parenthesis")),
				new Option ("AfterMethodDeclarationParameterComma", GettextCatalog.GetString ("after comma in parenthesis"))
			));
			
			example = @"class Example {
		int a, b, c;
}";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("Fields"), new Category (example,
				new Option ("BeforeFieldDeclarationComma", GettextCatalog.GetString ("before comma in multiple field declarations")),
				new Option ("AfterFieldDeclarationComma", GettextCatalog.GetString ("after comma in multiple field declarations"))
			));
			
			example = @"class Example {
	Example () 
	{
	}

	Example (int a, int b, int c) 
	{
	}
}";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("Constructors"), new Category (example,
				new Option ("BeforeConstructorDeclarationParentheses", GettextCatalog.GetString ("before opening parenthesis")),
				new Option ("WithinConstructorDeclarationParentheses", GettextCatalog.GetString ("within parenthesis")),
				new Option ("BetweenEmptyConstructorDeclarationParentheses", GettextCatalog.GetString ("between empty parenthesis")),
				new Option ("BeforeConstructorDeclarationParameterComma", GettextCatalog.GetString ("before comma in parenthesis")),
				new Option ("AfterConstructorDeclarationParameterComma", GettextCatalog.GetString ("after comma in parenthesis"))
			));
			
			example = @"class Example {
	public int this[int a, int b] {
		get {
			return a + b;
		}
	}
}";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("Indexer"), new Category (example,
				new Option ("BeforeIndexerDeclarationBracket", GettextCatalog.GetString ("before opening bracket")),
				new Option ("WithinIndexerDeclarationBracket", GettextCatalog.GetString ("within brackets")),
				new Option ("BeforeIndexerDeclarationParameterComma", GettextCatalog.GetString ("before comma in brackets")),
				new Option ("AfterIndexerDeclarationParameterComma", GettextCatalog.GetString ("after comma in brackets"))
			));
			
			example = @"delegate void FooBar (int a, int b, int c);
delegate void BarFoo ();
";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("Delegates"), new Category (example,
				new Option ("BeforeDelegateDeclarationParentheses", GettextCatalog.GetString ("before opening parenthesis")),
				new Option ("WithinDelegateDeclarationParentheses", GettextCatalog.GetString ("within parenthesis")),
				new Option ("BetweenEmptyDelegateDeclarationParentheses", GettextCatalog.GetString ("between empty parenthesis")),
				new Option ("BeforeDelegateDeclarationParameterComma", GettextCatalog.GetString ("before comma in parenthesis")),
				new Option ("AfterDelegateDeclarationParameterComma", GettextCatalog.GetString ("after comma in parenthesis"))
			));
			
			category = whiteSpaceCategory.AppendValues (GettextCatalog.GetString ("Statements"), null);
			
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("'if'"), new Category (simpleIf,
				new Option ("IfParentheses", GettextCatalog.GetString ("before opening parenthesis"))
			));
			
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("'while'"), new Category (simpleWhile,
				new Option ("WhileParentheses", GettextCatalog.GetString ("before opening parenthesis"))
			));
			
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("'for'"), new Category (simpleFor,
				new Option ("ForParentheses", GettextCatalog.GetString ("before opening parenthesis")),
				new Option ("SpacesBeforeForSemicolon", GettextCatalog.GetString ("before semicolon")),
				new Option ("SpacesAfterForSemicolon", GettextCatalog.GetString ("after semicolon"))
			));
			
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("'foreach'"), new Category (simpleForeach,
				new Option ("ForeachParentheses", GettextCatalog.GetString ("before opening parenthesis"))
			));
			
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("'catch'"), new Category (simpleCatch,
				new Option ("CatchParentheses", GettextCatalog.GetString ("before opening parenthesis"))
			));
			
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("'switch'"), new Category (switchExample,
				new Option ("SwitchParentheses", GettextCatalog.GetString ("before opening parenthesis"))
			));
			
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("'lock'"), new Category (simpleLock,
				new Option ("SwitchParentheses", GettextCatalog.GetString ("before opening parenthesis"))
			));
			
			category = whiteSpaceCategory.AppendValues (GettextCatalog.GetString ("Expressions"), null);
			example = @"class Example {
		void Test ()
		{
			Console.WriteLine();
			Console.WriteLine(""{0} {1}!"", ""Hello"", ""World"");
		}
}";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("Method invocations"), new Category (example,
				new Option ("BeforeMethodCallParentheses", GettextCatalog.GetString ("before opening parenthesis")),
				new Option ("WithinMethodCallParentheses", GettextCatalog.GetString ("within parenthesis")),
				new Option ("BetweenEmptyMethodCallParentheses", GettextCatalog.GetString ("between empty parenthesis")),
				new Option ("BeforeMethodCallParameterComma", GettextCatalog.GetString ("before comma in parenthesis")),
				new Option ("AfterMethodCallParameterComma", GettextCatalog.GetString ("after comma in parenthesis"))
			));
			
			example = @"class Example {
		void Test ()
		{
			a[1,2] = b[3];
		}
}";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("Element access"), new Category (example,
				new Option ("SpacesBeforeBrackets", GettextCatalog.GetString ("before opening bracket")),
				new Option ("SpacesWithinBrackets", GettextCatalog.GetString ("within brackets")),
				new Option ("BeforeBracketComma", GettextCatalog.GetString ("before comma in brackets")),
				new Option ("AfterBracketComma", GettextCatalog.GetString ("after comma in brackets"))
			));
			
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("Parentheses"), new Category (operatorExample,
				new Option ("WithinParentheses", GettextCatalog.GetString ("within parenthesis"))
			));
			
			example = @"class ClassDeclaration { 
		public void Test (object o)
		{
			int i = (int)o;
		}
	}";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("Type cast"), new Category (example,
				new Option ("WithinCastParentheses", GettextCatalog.GetString ("within parenthesis")),
				new Option ("SpacesAfterTypecast", GettextCatalog.GetString ("after type cast"))
			));
			
			example = @"class ClassDeclaration { 
		public void Test ()
		{
			int i = sizeof (ClassDeclaration);
		}
	}";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("'sizeof'"), new Category (example,
				new Option ("BeforeSizeOfParentheses", GettextCatalog.GetString ("before opening parenthesis")),
				new Option ("WithinSizeOfParentheses", GettextCatalog.GetString ("within parenthesis"))
			));
			
			example = @"class ClassDeclaration { 
		public void Test ()
		{
			Type t = typeof (ClassDeclaration);
		}
	}";
			whiteSpaceCategory.AppendValues (category, GettextCatalog.GetString ("'typeof'"), new Category (example,
				new Option ("BeforeTypeOfParentheses", GettextCatalog.GetString ("before opening parenthesis")),
				new Option ("WithinTypeOfParentheses", GettextCatalog.GetString ("within parenthesis"))
			));
			
			
			whiteSpaceCategory.AppendValues (GettextCatalog.GetString ("Around Operators"), new Category (operatorExample,
				new Option ("AroundAssignmentParentheses", GettextCatalog.GetString ("Assignment (=, -=, ...)")),
				new Option ("AroundLogicalOperatorParentheses", GettextCatalog.GetString ("Logical (&amp;&amp;,||) operators")),
				new Option ("AroundEqualityOperatorParentheses", GettextCatalog.GetString ("Equality (==, !=) operators")),
				new Option ("AroundRelationalOperatorParentheses", GettextCatalog.GetString ("Relational (&lt;,&gt;,&lt;=,&gt;=) operators")),
				new Option ("AroundBitwiseOperatorParentheses", GettextCatalog.GetString ("Bitwise (&amp;,|,^) operators")),
				new Option ("AroundAdditiveOperatorParentheses", GettextCatalog.GetString ("Additive (+,-) operators")),
				new Option ("AroundMultiplicativeOperatorParentheses", GettextCatalog.GetString ("Multiplicative (*,/,%) operators")),
				new Option ("AroundShiftOperatorParentheses", GettextCatalog.GetString ("Shift (&lt;&lt;,&gt;&gt;) operators"))
			));
			
			whiteSpaceCategory.AppendValues (GettextCatalog.GetString ("Conditional Operator (?:)"), new Category (condOpExample,
				new Option ("ConditionalOperatorBeforeConditionSpace", GettextCatalog.GetString ("before '?'")),
				new Option ("ConditionalOperatorAfterConditionSpace", GettextCatalog.GetString ("after '?'")),
				new Option ("ConditionalOperatorBeforeSeparatorSpace", GettextCatalog.GetString ("before ':'")),
				new Option ("ConditionalOperatorAfterSeparatorSpace", GettextCatalog.GetString ("after ':'"))
			));
			
			example = @"class ClassDeclaration { 
		string[][] field;
		int[] test;
	}";
			whiteSpaceCategory.AppendValues (GettextCatalog.GetString ("Array Declarations"), new Category (example,
				new Option ("SpacesBeforeArrayDeclarationBrackets", GettextCatalog.GetString ("before opening bracket"))
			));
			
			whiteSpaceOptions= new ListStore (typeof (Option), typeof (bool), typeof (bool)); 
			column = new TreeViewColumn ();
			// text column
			column.PackStart (cellRendererText, true);
			column.SetCellDataFunc (cellRendererText, delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				((CellRendererText)cell).Text = ((Option)model.GetValue (iter, 0)).DisplayName;
			});
			treeviewInsertWhiteSpaceOptions.AppendColumn (column);
			
			column = new TreeViewColumn ();
			cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = comboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;

			cellRendererCombo.Edited += delegate(object o, EditedArgs args) {
				TreeIter iter;
				var model = whiteSpaceOptions;
				if (model.GetIterFromString (out iter, args.Path)) {
					var option = (Option)model.GetValue (iter, 0);
					PropertyInfo info = GetPropertyByName (option.PropertyName);
					if (info == null)
						return;
					var value = Enum.Parse (info.PropertyType, args.NewText);
					info.SetValue (profile, value, null);
					UpdateExample (texteditor.Document.Text);
				}
			};
			
			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", 2);
			column.SetCellDataFunc (cellRendererCombo,  delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				((CellRendererCombo)cell).Text = GetValue (((Option)model.GetValue (iter, 0)).PropertyName).ToString ();
			});
			
			cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += delegate(object o, ToggledArgs args) {
				TreeIter iter;
				var model = whiteSpaceOptions;
				if (model.GetIterFromString (out iter, args.Path)) {
					var option = (Option)model.GetValue (iter, 0);
					PropertyInfo info = GetPropertyByName (option.PropertyName);
					if (info == null || info.PropertyType != typeof(bool))
						return;
					bool value = (bool)info.GetValue (this.profile, null);
					info.SetValue (profile, !value, null);
					UpdateExample (texteditor.Document.Text);
				}
			};
			
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", 1);
			column.SetCellDataFunc (cellRendererToggle,  delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				((CellRendererToggle)cell).Active = (bool)GetValue (((Option)model.GetValue (iter, 0)).PropertyName);
			});
			
			treeviewInsertWhiteSpaceOptions.AppendColumn (column);
			
			treeviewInsertWhiteSpaceOptions.Model = whiteSpaceOptions;
			treeviewInsertWhiteSpaceCategory.ExpandAll ();
			#endregion
			
			#region Blank line options
			entryBeforUsings.Text = profile.BlankLinesBeforeUsings.ToString ();
			entryAfterUsings.Text = profile.BlankLinesAfterUsings.ToString ();
			
			entryBeforeFirstDeclaration.Text = profile.BlankLinesBeforeFirstDeclaration.ToString ();
			entryBetweenTypes.Text = profile.BlankLinesBetweenTypes.ToString ();
			
			entryBetweenFields.Text = profile.BlankLinesBetweenFields.ToString ();
			entryBetweenEvents.Text = profile.BlankLinesBetweenEventFields.ToString ();
			entryBetweenMembers.Text = profile.BlankLinesBetweenMembers.ToString ();
			
			entryBeforUsings.Changed += HandleEntryBeforUsingsChanged;
			entryAfterUsings.Changed += HandleEntryBeforUsingsChanged;
			entryBeforeFirstDeclaration.Changed += HandleEntryBeforUsingsChanged;
			entryBetweenTypes.Changed += HandleEntryBeforUsingsChanged;
			entryBetweenFields.Changed += HandleEntryBeforUsingsChanged;
			entryBetweenEvents.Changed += HandleEntryBeforUsingsChanged;
			entryBetweenMembers.Changed += HandleEntryBeforUsingsChanged;
			
			#endregion
		}
		
		int SetFlag (Gtk.Entry entry, int oldValue)
		{
			int newValue;
			if (int.TryParse (entry.Text, out newValue)) 
				return newValue;
			return oldValue;
		}

		void HandleEntryBeforUsingsChanged (object sender, EventArgs e)
		{
			profile.BlankLinesBeforeUsings = SetFlag (entryBeforUsings, profile.BlankLinesBeforeUsings);
			profile.BlankLinesAfterUsings = SetFlag (entryAfterUsings, profile.BlankLinesAfterUsings);
			profile.BlankLinesBeforeFirstDeclaration = SetFlag (entryBeforeFirstDeclaration, profile.BlankLinesBeforeFirstDeclaration);
			profile.BlankLinesBetweenTypes = SetFlag (entryBetweenTypes, profile.BlankLinesBetweenTypes);
			profile.BlankLinesBetweenFields = SetFlag (entryBetweenFields, profile.BlankLinesBetweenFields);
			profile.BlankLinesBetweenMembers = SetFlag (entryBetweenMembers, profile.BlankLinesBetweenMembers);
			profile.BlankLinesBetweenEventFields = SetFlag (entryBetweenEvents, profile.BlankLinesBetweenMembers);
			UpdateExample (blankLineExample);
		}

		void WhitespaceCategoryChanged (object sender, EventArgs e)
		{
			Gtk.TreeSelection treeSelection = (Gtk.TreeSelection)sender;
			Gtk.TreeModel model;
			Gtk.TreeIter iter;
			if (treeSelection.GetSelected (out model, out iter)) {
				var category = (Category)model.GetValue (iter, 1);
				Console.WriteLine ("category:" + model.GetValue (iter, 1));
				if (category == null)
					return;
				whiteSpaceOptions.Clear ();
				foreach (var option in category.Options) {
					PropertyInfo info = GetPropertyByName (option.PropertyName);
					bool isBool = info.PropertyType == typeof (bool);
					whiteSpaceOptions.AppendValues (option, isBool, !isBool);
				}
				UpdateExample (category.Example);
			}
		}
		
		static PropertyInfo GetPropertyByName (string name)
		{
			PropertyInfo info = typeof(CSharpFormattingPolicy).GetProperty (name);
			if (info == null)
				throw new Exception (name + " property not found");
			return info;
		}
		
		
		Gtk.TreeIter AddOption (Gtk.TreeStore model, string propertyName, string displayName, string example)
		{
			bool isBool = false;
			if (!string.IsNullOrEmpty (propertyName)) {
				PropertyInfo info = GetPropertyByName (propertyName);
				isBool = info.PropertyType == typeof (bool);
			}
			
			return model.AppendValues (propertyName, displayName, example, !string.IsNullOrEmpty (propertyName) ? isBool : false, !string.IsNullOrEmpty (propertyName) ? !isBool : false);
		}
		
		Gtk.TreeIter AddOption (Gtk.TreeStore model, Gtk.TreeIter parent, string propertyName, string displayName, string example)
		{
			bool isBool = false;
			if (!string.IsNullOrEmpty (propertyName)) {
				PropertyInfo info = GetPropertyByName (propertyName);
				isBool = info.PropertyType == typeof (bool);
			}
			
			return model.AppendValues (parent, propertyName, displayName, example, !string.IsNullOrEmpty (propertyName) ? isBool : false, !string.IsNullOrEmpty (propertyName) ? !isBool : false);
		}
		
		void TreeSelectionChanged (object sender, EventArgs e)
		{
			Gtk.TreeSelection treeSelection = (Gtk.TreeSelection)sender;
			var model = treeSelection.TreeView.Model;
			Gtk.TreeIter iter;
			if (treeSelection.GetSelected (out model, out iter)) {
				var info = GetProperty (model, iter);
				if (info != null && info.PropertyType != typeof (bool)) {
					comboBoxStore.Clear ();
					foreach (var v in Enum.GetValues (info.PropertyType)) {
						comboBoxStore.AppendValues (v.ToString (), v.ToString ());
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
			CSharpFormatter formatter = new CSharpFormatter ();
			var parent = new MonoDevelop.Projects.DotNetAssemblyProject ();
			parent.Policies.Set<CSharpFormattingPolicy> (profile, CSharpFormatter.MimeType);
			texteditor.Document.Text  = formatter.FormatText (parent.Policies, CSharpFormatter.MimeType, example);
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
			return info.GetValue (this.profile, null);
		}
		
		void RenderIcon (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) 
		{
			var pixbufCellRenderer = (CellRendererPixbuf)cell;
			if (model.IterHasChild (iter)) {
				pixbufCellRenderer.Pixbuf = ImageService.GetPixbuf (((TreeView)col.TreeView).GetRowExpanded (model.GetPath (iter)) ? MonoDevelop.Ide.Gui.Stock.OpenFolder : MonoDevelop.Ide.Gui.Stock.ClosedFolder, IconSize.Menu);
			} else {
				pixbufCellRenderer.Pixbuf = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Property, IconSize.Menu);
			}
		}
		
		void ComboboxDataFunc (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) 
		{
			var cellRenderer = (CellRendererCombo)cell;
			var info = GetProperty (model, iter);
			if (info == null) {
				cellRenderer.Text = "<invalid>";
				return;
			}
			object value = info.GetValue (this.profile, null);
			cellRenderer.Text = value.ToString ();
		}
		
		void ToggleDataFunc (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) 
		{
			var cellRenderer = (CellRendererToggle)cell;
			var info = GetProperty (model, iter);
			if (info == null || info.PropertyType != typeof(bool)) 
				return;
			bool value = (bool)info.GetValue (this.profile, null);
			cellRenderer.Active = value;
		}
		
		class CellRendererToggledHandler
		{
			CSharpFormattingProfileDialog dialog;
			Gtk.TreeStore model;
			
			public CellRendererToggledHandler (CSharpFormattingProfileDialog dialog, Gtk.TreeStore model)
			{
				this.dialog = dialog;
				this.model = model;
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
				}
			}
		}
		
		class ComboboxEditedHandler
		{
			CSharpFormattingProfileDialog dialog;
			Gtk.TreeStore model;
			
			public ComboboxEditedHandler (CSharpFormattingProfileDialog dialog, Gtk.TreeStore model)
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
					var value = Enum.Parse (info.PropertyType, args.NewText);
					info.SetValue (dialog.profile, value, null);
					dialog.UpdateExample (dialog.texteditor.Document.Text);
				}
			}
		}
		
		class Option
		{
			public string PropertyName { get; set; }
			public string DisplayName { get; set; }
			
			public Option (string propertyName, string displayName)
			{
				this.PropertyName = propertyName;
				this.DisplayName = displayName;
			}
		}
		
		class Category
		{
			public readonly string Example;
			public readonly List<Option> Options = new List<Option> ();
			
			public Category (string example, params Option[] options)
			{
				Example = example;
				Options.AddRange (options);
			}
		}
	}
}