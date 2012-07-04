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
using ICSharpCode.NRefactory.CSharp;
namespace MonoDevelop.CSharp.Formatting
{
	public partial class CSharpFormattingProfileDialog : Gtk.Dialog
	{
		Mono.TextEditor.TextEditor texteditor = new Mono.TextEditor.TextEditor ();
		CSharpFormattingPolicy profile;
		Gtk.TreeStore indentOptions, bacePositionOptions, newLineOptions, whiteSpaceOptions;
		
		static Dictionary<Wrapping, string> arrayInitializerTranslationDictionary = new Dictionary<Wrapping, string> ();
		static Dictionary<BraceStyle, string> braceStyleTranslationDictionary = new Dictionary<BraceStyle, string> ();
		static Dictionary<BraceForcement, string> braceForcementTranslationDictionary = new Dictionary<BraceForcement, string> ();
		static Dictionary<PropertyFormatting, string> propertyFormattingTranslationDictionary = new Dictionary<PropertyFormatting, string> ();
		static Dictionary<NewLinePlacement, string>  newLinePlacementTranslationDictionary = new Dictionary<NewLinePlacement, string> ();
		
		static CSharpFormattingProfileDialog ()
		{
			braceStyleTranslationDictionary [BraceStyle.DoNotChange] = GettextCatalog.GetString ("Do not change");
			braceStyleTranslationDictionary [BraceStyle.EndOfLine] = GettextCatalog.GetString ("End of line");
			braceStyleTranslationDictionary [BraceStyle.EndOfLineWithoutSpace] = GettextCatalog.GetString ("End of line without space");
			braceStyleTranslationDictionary [BraceStyle.NextLine] = GettextCatalog.GetString ("Next line");
			braceStyleTranslationDictionary [BraceStyle.NextLineShifted] = GettextCatalog.GetString ("Next line shifted");
			braceStyleTranslationDictionary [BraceStyle.NextLineShifted2] = GettextCatalog.GetString ("Next line shifted2");
			braceStyleTranslationDictionary [BraceStyle.BannerStyle] = GettextCatalog.GetString ("Banner style");
			
			braceForcementTranslationDictionary [BraceForcement.DoNotChange] = GettextCatalog.GetString ("Do not change");
			braceForcementTranslationDictionary [BraceForcement.AddBraces] = GettextCatalog.GetString ("Add braces");
			braceForcementTranslationDictionary [BraceForcement.RemoveBraces] = GettextCatalog.GetString ("Remove braces");
			
			propertyFormattingTranslationDictionary [PropertyFormatting.AllowOneLine] = GettextCatalog.GetString ("Allow one line");
			propertyFormattingTranslationDictionary [PropertyFormatting.ForceOneLine] = GettextCatalog.GetString ("Force one line");
			propertyFormattingTranslationDictionary [PropertyFormatting.ForceNewLine] = GettextCatalog.GetString ("Force new line");
			
			arrayInitializerTranslationDictionary [Wrapping.DoNotChange] = GettextCatalog.GetString ("Do not change");
			arrayInitializerTranslationDictionary [Wrapping.DoNotWrap] = GettextCatalog.GetString ("Do not Wrap");
			arrayInitializerTranslationDictionary [Wrapping.WrapAlways] = GettextCatalog.GetString ("Wrap always");
			arrayInitializerTranslationDictionary [Wrapping.WrapIfTooLong] = GettextCatalog.GetString ("Wrap if too long");

			newLinePlacementTranslationDictionary [NewLinePlacement.DoNotCare] = GettextCatalog.GetString ("Allow both");
			newLinePlacementTranslationDictionary [NewLinePlacement.NewLine] = GettextCatalog.GetString ("Always new line");
			newLinePlacementTranslationDictionary [NewLinePlacement.SameLine] = GettextCatalog.GetString ("Always same line");
		}
		
		public static string TranslateValue (object value)
		{
			if (value is BraceStyle)
				return braceStyleTranslationDictionary [(BraceStyle)value];
			if (value is BraceForcement) 
				return braceForcementTranslationDictionary [(BraceForcement)value];
			if (value is PropertyFormatting)
				return propertyFormattingTranslationDictionary [(PropertyFormatting)value];
			if (value is Wrapping)
				return arrayInitializerTranslationDictionary [(Wrapping)value];
			if (value is NewLinePlacement)
				return newLinePlacementTranslationDictionary [(NewLinePlacement)value];
			throw new Exception ("unknown property type: " + value);
		}
		
