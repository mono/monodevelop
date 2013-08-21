// 
// HtmlClosingTagState.cs
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
using System.Collections.Generic;
using System.Diagnostics;

using MonoDevelop.Xml.StateEngine;
using MonoDevelop.Html;

namespace MonoDevelop.AspNet.StateEngine
{
	
	public class HtmlClosingTagState : XmlClosingTagState
	{
		bool warnAutoClose;
		
		public HtmlClosingTagState (bool warnAutoClose)
			: this (warnAutoClose, new XmlNameState ())
		{
		}
		
		public HtmlClosingTagState (bool warnAutoClose, XmlNameState nameState)
			: base (nameState)
		{
			this.warnAutoClose = warnAutoClose;
		}

		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			//NOTE: This is (mostly) duplicated in HtmlTagState
			//handle inline tags implicitly closed by block-level elements
			if (context.CurrentStateLength == 1 && context.PreviousState is XmlNameState)
			{
				XClosingTag ct = (XClosingTag) context.Nodes.Peek ();
				if (!ct.Name.HasPrefix && ct.Name.IsValid) {
					//Note: the node stack will always be at least 1 deep due to the XDocument
					var parent = context.Nodes.Peek (1) as XElement;
					//if it's not a matching closing tag
					if (parent != null && !string.Equals (ct.Name.Name, parent.Name.Name, StringComparison.OrdinalIgnoreCase)) {
						//attempt to implicitly close the parents
						while (parent != null && parent.ValidAndNoPrefix () && parent.IsImplicitlyClosedBy (ct)) {
							context.Nodes.Pop ();
							context.Nodes.Pop ();
							if (warnAutoClose) {
								context.LogWarning (string.Format ("Tag '{0}' implicitly closed by closing tag '{1}'.",
									parent.Name.Name, ct.Name.Name), parent.Region);
							}
							//parent.Region.End = element.Region.Start;
							//parent.Region.EndColumn = Math.Max (parent.Region.EndColumn - 1, 1);
							parent.Close (parent);
							context.Nodes.Push (ct);

							parent = context.Nodes.Peek (1) as XElement;
						}
					}
				} 
			}
			
			return base.PushChar (c, context, ref rollback);
		}
	}
}
