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
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;
using System.Threading;
using Gtk;

namespace MonoDevelop.Ide.Editor.Extension
{
	public class CompletionTextEditorExtension : TextEditorExtension
	{
		internal protected CodeCompletionContext CurrentCompletionContext {
			get;
			set;
		}

		// (Settings have been moved to IdeApp.Preferences)

		bool autoHideCompletionWindow = true, autoHideParameterWindow = true;

		ICompletionWidget completionWidget;
		internal virtual ICompletionWidget CompletionWidget
		{
			get { return completionWidget; }
			set
			{
				completionWidget = value;
			}
		}


		public virtual string CompletionLanguage
		{
			get
			{
				return "Other";
			}
		}

		public void ShowCompletion (ICompletionDataList completionList)
		{
			CurrentCompletionContext = CompletionWidget.CreateCodeCompletionContext (Editor.CaretOffset);
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CaretOffset;
				wlen = 0;
			}
			CurrentCompletionContext.TriggerOffset = cpos;
			CurrentCompletionContext.TriggerWordLength = wlen;

			CompletionWindowManager.ShowWindow (this, '\0', completionList, CompletionWidget, CurrentCompletionContext);
		}
		CancellationTokenSource completionTokenSrc = new CancellationTokenSource ();
		CancellationTokenSource parameterHintingSrc = new CancellationTokenSource ();
		// When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		// Return true if the key press should be processed by the editor.
		public override bool KeyPress (KeyDescriptor descriptor)
		{
			bool res;
			if (CurrentCompletionContext != null) {
				if (CompletionWindowManager.PreProcessKeyEvent (descriptor)) {
					CompletionWindowManager.PostProcessKeyEvent (descriptor);
					autoHideCompletionWindow = true;
					// in named parameter case leave the parameter window open.
					autoHideParameterWindow = descriptor.KeyChar != ':';
					if (!autoHideParameterWindow && ParameterInformationWindowManager.IsWindowVisible)
						ParameterInformationWindowManager.PostProcessKeyEvent (this, CompletionWidget, descriptor);

					return false;
				}
				autoHideCompletionWindow = autoHideParameterWindow = false;
			}

			if (ParameterInformationWindowManager.IsWindowVisible) {
				if (ParameterInformationWindowManager.ProcessKeyEvent (this, CompletionWidget, descriptor))
					return false;
				autoHideCompletionWindow = autoHideParameterWindow = false;
			}

			//			int oldPos = Editor.CursorPosition;
			//			int oldLen = Editor.TextLength;
			res = base.KeyPress (descriptor);

			CompletionWindowManager.PostProcessKeyEvent (descriptor);

			var ignoreMods = ModifierKeys.Control | ModifierKeys.Alt
				| ModifierKeys.Command;
			// Handle parameter completion
			if (ParameterInformationWindowManager.IsWindowVisible) {
				ParameterInformationWindowManager.PostProcessKeyEvent (this, CompletionWidget, descriptor);
			}

			if ((descriptor.ModifierKeys & ignoreMods) != 0)
				return res;

			// don't complete on block selection
			if (/*!EnableCodeCompletion ||*/ Editor.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block)
				return res;

			// Handle code completion
			if (descriptor.KeyChar != '\0' && CompletionWidget != null && !CompletionWindowManager.IsVisible) {
				CurrentCompletionContext = CompletionWidget.CurrentCodeCompletionContext;
				completionTokenSrc.Cancel ();
				completionTokenSrc = new CancellationTokenSource ();
				var token = completionTokenSrc.Token;
				var caretOffset = Editor.CaretOffset;
				try {
					var task = HandleCodeCompletionAsync (CurrentCompletionContext, descriptor.KeyChar, token);
					if (task != null) {
						var result = task.Result;
						if (result != null) {
							int triggerWordLength = result.TriggerWordLength;

							if (triggerWordLength > 0 && (triggerWordLength < caretOffset
								|| (triggerWordLength == 1 && caretOffset == 1))) {
								CurrentCompletionContext = CompletionWidget.CreateCodeCompletionContext (caretOffset - triggerWordLength);
								CurrentCompletionContext.TriggerWordLength = triggerWordLength;
							}
							if (!CompletionWindowManager.ShowWindow (this, descriptor.KeyChar, result, CompletionWidget, CurrentCompletionContext))
								CurrentCompletionContext = null;
						} else {
							CurrentCompletionContext = null;
						}
					} else {
						CurrentCompletionContext = null;
					}
				} catch (TaskCanceledException) {
					CurrentCompletionContext = null;
				} catch (AggregateException) {
					CurrentCompletionContext = null;
				}
			}

			if (CompletionWidget != null) {
				CodeCompletionContext ctx = CompletionWidget.CurrentCodeCompletionContext;
				parameterHintingSrc.Cancel ();
				parameterHintingSrc = new CancellationTokenSource ();
				var token = parameterHintingSrc.Token;
				try {
					var task = HandleParameterCompletionAsync (ctx, descriptor.KeyChar, token);
					if (task != null) {
						var result = task.Result;
						if (result != null) {
							ParameterInformationWindowManager.ShowWindow (this, CompletionWidget, ctx, result);
						}
					}
				} catch (TaskCanceledException) {
				} catch (AggregateException) {
				}

			}
			/*			autoHideCompletionWindow = true;
						autoHideParameterWindow = keyChar != ':';*/
			return res;
		}

