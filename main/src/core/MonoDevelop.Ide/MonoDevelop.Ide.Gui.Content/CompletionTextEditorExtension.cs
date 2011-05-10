// CompletionTextEditorExtension.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using MonoDevelop.Projects;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeTemplates;

namespace MonoDevelop.Ide.Gui.Content
{
	public class CompletionTextEditorExtension: TextEditorExtension
	{
		CodeCompletionContext currentCompletionContext;

		bool autoHideCompletionWindow = true;
		bool enableCodeCompletion = false;
		bool enableParameterInsight = false;
		
		public ICompletionWidget CompletionWidget {
			get;
			set;
		}
		
		public void ShowCompletion (ICompletionDataList completionList)
		{
			currentCompletionContext = CompletionWidget.CreateCodeCompletionContext (Document.Editor.Caret.Offset);
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Document.Editor.Caret.Offset;
				wlen = 0;
			}
			currentCompletionContext.TriggerOffset = cpos;
			currentCompletionContext.TriggerWordLength = wlen;
			
			CompletionWindowManager.ShowWindow ('\0', completionList, CompletionWidget, currentCompletionContext, OnCompletionWindowClosed);
		}

		// When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		// Return true if the key press should be processed by the editor.
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool res;
			
			KeyActions ka = KeyActions.None;
			if (currentCompletionContext != null) {
				if (CompletionWindowManager.PreProcessKeyEvent (key, keyChar, modifier, out ka)) {
					CompletionWindowManager.PostProcessKeyEvent (ka, key, keyChar, modifier);
					autoHideCompletionWindow = true;
					return false;
				}
				autoHideCompletionWindow = false;
			}
			
			if (ParameterInformationWindowManager.IsWindowVisible) {
				if (ParameterInformationWindowManager.ProcessKeyEvent (CompletionWidget, key, modifier))
					return false;
				autoHideCompletionWindow = false;
			}
			
//			int oldPos = Editor.CursorPosition;
//			int oldLen = Editor.TextLength;
			
			res = base.KeyPress (key, keyChar, modifier);
			
			CompletionWindowManager.PostProcessKeyEvent (ka, key, keyChar, modifier);
			
			var ignoreMods = Gdk.ModifierType.ControlMask | Gdk.ModifierType.MetaMask
				| Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.SuperMask;
			
			// Handle parameter completion
			if (ParameterInformationWindowManager.IsWindowVisible) {
				ParameterInformationWindowManager.PostProcessKeyEvent (CompletionWidget, key, modifier);
			}
			
			if ((modifier & ignoreMods) != 0)
				return res;
			/*
			if (Document.TextEditorData == null || Document.TextEditorData.IsSomethingSelected && Document.TextEditorData.SelectionMode != Mono.TextEditor.SelectionMode.Block) {
				int posChange = Editor.CursorPosition - oldPos;
				if (currentCompletionContext != null && (Math.Abs (posChange) > 1 || (Editor.TextLength - oldLen) != posChange)) {
					currentCompletionContext = null;
					CompletionWindowManager.HideWindow ();
					ParameterInformationWindowManager.HideWindow ();
					return res;
				}
			}*/

			if (!enableCodeCompletion)
				return res;
			
			// Handle code completion

			if (keyChar != '\0' && CompletionWidget != null && currentCompletionContext == null) {
				currentCompletionContext = CompletionWidget.CurrentCodeCompletionContext;
				
				int triggerWordLength = currentCompletionContext.TriggerWordLength;
				ICompletionDataList completionList = HandleCodeCompletion (currentCompletionContext, keyChar,
				                                                           ref triggerWordLength);
				
				if (triggerWordLength > 0 && (triggerWordLength < Editor.Caret.Offset
				                              || (triggerWordLength == 1 && Editor.Caret.Offset == 1))) {
					currentCompletionContext
						= CompletionWidget.CreateCodeCompletionContext (Editor.Caret.Offset - triggerWordLength);
					currentCompletionContext.TriggerWordLength = triggerWordLength;
				}
				
				if (completionList != null) {
					if (!CompletionWindowManager.ShowWindow (keyChar, completionList, CompletionWidget, 
					                                         currentCompletionContext, OnCompletionWindowClosed))
						currentCompletionContext = null;
				} else {
					currentCompletionContext = null;
				}
			}
			
			if (enableParameterInsight && CompletionWidget != null) {
				CodeCompletionContext ctx = CompletionWidget.CurrentCodeCompletionContext;
				IParameterDataProvider paramProvider = HandleParameterCompletion (ctx, keyChar);
				if (paramProvider != null)
					ParameterInformationWindowManager.ShowWindow (CompletionWidget, ctx, paramProvider);
			}
			
			autoHideCompletionWindow = true;
			
