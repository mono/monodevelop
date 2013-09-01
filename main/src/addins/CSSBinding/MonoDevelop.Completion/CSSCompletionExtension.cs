//
// CSSCompletionExtension.cs
//
// Author:
//       Diyoda Sajjana <>
//
// Copyright (c) 2013 Diyoda Sajjana
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
//using System;
//using MonoDevelop.Ide.Gui.Content;
//using MonoDevelop.Ide.CodeCompletion;
//using MonoDevelop.Core;
//using Mono.TextEditor;
//
//namespace MonoDevelop.Completion
//{
//	public class CSSCompletionExtension : CompletionTextEditorExtension
//	{
//		public CSSCompletionExtension ()
//		{
//		}

//		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
//		{
//			if (!EnableCodeCompletion)
//				return null;
//			if (!EnableAutoCodeCompletion && char.IsLetter (completionChar))
//				return null;
//
//			//	var timer = Counters.ResolveTime.BeginTiming ();
//			try {
//				if (char.IsLetterOrDigit (completionChar) || completionChar == '_') {
//					if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit (document.Editor.GetCharAt (completionContext.TriggerOffset - 2)))
//						return null;
//					triggerWordLength = 1;
//				}
//				return InternalHandleCodeCompletion (completionContext, completionChar, false, ref triggerWordLength);
//			} catch (Exception e) {
//				LoggingService.LogError ("Unexpected code completion exception." + Environment.NewLine + 
//				                         "FileName: " + Document.FileName + Environment.NewLine + 
//				                         "Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine + 
//				                         "Line text: " + Document.Editor.GetLineText (completionContext.TriggerLine), 
//				                         e);
//				return null;
//			} finally {
////							if (timer != null)
////								timer.Dispose ();
//			}
//		}
//	}

//	class CSSCompletionDataList : CompletionDataList
//	{
//		public CSharpResolver Resolver {
//			get;
//			set;
//		}
//	}
//}

