// 
// AspNetExpressionState.cs
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
	
	
	public class AspNetExpressionState : State
	{
		const int NONE = 0;
		const int PERCENT = 1;
		
		
		public override State PushChar (char c, IParseContext context, ref bool reject)
		{
			if (context.CurrentStateLength == 1) {
				Debug.Assert (c != '@' && c!= '-', 
					"AspNetExpressionState should not be passed a directive or comment");
				
				switch (c) {
				
				//DATABINDING EXPRESSION <%#
				case '#':
					context.Nodes.Push (new AspNetDataBindingExpression (context.Position - 2));
					break;
					
				//RESOURCE EXPRESSION <%$
				case '$':
					context.Nodes.Push (new AspNetResourceExpression (context.Position - 2));
					break;
				
				//RENDER EXPRESSION <%=
				case '=':
					context.Nodes.Push (new AspNetRenderExpression (context.Position - 2));
					break;
				
				// RENDER BLOCK
				default:
					context.Nodes.Push (new AspNetRenderBlock (context.Position - 2));
					break;
				}
				return null;
			}
			else if (c == '%') {
				if (context.StateTag != PERCENT)
					context.StateTag = PERCENT;
			}
			else if (c == '>') {
				if (context.StateTag == PERCENT) {
					XNode expr = (XNode) context.Nodes.Pop ();
					expr.End (context.Position);
					if (context.BuildTree) {
						XContainer container = (XContainer) context.Nodes.Peek ();
						container.AddChildNode (expr);
					}
					return Parent;
				} else {
					context.StateTag = NONE;
				}
			}
			
			return null;
		}
	}
}
