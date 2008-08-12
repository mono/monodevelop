// 
// XmlTagNameState.cs
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
using System.Text;
using System.Diagnostics;

namespace MonoDevelop.Xml.StateEngine
{
	
	
	public class XmlNameState : State
	{
		
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			INamedXObject namedObject = context.Nodes.Peek () as INamedXObject;
			if (namedObject == null || namedObject.Name.Prefix != null)
				throw new InvalidOperationException ("Invalid state");
			
			Debug.Assert (context.CurrentStateLength > 1 || char.IsLetter (c) || c == '_', 
				"First character pushed to a XmlTagNameState must be a letter.");
			Debug.Assert (context.CurrentStateLength > 1 || context.KeywordBuilder.Length == 0,
				"Keyword builder must be empty when state begins.");
			
			if (c == ':') {
				if (namedObject.Name.Name != null || context.KeywordBuilder.Length == 0) {
					context.LogError ("Unexpected ':' in name.");
					return Parent;
				}
				namedObject.Name = new XName (context.KeywordBuilder.ToString ());
				context.KeywordBuilder.Length = 0;
				return null;
			}
			
			if (char.IsWhiteSpace (c) || c == '<' || c == '>' || c == '/' || c == '=') {
				rollback = string.Empty;
				if (context.KeywordBuilder.Length == 0) {
					context.LogError ("Zero-length name.");
				} else if (namedObject.Name.Name != null) {
					//add prefix (and move current "name" to prefix)
					namedObject.Name = new XName (namedObject.Name.Name, context.KeywordBuilder.ToString ());
				} else {
					namedObject.Name = new XName (context.KeywordBuilder.ToString ());
				}
				
				//note: parent's MalformedTagState logs an error, so skip this
				//if (c == '<')
				//context.LogError ("Unexpected '<' in name.");
				
				return Parent;
			}
			if (c == ':') {
				if (namedObject.Name.Name != null || context.KeywordBuilder.Length == 0) {
					context.LogError ("Unexpected ':' in name.");
					return Parent;
				}
				namedObject.Name = new XName (context.KeywordBuilder.ToString ());
				context.KeywordBuilder.Length = 0;
				return null;
			}
			
			if (char.IsLetterOrDigit (c) || c == '_') {
				context.KeywordBuilder.Append (c);
				return null;
			}
			
			rollback = string.Empty;
			context.LogError ("Unexpected character '" + c +"'");
			return Parent;
		}
	}
}