			return res;
		}
		
		protected void ShowCompletion (ICompletionDataList completionList, int triggerWordLength, char keyChar)
		{
			if (CompletionWidget != null && currentCompletionContext == null) {
				currentCompletionContext = CompletionWidget.CurrentCodeCompletionContext;
				if (triggerWordLength > 0 && triggerWordLength < Editor.Caret.Offset) {
					currentCompletionContext =
						CompletionWidget.CreateCodeCompletionContext (Editor.Caret.Offset - triggerWordLength);	
					currentCompletionContext.TriggerWordLength = triggerWordLength;
				}
				if (completionList != null)
					CompletionWindowManager.ShowWindow (keyChar, completionList, CompletionWidget, 
					                                    currentCompletionContext, OnCompletionWindowClosed);
				else
					currentCompletionContext = null;
			}
			autoHideCompletionWindow = true;
		}
		
		void OnCompletionWindowClosed ()
		{
			currentCompletionContext = null;
		}
		
		protected void OnCompletionContextChanged (object o, EventArgs a)
		{
			if (autoHideCompletionWindow) {
				CompletionWindowManager.HideWindow ();
				ParameterInformationWindowManager.HideWindow (CompletionWidget);
			}
		}

		[CommandUpdateHandler (TextEditorCommands.ShowCompletionWindow)]
		internal void OnUpdateCompletionCommand (CommandInfo info)
		{
			info.Bypass = !CanRunCompletionCommand () && !CompletionWindowManager.IsVisible;
		}
		
		[CommandUpdateHandler (TextEditorCommands.ShowParameterCompletionWindow)]
		internal void OnUpdateParameterCompletionCommand (CommandInfo info)
		{
			info.Bypass = !CanRunParameterCompletionCommand ();
		}
		
		[CommandHandler (TextEditorCommands.ShowCompletionWindow)]
		public virtual void RunCompletionCommand ()
		{
			if (CompletionWindowManager.IsVisible) {
				CompletionWindowManager.Wnd.ToggleCategoryMode ();
				return;
			}
			
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.Caret.Offset;
				wlen = 0;
			}
			currentCompletionContext = CompletionWidget.CreateCodeCompletionContext (cpos);
			currentCompletionContext.TriggerWordLength = wlen;
			completionList = CodeCompletionCommand (currentCompletionContext);
				
			if (completionList != null)
				CompletionWindowManager.ShowWindow ((char)0, completionList, CompletionWidget, 
				                                    currentCompletionContext, OnCompletionWindowClosed);
			else
				currentCompletionContext = null;
		}
		
		[CommandHandler (TextEditorCommands.ShowCodeTemplateWindow)]
		public virtual void RunShowCodeTemplatesWindow ()
		{
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.Caret.Offset;
				wlen = 0;
			}
			
			currentCompletionContext = CompletionWidget.CreateCodeCompletionContext (cpos);
			currentCompletionContext.TriggerWordLength = wlen;
			completionList = Document.Editor.IsSomethingSelected ? ShowCodeSurroundingsCommand (currentCompletionContext) : ShowCodeTemplatesCommand (currentCompletionContext);
			
			if (completionList != null)
				CompletionWindowManager.ShowWindow ((char)0, completionList, CompletionWidget, currentCompletionContext, OnCompletionWindowClosed);
			else
				currentCompletionContext = null;
		}
		
		[CommandUpdateHandler (TextEditorCommands.ShowCodeTemplateWindow)]
		internal void OnUpdateShowCodeTemplatesWindow (CommandInfo info)
		{
			info.Bypass = !CanRunCompletionCommand ();
			info.Text = Document.Editor.IsSomethingSelected ? GettextCatalog.GetString ("_Surround With...") : GettextCatalog.GetString ("I_nsert Template...");
		}
	
		
		[CommandHandler (TextEditorCommands.ShowParameterCompletionWindow)]
		public virtual void RunParameterCompletionCommand ()
		{
			IParameterDataProvider cp = null;
			int cpos;
			if (!GetParameterCompletionCommandOffset (out cpos))
				cpos = Editor.Caret.Offset;
			CodeCompletionContext ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
			cp = ParameterCompletionCommand (ctx);
			if (cp != null) {
				ParameterInformationWindowManager.ShowWindow (CompletionWidget, ctx, cp);
				ParameterInformationWindowManager.PostProcessKeyEvent (CompletionWidget, Gdk.Key.F, Gdk.ModifierType.None);
			}
		}
		
		public virtual bool CanRunCompletionCommand ()
		{
			return (CompletionWidget != null && currentCompletionContext == null);
		}
		
		public virtual bool CanRunParameterCompletionCommand ()
		{
			return (CompletionWidget != null && !ParameterInformationWindowManager.IsWindowVisible);
		}
		
		
		public virtual ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext,
		                                                         char completionChar, ref int triggerWordLength)
		{
			return HandleCodeCompletion (completionContext, completionChar);
		}
		
		public virtual ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext,
		                                                         char completionChar)
		{
			return null;
		}
		
		public virtual IParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
		{
			return null;
		}
		
		// return false if completion can't be shown
		public virtual bool GetCompletionCommandOffset (out int cpos, out int wlen)
		{
			cpos = wlen = 0;
			int pos = Editor.Caret.Offset - 1;
			while (pos >= 0) {
				char c = Editor.GetCharAt (pos);
				if (!char.IsLetterOrDigit (c) && c != '_')
					break;
				pos--;
			}
			if (pos == -1)
				return false;
			
			pos++;
			cpos = pos;
			int len = Editor.Length;
			
			while (pos < len) {
				char c = Editor.GetCharAt (pos);
				if (!char.IsLetterOrDigit (c) && c != '_')
					break;
				pos++;
			}
			wlen = pos - cpos;
			return true;
		}

		public virtual bool GetParameterCompletionCommandOffset (out int cpos)
		{
			cpos = 0;
			return false;
		}
		
		public virtual ICompletionDataList ShowCodeSurroundingsCommand (CodeCompletionContext completionContext)
		{
			CompletionDataList list = new CompletionDataList ();
			list.CompletionSelectionMode = CompletionSelectionMode.OwnTextField;
			var templateWidget = Document.GetContent<ICodeTemplateContextProvider> ();
			CodeTemplateContext ctx = CodeTemplateContext.Standard;
			if (templateWidget != null)
				ctx = templateWidget.GetCodeTemplateContext ();
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesForFile (Document.FileName)) {
				if ((template.CodeTemplateType & CodeTemplateType.SurroundsWith) == CodeTemplateType.SurroundsWith)  {
					if (ctx == template.CodeTemplateContext)
						list.Add (new CodeTemplateCompletionData (Document, template));
				}
			}
			return list;
		}
		
		public virtual ICompletionDataList ShowCodeTemplatesCommand (CodeCompletionContext completionContext)
		{
			CompletionDataList list = new CompletionDataList ();
			list.CompletionSelectionMode = CompletionSelectionMode.OwnTextField;
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesForFile (Document.FileName)) {
				if (template.CodeTemplateType != CodeTemplateType.SurroundsWith)  {
					list.Add (new CodeTemplateCompletionData (Document, template));
				}
			}
			return list;
		}
		
		
		public virtual ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			// This default implementation of CodeCompletionCommand calls HandleCodeCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetTextBetween (pos - 1, pos);
			if (txt.Length > 0) {
				ICompletionDataList completionList = HandleCodeCompletion (completionContext, txt[0]);
				if (completionList != null)
					return completionList;
			}
			
			// If there is a parser context, try resolving by calling CtrlSpace.
			ProjectDom ctx = GetParserContext();
			if (ctx != null) {
// TODO:
				//CodeCompletionDataProvider completionProvider = new CodeCompletionDataProvider (ctx, GetAmbience ());
				//completionProvider.AddResolveResults (ctx.CtrlSpace (completionContext.TriggerLine + 1, 
//						completionContext.TriggerLineOffset + 1, FileName), true, SimpleTypeNameResolver.Instance);
//				if (!completionProvider.IsEmpty)
//					return completionProvider;
			}
			return null;
		}
		
		public virtual IParameterDataProvider ParameterCompletionCommand (CodeCompletionContext completionContext)
		{
			// This default implementation of ParameterCompletionCommand calls HandleParameterCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			
			int pos = completionContext.TriggerOffset;
			if (pos <= 0)
				return null;
			IParameterDataProvider cp = HandleParameterCompletion (completionContext, Editor.Document.GetCharAt (pos - 1));
			if (cp != null)
				return cp;
			return null;
		}
		
		public override void Initialize ()
		{
			base.Initialize ();

			enableCodeCompletion = (bool)PropertyService.Get ("EnableCodeCompletion", true);
			enableParameterInsight = (bool)PropertyService.Get ("EnableParameterInsight", true);
			
			PropertyService.PropertyChanged += OnPropertyUpdated;
			CompletionWidget = Document.GetContent <ICompletionWidget> ();
			if (CompletionWidget != null)
				CompletionWidget.CompletionContextChanged += OnCompletionContextChanged;
		}

		bool disposed = false;
		public override void Dispose ()
		{
			if (!disposed) {
				disposed = true;
				PropertyService.PropertyChanged -= OnPropertyUpdated;
				if (CompletionWidget != null)
					CompletionWidget.CompletionContextChanged -= OnCompletionContextChanged;
			}
			base.Dispose ();
		}

		void OnPropertyUpdated (object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == "EnableCodeCompletion" && e.NewValue != e.OldValue)
				enableCodeCompletion = (bool)e.NewValue;
			if (e.Key == "EnableParameterInsight" && e.NewValue != e.OldValue)
				enableParameterInsight = (bool)e.NewValue;
		}	
	}
	public interface ITypeNameResolver
	{
		string ResolveName (string typeName);
	}
	class SimpleTypeNameResolver: ITypeNameResolver
	{
		// This simple resolver removes the namespace from all class names.
		// Used in ctrl+space, since all classes shown in the completion list
		// are in scope
		
		public static SimpleTypeNameResolver Instance = new SimpleTypeNameResolver ();
		
		public string ResolveName (string typeName)
		{
			int i = typeName.LastIndexOf ('.');
			if (i == -1)
				return typeName;
			else
				return typeName.Substring (i+1);
		}
	}
}
