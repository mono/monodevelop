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
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Debugger
{
	public class WatchPad : ObjectValuePad, IMementoCapable, ICustomXmlSerializer
	{
		// Note: This can be removed once we make the switch to UseNewTreeView
		static readonly Gtk.TargetEntry[] DropTargets = {
			new Gtk.TargetEntry ("text/plain;charset=utf-8", Gtk.TargetFlags.App, 0)
		};
		readonly List<string> expressions = new List<string> ();

		public WatchPad () : base (true)
		{
			if (UseNewTreeView) {
				controller.ExpressionAdded += OnExpressionAdded;
				controller.ExpressionChanged += OnExpressionChanged;
				controller.ExpressionRemoved += OnExpressionRemoved;
			} else {
				tree.EnableModelDragDest (DropTargets, Gdk.DragAction.Copy);
				tree.DragDataReceived += HandleDragDataReceived;
			}
		}

		// Note: This can be removed once we make the switch to UseNewTreeView
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

		public void AddWatch (string expression)
		{
			LoggingService.LogInfo ("Adding expression '{0}'", expression);

			if (UseNewTreeView) {
				controller.AddExpression (expression);
			} else {
				tree.AddExpression (expression);
			}
		}

		void RestoreExpressions ()
		{
			controller.ExpressionAdded -= OnExpressionAdded;

			try {
				// remove the expressions because we're going to rebuild them
				controller.ClearAll ();

				// re-add the expressions which will reevaluate the expressions and repopulate the treeview
				controller.AddExpressions (expressions);
			} finally {
				controller.ExpressionAdded += OnExpressionAdded;
			}
		}

		public override void OnUpdateFrame ()
		{
			base.OnUpdateFrame ();

			if (UseNewTreeView)
				RestoreExpressions ();
		}

		public override void OnUpdateValues ()
		{
			base.OnUpdateValues ();

			if (UseNewTreeView) {
				RestoreExpressions ();
			} else {
				tree.Update ();
			}
		}

		void OnExpressionAdded (object sender, ExpressionEventArgs e)
		{
			LoggingService.LogInfo ("Expression added: {0}", e.Expression);
			expressions.Add (e.Expression);
		}

		void OnExpressionChanged (object sender, ExpressionChangedEventArgs e)
		{
			LoggingService.LogInfo ("Expression changed: '{0}' -> '{1}'", e.OldExpression, e.NewExpression);
			int index = expressions.IndexOf (e.OldExpression);

			if (index != -1) {
				expressions[index] = e.NewExpression;
			} else {
				LoggingService.LogWarning ("Failed to find old expression: {0}", e.OldExpression);
				expressions.Add (e.NewExpression);
			}
		}

		void OnExpressionRemoved (object sender, ExpressionEventArgs e)
		{
			LoggingService.LogInfo ("Expression removed: {0}", e.Expression);
			if (!expressions.Remove (e.Expression))
				LoggingService.LogWarning ("Failed to remove expression: {0}", e.Expression);
		}

		public override void Dispose ()
		{
			if (UseNewTreeView) {
				controller.ExpressionAdded -= OnExpressionAdded;
				controller.ExpressionChanged -= OnExpressionChanged;
				controller.ExpressionRemoved -= OnExpressionRemoved;
			} else {
				tree.DragDataReceived -= HandleDragDataReceived;
			}

			base.Dispose ();
		}

		#region IMementoCapable implementation 

		public ICustomXmlSerializer Memento {
			get {
				return this;
			}
			set {
				if (UseNewTreeView) {
					if (controller != null)
						RestoreExpressions ();
				} else {
					if (tree != null) {
						tree.ClearExpressions ();
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
					foreach (var expression in expressions)
						writer.WriteElementString ("Value", expression);
					writer.WriteEndElement ();
				}
			} else {
				if (tree != null) {
					writer.WriteStartElement ("Values");
					foreach (var name in tree.Expressions)
						writer.WriteElementString ("Value", name);
					writer.WriteEndElement ();
				}
			}
		}
		
		ICustomXmlSerializer ICustomXmlSerializer.ReadFrom (XmlReader reader)
		{
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
