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
		
		Gtk.TreeStore whiteSpaceCategory = new TreeStore (typeof (string), typeof (Category));
		ListStore whiteSpaceOptions= new ListStore (typeof (Option)); 
		
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
				TestMethod ("");
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
						Console.WriteLine (b++);
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
			cellRendererCombo.Editable = true;

			cellRendererCombo.EditingStarted += delegate(object o, EditingStartedArgs args) {
		/*		CodeFormatType type = description.GetCodeFormatType (settings, option);
				comboBoxStore.Clear ();
				foreach (KeyValuePair<string, string> v in type.Values) {
					comboBoxStore.AppendValues (v.Key, GettextCatalog.GetString (v.Value));
				}*/
			};
						
			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			CellRendererToggle cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += CellRendererToggleToggled;
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
			#endregion
			
			#region Brace options
			bacePositionOptions = new Gtk.TreeStore (typeof (string), typeof (string), typeof (string), typeof(bool), typeof(bool));
			
			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);
			
			// text column
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);
			
			treeviewBracePositions.Model = bacePositionOptions;
			treeviewBracePositions.HeadersVisible = false;
			treeviewBracePositions.Selection.Changed += TreeSelectionChanged;
			treeviewBracePositions.AppendColumn (column);
			
			column = new TreeViewColumn ();
			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += CellRendererToggleToggled;
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
			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += CellRendererToggleToggled;
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
			#endregion
			
			
			#region White space options
			
			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);
			
			// text column
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 0);
			
			treeviewInsertWhiteSpaceCategory.Model = whiteSpaceCategory;
			treeviewInsertWhiteSpaceCategory.HeadersVisible = false;
			treeviewInsertWhiteSpaceCategory.Selection.Changed += WhitespaceCategoryChanged;
			treeviewInsertWhiteSpaceCategory.AppendColumn (column);
			treeviewInsertWhiteSpaceOptions.Model = whiteSpaceOptions;
			
			category = whiteSpaceCategory.AppendValues (whiteSpaceCategory, GettextCatalog.GetString ("Declarations"), null);
			string example = @"class Example {
		void Test ()
		{
		}
		
		void Test (int a, int b, int c)
		{
		}
}";
			whiteSpaceCategory.AppendValues (whiteSpaceCategory, category, GettextCatalog.GetString ("Methods"), new Category (example,
				new Option ("BeforeMethodDeclarationParentheses", GettextCatalog.GetString ("before opening parenthesis")),
				new Option ("WithinMethodDeclarationParentheses", GettextCatalog.GetString ("within parenthesis")),
				new Option ("BetweenEmptyMethodDeclarationParentheses", GettextCatalog.GetString ("between empty parenthesis")),
				new Option ("BeforeMethodDeclarationParameterComma", GettextCatalog.GetString ("before comma in parenthesis")),
				new Option ("AfterMethodDeclarationParameterComma", GettextCatalog.GetString ("after comma in parenthesis"))
			));
			
			column = new TreeViewColumn ();
			// text column
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 0);
			
			
			
			#endregion
		}

		void WhitespaceCategoryChanged (object sender, EventArgs e)
		{
			Gtk.TreeSelection treeSelection = (Gtk.TreeSelection)sender;
			var model = treeSelection.TreeView.Model;
			Gtk.TreeIter iter;
			if (treeSelection.GetSelected (out model, out iter)) {
				var category = (Category)model.GetValue (iter, 1);
				if (category == null)
					return;
				whiteSpaceOptions.Clear ();
				foreach (var option in category.Options) {
					whiteSpaceOptions.AppendValues (option);
				}
				
				UpdateExample (category.Example);
			}
		}
		
		Gtk.TreeIter AddOption (Gtk.TreeStore model, string propertyName, string displayName, string example)
		{
			bool isBool = false;
			if (!string.IsNullOrEmpty (propertyName)) {
				PropertyInfo info = typeof(CSharpFormattingPolicy).GetProperty (propertyName);
				isBool = info.PropertyType == typeof (bool);
			}
			
			return model.AppendValues (propertyName, displayName, example, !string.IsNullOrEmpty (propertyName) ? isBool : false, !string.IsNullOrEmpty (propertyName) ? !isBool : false);
		}
		
		Gtk.TreeIter AddOption (Gtk.TreeStore model, Gtk.TreeIter parent, string propertyName, string displayName, string example)
		{
			bool isBool = false;
			if (!string.IsNullOrEmpty (propertyName)) {
				PropertyInfo info = typeof(CSharpFormattingPolicy).GetProperty (propertyName);
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
			return typeof(CSharpFormattingPolicy).GetProperty (propertyName);
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
		
		void CellRendererToggleToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			var model = indentOptions;
			if (model.GetIterFromString (out iter, args.Path)) {
				var info = GetProperty (model, iter);
				if (info == null || info.PropertyType != typeof(bool))
					return;
				bool value = (bool)info.GetValue (this.profile, null);
				info.SetValue (profile, !value, null);
				UpdateExample (model, iter);
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