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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.TextEditor
{
	[Name (nameof (WpfTipManagerCommandHandler))]
	[ContentType ("text")]
	[TextViewRole (PredefinedTextViewRoles.Interactive)]
	[Export (typeof (ICommandHandler))]
	[Order (Before = PredefinedCompletionNames.CompletionCommandHandler)]
	sealed class WpfTipManagerCommandHandler :
		ICommandHandler<EscapeKeyCommandArgs>
		//FIXME: this is currently internal
		//IDynamicCommandHandler<EscapeKeyCommandArgs>
	{
		#pragma warning disable 649 //field not assigned

		[Import]
		IObscuringTipManager obscuringTipManager;

		#pragma warning restore 649

		WpfObscuringTipManager cocoaTipManager;
		WpfObscuringTipManager TipManager {
			get {
				if (cocoaTipManager == null) {
					cocoaTipManager = (WpfObscuringTipManager)obscuringTipManager;
				}
				return cocoaTipManager;
			}
		}

		string INamed.DisplayName => nameof (WpfTipManagerCommandHandler);

		CommandState ICommandHandler<EscapeKeyCommandArgs>.GetCommandState (EscapeKeyCommandArgs args)
			=> TipManager.HasStack (args.TextView) ? CommandState.Available : CommandState.Unspecified;

		//bool IDynamicCommandHandler<EscapeKeyCommandArgs>.CanExecuteCommand (EscapeKeyCommandArgs args)
		//	=> TipManager.HasStack (args.TextView);

		bool ICommandHandler<EscapeKeyCommandArgs>.ExecuteCommand (EscapeKeyCommandArgs args, CommandExecutionContext executionContext)
			=> TipManager.DismissTopOfStack (args.TextView);
	}
}