//
// MSBuildWhitespace.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Projects.Utility;
using System.Linq;
using MonoDevelop.Projects.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.MSBuild
{
	class MSBuildWhitespace
	{
		bool isComment;
		MSBuildWhitespace next;
		object content;

		MSBuildWhitespace GetLast ()
		{
			var mws = this;
			while (mws.next != null)
				mws = mws.next;
			return mws;
		}

		void GenerateStrings ()
		{
			var mws = this;
			while (mws != null) {
				if (mws.content is StringBuilder)
					mws.content = mws.content.ToString ();
				mws = mws.next;
			}
		}

		public static object GenerateStrings (object ws)
		{
			if (ws == null)
				return null;
			if (ws is string)
				return ws;
			if (ws is StringBuilder)
				return ((StringBuilder)ws).ToString ();
			var mw = (MSBuildWhitespace)ws;
			mw.GenerateStrings ();
			return mw;
		}

		public static bool IsWhitespace (XmlReader reader)
		{
			return reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.SignificantWhitespace || reader.NodeType == XmlNodeType.Comment;
		}

		public static object Append (object ws, XmlReader reader)
		{
			if (reader.NodeType == XmlNodeType.Comment) {
				var newWs = new MSBuildWhitespace {
					isComment = true,
					content = reader.Value
				};
				if (ws == null)
					return newWs;
				else if (ws is string || ws is StringBuilder) {
					return new MSBuildWhitespace {
						content = ws.ToString (),
						next = newWs
					};
				} else {
					var last = ((MSBuildWhitespace)ws).GetLast ();
					last.next = newWs;
					return ws;
				}
			} else if (ws is MSBuildWhitespace) {
				var last = ((MSBuildWhitespace)ws).GetLast ();
				if (last.isComment) {
					last.next = new MSBuildWhitespace {
						content = reader.Value
					};
				} else if (last.content is StringBuilder)
					((StringBuilder)last.content).Append (reader.Value);
				else {
					var val = reader.Value;
					var sb = new StringBuilder ((string)last.content, ((string)last.content).Length + val.Length);
					sb.Append (val);
					last.content = sb;
				}
				return ws;
			} else {
				if (ws == null)
					return reader.Value;
				if (ws is StringBuilder) {
					((StringBuilder)ws).Append (reader.Value);
					return ws;
				}
				else {
					var val = reader.Value;
					var sb = new StringBuilder ((string)ws, ((string)ws).Length + val.Length);
					sb.Append (val);
					return sb;
				}
			}
		}

		public static object AppendSpace (object ws1, object ws2)
		{
			if (ws1 == null)
				return ws2;
			if (ws2 == null)
				return ws1;
			
			var ob2 = ws2 as MSBuildWhitespace;
			if (ob2 != null && ob2.isComment) {
				if (ws1 is string || ws1 is StringBuilder) {
					return new MSBuildWhitespace {
						content = ws1.ToString (),
						next = ob2
					};
				} else {
					var last = ((MSBuildWhitespace)ws1).GetLast ();
					last.next = ob2;
					return ws1;
				}
			} else if (ws1 is MSBuildWhitespace) {
				var last = ((MSBuildWhitespace)ws1).GetLast ();
				if (last.isComment) {
					var next = ob2 != null ? ob2 : new MSBuildWhitespace {
						content = ws2 // is a string or stringbuilder
					};
					last.next = new MSBuildWhitespace {
						content = next
					};
				} else if (last.content is StringBuilder) {
					var val = ob2 != null ? ob2.content.ToString () : ws2.ToString ();
					((StringBuilder)last.content).Append (val);
				} else {
					var val = ob2 != null ? ob2.content.ToString () : ws2.ToString ();
					var sb = new StringBuilder ((string)last.content, ((string)last.content).Length + val.Length);
					sb.Append (val);
					last.content = sb;
				}
				return ws1;
			} else {
				var val = ob2 != null ? ob2.content.ToString () : ws2.ToString ();
				if (ws1 is StringBuilder) {
					((StringBuilder)ws1).Append (val);
					return ws1;
				}
				else {
					var sb = new StringBuilder ((string)ws1, ((string)ws1).Length + val.Length);
					sb.Append (val);
					return sb;
				}
			}
		}

		public static void Write (object ws, XmlWriter writer)
		{
			if (ws == null)
				return;

			if (ws is string || ws is StringBuilder) {
				writer.WriteWhitespace (ws.ToString ());
				return;
			}

			var mws = (MSBuildWhitespace)ws;

			while (mws != null) {
				if (mws.isComment)
					writer.WriteComment ((string)mws.content);
				else
					writer.WriteWhitespace (mws.content.ToString ());
				mws = mws.next;
			}
		}

		public static object ConsumeUntilNewLine (ref object ws)
		{
			if (ws == null)
				return null;

			var s = ws as string;
			if (s != null) {
				for (int n = s.Length - 1; n >= 0; n--) {
					var c = s [n];
					if (c == '\r' || c == '\n') {
						if (n == s.Length - 1)
							break; // Default case, consume the whole string
						int len = n + 1;
						string res = StringInternPool.AddShared (s, 0, len);
						ws = StringInternPool.AddShared (s, len, s.Length - len);
						return res;
					}
				}
				var result = ws;
				ws = null;
				return result;
			}
			var sb = ws as StringBuilder;
			if (sb != null) {
				for (int n = sb.Length - 1; n >= 0; n--) {
					var c = sb [n];
					if (c == '\r' || c == '\n') {
						if (n == sb.Length - 1)
							break; // Default case, consume the whole string
						var res = sb.ToString (0, n + 1);
						sb.Remove (0, n + 1);
						return res;
					}
				}
				string result = StringInternPool.AddShared (sb);
				ws = null;
				return result;
			}

			// It's a MSBuildWhitespace

			var mw = (MSBuildWhitespace)ws;
			mw.GenerateStrings ();

			var toSplit = mw.FindLastWithNewLine ();
			if (toSplit == null) {
				var result = ws;
				ws = null;
				return result;
			} else {
				var remaining = toSplit.content;
				var result = ConsumeUntilNewLine (ref remaining);

				// Set the remaining value

				if (toSplit.next == null) {
					// New line found in last node. The remaining is just the split string
					ws = remaining;
				} else {
					if (remaining == null)
						return toSplit.next; // Consumed the whole string of this node. The remaining is the next node
					
					// New line found in the middle of the chain. A new node with the remaining has to be created
					ws = new MSBuildWhitespace {
						content = remaining,
						next = toSplit.next
					};
				}

				// Generate the consumed value

				if (toSplit != mw) {
					// New line found in the middle of the chain. Update the node content and split the chain.
					toSplit.content = result;
					toSplit.next = null;
					return mw;
				} else {
					// New line found in first node. The result is just the consumed string. Nothing else to do.
					return result;
				}
			}
		}

		MSBuildWhitespace FindLastWithNewLine ()
		{
			if (next != null) {
				var r = next.FindLastWithNewLine ();
				if (r != null)
					return r;
			}
			if (isComment)
				return null;
			
			var s = (string)content;

			for (int n = 0; n < s.Length; n++) {
				var c = s [n];
				if (c == '\r' || c == '\n')
					return this;
			}
			return null;
		}
	}
}
