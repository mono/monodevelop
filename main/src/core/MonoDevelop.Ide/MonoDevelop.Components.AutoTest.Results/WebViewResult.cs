//
// WebViewResult.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
#if MAC
using System;
using System.Collections.Generic;
using MonoDevelop.Components.Mac;
using ObjCRuntime;
using WebKit;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class WebViewResult : AppResult
	{
		readonly DomElement node;
		public WebViewResult (DomElement node)
		{
			this.node = node;
		}

		public override AppResult CheckType (Type desiredType)
		{
			// TODO: Implement this, we need to break API for CheckType
			return null;
		}

		static readonly Selector clickSelector = new Selector ("click");
		public override bool Click ()
		{
			bool clicked = node.RespondsToSelector (clickSelector);
			if (clicked)
				node.PerformSelector (clickSelector);
			return clicked;
		}

		public override bool Click (double x, double y)
		{
			return Click ();
		}

		public override bool EnterText (string text)
		{
			if (node is DomHtmlInputElement input) {
				input.Value += text;
				return true;
			}

			return true;
		}

		public override void Flash ()
		{
			throw new NotImplementedException ();
		}

		public override string GetResultType ()
		{
			return node.NodeType.ToString ();
		}

		public override AppResult Marked (string mark)
		{
			return Property ("name", mark) ?? (node.Name.Contains (mark)
				|| node.ClassName.Contains (mark)
				|| node.NodeValue.Contains (mark)
				? this
				: null);
		}

		public override AppResult Model (string column)
		{
			return null;
		}

		public override List<AppResult> NextSiblings ()
		{
			var results = new List<AppResult> ();

			DomElement nextSibling = node.NextElementSibling;
			while (nextSibling != null) {
				results.Add (new WebViewResult (nextSibling) { SourceQuery = SourceQuery });
				nextSibling = nextSibling.NextElementSibling;
			}

			return results;
		}

		public override ObjectProperties Properties ()
		{
			var props = new ObjectProperties ();
			foreach (var attr in node.Attributes) {
				props.Add (attr.Name, new ObjectResult (attr.NodeValue), null);
			}

			return props;
		}

		public override AppResult Property (string propertyName, object value)
		{
			var attributeValue = node.GetAttribute (propertyName);
			if (string.IsNullOrEmpty (attributeValue) || attributeValue != value.ToString ())
				return null;

			return this;
		}

		public override bool Select ()
		{
			node.Focus ();
			return true;
		}

		static readonly Selector activeSelector = new Selector ("activeElement");
		public override AppResult Selected ()
		{
			var doc = node.OwnerDocument;

			if (doc.RespondsToSelector (activeSelector)) {
				var result = doc.PerformSelector (activeSelector);
				if (result == node)
					return this;
			}

			return null;
		}

		public override void SetProperty (string propertyName, object value)
		{
			node.SetAttribute (propertyName, value.ToString ());
		}

		public override AppResult Text (string text, bool exact)
		{
			return CheckText (node, text, exact)
				? this
				: null;

			bool CheckText(DomElement elem, string toFind, bool full)
			{
				if (full)
					return elem.TextContent == toFind;
				return elem.TextContent?.Contains (toFind) ?? false;
			}
		}

		public override bool Toggle (bool active)
		{
			if (node is DomHtmlInputElement input && input.Type == "checkbox") {
				input.Checked ^= true;
				return true;
			}

			return false;
		}

		public override bool TypeKey (char key, string state = "")
		{
			ParseModifier (state, out bool ctrl, out bool alt, out bool shift, out bool meta);

			var view = node.OwnerDocument.DefaultView; //new FakeDomAbstractView ();//node.OwnerDocument.DefaultView;
			var ev = new DomKeyboardEvent ("keypress", true, true, view, "U+" + ((int)key).ToString("X4"), DomKeyLocation.Standard, ctrl, alt, shift, meta);
			return node.DispatchEvent (ev);
		}

		class FakeDomAbstractView : DomAbstractView
		{
			public FakeDomAbstractView() : base (IntPtr.Zero)
			{
			}
		}

		public override bool TypeKey (string keyString, string state = "")
		{
			var ch = ToKeyChar (keyString);
			return TypeKey (ch, state);
		}

		static char ToKeyChar (string keyString)
		{
			switch (keyString) {
			case "ESC":
				return (char)27;

			case "UP":
				return (char)38;

			case "DOWN":
				return (char)40;

			case "LEFT":
				return (char)37;

			case "RIGHT":
				return (char)39;

			case "RETURN":
				return (char)13;

			case "TAB":
				return (char)9;

			case "BKSP":
				return (char)8;

			case "DELETE":
				return (char)46;

			default:
				throw new Exception ("Unknown keystring: " + keyString);
			}
		}

		void ParseModifier (string modifierString, out bool ctrl, out bool alt, out bool shift, out bool meta)
		{
			ctrl = alt = shift = meta = false;
			string [] modifiers = modifierString.Split ('|');

			foreach (var m in modifiers) {
				switch (m) {
				case "Shift":
					shift = true;
					break;

				case "Control":
					ctrl = true;
					break;

				case "Meta":
					meta = true;
					break;

				case "Mod1":
					alt = true;
					break;

				case "Mod2":
				case "Mod3":
				case "Mod4":
				case "Mod5":
				case "Super":
				case "Hyper":
					// ?
					break;

				default:
					break;
				}
			}
		}


		public override string ToString ()
		{
			return string.Format ("WebViewObject: Type: {0} {1}", node.NodeType.ToString());
		}
	}
}
#endif

