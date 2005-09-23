// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Drawing;
using System.Diagnostics;

using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Gui;
using MonoDevelop.DefaultEditor.Actions;
using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Actions;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Services;
using MonoDevelop.Gui.Components;
using MonoDevelop.TextEditor.Gui.InsightWindow;
using MonoDevelop.TextEditor.Gui.CompletionWindow;

using MonoDevelop.EditorBindings.FormattingStrategy;

namespace MonoDevelop.DefaultEditor.Gui.Editor
{
	public class SharpDevelopTextAreaControl : TextEditorControl
	{
		readonly static string contextMenuPath       = "/SharpDevelop/ViewContent/DefaultTextEditor/ContextMenu";
		readonly static string editActionsPath       = "/AddIns/DefaultTextEditor/EditActions";
		readonly static string formatingStrategyPath = "/AddIns/DefaultTextEditor/Formater";
				
		public SharpDevelopTextAreaControl()
		{
			Document.FoldingManager.FoldingStrategy = new MonoDevelop.DefaultEditor.Gui.Editor.ParserFoldingStrategy();
			GenerateEditActions();
			
			TextAreaDragDropHandler dragDropHandler = new TextAreaDragDropHandler();
			Document.TextEditorProperties = new SharpDevelopTextEditorProperties();
		}
		
		protected override void InitializeTextAreaControl(TextAreaControl newControl)
		{
			base.InitializeTextAreaControl(newControl);
			MenuService menuService = (MenuService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(MenuService));
			newControl.ContextMenu = menuService.CreateContextMenu(this, contextMenuPath);
			newControl.TextArea.KeyEventHandler += new MonoDevelop.TextEditor.KeyEventHandler(HandleKeyPress);
			newControl.SelectionManager.SelectionChanged += new EventHandler(SelectionChanged);
			newControl.Caret.PositionChanged += new EventHandler(CaretPositionChanged);
		}
		
		void CaretPositionChanged(object sender, EventArgs e)
		{
			IStatusBarService statusBarService = (IStatusBarService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IStatusBarService));
			statusBarService.SetCaretPosition(ActiveTextAreaControl.TextArea.TextView.GetVisualColumn(ActiveTextAreaControl.Caret.Line, ActiveTextAreaControl.Caret.Column), ActiveTextAreaControl.Caret.Line, ActiveTextAreaControl.Caret.Column);
		}
		
		bool lastStatus;
		void SelectionChanged(object sender, EventArgs e)
		{
			if (ActiveTextAreaControl.SelectionManager.HasSomethingSelected != lastStatus) {
				lastStatus = ActiveTextAreaControl.SelectionManager.HasSomethingSelected;
				((DefaultWorkbench)WorkbenchSingleton.Workbench).UpdateMenu(null, null);
			}
		}
		
		void GenerateEditActions()
		{
			try {
				IEditAction[] actions = (IEditAction[])(AddInTreeSingleton.AddInTree.GetTreeNode(editActionsPath).BuildChildItems(this)).ToArray(typeof(IEditAction));
				
				foreach (IEditAction action in actions) {
					//foreach (Keys key in action.Keys) {
					//	editactions[key] = action;
					//}
				}
			} catch (TreePathNotFoundException) {
				Console.WriteLine(editActionsPath + " doesn't exists in the AddInTree");
			}
		}
		
		
		InsightWindow insightWindow = null;
		bool HandleKeyPress(char ch)
		{
			CompletionWindow completionWindow;
			
			string fileName = FileName;
			
			switch (ch) {
				case ' ':
					//TextEditorProperties.AutoInsertTemplates
					if (1 == 1) {
						string word = GetWordBeforeCaret();
						if (word != null) {
							CodeTemplateGroup templateGroup = CodeTemplateLoader.GetTemplateGroupPerFilename(FileName);
							
							if (templateGroup != null) {
								foreach (CodeTemplate template in templateGroup.Templates) {
									if (template.Shortcut == word) {
										InsertTemplate(template);
										return false;
									}
								}
							}
						}
					}
					goto case '.';
				case '<':
					try {
						completionWindow = new CompletionWindow(this, fileName, new CommentCompletionDataProvider());
						completionWindow.ShowCompletionWindow('<');
					} catch (Exception e) {
						Console.WriteLine("EXCEPTION: " + e);
					}
					return false;
				case '(':
					try {
						if (insightWindow == null ) {//|| insightWindow.IsDisposed) {
							insightWindow = new InsightWindow(this, fileName);
						}
						
						insightWindow.AddInsightDataProvider(new MethodInsightDataProvider());
						insightWindow.ShowInsightWindow();
					} catch (Exception e) {
						Console.WriteLine("EXCEPTION: " + e);
					}
					return false;
				case '[':
					try {
						if (insightWindow == null ) {//|| insightWindow.IsDisposed) {
							insightWindow = new InsightWindow(this, fileName);
						}
						
						insightWindow.AddInsightDataProvider(new IndexerInsightDataProvider());
						insightWindow.ShowInsightWindow();
					} catch (Exception e) {
						Console.WriteLine("EXCEPTION: " + e);
					}
					return false;
				case '.':
					try {
//						TextAreaPainter.IHaveTheFocusLock = true;
						completionWindow = new CompletionWindow(this, fileName, new CodeCompletionDataProvider());
						completionWindow.ShowCompletionWindow(ch);
//						TextAreaPainter.IHaveTheFocusLock = false;
					} catch (Exception e) {
						Console.WriteLine("EXCEPTION: " + e);
					}
					return false;
			}
			return false;
		}
		
		
		public string GetWordBeforeCaret()
		{
			int start = TextUtilities.FindPrevWordStart(Document, ActiveTextAreaControl.TextArea.Caret.Offset);
			return Document.GetText(start, ActiveTextAreaControl.TextArea.Caret.Offset - start);
		}
		
