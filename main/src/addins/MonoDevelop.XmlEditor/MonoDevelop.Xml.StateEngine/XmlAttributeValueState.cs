// 
// XmlAttributeValueState.cs
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

namespace MonoDevelop.Xml.StateEngine
{
	public class XmlAttributeValueState : State
	{
		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			System.Diagnostics.Debug.Assert (((XAttribute) context.Nodes.Peek ()).Value == null);

			if (c == '<') {
				//the parent state should report the error
				rollback = string.Empty;
				return Parent;
			}

			if (context.CurrentStateLength == 1) {
				if (c == '\'' || c == '"') {
					context.StateTag = c;
					return null;
				}
				context.StateTag = '\0';
			} else if (context.StateTag == '\0') {
				return BuildUnquotedValue (c, context, ref rollback);
			}

			if (c == context.StateTag) {
				//ending the value
				var att = (XAttribute) context.Nodes.Peek ();
				att.Value = context.KeywordBuilder.ToString ();
				return Parent;
			}

			context.KeywordBuilder.Append (c);
			return null;
		}

		State BuildUnquotedValue (char c, IParseContext context, ref string rollback)
		{
			if (c == '>' && context.KeywordBuilder.Length > 0) {
				string fullName = ((XAttribute)context.Nodes.Peek ()).Name.FullName;
				context.LogError ("The value of attribute '" + fullName + "' ended unexpectedly.");
				rollback = string.Empty;
				return Parent;
			}
			
			if (char.IsLetterOrDigit(c) || c == '_' || c == '.') {
				context.KeywordBuilder.Append (c);
				return null;
			}

			var att = (XAttribute)context.Nodes.Peek ();
			att.Value = context.KeywordBuilder.ToString ();
			rollback = string.Empty;
			return Parent;
		}
	}
}
