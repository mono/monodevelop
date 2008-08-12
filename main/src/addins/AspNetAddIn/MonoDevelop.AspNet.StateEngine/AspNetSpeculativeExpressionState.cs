// 
// XmlSpeculativeCommentState.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;

using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.StateEngine
{
	
	
	public class AspNetSpeculativeExpressionState : XmlMalformedTagState
	{
		AspNetServerCommentState CommentState;
		AspNetExpressionState ExpressionState;
		XmlMalformedTagState MalformedTagState;
		
		public AspNetSpeculativeExpressionState () : this (
			new AspNetServerCommentState (),
			new AspNetExpressionState ())
		{
		}
		
		public AspNetSpeculativeExpressionState (
			AspNetServerCommentState commentState,
			AspNetExpressionState expressionState)
		{
			this.CommentState = commentState;
			this.ExpressionState = expressionState;
			Adopt (commentState);
			Adopt (expressionState);
		}
		
		const int INCOMING = 0;
		const int BRACKET = 1;
		const int PERCENT = 2;
		const int PERCENT_DASH = 3;
		const int MALFORMED = 4;
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			switch (context.StateTag) {
			case INCOMING:
				if (c == '<' && context.PreviousState != ExpressionState && context.PreviousState != CommentState) {
					context.StateTag = BRACKET;
					return null;
				} else if (context.PreviousState == ExpressionState || context.PreviousState == CommentState) {
					//expression has successfully been collected
					//we should go back to the AttributeValueState or the TagState, not the AttributeState
					rollback = string.Empty;
					return Parent is XmlAttributeState ? Parent.Parent : Parent;
				}
				break;
				
			case BRACKET:
				if (c == '%') {
					context.StateTag = PERCENT;
					return null;
				} else {
					context.StateTag = MALFORMED;
					context.LogError ("Unexpected tag start");
					rollback = "<";
				}
				break;
				
			case PERCENT:
				if (c == '-') {
					context.StateTag = PERCENT_DASH;
					return null;
				} else if (c == '@') {
					context.StateTag = MALFORMED;
					context.LogError ("Invalid directive location");
					rollback = "<%";
				} else {
					rollback = string.Empty;
					return ExpressionState;
				}
				break;
				
			case PERCENT_DASH:
				if (c == '-') {
					return CommentState;
				} else {
					context.StateTag = MALFORMED;
					context.LogError ("Malformed server comment");
					rollback = "<%-";
				}
				break;
				
			case MALFORMED:
				break;
			}
			
			return base.PushChar (c, context, ref rollback);
		}
	}
}
