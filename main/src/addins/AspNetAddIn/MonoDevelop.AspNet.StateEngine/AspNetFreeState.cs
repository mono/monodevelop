// 
// AspNetFreeState.cs
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

using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.StateEngine
{
	
	public class AspNetFreeState : XmlFreeState
	{
		protected const int BRACKET_PERCENT = MAXCONST + 1;
		
		public AspNetFreeState () : base (new XmlTagState (), new XmlClosingTagState ())
		{
			this.ExpressionState = new AspNetExpressionState ();
			this.DirectiveState = new AspNetDirectiveState ();
			this.ServerCommentState = new AspNetServerCommentState ();
			
			Adopt (this.ExpressionState);
			Adopt (this.DirectiveState);
			Adopt (this.ServerCommentState);
		}
		
		public AspNetFreeState (
			XmlTagState tagState,
			XmlClosingTagState closingTagState,
			XmlCommentState commentState,
			XmlCDataState cDataState,
			XmlDocTypeState docTypeState,
		        XmlProcessingInstructionState processingInstructionState,
			AspNetExpressionState expressionState,
			AspNetDirectiveState directiveState,
			AspNetServerCommentState serverCommentState
			)
			: base (tagState, closingTagState, commentState, cDataState, docTypeState, processingInstructionState)
		{
			this.ExpressionState = expressionState;
			this.DirectiveState = directiveState;
			this.ServerCommentState = serverCommentState;
			
			Adopt (this.ExpressionState);
			Adopt (this.DirectiveState);
			Adopt (this.ServerCommentState);
		}
		
		protected AspNetExpressionState ExpressionState { get; private set; } 
		protected AspNetDirectiveState DirectiveState { get; private set; }
		protected AspNetServerCommentState ServerCommentState { get; private set; }
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			if (c == '%' && context.StateTag == BRACKET) {
				context.StateTag = BRACKET_PERCENT;
				return null;
			}
			else if (context.StateTag == BRACKET_PERCENT) {
				switch (c) {
				
				case '@': //DIRECTIVE <%@
					return DirectiveState;
				
				case '-': // SERVER COMMENT: <%--
					return ServerCommentState;
					break;
				
				case '=': //RENDER EXPRESSION <%=
				case '#': //DATABINDING EXPRESSION <%#
				case '$': //RESOURCE EXPRESSION <%$
				default:  // RENDER BLOCK
					return ExpressionState;
				}
			}
			
			return base.PushChar (c, context, ref rollback);
			
		}
	}
}
