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


using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.StateEngine
{
	public class AspNetAttributeValueState : XmlAttributeValueState
	{
		enum AttState {
			Incoming = 0,
			Bracket,
			Percent,
			PercentDash,
			Expression,
			Comment,
			EndPercent,
			EndDash,
			EndDashDash,
			EndDashDashPercent,
		}

		protected new static readonly int TagMask = 15 << XmlAttributeValueState.TagShift;
		protected new static readonly int TagShift = 4 + XmlAttributeValueState.TagShift;

		void SetTag (IParseContext context, AttState value)
		{
			context.StateTag = (context.StateTag & ~TagMask) | ((int)value << XmlAttributeValueState.TagShift);
		}

		public override State PushChar (char c, IParseContext context, ref string rollback)
		{
			var maskedTag = (AttState) ((context.StateTag & TagMask) >> XmlAttributeValueState.TagShift);
			switch (maskedTag) {
			case AttState.Incoming:
				if (c == '<') {
					SetTag (context, AttState.Bracket);
					return null;
				}
				return base.PushChar (c, context, ref rollback);
			case AttState.Bracket:
				if (c == '%') {
					SetTag (context, AttState.Percent);
					return null;
				}
				rollback = "<";
				return Parent;
			case AttState.Percent:
				if (c == '-') {
					SetTag (context, AttState.PercentDash);
					return null;
				}
				if (c == '@') {
					context.LogError ("Invalid directive location");
					rollback = "<%";
					return Parent;
				}
				AspNetExpressionState.AddExpressionNode (c, context);
				SetTag (context, AttState.Expression);
				return null;
			case AttState.PercentDash:
				if (c == '-') {
					context.Nodes.Push (new AspNetServerComment (context.LocationMinus (4)));
					SetTag (context, AttState.Comment);
					return null;
				}
				context.LogError ("Malformed server comment");
				rollback = "<%-";
				return Parent;
			case AttState.Expression:
				if (c == '%')
					SetTag (context, AttState.EndPercent);
				return null;
			case AttState.EndPercent:
				if (c == '>') {
					//TODO: attach nodes
					var n = context.Nodes.Pop ();
					n.End (context.Location);
					SetTag (context, AttState.Incoming);
					//ensure attribute get closed if value is unquoted
					var baseState = (context.StateTag & XmlAttributeValueState.TagMask);
					if (baseState == FREE || baseState == UNQUOTED) {
						var att = (XAttribute)context.Nodes.Peek ();
						att.Value = "";
						return Parent;
					}
					return null;
				}
				SetTag (context, AttState.Expression);
				return null;
			case AttState.Comment:
				if (c == '-')
					SetTag (context, AttState.EndDash);
				return null;
			case AttState.EndDash:
				if (c == '-')
					SetTag (context, AttState.EndDashDash);
				else
					SetTag (context, AttState.Comment);
				return null;
			case AttState.EndDashDash:
				if (c == '%')
					SetTag (context, AttState.EndDashDashPercent);
				else if (c != '-')
					SetTag (context, AttState.Comment);
				return null;
			case AttState.EndDashDashPercent:
				if (c == '>') {
					//TODO: attach nodes
					var n = context.Nodes.Pop ();
					n.End (context.Location);
					SetTag (context, AttState.Incoming);
					return null;
				}
				SetTag (context, AttState.Comment);
				return null;
			default:
				return base.PushChar (c, context, ref rollback);
			}
		}
	}
}