		public static object ConvertProperty (Type propertyType, string newText)
		{
			if (propertyType == typeof(BraceStyle))
				return braceStyleTranslationDictionary.First (p => p.Value == newText).Key;
			if (propertyType == typeof(BraceForcement)) 
				return braceForcementTranslationDictionary.First (p => p.Value == newText).Key;
			if (propertyType == typeof(PropertyFormatting)) 
				return propertyFormattingTranslationDictionary.First (p => p.Value == newText).Key;
			if (propertyType == typeof(Wrapping))
				return arrayInitializerTranslationDictionary.First (p => p.Value == newText).Key;
			if (propertyType == typeof(NewLinePlacement))
				return newLinePlacementTranslationDictionary.First (p => p.Value == newText).Key;
			throw new Exception ("unknown property type: " + propertyType);
		}
		
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
		object Test (object a, object b)
		{
			return a ?? b;
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
			
			notebookCategories.SwitchPage += delegate {
				TreeView treeView;
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
					treeView = treeviewInsertWhiteSpaceCategory;
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
			texteditor.Document.ReadOnly = true;
			texteditor.Document.MimeType = CSharpFormatter.MimeType;
			scrolledwindow.Child = texteditor;
			ShowAll ();
			
			#region Indent options
			indentOptions = new Gtk.TreeStore (typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool));
			
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

			cellRendererCombo.Edited += new ComboboxEditedHandler (this, indentOptions).ComboboxEdited;
			
			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			CellRendererToggle cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, treeviewIndentOptions, indentOptions).Toggled;
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
			bacePositionOptions = new Gtk.TreeStore (typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool));
			
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
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, treeviewBracePositions, bacePositionOptions).Toggled;
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
			newLineOptions = new Gtk.TreeStore (typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool));
			
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
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, treeviewNewLines, newLineOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);
			
			treeviewNewLines.AppendColumn (column);
			
			AddOption (newLineOptions, "ElseNewLinePlacement", GettextCatalog.GetString ("Place 'else' on new line"), simpleIf);
			AddOption (newLineOptions, "ElseIfNewLinePlacement", GettextCatalog.GetString ("Place 'else if' on new line"), simpleIf);
			AddOption (newLineOptions, "CatchNewLinePlacement", GettextCatalog.GetString ("Place 'catch' on new line"), simpleCatch);
			AddOption (newLineOptions, "FinallyNewLinePlacement", GettextCatalog.GetString ("Place 'finally' on new line"), simpleCatch);
			AddOption (newLineOptions, "WhileNewLinePlacement", GettextCatalog.GetString ("Place 'while' on new line"), simpleDoWhile);
			AddOption (newLineOptions, "ArrayInitializerWrapping", GettextCatalog.GetString ("Place array initializers on new line"), simpleArrayInitializer);
			treeviewNewLines.ExpandAll ();
			#endregion
			
			#region White space options
			whiteSpaceOptions = new Gtk.TreeStore (typeof (string), typeof (string), typeof (string), typeof(bool), typeof(bool));
			
			
			column = new TreeViewColumn ();
			// pixbuf column
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, RenderIcon);
			
			// text column
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", 1);
			
			treeviewInsertWhiteSpaceCategory.Model = whiteSpaceOptions;
			treeviewInsertWhiteSpaceCategory.HeadersVisible = false;
			treeviewInsertWhiteSpaceCategory.Selection.Changed += TreeSelectionChanged;
			treeviewInsertWhiteSpaceCategory.AppendColumn (column);
			
			column = new TreeViewColumn ();
			cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = comboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = !profile.IsBuiltIn;
			cellRendererCombo.Edited += new ComboboxEditedHandler (this, whiteSpaceOptions).ComboboxEdited;

			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "visible", comboVisibleColumn);
			column.SetCellDataFunc (cellRendererCombo, ComboboxDataFunc);
			
			cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Activatable = !profile.IsBuiltIn;
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += new CellRendererToggledHandler (this, treeviewInsertWhiteSpaceCategory, whiteSpaceOptions).Toggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "visible", toggleVisibleColumn);
			column.SetCellDataFunc (cellRendererToggle, ToggleDataFunc);
			
			treeviewInsertWhiteSpaceCategory.AppendColumn (column);
			
			string example = @"class Example {
		void Test ()
		{
		}
		
		void Test (int a, int b, int c)
		{
		}
}";
			category = AddOption (whiteSpaceOptions, null, GettextCatalog.GetString ("Declarations"), example);
			AddOption (whiteSpaceOptions, category, "BeforeMethodDeclarationParentheses", GettextCatalog.GetString ("before opening parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "WithinMethodDeclarationParentheses", GettextCatalog.GetString ("within parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BetweenEmptyMethodDeclarationParentheses", GettextCatalog.GetString ("between empty parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BeforeMethodDeclarationParameterComma", GettextCatalog.GetString ("before comma in parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "AfterMethodDeclarationParameterComma", GettextCatalog.GetString ("after comma in parenthesis"), example);
			
			example = @"class Example {
		int a, b, c;
}";
			category = AddOption (whiteSpaceOptions, null, GettextCatalog.GetString ("Fields"), example);
			AddOption (whiteSpaceOptions, category, "BeforeFieldDeclarationComma", GettextCatalog.GetString ("before comma in multiple field declarations"), example);
			AddOption (whiteSpaceOptions, category, "AfterFieldDeclarationComma", GettextCatalog.GetString ("after comma in multiple field declarations"), example);
			
			example = @"class Example {
	Example () 
	{
	}

	Example (int a, int b, int c) 
	{
	}
}";
			category = AddOption (whiteSpaceOptions, null, GettextCatalog.GetString ("Constructors"), example);
			AddOption (whiteSpaceOptions, category, "BeforeConstructorDeclarationParentheses", GettextCatalog.GetString ("before opening parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "WithinConstructorDeclarationParentheses", GettextCatalog.GetString ("within parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BetweenEmptyConstructorDeclarationParentheses", GettextCatalog.GetString ("between empty parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BeforeConstructorDeclarationParameterComma", GettextCatalog.GetString ("before comma in parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "AfterConstructorDeclarationParameterComma", GettextCatalog.GetString ("after comma in parenthesis"), example);
			
			example = @"class Example {
	public int this[int a, int b] {
		get {
			return a + b;
		}
	}
}";
			category = AddOption (whiteSpaceOptions, null, GettextCatalog.GetString ("Indexer"), example);
			AddOption (whiteSpaceOptions, category, "BeforeIndexerDeclarationBracket", GettextCatalog.GetString ("before opening bracket"), example);
			AddOption (whiteSpaceOptions, category, "WithinIndexerDeclarationBracket", GettextCatalog.GetString ("within brackets"), example);
			AddOption (whiteSpaceOptions, category, "BeforeIndexerDeclarationParameterComma", GettextCatalog.GetString ("before comma in brackets"), example);
			AddOption (whiteSpaceOptions, category, "AfterIndexerDeclarationParameterComma", GettextCatalog.GetString ("after comma in brackets"), example);
			
			example = @"delegate void FooBar (int a, int b, int c);
