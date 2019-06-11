// WatchPad.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Xamarin Inc. (http://www.xamarin.com)
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
//
//

using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public class WatchPad : ObjectValuePad, IMementoCapable, ICustomXmlSerializer
	{
		static readonly Gtk.TargetEntry[] DropTargets = {
			new Gtk.TargetEntry ("text/plain;charset=utf-8", Gtk.TargetFlags.App, 0)
		};
		readonly Dictionary<string, ObjectValue> cachedValues = new Dictionary<string, ObjectValue> ();
		readonly List<string> expressions = new List<string> ();
		
		public WatchPad () : base (true)
		{
			if (UseNewTreeView) {
				controller.AllowAdding = true;
				// TODO: drag & drop
			} else {
				tree.EnableModelDragDest (DropTargets, Gdk.DragAction.Copy);
				tree.DragDataReceived += HandleDragDataReceived;
				tree.AllowAdding = true;
			}
		}

		void HandleDragDataReceived (object o, Gtk.DragDataReceivedArgs args)
		{
			var text = args.SelectionData.Text;

			args.RetVal = true;

			if (string.IsNullOrEmpty (text))
				return;

			foreach (var expr in text.Split (new [] { '\n' })) {
				if (string.IsNullOrWhiteSpace (expr))
					continue;

				AddWatch (expr.Trim ());
			}
		}

		ObjectValue GetExpressionValue (string expression)
		{
			var frame = ((ProxyStackFrame) controller.Frame).StackFrame;
			ObjectValue value;

			if (cachedValues.TryGetValue (expression, out value))
				return value;

			if (frame != null)
				value = frame.GetExpressionValue (expression, true);
			else
				value = ObjectValue.CreateUnknown (expression);

			cachedValues[expression] = value;

			return value;
		}

		ObjectValue[] GetExpressionValues ()
		{
			var frame = ((ProxyStackFrame) controller.Frame).StackFrame;
			var values = new ObjectValue[expressions.Count];
			var unknown = new List<string> ();

			for (int i = 0; i < expressions.Count; i++) {
				ObjectValue value;

				if (cachedValues.TryGetValue (expressions[i], out value))
					values[i] = value;
				else
					unknown.Add (expressions[i]);
			}

			ObjectValue[] qvalues;

			if (frame != null) {
				qvalues = frame.GetExpressionValues (unknown.ToArray (), true);
			} else {
				qvalues = new ObjectValue[unknown.Count];
				for (int i = 0; i < qvalues.Length; i++)
					qvalues[i] = ObjectValue.CreateUnknown (unknown[i]);
			}

			int n = 0;
			for (int i = 0; i < values.Length; i++) {
				if (values[i] == null) {
					values[i] = qvalues[n++];
					cachedValues[expressions[i]] = values[i];
				}
			}

			return values;
		}

		public void AddWatch (string expression)
		{
			if (UseNewTreeView) {
				var value = GetExpressionValue (expression);
				expressions.Add (expression);

				controller.AddValue (new ObjectValueNode (value, string.Empty));
			} else {
				tree.AddExpression (expression);
			}
		}

		public override void OnUpdateValues ()
		{
			base.OnUpdateValues ();

			if (UseNewTreeView) {
				//controller.Update ();
			} else {
				tree.Update ();
			}
		}

		#region IMementoCapable implementation 

		public ICustomXmlSerializer Memento {
			get {
				return this;
			}
			set {
				if (UseNewTreeView) {
					if (controller != null) {
						var values = GetExpressionValues ();

						controller.ClearValues ();
						controller.AddValues (values.Select (v => new ObjectValueNode (v, string.Empty)));
					}
				} else {
					if (tree != null) {
						tree.ClearExpressions ();
						if (expressions != null)
							tree.AddExpressions (expressions);
					}
				}
			}
		}
		
		void ICustomXmlSerializer.WriteTo (XmlWriter writer)
		{
			if (UseNewTreeView) {
				if (controller != null) {
					writer.WriteStartElement ("Values");
					foreach (string expression in expressions)
						writer.WriteElementString ("Value", expression);
					writer.WriteEndElement ();
				}
			} else {
				if (tree != null) {
					writer.WriteStartElement ("Values");
					foreach (string name in tree.Expressions)
						writer.WriteElementString ("Value", name);
					writer.WriteEndElement ();
				}
			}
		}
		
		ICustomXmlSerializer ICustomXmlSerializer.ReadFrom (XmlReader reader)
		{
			cachedValues.Clear ();
			expressions.Clear ();
			
			reader.MoveToContent ();
			if (reader.IsEmptyElement) {
				reader.Read ();
				return null;
			}
			reader.ReadStartElement ();
			reader.MoveToContent ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element) {
					expressions.Add (reader.ReadElementString ());
				} else {
					reader.Skip ();
				}
			}
			reader.ReadEndElement ();
			return null;
		}
		
		#endregion 
	}
}
