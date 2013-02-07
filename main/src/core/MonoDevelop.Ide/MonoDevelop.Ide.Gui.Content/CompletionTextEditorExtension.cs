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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeTemplates;
using ICSharpCode.NRefactory.Completion;

namespace MonoDevelop.Ide.Gui.Content
{
	public class CompletionTextEditorExtension: TextEditorExtension
	{
		CodeCompletionContext currentCompletionContext;

		bool autoHideCompletionWindow = true, autoHideParameterWindow = true;

		#region Completion related IDE
		public readonly static PropertyWrapper<bool> EnableCodeCompletion = PropertyService.Wrap ("EnableCodeCompletion", true);
		public readonly static PropertyWrapper<bool> EnableParameterInsight = PropertyService.Wrap ("EnableParameterInsight", true);
		public readonly static PropertyWrapper<bool> EnableAutoCodeCompletion = PropertyService.Wrap ("EnableAutoCodeCompletion", true);
//		public readonly static PropertyWrapper<bool> HideObsoleteItems = PropertyService.Wrap ("HideObsoleteItems", false);
		#endregion

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
			
			CompletionWindowManager.ShowWindow (this, '\0', completionList, CompletionWidget, currentCompletionContext);
		}

		// When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		// Return true if the key press should be processed by the editor.
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool res;
			if (currentCompletionContext != null) {
				if (CompletionWindowManager.PreProcessKeyEvent (key, keyChar, modifier)) {
					CompletionWindowManager.PostProcessKeyEvent (key, keyChar, modifier);
					autoHideCompletionWindow = true;
					// in named parameter case leave the parameter window open.
					autoHideParameterWindow = keyChar != ':';
					if (!autoHideParameterWindow && ParameterInformationWindowManager.IsWindowVisible)
						ParameterInformationWindowManager.PostProcessKeyEvent (this, CompletionWidget, key, modifier);
					
					return false;
				}
				autoHideCompletionWindow = autoHideParameterWindow = false;
			}
			
			if (ParameterInformationWindowManager.IsWindowVisible) {
				if (ParameterInformationWindowManager.ProcessKeyEvent (this, CompletionWidget, key, modifier))
					return false;
				autoHideCompletionWindow = autoHideParameterWindow = false;
			}
			
			//			int oldPos = Editor.CursorPosition;
			//			int oldLen = Editor.TextLength;
			res = base.KeyPress (key, keyChar, modifier);
			
			CompletionWindowManager.PostProcessKeyEvent (key, keyChar, modifier);
			
			var ignoreMods = Gdk.ModifierType.ControlMask | Gdk.ModifierType.MetaMask
					| Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.SuperMask;
			// Handle parameter completion
			if (ParameterInformationWindowManager.IsWindowVisible) {
				ParameterInformationWindowManager.PostProcessKeyEvent (this, CompletionWidget, key, modifier);
			}

			if ((modifier & ignoreMods) != 0)
				return res;
			
			// don't complete on block selection
			if (!EnableCodeCompletion || Document.Editor.SelectionMode == Mono.TextEditor.SelectionMode.Block)
				return res;
			
			// Handle code completion
			if (keyChar != '\0' && CompletionWidget != null && !CompletionWindowManager.IsVisible) {
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
					if (!CompletionWindowManager.ShowWindow (this, keyChar, completionList, CompletionWidget, currentCompletionContext))
						currentCompletionContext = null;
				} else {
					currentCompletionContext = null;
				}
			}
			
			if (EnableParameterInsight && CompletionWidget != null) {
				CodeCompletionContext ctx = CompletionWidget.CurrentCodeCompletionContext;
				var paramProvider = HandleParameterCompletion (ctx, keyChar);
				if (paramProvider != null)
					ParameterInformationWindowManager.ShowWindow (this, CompletionWidget, ctx, paramProvider);
			}