delegate void BarFoo ();
";
			
			category = AddOption (whiteSpaceOptions, null, GettextCatalog.GetString ("Delegates"), example);
			AddOption (whiteSpaceOptions, category, "BeforeDelegateDeclarationParentheses", GettextCatalog.GetString ("before opening parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "WithinDelegateDeclarationParentheses", GettextCatalog.GetString ("within parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BetweenEmptyDelegateDeclarationParentheses", GettextCatalog.GetString ("between empty parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BeforeDelegateDeclarationParameterComma", GettextCatalog.GetString ("before comma in parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "AfterDelegateDeclarationParameterComma", GettextCatalog.GetString ("after comma in parenthesis"), example);
			
			var upperCategory = AddOption (whiteSpaceOptions, null, GettextCatalog.GetString ("Statements"), null);
			
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'if'"), simpleIf);
			AddOption (whiteSpaceOptions, category, "IfParentheses", GettextCatalog.GetString ("before opening parenthesis"), simpleIf);
			AddOption (whiteSpaceOptions, category, "WithinIfParentheses", GettextCatalog.GetString ("within parenthesis"), simpleIf);
			
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'while'"), simpleWhile);
			AddOption (whiteSpaceOptions, category, "WhileParentheses", GettextCatalog.GetString ("before opening parenthesis"), simpleWhile);
			AddOption (whiteSpaceOptions, category, "WithinWhileParentheses", GettextCatalog.GetString ("within parenthesis"), simpleWhile);

			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'for'"), simpleFor);
			AddOption (whiteSpaceOptions, category, "ForParentheses", GettextCatalog.GetString ("before opening parenthesis"), simpleFor);
			AddOption (whiteSpaceOptions, category, "WithinForParentheses", GettextCatalog.GetString ("within parenthesis"), simpleFor);
			AddOption (whiteSpaceOptions, category, "SpacesBeforeForSemicolon", GettextCatalog.GetString ("before semicolon"), simpleFor);
			AddOption (whiteSpaceOptions, category, "SpacesAfterForSemicolon", GettextCatalog.GetString ("after semicolon"), simpleFor);

			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'foreach'"), simpleForeach);
			AddOption (whiteSpaceOptions, category, "ForeachParentheses", GettextCatalog.GetString ("before opening parenthesis"), simpleForeach);
			AddOption (whiteSpaceOptions, category, "WithinForEachParentheses", GettextCatalog.GetString ("within parenthesis"), simpleForeach);

			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'catch'"), simpleCatch);
			AddOption (whiteSpaceOptions, category, "CatchParentheses", GettextCatalog.GetString ("before opening parenthesis"), simpleCatch);
			AddOption (whiteSpaceOptions, category, "WithinCatchParentheses", GettextCatalog.GetString ("within parenthesis"), simpleCatch);
			
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'switch'"), switchExample);
			AddOption (whiteSpaceOptions, category, "SwitchParentheses", GettextCatalog.GetString ("before opening parenthesis"), switchExample);
			AddOption (whiteSpaceOptions, category, "WithinSwitchParentheses", GettextCatalog.GetString ("within parenthesis"), switchExample);
			
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'lock'"), simpleLock);
			AddOption (whiteSpaceOptions, category, "LockParentheses", GettextCatalog.GetString ("before opening parenthesis"), simpleLock);
			AddOption (whiteSpaceOptions, category, "WithinLockParentheses", GettextCatalog.GetString ("within parenthesis"), simpleLock);

			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'using'"), simpleUsingStatement);
			AddOption (whiteSpaceOptions, category, "UsingParentheses", GettextCatalog.GetString ("before opening parenthesis"), simpleUsingStatement);
			AddOption (whiteSpaceOptions, category, "WithinUsingParentheses", GettextCatalog.GetString ("within parenthesis"), simpleUsingStatement);
			
			
			upperCategory = AddOption (whiteSpaceOptions, null, GettextCatalog.GetString ("Expressions"), null);
			
			example = @"class Example {
		void Test ()
		{
			Console.WriteLine();
			Console.WriteLine(""{0} {1}!"", ""Hello"", ""World"");
		}
}";
			
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("Method invocations"), example);
			AddOption (whiteSpaceOptions, category, "BeforeMethodCallParentheses", GettextCatalog.GetString ("before opening parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "WithinMethodCallParentheses", GettextCatalog.GetString ("within parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BetweenEmptyMethodCallParentheses", GettextCatalog.GetString ("between empty parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BeforeMethodCallParameterComma", GettextCatalog.GetString ("before comma in parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "AfterMethodCallParameterComma", GettextCatalog.GetString ("after comma in parenthesis"), example);
			
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("Object creation"), example);
			example = @"partial class Example {
		void Test ()
		{
			var anExample = new Example (1, 2, 3);
			var emptyExample = new Example ();
		}
}";
			AddOption (whiteSpaceOptions, category, "NewParentheses", GettextCatalog.GetString ("before opening parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "WithinNewParentheses", GettextCatalog.GetString ("within parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BetweenEmptyNewParentheses", GettextCatalog.GetString ("between empty parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "BeforeNewParameterComma", GettextCatalog.GetString ("before comma in parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "AfterNewParameterComma", GettextCatalog.GetString ("after comma in parenthesis"), example);
			
			
			example = @"class Example {
		void Test ()
		{
			a[1,2] = b[3];
		}
}";
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("Element access"), example);
			AddOption (whiteSpaceOptions, category, "SpacesBeforeBrackets", GettextCatalog.GetString ("before opening bracket"), example);
			AddOption (whiteSpaceOptions, category, "SpacesWithinBrackets", GettextCatalog.GetString ("within brackets"), example);
			AddOption (whiteSpaceOptions, category, "BeforeBracketComma", GettextCatalog.GetString ("before comma in brackets"), example);
			AddOption (whiteSpaceOptions, category, "AfterBracketComma", GettextCatalog.GetString ("after comma in brackets"), example);
			
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("Parentheses"), operatorExample);
			AddOption (whiteSpaceOptions, category, "WithinParentheses", GettextCatalog.GetString ("within parenthesis"), operatorExample);
			
			example = @"class ClassDeclaration { 
		public void Test (object o)
		{
			int i = (int)o;
		}
	}";
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("Type cast"), example);
			AddOption (whiteSpaceOptions, category, "WithinCastParentheses", GettextCatalog.GetString ("within parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "SpacesAfterTypecast", GettextCatalog.GetString ("after type cast"), example);
			
			example = @"class ClassDeclaration { 
		public void Test ()
		{
			int i = sizeof (ClassDeclaration);
		}
	}";
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'sizeof'"), example);
			AddOption (whiteSpaceOptions, category, "BeforeSizeOfParentheses", GettextCatalog.GetString ("before opening parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "WithinSizeOfParentheses", GettextCatalog.GetString ("within parenthesis"), example);
			
			example = @"class ClassDeclaration { 
		public void Test ()
		{
			Type t = typeof (ClassDeclaration);
		}
	}";
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("'typeof'"), example);
			AddOption (whiteSpaceOptions, category, "BeforeTypeOfParentheses", GettextCatalog.GetString ("before opening parenthesis"), example);
			AddOption (whiteSpaceOptions, category, "WithinTypeOfParentheses", GettextCatalog.GetString ("within parenthesis"), example);
			
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("Around Operators"), operatorExample);
			AddOption (whiteSpaceOptions, category, "AroundAssignmentParentheses", GettextCatalog.GetString ("Assignment (=, +=, -=, ...)"), operatorExample);
			AddOption (whiteSpaceOptions, category, "AroundLogicalOperatorParentheses", GettextCatalog.GetString ("Logical (&&, ||) operators"), operatorExample);
			AddOption (whiteSpaceOptions, category, "AroundEqualityOperatorParentheses", GettextCatalog.GetString ("Equality (==, !=) operators"), operatorExample);
			AddOption (whiteSpaceOptions, category, "AroundRelationalOperatorParentheses", GettextCatalog.GetString ("Relational (<, >, <=, >=) operators"), operatorExample);
			AddOption (whiteSpaceOptions, category, "AroundBitwiseOperatorParentheses", GettextCatalog.GetString ("Bitwise &, |, ^, ~() operators"), operatorExample);
			AddOption (whiteSpaceOptions, category, "AroundAdditiveOperatorParentheses", GettextCatalog.GetString ("Additive (+, -) operators"), operatorExample);
			AddOption (whiteSpaceOptions, category, "AroundMultiplicativeOperatorParentheses", GettextCatalog.GetString ("Multiplicative (*, /, %) operators"), operatorExample);
			AddOption (whiteSpaceOptions, category, "AroundShiftOperatorParentheses", GettextCatalog.GetString ("Shift (<<, >>) operators"), operatorExample);
			AddOption (whiteSpaceOptions, category, "AroundNullCoalescingOperator", GettextCatalog.GetString ("Null coalescing (??) operator"), operatorExample);
			
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("Conditional Operator (?:)"), condOpExample);
			AddOption (whiteSpaceOptions, category, "ConditionalOperatorBeforeConditionSpace", GettextCatalog.GetString ("before '?'"), condOpExample);
			AddOption (whiteSpaceOptions, category, "ConditionalOperatorAfterConditionSpace", GettextCatalog.GetString ("after '?'"), condOpExample);
			AddOption (whiteSpaceOptions, category, "ConditionalOperatorBeforeSeparatorSpace", GettextCatalog.GetString ("before ':'"), condOpExample);
			AddOption (whiteSpaceOptions, category, "ConditionalOperatorAfterSeparatorSpace", GettextCatalog.GetString ("after ':'"), condOpExample);
			
			example = @"class ClassDeclaration { 
		string[][] field;
		int[] test;
	}";
			category = AddOption (whiteSpaceOptions, upperCategory, null, GettextCatalog.GetString ("Array Declarations"), example);
			AddOption (whiteSpaceOptions, category, "SpacesBeforeArrayDeclarationBrackets", GettextCatalog.GetString ("before opening bracket"), example);
			/*
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
			
			treeviewInsertWhiteSpaceOptions.Model = whiteSpaceOptions;*/
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
						comboBoxStore.AppendValues (v.ToString (),  TranslateValue (v));
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
			var formatter = new CSharpFormatter ();
			string text;
			if (!string.IsNullOrEmpty (example)) {
				text = Environment.NewLine != "\n" ? example.Replace ("\n", Environment.NewLine) : example;
			} else {
				text = "";
			}
			texteditor.Document.Text = formatter.FormatText (profile, null, CSharpFormatter.MimeType, text, 0, text.Length);
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
			
			cellRenderer.Text = value is Enum ? TranslateValue (value) : value.ToString ();
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
			Gtk.TreeView treeView;
			
			public CellRendererToggledHandler (CSharpFormattingProfileDialog dialog, Gtk.TreeView treeView, Gtk.TreeStore model)
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
					var value = ConvertProperty (info.PropertyType, args.NewText);
					info.SetValue (dialog.profile, value, null);
					dialog.UpdateExample (model, iter);
				}
			}
		}
	}
}
