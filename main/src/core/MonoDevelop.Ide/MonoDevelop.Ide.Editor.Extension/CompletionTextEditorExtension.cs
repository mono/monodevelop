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
	public enum SignatureHelpTriggerReason
	{
		InvokeSignatureHelpCommand,
		TypeCharCommand,
		RetriggerCommand
	}

	public struct SignatureHelpTriggerInfo
	{
		public char? TriggerCharacter {
			get;
		}

		public SignatureHelpTriggerReason TriggerReason {
			get;
		}

		public SignatureHelpTriggerInfo (SignatureHelpTriggerReason triggerReason, char? triggerCharacter = null)
		{
			TriggerReason = triggerReason;
			TriggerCharacter = triggerCharacter;
		}

		internal Microsoft.CodeAnalysis.SignatureHelp.SignatureHelpTriggerInfo ToRoslyn()
		{
			return new Microsoft.CodeAnalysis.SignatureHelp.SignatureHelpTriggerInfo ((Microsoft.CodeAnalysis.SignatureHelp.SignatureHelpTriggerReason)TriggerReason, TriggerCharacter);
		}
	}

	public class CompletionTextEditorExtension : TextEditorExtension
	{
		internal protected CodeCompletionContext CurrentCompletionContext {
			get;
			set;
		}

		// (Settings have been moved to IdeApp.Preferences)

		bool autoHideCompletionWindow, autoHideParameterWindow;

		ICompletionWidget completionWidget;
		internal virtual ICompletionWidget CompletionWidget
		{
			get { return completionWidget; }
			set {
				UnsubscribeCompletionContextChanged ();
				completionWidget = value;
				if (completionWidget != null)
					completionWidget.CompletionContextChanged += OnCompletionContextChanged;
			}
		}
		internal void UnsubscribeCompletionContextChanged ()
		{
			if (completionWidget != null)
				completionWidget.CompletionContextChanged -= OnCompletionContextChanged;
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
		bool parameterHingtingCursorPositionChanged = false;

		// When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		// Return true if the key press should be processed by the editor.
		public override bool KeyPress (KeyDescriptor descriptor)
		{
			if (!IsActiveExtension ())
				return base.KeyPress (descriptor);
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
			char deleteOrBackspaceTriggerChar = '\0';
			if (descriptor.SpecialKey == SpecialKey.Delete && Editor.CaretOffset < Editor.Length)
				deleteOrBackspaceTriggerChar = Editor.GetCharAt (Editor.CaretOffset);
			if (descriptor.SpecialKey == SpecialKey.BackSpace && Editor.CaretOffset > 0)
				deleteOrBackspaceTriggerChar = Editor.GetCharAt (Editor.CaretOffset - 1);
			
			res = base.KeyPress (descriptor);
			if (Editor.EditMode == EditMode.TextLink && Editor.TextLinkPurpose == TextLinkPurpose.Rename) {
				return res;
			}
			if (descriptor.KeyChar == (char)16 || descriptor.KeyChar == (char)17)
				return res;

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
			if (!IdeApp.Preferences.EnableAutoCodeCompletion || Editor.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block)
				return res;

			// Handle code completion
			if (descriptor.KeyChar != '\0' && CompletionWidget != null && !CompletionWindowManager.IsVisible) {
				completionTokenSrc.Cancel ();
				CurrentCompletionContext = CompletionWidget.CurrentCodeCompletionContext;
				completionTokenSrc = new CancellationTokenSource ();
				var caretOffset = Editor.CaretOffset;
				var token = completionTokenSrc.Token;
				try {
					var task = HandleCodeCompletionAsync (CurrentCompletionContext, new CompletionTriggerInfo (CompletionTriggerReason.CharTyped, descriptor.KeyChar), token);
					if (task != null) {
						// Show the completion window in two steps. The call to PrepareShowWindow creates the window but
						// it doesn't show it. It is used only to process the keys while the completion data is being retrieved.
						CompletionWindowManager.PrepareShowWindow (this, descriptor.KeyChar, CompletionWidget, CurrentCompletionContext);
						EventHandler windowClosed = delegate (object o, EventArgs a) {
							completionTokenSrc.Cancel ();
						};
						CompletionWindowManager.WindowClosed += windowClosed;
						task.ContinueWith (t => {
							CompletionWindowManager.WindowClosed -= windowClosed;
							if (token.IsCancellationRequested)
								return;
							var result = t.Result;
							if (result != null) {
								int triggerWordLength = result.TriggerWordLength + (Editor.CaretOffset - caretOffset);
								if (triggerWordLength > 0 && (triggerWordLength < Editor.CaretOffset
															  || (triggerWordLength == 1 && Editor.CaretOffset == 1))) {
									CurrentCompletionContext = CompletionWidget.CreateCodeCompletionContext (Editor.CaretOffset - triggerWordLength);
									if (result.TriggerWordStart >= 0)
										CurrentCompletionContext.TriggerOffset = result.TriggerWordStart;
									CurrentCompletionContext.TriggerWordLength = triggerWordLength;
								}
								// Now show the window for real.
								if (!CompletionWindowManager.ShowWindow (result, CurrentCompletionContext))
									CurrentCompletionContext = null;
							} else {
								CompletionWindowManager.HideWindow ();
								CurrentCompletionContext = null;
							}
						}, Runtime.MainTaskScheduler);
					} else {
						CurrentCompletionContext = null;
					}
				} catch (TaskCanceledException) {
				} catch (AggregateException) {
				}
			}

			if ((descriptor.SpecialKey == SpecialKey.Delete || descriptor.SpecialKey == SpecialKey.BackSpace) && CompletionWidget != null && !CompletionWindowManager.IsVisible) {
				if (!char.IsLetterOrDigit (deleteOrBackspaceTriggerChar) && deleteOrBackspaceTriggerChar != '_')
					return res;
				CurrentCompletionContext = CompletionWidget.CurrentCodeCompletionContext;

				int cpos, wlen;
				if (!GetCompletionCommandOffset (out cpos, out wlen)) {
					cpos = Editor.CaretOffset;
					wlen = 0;
				}

				CurrentCompletionContext.TriggerOffset = cpos;
				CurrentCompletionContext.TriggerWordLength = wlen;
				
				completionTokenSrc.Cancel ();
				completionTokenSrc = new CancellationTokenSource ();
				var caretOffset = Editor.CaretOffset;
				var token = completionTokenSrc.Token;
				try {
					var task = HandleCodeCompletionAsync (CurrentCompletionContext, new CompletionTriggerInfo (CompletionTriggerReason.BackspaceOrDeleteCommand, deleteOrBackspaceTriggerChar), token);
					if (task != null) {
						// Show the completion window in two steps. The call to PrepareShowWindow creates the window but
						// it doesn't show it. It is used only to process the keys while the completion data is being retrieved.
						CompletionWindowManager.PrepareShowWindow (this, descriptor.KeyChar, CompletionWidget, CurrentCompletionContext);
						EventHandler windowClosed = delegate (object o, EventArgs a) {
							completionTokenSrc.Cancel ();
						};
						CompletionWindowManager.WindowClosed += windowClosed;

						task.ContinueWith (t => {
							CompletionWindowManager.WindowClosed -= windowClosed;
							if (token.IsCancellationRequested)
								return;
							var result = t.Result;
							if (result != null) {
								int triggerWordLength = result.TriggerWordLength + (Editor.CaretOffset - caretOffset);

								if (triggerWordLength > 0 && (triggerWordLength < Editor.CaretOffset
								                              || (triggerWordLength == 1 && Editor.CaretOffset == 1))) {
									CurrentCompletionContext = CompletionWidget.CreateCodeCompletionContext (Editor.CaretOffset - triggerWordLength);
									if (result.TriggerWordStart >= 0)
										CurrentCompletionContext.TriggerOffset = result.TriggerWordStart;
									CurrentCompletionContext.TriggerWordLength = triggerWordLength;
								}
								// Now show the window for real.
								if (!CompletionWindowManager.ShowWindow (result, CurrentCompletionContext)) {
									CurrentCompletionContext = null;
								} else {
									CompletionWindowManager.Wnd.StartOffset = CurrentCompletionContext.TriggerOffset;
								}
							} else {
								CompletionWindowManager.HideWindow ();
								CurrentCompletionContext = null;
							}
						}, Runtime.MainTaskScheduler);
					} else {
						CurrentCompletionContext = null;
					}
				} catch (TaskCanceledException) {
					CurrentCompletionContext = null;
				} catch (AggregateException) {
					CurrentCompletionContext = null;
				}
			}

			if (CompletionWidget != null && ParameterInformationWindowManager.CurrentMethodGroup == null) {
				CodeCompletionContext ctx = CompletionWidget.CurrentCodeCompletionContext;
				var newparameterHintingSrc = new CancellationTokenSource ();
				var token = newparameterHintingSrc.Token;
				try {
					var task = HandleParameterCompletionAsync (ctx, new SignatureHelpTriggerInfo (SignatureHelpTriggerReason.TypeCharCommand, descriptor.KeyChar), token);
					if (task != null) {
						parameterHintingSrc.Cancel ();
						parameterHintingSrc = newparameterHintingSrc;
						parameterHingtingCursorPositionChanged = false;
						task.ContinueWith (t => {
							if (!token.IsCancellationRequested && t.Result != null) {
								ParameterInformationWindowManager.ShowWindow (this, CompletionWidget, ctx, t.Result);
								if (parameterHingtingCursorPositionChanged)
									ParameterInformationWindowManager.UpdateCursorPosition (this, CompletionWidget);
							}
						}, token, TaskContinuationOptions.None, Runtime.MainTaskScheduler);
					} else {
						//Key was typed that was filtered out, no heavy processing will be performed(task==null)
						//but we still want to update ParameterInfo window to avoid displaying it outside method call
						parameterHingtingCursorPositionChanged = true;
					}
				} catch (TaskCanceledException) {
				} catch (AggregateException) {
				}
			}
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

		public virtual Task<int> GetCurrentParameterIndex (int startOffset, CancellationToken token = default(CancellationToken))
		{
			return Task.FromResult (-1);
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
			info.Bypass = !IsActiveExtension () || (!CanRunCompletionCommand () && !CompletionWindowManager.IsVisible);
		}

		[CommandUpdateHandler(TextEditorCommands.ShowParameterCompletionWindow)]
		internal void OnUpdateParameterCompletionCommand (CommandInfo info)
		{
			info.Bypass = !IsActiveExtension () || !CanRunParameterCompletionCommand ();
		}

		[CommandHandler (TextEditorCommands.ShowCompletionWindow)]
		public virtual async void RunCompletionCommand ()
		{
			await TriggerCompletion(CompletionTriggerReason.CompletionCommand);
		}

		public virtual async Task TriggerCompletion(CompletionTriggerReason reason)
		{
			if (Editor.SelectionMode == SelectionMode.Block)
				return;

			if (CompletionWindowManager.IsVisible) {
				CompletionWindowManager.Wnd.ToggleCategoryMode ();
				return;
			}
			Editor.EnsureCaretIsNotVirtual ();
			ICompletionDataList completionList = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CaretOffset;
				wlen = 0;
			}
			CurrentCompletionContext = CompletionWidget.CreateCodeCompletionContext (cpos);
			CurrentCompletionContext.TriggerWordLength = wlen;
			completionList = await HandleCodeCompletionAsync (CurrentCompletionContext, new CompletionTriggerInfo (reason));
			if (completionList != null && completionList.TriggerWordStart >= 0) {
				CurrentCompletionContext.TriggerOffset = completionList.TriggerWordStart;
				CurrentCompletionContext.TriggerWordLength = completionList.TriggerWordLength;
			}
			if (completionList == null || !CompletionWindowManager.ShowWindow (this, (char)0, completionList, CompletionWidget, CurrentCompletionContext)) {
				CurrentCompletionContext = null;
			}
		}

		[CommandHandler (TextEditorCommands.ShowCodeTemplateWindow)]
		[CommandHandler (TextEditorCommands.ShowCodeSurroundingsWindow)]
		public virtual void RunShowCodeTemplatesWindow ()
		{
			Editor.EnsureCaretIsNotVirtual ();
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
			var wnd = CompletionListWindow.CreateAsDialog ();
			wnd.Extension = this;
			wnd.ShowListWindow ((char)0, completionList, CompletionWidget, ctx);
		}

		[CommandUpdateHandler(TextEditorCommands.ShowCodeTemplateWindow)]
		internal void OnUpdateShowCodeTemplatesWindow (CommandInfo info)
		{
			info.Enabled = !Editor.IsSomethingSelected;
			info.Bypass = !IsActiveExtension () || !info.Enabled;
			if (info.Enabled) {
				int cpos, wlen;
				if (!GetCompletionCommandOffset (out cpos, out wlen)) {
					cpos = Editor.CaretOffset;
					wlen = 0;
				}
				try {
					var ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
					ctx.TriggerWordLength = wlen;

					info.Bypass = ShowCodeTemplatesCommand (ctx) == null;
				} catch (Exception e) {
					LoggingService.LogError ("Error while update show code templates window", e);
					info.Bypass = true;
				}
			}
		}

		[CommandUpdateHandler (TextEditorCommands.ShowCodeSurroundingsWindow)]
		internal void OnUpdateSelectionSurroundWith (CommandInfo info)
		{
			info.Enabled = Editor.IsSomethingSelected;
			info.Bypass = !IsActiveExtension () || !info.Enabled;
			if (info.Enabled) {
				int cpos, wlen;
				if (!GetCompletionCommandOffset (out cpos, out wlen)) {
					cpos = Editor.CaretOffset;
					wlen = 0;
				}
				try {
					var ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
					ctx.TriggerWordLength = wlen;

					info.Bypass = ShowCodeSurroundingsCommand (ctx) == null;
				} catch (Exception e) {
					LoggingService.LogError ("Error while update show code surroundings window", e);
					info.Bypass = true;
				}
			}
		}


		[CommandHandler(TextEditorCommands.ShowParameterCompletionWindow)]
		public virtual async void RunParameterCompletionCommand ()
		{
			if (Editor.SelectionMode == SelectionMode.Block || CompletionWidget == null)
				return;
			ParameterHintingResult cp = null;
			int cpos = Editor.CaretOffset;
			CodeCompletionContext ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
			cp = await ParameterCompletionCommand (ctx);
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

		public virtual Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext, CompletionTriggerInfo triggerInfo, CancellationToken token = default (CancellationToken))
		{
			return Task.FromResult (emptyList);
		}

		[Obsolete("Use HandleCodeCompletionAsync")]
		public Task<ICompletionDataList> CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			return HandleCodeCompletionAsync (completionContext, CompletionTriggerInfo.CodeCompletionCommand);
		}

		[Obsolete("Use HandleParameterCompletionAsync (CodeCompletionContext completionContext, SignatureHelpTriggerInfo triggerInfo, CancellationToken token)")]
		public virtual Task<ParameterHintingResult> HandleParameterCompletionAsync (CodeCompletionContext completionContext, char completionChar, CancellationToken token = default (CancellationToken))
		{
			return Task.FromResult (ParameterHintingResult.Empty);
		}

		public virtual Task<ParameterHintingResult> HandleParameterCompletionAsync (CodeCompletionContext completionContext, SignatureHelpTriggerInfo triggerInfo, CancellationToken token = default (CancellationToken))
		{
			return HandleParameterCompletionAsync (completionContext, triggerInfo.TriggerCharacter.HasValue ? triggerInfo.TriggerCharacter.Value : '\0', token);
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
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesAsync (Editor).WaitAndGetResult (CancellationToken.None)) {
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
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesAsync (Editor).WaitAndGetResult (CancellationToken.None)) {
				if (template.CodeTemplateType != CodeTemplateType.SurroundsWith) {
					list.Add (new CodeTemplateCompletionData (this, template));
				}
			}
			return list;
		}
		
		public virtual async Task<ParameterHintingResult> ParameterCompletionCommand (CodeCompletionContext completionContext)
		{
			// This default implementation of ParameterCompletionCommand calls HandleParameterCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			
			int pos = completionContext.TriggerOffset;
			if (pos <= 0)
				return null;
			
			parameterHintingSrc.Cancel ();
			parameterHintingSrc = new CancellationTokenSource ();

			try {
				return await HandleParameterCompletionAsync (completionContext, new SignatureHelpTriggerInfo (SignatureHelpTriggerReason.InvokeSignatureHelpCommand), parameterHintingSrc.Token);
			} catch (TaskCanceledException) {
			} catch (AggregateException) {
			}
			return null;
		}

		public virtual async Task<int> GuessBestMethodOverload (ParameterHintingResult provider, int currentOverload, System.Threading.CancellationToken token)
		{
			if (provider.SelectedItemIndex.HasValue)
				return provider.SelectedItemIndex.Value;
			var currentHintingData = provider [currentOverload];
			int cparam = await GetCurrentParameterIndex (provider.ParameterListStart, token).ConfigureAwait (false);
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
		void HandleFocusOutEvent (object sender, EventArgs args)
		{
			ParameterInformationWindowManager.HideWindow (this, CompletionWidget);
			CompletionWindowManager.HideWindow ();
		}

		protected override void Initialize ()
		{
			base.Initialize ();
			CompletionWindowManager.WindowClosed += HandleWindowClosed;
			CompletionWidget = CompletionWidget ?? DocumentContext.GetContent<ICompletionWidget> ();           
			Editor.CaretPositionChanged += HandlePositionChanged;
//			document.Editor.Paste += HandlePaste;
			Editor.FocusLost += HandleFocusOutEvent;
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
				completionTokenSrc.Cancel ();
				parameterHintingSrc.Cancel ();

				if (CurrentCompletionContext != null) {
					CompletionWindowManager.HideWindow ();
					ParameterInformationWindowManager.HideWindow (this, CompletionWidget);
				}
                disposed = true;
                Editor.FocusLost -= HandleFocusOutEvent;
                //				document.Editor.Paste -= HandlePaste;
				Deinitialize();
            }
            base.Dispose ();
		}

		internal void Deinitialize ()
		{
			Editor.CaretPositionChanged -= HandlePositionChanged;
			CompletionWindowManager.WindowClosed -= HandleWindowClosed;
			CompletionWidget = null;
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
