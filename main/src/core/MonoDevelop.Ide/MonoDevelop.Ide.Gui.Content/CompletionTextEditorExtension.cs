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
using MonoDevelop.Projects.Gui.Completion;
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
		ICompletionWidget completionWidget;
		bool autoHideCompletionWindow = true;
		bool enableCodeCompletion = false;

		// When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		// Return true if the key press should be processed by the editor.
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool res;
			
			KeyAction ka = KeyAction.None;
			if (currentCompletionContext != null) {
				autoHideCompletionWindow = false;
				if (CompletionWindowManager.PreProcessKeyEvent (key, modifier, out ka)) {
					CompletionWindowManager.PostProcessKeyEvent (ka);
					autoHideCompletionWindow = true;
					return false;
				}
				autoHideCompletionWindow = false;
			}
			
			if (ParameterInformationWindowManager.IsWindowVisible) {
				if (ParameterInformationWindowManager.ProcessKeyEvent (key, modifier))
					return false;
				autoHideCompletionWindow = false;
			}
			
			int oldPos = Editor.CursorPosition;
			int oldLen = Editor.TextLength;
			
			res = base.KeyPress (key, keyChar, modifier);
			
			CompletionWindowManager.PostProcessKeyEvent (ka);
			
			if ((modifier & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
				return res;
			
			int posChange = Editor.CursorPosition - oldPos;
			if (currentCompletionContext != null && (Math.Abs (posChange) > 1 || (Editor.TextLength - oldLen) != posChange)) {
				currentCompletionContext = null;
				CompletionWindowManager.HideWindow ();
				ParameterInformationWindowManager.HideWindow ();
				return res;
			}

			if (!enableCodeCompletion)
				return res;
			
			// Handle code completion

			if (completionWidget != null && currentCompletionContext == null) {
				currentCompletionContext = completionWidget.CreateCodeCompletionContext (Editor.CursorPosition);
				
				int triggerWordLength = currentCompletionContext.TriggerWordLength;
				ICompletionDataList completionList = HandleCodeCompletion (currentCompletionContext, keyChar,
				                                                           ref triggerWordLength);

				if (triggerWordLength > 0 && (triggerWordLength < Editor.CursorPosition
				                              || (triggerWordLength == 1 && Editor.CursorPosition == 1)))
				{
					currentCompletionContext
						= completionWidget.CreateCodeCompletionContext (Editor.CursorPosition - triggerWordLength);	
					currentCompletionContext.TriggerWordLength = triggerWordLength;
				}
				
				if (completionList != null) {
					if (!CompletionWindowManager.ShowWindow (keyChar, completionList, completionWidget, 
					                                         currentCompletionContext, OnCompletionWindowClosed))
						currentCompletionContext = null;
				} else {
					currentCompletionContext = null;
				}
			}
			
			// Handle parameter completion
			
			if (ParameterInformationWindowManager.IsWindowVisible) {
				ParameterInformationWindowManager.CurrentCodeCompletionContext = Editor.CurrentCodeCompletionContext;
				ParameterInformationWindowManager.PostProcessKeyEvent (key, modifier);
			}

			if (completionWidget != null) {
				ICodeCompletionContext ctx = completionWidget.CreateCodeCompletionContext (Editor.CursorPosition);
				IParameterDataProvider paramProvider = HandleParameterCompletion (ctx, keyChar);
				if (paramProvider != null)
					ParameterInformationWindowManager.ShowWindow (ctx, paramProvider);
			}
			
			autoHideCompletionWindow = true;
			
			return res;
		}
		
		protected void ShowCompletion (ICompletionDataList completionList, int triggerWordLength, char keyChar)
		{
			if (completionWidget != null && currentCompletionContext == null) {
				currentCompletionContext = completionWidget.CreateCodeCompletionContext (Editor.CursorPosition);
				if (triggerWordLength > 0 && triggerWordLength < Editor.CursorPosition) {
					currentCompletionContext =
						completionWidget.CreateCodeCompletionContext (Editor.CursorPosition - triggerWordLength);	
					currentCompletionContext.TriggerWordLength = triggerWordLength;
				}
				if (completionList != null)
					CompletionWindowManager.ShowWindow (keyChar, completionList, completionWidget, 
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
		
		void OnCompletionContextChanged (object o, EventArgs a)
		{
			if (autoHideCompletionWindow) {
				CompletionWindowManager.HideWindow ();
				ParameterInformationWindowManager.HideWindow ();
			}
		}

		[CommandUpdateHandler (TextEditorCommands.ShowCompletionWindow)]
		internal void OnUpdateCompletionCommand (CommandInfo info)
		{
			info.Bypass = !CanRunCompletionCommand ();
		}
		
		[CommandUpdateHandler (TextEditorCommands.ShowParameterCompletionWindow)]
		internal void OnUpdateParameterCompletionCommand (CommandInfo info)
		{
			info.Bypass = !CanRunParameterCompletionCommand ();
		}
		
		[CommandHandler (TextEditorCommands.ShowCompletionWindow)]
		public virtual void RunCompletionCommand ()
		{
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CursorPosition;
				wlen = 0;
			}
			currentCompletionContext = completionWidget.CreateCodeCompletionContext (cpos);
			currentCompletionContext.TriggerWordLength = wlen;
			completionList = CodeCompletionCommand (currentCompletionContext);
				
			if (completionList != null)
				CompletionWindowManager.ShowWindow ((char)0, completionList, completionWidget, 
				                                    currentCompletionContext, OnCompletionWindowClosed);
			else
				currentCompletionContext = null;
		}
		
		[CommandHandler (TextEditorCommands.ShowCodeSurroundingsWindow)]
		public virtual void RunShowCodeSurroundingsWindow ()
		{
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CursorPosition;
				wlen = 0;
			}
			
			currentCompletionContext = completionWidget.CreateCodeCompletionContext (cpos);
			currentCompletionContext.TriggerWordLength = wlen;
			completionList = ShowCodeSurroundingsCommand (currentCompletionContext);
			
			if (completionList != null)
				CompletionWindowManager.ShowWindow ((char)0, completionList, completionWidget, currentCompletionContext, OnCompletionWindowClosed);
			else
				currentCompletionContext = null;
		}
		
		[CommandUpdateHandler (TextEditorCommands.ShowCodeSurroundingsWindow)]
		internal void OnUpdateShowCodeSurroundingsWindow (CommandInfo info)
		{
			info.Bypass = !CanRunCompletionCommand ();
		}
		
		[CommandHandler (TextEditorCommands.ShowCodeTemplateWindow)]
		public virtual void RunShowCodeTemplatesWindow ()
		{
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CursorPosition;
				wlen = 0;
			}
			
			currentCompletionContext = completionWidget.CreateCodeCompletionContext (cpos);
			currentCompletionContext.TriggerWordLength = wlen;
			completionList = ShowCodeTemplatesCommand (currentCompletionContext);
				
			if (completionList != null)
				CompletionWindowManager.ShowWindow ((char)0, completionList, completionWidget, currentCompletionContext, OnCompletionWindowClosed);
			else
				currentCompletionContext = null;
		}
		
		[CommandUpdateHandler (TextEditorCommands.ShowCodeTemplateWindow)]
		internal void OnUpdateShowCodeTemplatesWindow (CommandInfo info)
		{
			info.Bypass = !CanRunCompletionCommand ();
		}
	
		
		[CommandHandler (TextEditorCommands.ShowParameterCompletionWindow)]
		public virtual void RunParameterCompletionCommand ()
		{
			IParameterDataProvider cp = null;
			int cpos;
			if (!GetParameterCompletionCommandOffset (out cpos))
				cpos = Editor.CursorPosition;
			ICodeCompletionContext ctx = completionWidget.CreateCodeCompletionContext (cpos);
			cp = ParameterCompletionCommand (ctx);

			if (cp != null) {
				ParameterInformationWindowManager.ShowWindow (ctx, cp);
				ParameterInformationWindowManager.PostProcessKeyEvent (Gdk.Key.F, Gdk.ModifierType.None);
			}
		}
		
		public virtual bool CanRunCompletionCommand ()
		{
			return (completionWidget != null && currentCompletionContext == null);
		}
		
		public virtual bool CanRunParameterCompletionCommand ()
		{
			return (completionWidget != null && !ParameterInformationWindowManager.IsWindowVisible);
		}
		
		
		public virtual ICompletionDataList HandleCodeCompletion (ICodeCompletionContext completionContext,
		                                                         char completionChar, ref int triggerWordLength)
		{
			return HandleCodeCompletion (completionContext, completionChar);
		}
		
		public virtual ICompletionDataList HandleCodeCompletion (ICodeCompletionContext completionContext,
		                                                         char completionChar)
		{
			return null;
		}
		
		public virtual IParameterDataProvider HandleParameterCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			return null;
		}
		
		// return false if completion can't be shown
		public virtual bool GetCompletionCommandOffset (out int cpos, out int wlen)
		{
			cpos = wlen = 0;
			int pos = Editor.CursorPosition - 1;
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
			int len = Editor.TextLength;
			
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
		
		public virtual ICompletionDataList ShowCodeSurroundingsCommand (ICodeCompletionContext completionContext)
		{
			CompletionDataList list = new CompletionDataList ();
			ITemplateWidget templateWidget = Document.GetContent <ITemplateWidget> ();
			CodeTemplateContext ctx = CodeTemplateContext.Standard;
			if (templateWidget != null)
				ctx = templateWidget.GetCodeTemplateContext();
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesForFile (Document.FileName)) {
				if ((template.CodeTemplateType & CodeTemplateType.SurroundsWith) == CodeTemplateType.SurroundsWith)  {
					if (ctx == template.CodeTemplateContext)
						list.Add (new CodeTemplateCompletionData (Document, template));
				}
			}
			return list;
		}
		
		public virtual ICompletionDataList ShowCodeTemplatesCommand (ICodeCompletionContext completionContext)
		{
			CompletionDataList list = new CompletionDataList ();
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesForFile (Document.FileName)) {
				if (template.CodeTemplateType != CodeTemplateType.SurroundsWith)  {
					list.Add (new CodeTemplateCompletionData (Document, template));
				}
			}
			return list;
		}
		
		
		public virtual ICompletionDataList CodeCompletionCommand (ICodeCompletionContext completionContext)
		{
			// This default implementation of CodeCompletionCommand calls HandleCodeCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
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
		
		public virtual IParameterDataProvider ParameterCompletionCommand (ICodeCompletionContext completionContext)
		{
			// This default implementation of ParameterCompletionCommand calls HandleParameterCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
			if (txt.Length > 0) {
				IParameterDataProvider cp = HandleParameterCompletion (completionContext, txt[0]);
				if (cp != null)
					return cp;
			}
			return null;
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			
			enableCodeCompletion = (bool)PropertyService.Get ("EnableCodeCompletion", true);
			PropertyService.PropertyChanged += OnPropertyUpdated;
			completionWidget = Document.GetContent <ICompletionWidget> ();
			if (completionWidget != null)
				completionWidget.CompletionContextChanged += OnCompletionContextChanged;
		}

		bool disposed;
		public override void Dispose ()
		{
			if (!disposed) {
				disposed = false;
				PropertyService.PropertyChanged -= OnPropertyUpdated;
				base.Dispose ();
			}
		}

		void OnPropertyUpdated (object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == "EnableCodeCompletion" && e.NewValue != e.OldValue)
				enableCodeCompletion  = (bool)e.NewValue;
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