		protected void ShowCompletion (ICompletionDataList completionList, int triggerWordLength, char keyChar)
		{
			if (Editor.SelectionMode == SelectionMode.Block)
				return;
			if (CompletionWidget != null && CurrentCompletionContext == null) {
				CurrentCompletionContext = CompletionWidget.CurrentCodeCompletionContext;
				if (triggerWordLength > 0 && triggerWordLength < Editor.CaretOffset) {
					CurrentCompletionContext =
						CompletionWidget.CreateCodeCompletionContext (Editor.CaretOffset - triggerWordLength);
					CurrentCompletionContext.TriggerWordLength = triggerWordLength;
				}
				if (completionList != null)
					CompletionWindowManager.ShowWindow (this, keyChar, completionList, CompletionWidget, CurrentCompletionContext);
				else
					CurrentCompletionContext = null;
			}
			autoHideCompletionWindow = autoHideParameterWindow = true;
		}

		public virtual int GetCurrentParameterIndex (int startOffset)
		{
			return -1;
		}


		internal protected virtual void OnCompletionContextChanged (object o, EventArgs a)
		{
			if (!IsActiveExtension ())
				return;
			if (autoHideCompletionWindow) {
				CompletionWindowManager.HideWindow ();
			}
			if (autoHideParameterWindow)
				ParameterInformationWindowManager.HideWindow (this, CompletionWidget);
			ParameterInformationWindowManager.UpdateCursorPosition (this, CompletionWidget);
		}

		internal protected virtual bool IsActiveExtension ()
		{
			return true;
		}

		[CommandUpdateHandler(TextEditorCommands.ShowCompletionWindow)]
		internal void OnUpdateCompletionCommand (CommandInfo info)
		{
			info.Bypass = !CanRunCompletionCommand () && !CompletionWindowManager.IsVisible;
		}

		[CommandUpdateHandler(TextEditorCommands.ShowParameterCompletionWindow)]
		internal void OnUpdateParameterCompletionCommand (CommandInfo info)
		{
			info.Bypass = !CanRunParameterCompletionCommand ();
		}

