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
		
		public AspNetExpressionState ()
		{
		}
		
		public override State PushChar (char c, IParseContext context, ref bool reject)
		{
			if (context.CurrentStateLength == 1) {
				switch (c) {
				//RENDER EXPRESSION <%=
				case '=':
					break;
					
				//DATABINDING EXPRESSION <%#
				case '#':
					break;
					
				//RESOURCE EXPRESSION <%$
				case '$': 
					break;
					
				//DIRECTIVE <%@
				case '@': 
					break;
				
				// SERVER COMMENT: <%--
				case '-':
					break;
				
				// RENDER BLOCK
				default:
					break;
				}
			}
			
			reject = true;
			context.LogError ("Unexpected character '" + c + "' in ASP.NET expression.");
			return null;
		}

	}
}
