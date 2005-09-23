// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Text;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Gui;
using MonoDevelop.TextEditor.Actions;
using MonoDevelop.TextEditor;
using MonoDevelop.Services;
using MonoDevelop.EditorBindings.FormattingStrategy;
using MonoDevelop.SourceEditor.Gui;

namespace MonoDevelop.DefaultEditor.Commands
{
	public class GenerateCodeAction : AbstractMenuCommand
	{
		public override void Run()
		{
			Console.WriteLine ("Not ported to the new editor yet");
			/*
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			
			if (window == null || !(window.ViewContent is ITextEditorControlProvider)) {
				return;
			}
			TextEditorControl textEditorControl = ((ITextEditorControlProvider)window.ViewContent).TextEditorControl;
			
			IParserService parserService = (IParserService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
			
			IParseInformation parseInformation = parserService.GetParseInformation(textEditorControl.FileName);
			
			if (parseInformation == null) {
				return;
			}
			
			ICompilationUnit cu = parseInformation.MostRecentCompilationUnit as ICompilationUnit;
			if (cu == null) {
				return;
			}
			IClass currentClass = GetCurrentClass(textEditorControl, cu, textEditorControl.FileName);
			
			if (currentClass != null) {
				ArrayList categories = new ArrayList();
				/*using (FormVersion1 form = new FormVersion1(textEditorControl, new CodeGenerator[] {
					new ConstructorCodeGenerator(currentClass),
					new GetPropertiesCodeGenerator(currentClass),
					new SetPropertiesCodeGenerator(currentClass),
					new GetSetPropertiesCodeGenerator(currentClass),
					new OnXXXMethodsCodeGenerator(currentClass),
					new OverrideMethodsCodeGenerator(currentClass),
					new InterfaceImplementorCodeGenerator(currentClass),
					new AbstractClassImplementorCodeGenerator(currentClass)
				})) {
					form.ShowDialog();
				}*/
			}
		}
		
