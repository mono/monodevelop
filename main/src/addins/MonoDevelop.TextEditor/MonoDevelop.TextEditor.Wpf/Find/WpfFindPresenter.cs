//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Find;
using Microsoft.VisualStudio.Text.Find.Implementation;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace MonoDevelop.TextEditor.Wpf.Find
{
	class WpfFindPresenter : IFindPresenter
	{
		readonly WpfFindPresenterFactory factory;
		readonly ITextView textView;
		readonly FindUI findUI;
		readonly IEditorCommandHandlerService commandHandlerService;
		FindController controller;

		static Action Noop { get; } = () => { };
		FindBroker Broker => factory.FindBroker;

		public WpfFindPresenter (WpfFindPresenterFactory factory, ITextView textView)
		{
			this.factory = factory;
			this.textView = textView;
			this.findUI = new FindUI ();
			this.commandHandlerService = factory.EditorCommandHandlerServiceFactory.GetService (textView);

			SubscribeToUIEvents ();
		}

		/// <summary>
		/// Controller is lazy to avoid a MEF loop
		/// </summary>
		FindController Controller {
			get {
				if (controller == null) {
					controller = Broker.GetFindController (textView);
					SubscribeToControllerEvents ();
				}

				return controller;
			}
		}

		public bool IsVisible => findUI.IsVisible;

		public bool IsFocused => findUI.IsKeyboardFocusWithin;

		public void ShowFind ()
		{
			findUI.ShowAdornment ((IWpfTextView)textView);
			findUI.IsInReplaceMode = false;
			Controller.UpdateSearchTextFromCurrentWord ();
		}

		public void ShowReplace ()
		{
			findUI.ShowAdornment ((IWpfTextView)textView);
			findUI.IsInReplaceMode = true;
			Controller.UpdateSearchTextFromCurrentWord ();
		}

		public void Hide ()
		{
			findUI.HideAdornment ();
			Controller.ClearTags ();
		}

		void SubscribeToUIEvents ()
		{
			findUI.CloseRequested += Hide;
			findUI.SearchTextChanged += OnSearchTextChangedInUI;
			findUI.ReplaceTextChanged += OnReplaceTextChangedInUI;
			findUI.FindOptionsChanged += OnFindOptionsChangedInUI;
			findUI.FindPrevious += OnFindPreviousClicked;
			findUI.FindNext += OnFindNextClicked;
			findUI.Replace += OnReplaceClicked;
			findUI.ReplaceAll += OnReplaceAllClicked;
		}

		void SubscribeToControllerEvents ()
		{
			controller.ResultsAvailable += OnResultsAvailable;
			controller.FindOptionsChanged += OnFindOptionsChanged;
			controller.SearchTextChanged += OnSearchTextChanged;
		}

		void OnSearchTextChanged ()
		{
			if (findUI.SearchControl.Text != controller.SearchText) {
				findUI.SearchControl.Text = controller.SearchText;
				findUI.SearchControl.SelectAll ();
			}
		}

		void OnResultsAvailable ((int index, int count) args)
		{
			string summaryText = null;
			if (args.count == 0) {
				summaryText = "No results";
			} else {
				summaryText = $"{args.index + 1} of {args.count}";
			}

			findUI.Dispatcher.InvokeAsync (() => {
				findUI.ResultIndexAndCount.Text = summaryText;
			});
		}

		void OnFindOptionsChanged ()
		{
			findUI.FindOptions = Broker.FindOptions;
		}

		void OnFindPreviousClicked ()
		{
			commandHandlerService.Execute ((v, b) => new FindPreviousCommandArgs (v, b), Noop);
		}

		void OnFindNextClicked ()
		{
			commandHandlerService.Execute ((v, b) => new FindNextCommandArgs (v, b), Noop);
		}

		void OnReplaceClicked ()
		{
			commandHandlerService.Execute ((v, b) => new ReplaceNextCommandArgs (v, b), Noop);
		}

		void OnReplaceAllClicked ()
		{
			commandHandlerService.Execute ((v, b) => new ReplaceAllCommandArgs (v, b), Noop);
		}

		void OnFindOptionsChangedInUI (FindOptions findOptions)
		{
			Broker.FindOptions = findOptions;
		}

		void OnSearchTextChangedInUI (string text)
		{
			Controller.SearchText = text;
		}

		void OnReplaceTextChangedInUI (string text)
		{
			Controller.ReplaceText = text;
		}
	}
}