		[CommandHandler(TextEditorCommands.ShowCompletionWindow)]
		public virtual void RunCompletionCommand ()
		{
			if (Editor.SelectionMode == SelectionMode.Block)
				return;

			if (CompletionWindowManager.IsVisible) {
				CompletionWindowManager.Wnd.ToggleCategoryMode ();
				return;
			}
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CaretOffset;
				wlen = 0;
			}
			CurrentCompletionContext = CompletionWidget.CreateCodeCompletionContext (cpos);
			CurrentCompletionContext.TriggerWordLength = wlen;
			completionList = CodeCompletionCommand (CurrentCompletionContext);
			if (completionList != null)
				CompletionWindowManager.ShowWindow (this, (char)0, completionList, CompletionWidget, CurrentCompletionContext);
			else
				CurrentCompletionContext = null;
		}

		[CommandHandler(TextEditorCommands.ShowCodeTemplateWindow)]
		public virtual void RunShowCodeTemplatesWindow ()
		{
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CaretOffset;
				wlen = 0;
			}

			var ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
			ctx.TriggerWordLength = wlen;
			completionList = Editor.IsSomethingSelected ? ShowCodeSurroundingsCommand (ctx) : ShowCodeTemplatesCommand (ctx);
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

		[CommandUpdateHandler(TextEditorCommands.ShowCodeTemplateWindow)]
		internal void OnUpdateShowCodeTemplatesWindow (CommandInfo info)
		{
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CaretOffset;
				wlen = 0;
			}
			try {
				var ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
				ctx.TriggerWordLength = wlen;
				completionList = Editor.IsSomethingSelected ? ShowCodeSurroundingsCommand (ctx) : ShowCodeTemplatesCommand (ctx);

				info.Bypass = completionList == null;
				info.Text = Editor.IsSomethingSelected ? GettextCatalog.GetString ("_Surround With...") : GettextCatalog.GetString ("I_nsert Template...");
			} catch (Exception e) {
				LoggingService.LogError ("Error while update show code templates window", e);
				info.Bypass = true;
			}
		}


		[CommandHandler(TextEditorCommands.ShowParameterCompletionWindow)]
		public virtual void RunParameterCompletionCommand ()
		{
			if (Editor.SelectionMode == SelectionMode.Block || CompletionWidget == null)
				return;
			ParameterHintingResult cp = null;
			int cpos = Editor.CaretOffset;
			CodeCompletionContext ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
			cp = ParameterCompletionCommand (ctx);
			if (cp != null) {
				ParameterInformationWindowManager.ShowWindow (this, CompletionWidget, ctx, cp);
				ParameterInformationWindowManager.PostProcessKeyEvent (this, CompletionWidget, KeyDescriptor.FromGtk (Gdk.Key.F, 'f', Gdk.ModifierType.None));
			}
		}

		public virtual bool CanRunCompletionCommand ()
		{
			return (CompletionWidget != null && CurrentCompletionContext == null);
		}

		public virtual bool CanRunParameterCompletionCommand ()
		{
			return (CompletionWidget != null && !ParameterInformationWindowManager.IsWindowVisible);
		}

		static readonly ICompletionDataList emptyList = new CompletionDataList ();

		public virtual Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext, char completionChar, CancellationToken token = default(CancellationToken))
		{
			return Task.FromResult (emptyList);
		}

		public virtual Task<ParameterHintingResult> HandleParameterCompletionAsync (CodeCompletionContext completionContext, char completionChar, CancellationToken token = default(CancellationToken))
		{
			return Task.FromResult (ParameterHintingResult.Empty);
		}

		// return false if completion can't be shown
		public virtual bool GetCompletionCommandOffset (out int cpos, out int wlen)
		{
			cpos = wlen = 0;
			int pos = Editor.CaretOffset - 1;
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


		public virtual ICompletionDataList ShowCodeSurroundingsCommand (CodeCompletionContext completionContext)
		{
			CompletionDataList list = new CompletionDataList ();
			list.AutoSelect = true;
			list.AutoCompleteEmptyMatch = true;
			list.CompletionSelectionMode = CompletionSelectionMode.OwnTextField;
			var templateWidget = DocumentContext.GetContent<ICodeTemplateContextProvider> ();
			CodeTemplateContext ctx = CodeTemplateContext.Standard;
			if (templateWidget != null)
				ctx = templateWidget.GetCodeTemplateContext ();
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesForFile (DocumentContext.Name)) {
				if ((template.CodeTemplateType & CodeTemplateType.SurroundsWith) == CodeTemplateType.SurroundsWith) {
					if (ctx == template.CodeTemplateContext)
						list.Add (new CodeTemplateCompletionData (this, template));
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
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesForFile (DocumentContext.Name)) {
				if (template.CodeTemplateType != CodeTemplateType.SurroundsWith) {
					list.Add (new CodeTemplateCompletionData (this, template));
				}
			}
			return list;
		}
		const int CompletionTimeoutInMs = 500;
		
		public virtual ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			// This default implementation of CodeCompletionCommand calls HandleCodeCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			
			int pos = completionContext.TriggerOffset;
			if (pos > 0) {
				char ch = Editor.GetCharAt (pos - 1);
				var csc = new CancellationTokenSource (CompletionTimeoutInMs);
				try {
					var task = HandleCodeCompletionAsync (completionContext, ch, csc.Token);
					if (task == null)
						return null;
					task.Wait (csc.Token);
					if (!task.IsCompleted)
						return null;
					var completionList = task.Result;
					if (completionList != null)
						return completionList;
				} catch (TaskCanceledException) {
				} catch (AggregateException) {
				}
			}
			return null;
		}
		
		public virtual ParameterHintingResult ParameterCompletionCommand (CodeCompletionContext completionContext)
		{
			// This default implementation of ParameterCompletionCommand calls HandleParameterCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			
			int pos = completionContext.TriggerOffset;
			if (pos <= 0)
				return null;
			var csc = new CancellationTokenSource (CompletionTimeoutInMs);
			try {
				var task = HandleParameterCompletionAsync (completionContext, Editor.GetCharAt (pos - 1), csc.Token);
				if (task == null)
					return null;
				task.Wait (csc.Token);
				if (!task.IsCompleted)
					return null;
				var cp = task.Result;
				if (cp != null)
					return cp;
			} catch (TaskCanceledException) {
			} catch (AggregateException) {
			}
			return null;
		}

		public virtual int GuessBestMethodOverload (ParameterHintingResult provider, int currentOverload)
		{
			int cparam = GetCurrentParameterIndex (provider.StartOffset);

			var currentHintingData = provider [currentOverload];
			if (cparam > currentHintingData.ParameterCount && !currentHintingData.IsParameterListAllowed) {
				// Look for an overload which has more parameters
				int bestOverload = -1;
				int bestParamCount = int.MaxValue;
				for (int n=0; n<provider.Count; n++) {
					int pc = provider[n].ParameterCount;
					if (pc < bestParamCount && pc >= cparam) {
						bestOverload = n;
						bestParamCount = pc;
					}
				}
				if (bestOverload == -1) {
					for (int n=0; n<provider.Count; n++) {
						if (provider[n].IsParameterListAllowed) {
							bestOverload = n;
							break;
						}
					}
				}
				return bestOverload;
			}
			return -1;
		}
		