		/// <remarks>
		/// Returns the class in which the carret currently is, returns null
		/// if the carret is outside the class boundaries.
		/// </remarks>
		/*IClass GetCurrentClass(TextEditorControl textEditorControl, ICompilationUnit cu, string fileName)
		{
			
			IDocument document = textEditorControl.Document;
			if (cu != null) {
				int caretLineNumber = document.GetLineNumberForOffset(textEditorControl.ActiveTextAreaControl.Caret.Offset) + 1;
				int caretColumn     = textEditorControl.ActiveTextAreaControl.Caret.Offset - document.GetLineSegment(caretLineNumber - 1).Offset + 1;
				
				foreach (IClass c in cu.Classes) {
					if (c.Region.IsInside(caretLineNumber, caretColumn)) {
						return c;
					}
				}
			}
			return null;
		}*/
	}
	
	public class SurroundCodeAction : AbstractEditAction
	{
		public override void Execute(SourceEditorView editActionHandler)
		{
//			SelectionWindow selectionWindow = new SelectionWindow("Surround");
//			selectionWindow.Show();
		}
	}
	
	/// <summary>
	///     Add summary description for form
	/// </summary>
	/*
	public class FormVersion1 //: Form
	{
		//private System.Windows.Forms.ColumnHeader createdObject0;
		//private System.Windows.Forms.ListView categoryListView;
		//private System.Windows.Forms.Label statusLabel;
		//private System.Windows.Forms.CheckedListBox selectionListBox;
		
		
		TextEditorControl textEditorControl;
		
		CodeGenerator SelectedCodeGenerator {
			get {
				if (categoryListView.SelectedItems.Count != 1) {
					return null;
				}
				return (CodeGenerator)categoryListView.SelectedItems[0].Tag;
				return null;
			}
		}
		
		public FormVersion1(TextEditorControl textEditorControl, CodeGenerator[] codeGenerators)
		{
			this.textEditorControl = textEditorControl;
			
			//  Must be called for initialization
			this.InitializeComponents();
			
			Point caretPos  = textEditorControl.ActiveTextAreaControl.Caret.Position;
			Point visualPos = new Point(textEditorControl.ActiveTextAreaControl.TextArea.TextView.GetDrawingXPos(caretPos.Y, caretPos.X) + textEditorControl.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.X,
			          (int)((1 + caretPos.Y) * textEditorControl.ActiveTextAreaControl.TextArea.TextView.FontHeight) - textEditorControl.ActiveTextAreaControl.TextArea.VirtualTop.Y - 1 + textEditorControl.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.Y);
			//Location = textEditorControl.ActiveTextAreaControl.TextArea.PointToScreen(visualPos);  //FIXME:Should we be defining this pedro?
			//StartPosition   = FormStartPosition.Manual;
			
			ClassBrowserIconsService classBrowserIconService = (ClassBrowserIconsService)ServiceManager.Services.GetService(typeof(ClassBrowserIconsService));
			//categoryListView.SmallImageList = categoryListView.LargeImageList = classBrowserIconService.ImageList;
			
			foreach (CodeGenerator codeGenerator in codeGenerators) {
				if (codeGenerator.Content.Count > 0) {
					//ListViewItem newItem = new ListViewItem(codeGenerator.CategoryName);
					//newItem.ImageIndex = codeGenerator.ImageIndex;
					//newItem.Tag        = codeGenerator;
					//categoryListView.Items.Add(newItem);
				}
			}
			
			//categoryListView.SelectedIndexChanged += new EventHandler(CategoryListViewItemChanged);
		}
		protected void OnActivated(EventArgs e)
		{
			//base.OnActivated(e);
			if (categoryListView.Items.Count > 0) {
				categoryListView.Select();
				categoryListView.Focus();
				categoryListView.Items[0].Focused = categoryListView.Items[0].Selected = true;
			} else {
				Close();
			}
		}
		
		protected bool ProcessDialogKey()
		{
			
			switch (keyData) {
				case Keys.Escape:
					Close();
					return true;
				case Keys.Back:
					categoryListView.Focus();
					return true;
				case Keys.Return:
					if (categoryListView.Focused) {
						selectionListBox.Focus();
					} else {
						Close();
						SelectedCodeGenerator.GenerateCode(textEditorControl.ActiveTextAreaControl.TextArea, selectionListBox.CheckedItems.Count > 0 ? (IList)selectionListBox.CheckedItems : (IList)selectionListBox.SelectedItems);
					}
					return true;
			}
			return base.ProcessDialogKey(keyData);
			
			return false;
		}
		
		void CategoryListViewItemChanged(object sender, EventArgs e)
		{
			
			CodeGenerator codeGenerator = SelectedCodeGenerator;
			if (codeGenerator == null) {
				return;
			}
			statusLabel.Text = codeGenerator.Hint;
			selectionListBox.BeginUpdate();
			selectionListBox.Items.Clear();
			foreach (object o in codeGenerator.Content) {
				selectionListBox.Items.Add(o);
			}
			selectionListBox.SelectedIndex = 0;
			selectionListBox.EndUpdate();
			
		}
		
		/// <summary>
		///   This method was autogenerated - do not change the contents manually
		/// </summary>
		private void InitializeComponents()
		{
			
			// 
			//  Set up generated class form
			// 
			this.SuspendLayout();
			this.Name = "form";
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Size = new System.Drawing.Size(264, 312);
			this.ShowInTaskbar = false;
			
			// 
			//  Set up member selectionListBox
			// 
			selectionListBox = new System.Windows.Forms.CheckedListBox();
			selectionListBox.Name = "selectionListBox";
			selectionListBox.Location = new System.Drawing.Point(0, 128);
			selectionListBox.Size = new System.Drawing.Size(264, 184);
			selectionListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			selectionListBox.TabIndex = 2;
			this.Controls.Add(selectionListBox);
			
			// 
			//  Set up member statusLabel
			// 
			statusLabel = new System.Windows.Forms.Label();
			statusLabel.Name = "statusLabel";
			statusLabel.Text = "Choose fields to generate getters and setters";
			statusLabel.TabIndex = 1;
			statusLabel.Size = new System.Drawing.Size(264, 16);
			statusLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			statusLabel.Location = new System.Drawing.Point(0, 112);
			statusLabel.Dock = System.Windows.Forms.DockStyle.Top;
			this.Controls.Add(statusLabel);
			
			// 
			//  Set up member categoryListView
			// 
			categoryListView = new System.Windows.Forms.ListView();
			categoryListView.Name = "categoryListView";
			categoryListView.Dock = System.Windows.Forms.DockStyle.Top;
			categoryListView.TabIndex = 0;
			categoryListView.View = System.Windows.Forms.View.Details;
			categoryListView.Size = new System.Drawing.Size(264, 112);
			categoryListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			
			// 
			//  Set up member createdObject0
			// 
			createdObject0 = new System.Windows.Forms.ColumnHeader();
			createdObject0.Width = 258;
			categoryListView.Columns.Add(createdObject0);
			this.Controls.Add(categoryListView);
			this.ResumeLayout(false);
			
		}
	}
	*/
	
	/*
	public abstract class CodeGenerator
	{
		ArrayList content = new ArrayList();
		protected int       numOps  = 0;
		protected IAmbience csa;
		protected IClass    currentClass = null;
		//protected TextArea editActionHandler;
		
		public CodeGenerator(IClass currentClass)
		{	
			try {
				csa = (IAmbience)AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/Ambiences").BuildChildItem("CSharp", this);
			} catch {
				Console.WriteLine("CSharpAmbience not found -- is the C# backend binding loaded???");
				return;
			}
			
			this.currentClass = currentClass;
			csa.ConversionFlags = ConversionFlags.All;
		}
		
		public abstract string CategoryName {
			get;
		}
		public abstract string Hint {
			get;
		}
		public abstract int ImageIndex {
			get;
		}
		
		public ArrayList Content {
			get {
				return content;
			}
		}
		
		public void GenerateCode(TextArea editActionHandler, IList items)
		{
			numOps = 0;
			this.editActionHandler = editActionHandler;
			editActionHandler.BeginUpdate();
			
			bool save1         = editActionHandler.TextEditorProperties.AutoInsertCurlyBracket;
			IndentStyle save2  = editActionHandler.TextEditorProperties.IndentStyle;
			editActionHandler.TextEditorProperties.AutoInsertCurlyBracket = false;
			editActionHandler.TextEditorProperties.IndentStyle            = IndentStyle.Smart;
						
			
			StartGeneration(items);
			
			if (numOps > 0) {
				editActionHandler.Document.UndoStack.UndoLast(numOps);
			}
			// restore old property settings
			editActionHandler.TextEditorProperties.AutoInsertCurlyBracket = save1;
			editActionHandler.TextEditorProperties.IndentStyle            = save2;
			editActionHandler.EndUpdate();
			
			editActionHandler.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			editActionHandler.Document.CommitUpdate();
		}
		
		protected abstract void StartGeneration(IList items);
		
		protected void Return()
		{
			IndentLine();
			new Return().Execute(editActionHandler);++numOps;
		}
		
		protected void IndentLine()
		{
			int delta = editActionHandler.Document.FormattingStrategy.IndentLine(editActionHandler.Document, editActionHandler.Document.GetLineNumberForOffset(editActionHandler.Caret.Offset));
			if (delta != 0) {
				++numOps;
				LineSegment caretLine = editActionHandler.Document.GetLineSegmentForOffset(editActionHandler.Caret.Offset);
				editActionHandler.Caret.Position = editActionHandler.Document.OffsetToPosition(Math.Min(editActionHandler.Caret.Offset + delta, caretLine.Offset + caretLine.Length));
			}
		}
	}
	*/

	/*
	public abstract class FieldCodeGenerator : CodeGenerator
	{
		public FieldCodeGenerator(IClass currentClass) : base(currentClass)
		{
			foreach (IField field in currentClass.Fields) {
				Content.Add(new FieldWrapper(field));
			}
		}
		
		public class FieldWrapper
		{
			IField field;
			
			public IField Field {
				get {
					return field;
				}
			}
			
			public FieldWrapper(IField field)
			{
				this.field = field;
			}
			
			public override string ToString()
			{
				AmbienceService ambienceService = (AmbienceService)ServiceManager.Services.GetService(typeof(AmbienceService));
				return ambienceService.CurrentAmbience.Convert(field);
			}
		}
	}
	
	public class ConstructorCodeGenerator : FieldCodeGenerator
	{
		public override string CategoryName {
			get {
				return "Constructor";
			}
		}
		
		public override  string Hint {
			get {
				return "Choose fields to initialize by constructor";
			}
		}
		
		public override int ImageIndex {
			get {
				ClassBrowserIconsService classBrowserIconService = (ClassBrowserIconsService)ServiceManager.Services.GetService(typeof(ClassBrowserIconsService));
				return classBrowserIconService.MethodIndex;
			}
		}
		
		public ConstructorCodeGenerator(IClass currentClass) : base(currentClass)
		{
		}
		
		protected override void StartGeneration(IList items)
		{
			editActionHandler.InsertString("public " + currentClass.Name + "(");
			++numOps;
			
			for (int i = 0; i < items.Count; ++i) {
				FieldWrapper fw = (FieldWrapper)items[i];
				editActionHandler.InsertString(csa.Convert(fw.Field.ReturnType) + " " + fw.Field.Name);
				++numOps;
				if (i + 1 < items.Count) {
					editActionHandler.InsertString(", ");
					++numOps;
				}
			}
			
			editActionHandler.InsertChar(')');++numOps;
			Return();
			editActionHandler.InsertChar('{');++numOps;
			Return();
			
			for (int i = 0; i < items.Count; ++i) {
				FieldWrapper fw = (FieldWrapper)items[i];
				editActionHandler.InsertString("this." + fw.Field.Name + " = " + fw.Field.Name + ";");++numOps;
				Return();
			}
			editActionHandler.InsertChar('}');++numOps;
			Return();
			IndentLine();
		}
	}
	
	public abstract class PropertiesCodeGenerator : FieldCodeGenerator
	{
		
		public PropertiesCodeGenerator(IClass currentClass) : base(currentClass)
		{
		}
		
		public override int ImageIndex {
			get {
				ClassBrowserIconsService classBrowserIconService = (ClassBrowserIconsService)ServiceManager.Services.GetService(typeof(ClassBrowserIconsService));
				return classBrowserIconService.PropertyIndex;
			}
		}
		
		protected override void StartGeneration(IList items)
		{
			for (int i = 0; i < items.Count; ++i) {
				FieldWrapper fw = (FieldWrapper)items[i];
				
				editActionHandler.InsertString("public " + (fw.Field.IsStatic ? "static " : "") + csa.Convert(fw.Field.ReturnType) + " " + Char.ToUpper(fw.Field.Name[0]) + fw.Field.Name.Substring(1) + " {");++numOps;
				Return();
				
				GeneratePropertyBody(editActionHandler, fw);
				
				editActionHandler.InsertChar('}');++numOps;
				Return();
				IndentLine();
			}
		}
		
		protected void GenerateGetter(TextArea editActionHandler, FieldWrapper fw)
		{
			editActionHandler.InsertString("get {");++numOps;
			Return();
			
			editActionHandler.InsertString("return " + fw.Field.Name+ ";");++numOps;
			Return();
			
			editActionHandler.InsertChar('}');++numOps;
			Return();
		}
		
		protected void GenerateSetter(TextArea editActionHandler, FieldWrapper fw)
		{
			editActionHandler.InsertString("set {");++numOps;
			Return();
			
			editActionHandler.InsertString(fw.Field.Name+ " = value;");++numOps;
			Return();
			
			editActionHandler.InsertChar('}');++numOps;
			Return();
		}
		
		protected abstract void GeneratePropertyBody(TextArea editActionHandler, FieldWrapper fw);
	}*/

	/*
	
	public class GetPropertiesCodeGenerator : PropertiesCodeGenerator
	{
		public override string CategoryName {
			get {
				return "Getter";
			}
		}
		
		public override  string Hint {
			get {
				return "Choose fields to generate getters";
			}
		}
		
		public GetPropertiesCodeGenerator(IClass currentClass) : base(currentClass)
		{
		}
		
		protected override void GeneratePropertyBody(TextArea editActionHandler, FieldWrapper fw)
		{
			GenerateGetter(editActionHandler, fw);
		}
	}
	
	public class SetPropertiesCodeGenerator : PropertiesCodeGenerator
	{
		public override string CategoryName {
			get {
				return "Setter";
			}
		}
		
		public override  string Hint {
			get {
				return "Choose fields to generate setters";
			}
		}
		
		public SetPropertiesCodeGenerator(IClass currentClass) : base(currentClass)
		{
		}
		
		protected override void GeneratePropertyBody(TextArea editActionHandler, FieldWrapper fw)
		{
			GenerateSetter(editActionHandler, fw);
		}
	}
	
	public class GetSetPropertiesCodeGenerator : PropertiesCodeGenerator
	{
		public override string CategoryName {
			get {
				return "Getter and Setter";
			}
		}
		
		public override  string Hint {
			get {
				return "Choose fields to generate getters and setters";
			}
		}
		
		public GetSetPropertiesCodeGenerator(IClass currentClass) : base(currentClass)
		{
		}
		protected override void GeneratePropertyBody(TextArea editActionHandler, FieldWrapper fw)
		{
			GenerateGetter(editActionHandler, fw);
			GenerateSetter(editActionHandler, fw);
		}
	}
	
	public class OnXXXMethodsCodeGenerator : CodeGenerator
	{
		public override string CategoryName {
			get {
				return "Event OnXXX methods";
			}
		}
		
		public override  string Hint {
			get {
				return "Choose events to generate OnXXX methods";
			}
		}
		
		public override int ImageIndex {
			get {
				ClassBrowserIconsService classBrowserIconService = (ClassBrowserIconsService)ServiceManager.Services.GetService(typeof(ClassBrowserIconsService));
				return classBrowserIconService.EventIndex;
			}
		}
		
		public OnXXXMethodsCodeGenerator(IClass currentClass) : base(currentClass)
		{
			foreach (IEvent evt in currentClass.Events) {
				Content.Add(new EventWrapper(evt));
			}
		}
		
		protected override void StartGeneration(IList items)
		{
			for (int i = 0; i < items.Count; ++i) {
				EventWrapper ew = (EventWrapper)items[i];
				string eventArgsName = String.Empty;
				if (ew.Event.ReturnType.FullyQualifiedName.EndsWith("Handler")) {
					eventArgsName = ew.Event.ReturnType.FullyQualifiedName.Substring(0, ew.Event.ReturnType.FullyQualifiedName.Length - "Handler".Length);
				} else {
					eventArgsName = ew.Event.ReturnType.FullyQualifiedName;
				}
				eventArgsName += "Args";
				
				editActionHandler.InsertString("protected " + (ew.Event.IsStatic ? "static" : "virtual") + " void On" + ew.Event.Name + "(" + eventArgsName + " e)");++numOps;
				Return();
				editActionHandler.InsertChar('{');++numOps;
				Return();
				
				editActionHandler.InsertString("if (" + ew.Event.Name + " != null) {");++numOps;
				Return();
				editActionHandler.InsertString(ew.Event.Name + "(this, e);");++numOps;
				Return();
				editActionHandler.InsertChar('}');++numOps;
				Return();
				editActionHandler.InsertChar('}');++numOps;
				Return();
				IndentLine();
			}
		}
		
		class EventWrapper
		{
			IEvent evt;
			public IEvent Event {
				get {
					return evt;
				}
			}
			public EventWrapper(IEvent evt)
			{
				this.evt = evt;
			}
			
			public override string ToString()
			{
				AmbienceService ambienceService = (AmbienceService)ServiceManager.Services.GetService(typeof(AmbienceService));
				return ambienceService.CurrentAmbience.Convert(evt);
			}
		}
	}
	
	public class InterfaceImplementorCodeGenerator : CodeGenerator
	{
		ICompilationUnit unit;
		
		public override string CategoryName {
			get {
				return "Interface implementation";
			}
		}
		
		public override  string Hint {
			get {
				return "Choose interfaces to implement";
			}
		}
		
		public override int ImageIndex {
			get {
				ClassBrowserIconsService classBrowserIconService = (ClassBrowserIconsService)ServiceManager.Services.GetService(typeof(ClassBrowserIconsService));
				return classBrowserIconService.InterfaceIndex;
			}
		}
		
		public InterfaceImplementorCodeGenerator(IClass currentClass) : base(currentClass)
		{
			IParserService parserService = (IParserService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
			
			foreach (string className in currentClass.BaseTypes) {
				IClass baseType = parserService.GetClass(className);
				if (baseType == null) {
					this.unit = currentClass == null ? null : currentClass.CompilationUnit;
					if (unit != null) {
						foreach (IUsing u in unit.Usings) {
							baseType = u.SearchType(className);
							if (baseType != null) {
								break;
							}
						}
					}
				}
				
				if (baseType != null && baseType.ClassType == ClassType.Interface) {
					Content.Add(new ClassWrapper(baseType));
				}
			}
		}
		
		protected override void StartGeneration(IList items)
		{
			for (int i = 0; i < items.Count; ++i) {
				ClassWrapper cw = (ClassWrapper)items[i];
				Queue interfaces = new Queue();
				interfaces.Enqueue(cw.Class);
				while (interfaces.Count > 0) {
					IClass intf = (IClass)interfaces.Dequeue();
					GenerateInterface(intf);
					
					// search an enqueue all base interfaces
					foreach (string interfaceName in intf.BaseTypes) {
						IClass baseType = null;
						foreach (IUsing u in unit.Usings) {
							baseType = u.SearchType(interfaceName);
							if (baseType != null) {
								break;
							}
						}
						if (baseType != null) {
							interfaces.Enqueue(baseType);
						}
					}
				}
			}
		}
		
		void GenerateInterface(IClass intf)
		{
			Return();
			Return();
			editActionHandler.InsertString("#region " + intf.FullyQualifiedName + " interface implementation\n\t\t");++numOps;
			
			foreach (IProperty property in intf.Properties) {
				string returnType = csa.Convert(property.ReturnType);
				editActionHandler.InsertString("public " + returnType + " " + property.Name + " {");++numOps;
				Return();
				
				if (property.CanGet) {
					editActionHandler.InsertString("\tget {");++numOps;
					Return();
					editActionHandler.InsertString("\t\treturn " + GetReturnValue(returnType) +";");++numOps;
					Return();
					editActionHandler.InsertString("\t}");++numOps;
					Return();
				}
				
				if (property.CanSet) {
					editActionHandler.InsertString("\tset {");++numOps;
					Return();
					editActionHandler.InsertString("\t}");++numOps;
					Return();
				}
				
				editActionHandler.InsertChar('}');++numOps;
				Return();
				Return();
				IndentLine();
			}
			
			for (int i = 0; i < intf.Methods.Count; ++i) {
				IMethod method = intf.Methods[i];
				string parameters = String.Empty;
				string returnType = csa.Convert(method.ReturnType);
				
				for (int j = 0; j < method.Parameters.Count; ++j) {
					parameters += csa.Convert(method.Parameters[j]);
					if (j + 1 < method.Parameters.Count) {
						parameters += ", ";
					}
				}
				
				editActionHandler.InsertString("public " + returnType + " " + method.Name + "(" + parameters + ")");++numOps;
				Return();++numOps;
				editActionHandler.InsertChar('{');++numOps;
				Return();
				
				switch (returnType) {
					case "void":
						break;
					default:
						editActionHandler.InsertString("return " + GetReturnValue(returnType) + ";");++numOps;
						break;
				}
				Return();
				
				editActionHandler.InsertChar('}');++numOps;
				if (i + 1 < intf.Methods.Count) {
					Return();
					Return();
					IndentLine();
				} else {
					IndentLine();
				}
			}
			
			Return();
			editActionHandler.InsertString("#endregion");++numOps;
			Return();
		}
		
		string GetReturnValue(string returnType)
		{
			switch (returnType) {
				case "string":
					return "String.Empty";
				case "char":
					return "'\\0'";
				case "bool":
					return "false";
				case "int":
				case "long":
				case "short":
				case "byte":
				case "uint":
				case "ulong":
				case "ushort":
				case "double":
				case "float":
				case "decimal":
					return "0";
				default:
					return "null";
			}
		}
		
		class ClassWrapper
		{
			IClass c;
			public IClass Class {
				get {
					return c;
				}
			}
			public ClassWrapper(IClass c)
			{
				this.c = c;
			}
			
			public override string ToString()
			{
				AmbienceService ambienceService = (AmbienceService)ServiceManager.Services.GetService(typeof(AmbienceService));
				return ambienceService.CurrentAmbience.Convert(c);
			}
		}
	}
	
	public class OverrideMethodsCodeGenerator : CodeGenerator
	{
		public override string CategoryName {
			get {
				return "Override methods";
			}
		}
		
		public override  string Hint {
			get {
				return "Choose methods to override";
			}
		}
		
		public override int ImageIndex {
			get {
				ClassBrowserIconsService classBrowserIconService = (ClassBrowserIconsService)ServiceManager.Services.GetService(typeof(ClassBrowserIconsService));
				return classBrowserIconService.MethodIndex;
			}
		}
		
		public OverrideMethodsCodeGenerator(IClass currentClass) : base(currentClass)
		{
			foreach (IClass c in currentClass.ClassInheritanceTree) {
				if (c.FullyQualifiedName != currentClass.FullyQualifiedName) {
					foreach (IMethod method in c.Methods) {
						if (!method.IsPrivate && (method.IsAbstract || method.IsVirtual || method.IsOverride)) {
							Content.Add(new MethodWrapper(method));
						}
					}
				}
			}
		}
		
		protected override void StartGeneration(IList items)
		{
//			bool moveToMethod = sf.SelectedItems.Count == 1;
//			int  caretPos     = 0;
			for (int i = 0; i < items.Count; ++i) {
				MethodWrapper mw = (MethodWrapper)items[i];
				
				string parameters = String.Empty;
				string paramList  = String.Empty;
				string returnType = csa.Convert(mw.Method.ReturnType);
				
				for (int j = 0; j < mw.Method.Parameters.Count; ++j) {
					paramList  += mw.Method.Parameters[j].Name;
					parameters += csa.Convert(mw.Method.Parameters[j]);
					if (j + 1 < mw.Method.Parameters.Count) {
						parameters += ", ";
						paramList  += ", ";
					}
				}
				
				editActionHandler.InsertString(csa.Convert(mw.Method.Modifiers) + "override " + returnType + " " + mw.Method.Name + "(" + parameters + ")");++numOps;
				Return();
				editActionHandler.InsertChar('{');++numOps;
				Return();
				
				if (returnType != "void") {
					string str = "return base." + mw.Method.Name + "(" + paramList + ");";
					editActionHandler.InsertString(str);++numOps;
				}
				
				Return();
//				caretPos = editActionHandler.Document.Caret.Offset;

				editActionHandler.InsertChar('}');++numOps;
				Return();
				IndentLine();
			}
//			if (moveToMethod) {
//				editActionHandler.Document.Caret.Offset = caretPos;
//			}
		}
		
		class MethodWrapper
		{
			IMethod method;
			
			public IMethod Method {
				get {
					return method;
				}
			}
			
			public MethodWrapper(IMethod method)
			{
				this.method = method;
			}
			
			public override string ToString()
			{
				AmbienceService ambienceService = (AmbienceService)ServiceManager.Services.GetService(typeof(AmbienceService));
				IAmbience ambience = ambienceService.CurrentAmbience;
				ambience.ConversionFlags = ConversionFlags.None;
				return ambience.Convert(method);
			}
		}
	}
	
	public class AbstractClassImplementorCodeGenerator : CodeGenerator
	{
		ICompilationUnit unit;
		
		public override string CategoryName {
			get {
				return "Abstract class overridings";
			}
		}
		
		public override  string Hint {
			get {
				return "Choose abstract class to override";
			}
		}
		
		public override int ImageIndex {
			get {
				ClassBrowserIconsService classBrowserIconService = (ClassBrowserIconsService)ServiceManager.Services.GetService(typeof(ClassBrowserIconsService));
				return classBrowserIconService.InterfaceIndex;
			}
		}
		
		public AbstractClassImplementorCodeGenerator(IClass currentClass) : base(currentClass)
		{
			IParserService parserService = (IParserService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
			
			foreach (string className in currentClass.BaseTypes) {
				IClass baseType = parserService.GetClass(className);
				if (baseType == null) {
					this.unit = currentClass == null ? null : currentClass.CompilationUnit;
					if (unit != null) {
						foreach (IUsing u in unit.Usings) {
							baseType = u.SearchType(className);
							if (baseType != null) {
								break;
							}
						}
					}
				}
				
				if (baseType != null && baseType.ClassType == ClassType.Class && baseType.IsAbstract) {
					Content.Add(new ClassWrapper(baseType));
				}
			}
		}
		
		protected override void StartGeneration(IList items)
		{
			for (int i = 0; i < items.Count; ++i) {
				ClassWrapper cw = (ClassWrapper)items[i];
				Queue interfaces = new Queue();
				interfaces.Enqueue(cw.Class);
				while (interfaces.Count > 0) {
					IClass intf = (IClass)interfaces.Dequeue();
					GenerateInterface(intf);
					
					// search an enqueue all base interfaces
					foreach (string interfaceName in intf.BaseTypes) {
						IClass baseType = null;
						foreach (IUsing u in unit.Usings) {
							baseType = u.SearchType(interfaceName);
							if (baseType != null) {
								break;
							}
						}
						if (baseType != null) {
							interfaces.Enqueue(baseType);
						}
					}
				}
			}
		}
		
		void GenerateInterface(IClass intf)
		{
			Return();Return();
			editActionHandler.InsertString("#region " + intf.FullyQualifiedName + " abstract class implementation\n\t\t");++numOps;
			
			foreach (IProperty property in intf.Properties) {
				if (!property.IsAbstract) {
					continue;
				}
				string returnType = csa.Convert(property.ReturnType);
				if (property.IsProtected) {
					editActionHandler.InsertString("protected ");
				} else {
					editActionHandler.InsertString("public ");
				}
				
				editActionHandler.InsertString("override " + returnType + " " + property.Name + " {\n");++numOps;
				
				if (property.CanGet) {
					editActionHandler.InsertString("\tget {");++numOps;
					Return();
					editActionHandler.InsertString("\t\treturn " + GetReturnValue(returnType) +";");++numOps;
					Return();
					editActionHandler.InsertString("\t}");++numOps;
					Return();
				}
				
				if (property.CanSet) {
					editActionHandler.InsertString("\tset {");++numOps;
					Return();
					editActionHandler.InsertString("\t}");++numOps;
					Return();
				}
				
				editActionHandler.InsertChar('}');++numOps;
				Return();
				Return();
				IndentLine();
			}
			
			for (int i = 0; i < intf.Methods.Count; ++i) {
				IMethod method = intf.Methods[i];
				string parameters = String.Empty;
				string returnType = csa.Convert(method.ReturnType);
				if (!method.IsAbstract) {
					continue;
				}
				for (int j = 0; j < method.Parameters.Count; ++j) {
					parameters += csa.Convert(method.Parameters[j]);
					if (j + 1 < method.Parameters.Count) {
						parameters += ", ";
					}
				}
				if (method.IsProtected) {
					editActionHandler.InsertString("protected ");
				} else {
					editActionHandler.InsertString("public ");
				}
				
				editActionHandler.InsertString("override " + returnType + " " + method.Name + "(" + parameters + ")");++numOps;
				Return();
				editActionHandler.InsertChar('{');++numOps;
				Return();
				
				switch (returnType) {
					case "void":
						break;
					default:
						editActionHandler.InsertString("return " + GetReturnValue(returnType) + ";");++numOps;
						break;
				}
				Return();
				
				editActionHandler.InsertChar('}');++numOps;
				if (i + 1 < intf.Methods.Count) {
					Return();
					Return();
					IndentLine();
				} else {
					IndentLine();
				}
			}
			Return();
			editActionHandler.InsertString("#endregion");++numOps;
			Return();
		}
		
		string GetReturnValue(string returnType)
		{
			switch (returnType) {
				case "string":
					return "String.Empty";
				case "char":
					return "'\\0'";
				case "bool":
					return "false";
				case "int":
				case "long":
				case "short":
				case "byte":
				case "uint":
				case "ulong":
				case "ushort":
				case "double":
				case "float":
				case "decimal":
					return "0";
				default:
					return "null";
			}
		}
		
		class ClassWrapper
		{
			IClass c;
			public IClass Class {
				get {
					return c;
				}
			}
			public ClassWrapper(IClass c)
			{
				this.c = c;
			}
			
			public override string ToString()
			{
				AmbienceService ambienceService = (AmbienceService)ServiceManager.Services.GetService(typeof(AmbienceService));
				return ambienceService.CurrentAmbience.Convert(c);
			}
		}
	}
}*/