		public int DeleteWordBeforeCaret()
		{
			int start = TextUtilities.FindPrevWordStart(Document, ActiveTextAreaControl.TextArea.Caret.Offset);
			Document.Remove(start, ActiveTextAreaControl.TextArea.Caret.Offset - start);
			return start;
		}
		
		/// <remarks>
		/// This method inserts a code template at the current caret position
		/// </remarks>
		public void InsertTemplate(CodeTemplate template)
		{
			int newCaretOffset   = ActiveTextAreaControl.TextArea.Caret.Offset;
			string word = GetWordBeforeCaret().Trim();
			if (word.Length > 0) {
				newCaretOffset = DeleteWordBeforeCaret();
			}
			int finalCaretOffset = newCaretOffset;
			int firstLine        = Document.GetLineNumberForOffset(newCaretOffset);
			
			// save old properties, these properties cause strange effects, when not
			// be turned off (like insert curly braces or other formatting stuff)
			bool save1         = TextEditorProperties.AutoInsertCurlyBracket;
			IndentStyle save2  = TextEditorProperties.IndentStyle;
			TextEditorProperties.AutoInsertCurlyBracket = false;
			TextEditorProperties.IndentStyle            = IndentStyle.Auto;
			
			BeginUpdate();
			for (int i =0; i < template.Text.Length; ++i) {
				switch (template.Text[i]) {
					case '|':
						finalCaretOffset = newCaretOffset;
						break;
					case '\r':
						break;
					case '\t':
						new Tab().Execute(ActiveTextAreaControl.TextArea);
					break;
					case '\n':
						ActiveTextAreaControl.TextArea.Caret.Position = Document.OffsetToPosition(newCaretOffset);
						new Return().Execute(ActiveTextAreaControl.TextArea);
						newCaretOffset = ActiveTextAreaControl.TextArea.Caret.Offset;
						break;
					default:
						ActiveTextAreaControl.TextArea.InsertChar(template.Text[i]);
						newCaretOffset = ActiveTextAreaControl.TextArea.Caret.Offset;
						break;
				}
			}
			EndUpdate();
			Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, firstLine, Document.GetLineNumberForOffset(newCaretOffset)));
			Document.CommitUpdate();
			ActiveTextAreaControl.TextArea.Caret.Position = Document.OffsetToPosition(finalCaretOffset);
			
			// restore old property settings
			TextEditorProperties.AutoInsertCurlyBracket = save1;
			TextEditorProperties.IndentStyle            = save2;
		}
		
		public void InitializeFormatter()
		{
			try {
				IFormattingStrategy[] formater = (IFormattingStrategy[])(AddInTreeSingleton.AddInTree.GetTreeNode(formatingStrategyPath).BuildChildItems(this)).ToArray(typeof(IFormattingStrategy));
				//Console.WriteLine("SET FORMATTER : " + formater[0]);
				if (formater != null && formater.Length > 0) {
//					formater[0].Document = Document;
					Document.FormattingStrategy = formater[0];
				}
			} catch (TreePathNotFoundException) {
				Console.WriteLine(formatingStrategyPath + " doesn't exists in the AddInTree");
			}
		}
	}
}
