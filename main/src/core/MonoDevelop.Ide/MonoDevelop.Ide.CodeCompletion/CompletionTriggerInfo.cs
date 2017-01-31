//
// CompletionTriggerInfo.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Ide.CodeCompletion
{
	/// <summary>
	/// Provides information about what triggered completion.
	/// </summary>
	public struct CompletionTriggerInfo
	{
		public static readonly CompletionTriggerInfo CodeCompletionCommand = new CompletionTriggerInfo (CompletionTriggerReason.CompletionCommand);

		/// <summary>
		/// Provides the reason that completion was triggered.
		/// </summary>
		public CompletionTriggerReason CompletionTriggerReason { get; private set; }

		/// <summary>
		/// If the <see cref="CompletionTriggerReason"/> was <see
		/// cref="CompletionTriggerReason.CharTyped"/> then this was the character that was
		/// typed or deleted by backspace.  Otherwise it is null.
		/// </summary>
		public char? TriggerCharacter { get; private set; }

		/// <summary>
		/// Returns true if the reason completion was triggered was to augment an existing list of
		/// completion items.
		/// </summary>
		public bool IsAugment { get; private set; }

		/// <summary>
		///  Returns true if completion was triggered by the debugger.
		/// </summary>
		public bool IsDebugger { get; private set; }

		/// <summary>
		/// Return true if completion is running in the Immediate Window.
		/// </summary>
		public bool IsImmediateWindow { get; private set; }

		public CompletionTriggerInfo (CompletionTriggerReason completionTriggerReason, char? triggerCharacter = null, bool isAugment = false, bool isDebugger = false, bool isImmediateWindow = false) : this()
		{
			this.CompletionTriggerReason = completionTriggerReason;
			this.TriggerCharacter = triggerCharacter;
			this.IsAugment = isAugment;
			this.IsDebugger = isDebugger;
			this.IsImmediateWindow = isImmediateWindow;
		}

		public CompletionTriggerInfo WithIsAugment(bool isAugment)
		{
			return this.IsAugment == isAugment
				? this
				: new CompletionTriggerInfo(this.CompletionTriggerReason, this.TriggerCharacter, isAugment, this.IsDebugger, this.IsImmediateWindow);
		}

		public CompletionTriggerInfo WithIsDebugger(bool isDebugger)
		{
			return this.IsDebugger == isDebugger
				? this
				: new CompletionTriggerInfo(this.CompletionTriggerReason, this.TriggerCharacter, this.IsAugment, isDebugger, this.IsImmediateWindow);
		}

		public CompletionTriggerInfo WithIsImmediateWindow(bool isImmediateWIndow)
		{
			return this.IsImmediateWindow == isImmediateWIndow
				       ? this
					       : new CompletionTriggerInfo(this.CompletionTriggerReason, this.TriggerCharacter, this.IsAugment, this.IsDebugger, isImmediateWIndow);
		}

		public CompletionTriggerInfo WithCompletionTriggerReason(CompletionTriggerReason reason)
		{
			return this.CompletionTriggerReason == reason
				       ? this
					       : new CompletionTriggerInfo(reason, this.TriggerCharacter, this.IsAugment, this.IsDebugger, this.IsImmediateWindow);
		}
	}
}

