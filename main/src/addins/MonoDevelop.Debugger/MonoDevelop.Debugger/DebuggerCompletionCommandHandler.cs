//
// MacDebuggerCompletionCommandHandler.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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

using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;

namespace MonoDevelop.Debugger
{
	[Name ("Debbugger Completion CommandHandler")]
	[ContentType (DebuggerCompletion.ContentType)]
	[TextViewRole (PredefinedTextViewRoles.Interactive)]
	[Export (typeof (ICommandHandler))]
	[Order (After = PredefinedCompletionNames.CompletionCommandHandler)]
	sealed class DebuggerCompletionCommandHandler :
		ICommandHandler<EscapeKeyCommandArgs>,
		ICommandHandler<ReturnKeyCommandArgs>,
		ICommandHandler<TabKeyCommandArgs>
	{
		public string DisplayName => nameof (DebuggerCompletionCommandHandler);

		#region EscapeKey

		public CommandState GetCommandState (EscapeKeyCommandArgs args)
		{
			return CommandState.Available;
		}

		public bool ExecuteCommand (EscapeKeyCommandArgs args, CommandExecutionContext executionContext)
		{
			var cocoaTextView = (ICocoaTextView) args.TextView;
			var bgView = cocoaTextView.VisualElement.Superview; // the NSView that draws the background color
			var superview = bgView?.Superview;

			if (superview is MacDebuggerObjectNameView nameView)
				nameView.CancelEdit ();
			else
				System.Console.WriteLine ("superview is {0}", superview.GetType ().FullName);

			return true;
		}

		#endregion // EscapeKey

		#region ReturnKey

		public CommandState GetCommandState (ReturnKeyCommandArgs args)
		{
			return CommandState.Available;
		}

		public bool ExecuteCommand (ReturnKeyCommandArgs args, CommandExecutionContext executionContext)
		{
			var cocoaTextView = (ICocoaTextView) args.TextView;

			cocoaTextView.VisualElement.ResignFirstResponder ();

			return true;
		}

		#endregion // ReturnKey

		#region TabKey

		public CommandState GetCommandState (TabKeyCommandArgs args)
		{
			return CommandState.Available;
		}

		public bool ExecuteCommand (TabKeyCommandArgs args, CommandExecutionContext executionContext)
		{
			var cocoaTextView = (ICocoaTextView) args.TextView;

			cocoaTextView.VisualElement.ResignFirstResponder ();

			return true;
		}

		#endregion // TabKey
	}
}