/*			autoHideCompletionWindow = true;
			autoHideParameterWindow = keyChar != ':';*/
			return res;
		}
		
		protected void ShowCompletion (ICompletionDataList completionList, int triggerWordLength, char keyChar)
		{
			if (Document.Editor.SelectionMode == Mono.TextEditor.SelectionMode.Block)
				return;
			if (CompletionWidget != null && currentCompletionContext == null) {
				currentCompletionContext = CompletionWidget.CurrentCodeCompletionContext;
				if (triggerWordLength > 0 && triggerWordLength < Editor.Caret.Offset) {
					currentCompletionContext =
						CompletionWidget.CreateCodeCompletionContext (Editor.Caret.Offset - triggerWordLength);	
					currentCompletionContext.TriggerWordLength = triggerWordLength;
				}
				if (completionList != null)
					CompletionWindowManager.ShowWindow (this, keyChar, completionList, CompletionWidget, currentCompletionContext);
				else
					currentCompletionContext = null;
			}
			autoHideCompletionWindow = autoHideParameterWindow = true;
		}
		
		public virtual int GetCurrentParameterIndex (int startOffset)
		{
			return -1;
		}
		

		protected void OnCompletionContextChanged (object o, EventArgs a)
		{
			if (autoHideCompletionWindow)
				CompletionWindowManager.HideWindow ();
			if (autoHideParameterWindow)
				ParameterInformationWindowManager.HideWindow (this, CompletionWidget);
			CompletionWindowManager.UpdateCursorPosition ();
			ParameterInformationWindowManager.UpdateCursorPosition (this, CompletionWidget);
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
			if (Document.Editor.SelectionMode == Mono.TextEditor.SelectionMode.Block)
				return;
			
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
				CompletionWindowManager.ShowWindow (this, (char)0, completionList, CompletionWidget, currentCompletionContext);
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
			
			var ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
			ctx.TriggerWordLength = wlen;
			completionList = Document.Editor.IsSomethingSelected ? ShowCodeSurroundingsCommand (ctx) : ShowCodeTemplatesCommand (ctx);
			if (completionList == null) {
				return;
			}
			var wnd = new CompletionListWindow (Gtk.WindowType.Toplevel);
			wnd.TypeHint = Gdk.WindowTypeHint.Dialog;
			wnd.SkipPagerHint = true;
			wnd.SkipTaskbarHint = true;
			wnd.Decorated = false;
			wnd.Extension = this;
			wnd.ShowListWindow ((char)0, completionList, CompletionWidget, ctx);
		}
		
		[CommandUpdateHandler (TextEditorCommands.ShowCodeTemplateWindow)]
		internal void OnUpdateShowCodeTemplatesWindow (CommandInfo info)
		{
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.Caret.Offset;
				wlen = 0;
			}
			
			var ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
			ctx.TriggerWordLength = wlen;
			completionList = Document.Editor.IsSomethingSelected ? ShowCodeSurroundingsCommand (ctx) : ShowCodeTemplatesCommand (ctx);

			info.Bypass = completionList == null;
			info.Text = Document.Editor.IsSomethingSelected ? GettextCatalog.GetString ("_Surround With...") : GettextCatalog.GetString ("I_nsert Template...");
		}
	
		
		[CommandHandler (TextEditorCommands.ShowParameterCompletionWindow)]
		public virtual void RunParameterCompletionCommand ()
		{
			if (Document.Editor.SelectionMode == Mono.TextEditor.SelectionMode.Block)
				return;
			ParameterDataProvider cp = null;
			int cpos;
			if (!GetParameterCompletionCommandOffset (out cpos))
				cpos = Editor.Caret.Offset;
			CodeCompletionContext ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
			cp = ParameterCompletionCommand (ctx);
			if (cp != null) {
				ParameterInformationWindowManager.ShowWindow (this, CompletionWidget, ctx, cp);
				ParameterInformationWindowManager.PostProcessKeyEvent (this, CompletionWidget, Gdk.Key.F, Gdk.ModifierType.None);
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
			return null;
		}
		
		public virtual ParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
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
			// for named arguments invoke(arg:<Expr>);
			if (pos + 1 < len && Editor.GetCharAt (pos) == ':' && Editor.GetCharAt (pos + 1) != ':') 
				pos++;
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
			list.AutoSelect = true;
			list.AutoCompleteEmptyMatch = true;
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
			list.AutoSelect = true;
			list.AutoCompleteEmptyMatch = true;
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
			if (pos > 0) {
				char ch = Editor.GetCharAt (pos - 1);
				int triggerWordLength = completionContext.TriggerWordLength;
				ICompletionDataList completionList = HandleCodeCompletion (completionContext, ch, ref triggerWordLength);
				if (completionList != null)
					return completionList;
			}
			return null;
		}
		
		public virtual ParameterDataProvider ParameterCompletionCommand (CodeCompletionContext completionContext)
		{
			// This default implementation of ParameterCompletionCommand calls HandleParameterCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			
			int pos = completionContext.TriggerOffset;
			if (pos <= 0)
				return null;
			var cp = HandleParameterCompletion (completionContext, Editor.Document.GetCharAt (pos - 1));
			if (cp != null)
				return cp;
			return null;
		}

		public virtual int GuessBestMethodOverload (IParameterDataProvider provider, int currentOverload)
		{
			int cparam = GetCurrentParameterIndex (provider.StartOffset);

			if (cparam > provider.GetParameterCount (currentOverload) && !provider.AllowParameterList (currentOverload)) {
				// Look for an overload which has more parameters
				int bestOverload = -1;
				int bestParamCount = int.MaxValue;
				for (int n=0; n<provider.Count; n++) {
					int pc = provider.GetParameterCount (n);
					if (pc < bestParamCount && pc >= cparam) {
						bestOverload = n;
						bestParamCount = pc;
					}
				}
				if (bestOverload == -1) {
					for (int n=0; n<provider.Count; n++) {
						if (provider.AllowParameterList (n)) {
							bestOverload = n;
							break;
						}
					}
				}
				return bestOverload;
			}
			return -1;
		}
		
		public override void Initialize ()
		{
			base.Initialize ();

			CompletionWindowManager.WindowClosed += HandleWindowClosed;
			CompletionWidget = Document.GetContent <ICompletionWidget> ();
			if (CompletionWidget != null)
				CompletionWidget.CompletionContextChanged += OnCompletionContextChanged;
			document.Editor.Paste += (insertionOffset, text, insertedChars) => {
				ParameterInformationWindowManager.HideWindow (this, CompletionWidget);
				CompletionWindowManager.HideWindow ();
			};
			if (document.Editor.Parent != null) {
				document.Editor.Parent.TextArea.FocusOutEvent += delegate {
					ParameterInformationWindowManager.HideWindow (this, CompletionWidget);
					CompletionWindowManager.HideWindow ();
				};
			}
		}

		void HandleWindowClosed (object sender, EventArgs e)
		{
			currentCompletionContext = null;
		}

		bool disposed = false;
		public override void Dispose ()
		{
			if (!disposed) {
				CompletionWindowManager.HideWindow ();
				ParameterInformationWindowManager.HideWindow (this, CompletionWidget);

				disposed = true;
				CompletionWindowManager.WindowClosed -= HandleWindowClosed;
				if (CompletionWidget != null)
					CompletionWidget.CompletionContextChanged -= OnCompletionContextChanged;
			}
			base.Dispose ();
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