//		void HandlePaste (int insertionOffset, string text, int insertedChars)
//		{
//			ParameterInformationWindowManager.HideWindow (this, CompletionWidget);
//			CompletionWindowManager.HideWindow ();
//		}
//
//		void HandleFocusOutEvent (object o, Gtk.FocusOutEventArgs args)
//		{
//			ParameterInformationWindowManager.HideWindow (this, CompletionWidget);
//			CompletionWindowManager.HideWindow ();
//		}

		protected override void Initialize ()
		{
			base.Initialize ();
			CompletionWindowManager.WindowClosed += HandleWindowClosed;
			CompletionWidget = DocumentContext.GetContent <ICompletionWidget> () ?? CompletionWidget;
			if (CompletionWidget != null)
				CompletionWidget.CompletionContextChanged += OnCompletionContextChanged;
			Editor.CaretPositionChanged += HandlePositionChanged;
//			document.Editor.Paste += HandlePaste;
//			if (document.Editor.Parent != null)
//				document.Editor.Parent.TextArea.FocusOutEvent += HandleFocusOutEvent;
		}

		internal void InternalInitialize ()
		{
			Initialize ();
		}

		internal protected virtual void HandlePositionChanged (object sender, EventArgs e)
		{
			if (!IsActiveExtension ())
				return;
			CompletionWindowManager.UpdateCursorPosition ();
		}

		void HandleWindowClosed (object sender, EventArgs e)
		{
			CurrentCompletionContext = null;
		}

		bool disposed = false;
		public override void Dispose ()
		{
			if (!disposed)
            {
                CompletionWindowManager.HideWindow();
                ParameterInformationWindowManager.HideWindow(this, CompletionWidget);

                disposed = true;
                //				if (document.Editor.Parent != null)
                //					document.Editor.Parent.TextArea.FocusOutEvent -= HandleFocusOutEvent;
                //				document.Editor.Paste -= HandlePaste;
				Deinitialize();
            }
            base.Dispose ();
		}

		internal void Deinitialize ()
		{
			Editor.CaretPositionChanged -= HandlePositionChanged;
			CompletionWindowManager.WindowClosed -= HandleWindowClosed;
			if (CompletionWidget != null)
				CompletionWidget.CompletionContextChanged -= OnCompletionContextChanged;
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
