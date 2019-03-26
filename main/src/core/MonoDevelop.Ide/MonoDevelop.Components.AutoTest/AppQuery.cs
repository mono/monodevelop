//
// AppQuery.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.Collections.Generic;
using System.Text;
using Gtk;
using MonoDevelop.Components.AutoTest.Operations;
using MonoDevelop.Components.AutoTest.Results;
using System.Linq;
using System.Xml;
using MonoDevelop.Core;
using System.Runtime.Remoting;


namespace MonoDevelop.Components.AutoTest
{
	public class AppQuery : MarshalByRefObject, IDisposable
	{
		List<AppResult> toDispose = new List<AppResult> ();
		List<Operation> operations = new List<Operation> ();

		public AutoTestSessionDebug SessionDebug { get; set; }


		public AppQuery ()
		{
		}

		public AppResult[] Execute ()
		{
			var (rootNode, result) = new AppQueryRunner (operations).Execute ();
			toDispose.Add (rootNode);
			return result;
		}

		public AppQuery Marked (string mark)
		{
			operations.Add (new MarkedOperation (mark));
			return this;
		}

		public AppQuery CheckType (Type desiredType, string name = null)
		{
			operations.Add (new TypeOperation (desiredType, name));
			return this;
		}

		public AppQuery Button ()
		{
			return CheckType (typeof(Gtk.Button), "Button");
		}

		public AppQuery Textfield ()
		{
			return CheckType (typeof(Gtk.Entry), "Textfield");
		}

		public AppQuery CheckButton ()
		{
			return CheckType (typeof(Gtk.CheckButton), "CheckButton");
		}

		public AppQuery RadioButton ()
		{
			return CheckType (typeof(Gtk.RadioButton), "RadioButton");
		}

		public AppQuery TreeView ()
		{
			return CheckType (typeof(Gtk.TreeView), "TreeView");
		}

		public AppQuery Window ()
		{
			return CheckType (typeof(Gtk.Window), "Window");
		}

		public AppQuery TextView ()
		{
			return CheckType (typeof(Gtk.TextView), "TextView");
		}

		public AppQuery Notebook ()
		{
			return CheckType (typeof(Gtk.Notebook), "Notebook");
		}

		public AppQuery Text (string text)
		{
			operations.Add (new TextOperation (text));
			return this;
		}

		public AppQuery Contains (string text)
		{
			operations.Add (new TextOperation (text, false));
			return this;
		}

		public AppQuery Selected ()
		{
			operations.Add (new SelectedOperation ());
			return this;
		}

		public AppQuery Model (string column = null)
		{
			operations.Add (new ModelOperation (column));
			return this;
		}

		public AppQuery Sensitivity (bool sensitivity)
		{
			operations.Add (new PropertyOperation ("Sensitive", sensitivity));
			return this;
		}

		public AppQuery Visibility (bool visibility)
		{
			operations.Add (new PropertyOperation ("Visible", visibility));
			return this;
		}

		public AppQuery Property (string propertyName, object desiredValue)
		{
			operations.Add (new PropertyOperation (propertyName, desiredValue));
			return this;
		}

		public AppQuery Toggled (bool toggled)
		{
			operations.Add (new PropertyOperation ("Active", toggled));
			return this;
		}

		public AppQuery NextSiblings ()
		{
			operations.Add (new NextSiblingsOperation ());
			return this;
		}

		public AppQuery Index (int index)
		{
			operations.Add (new IndexOperation (index));
			return this;
		}

		public AppQuery Children (bool recursive = true)
		{
			operations.Add (new ChildrenOperation (recursive));
			return this;
		}

		public override string ToString ()
		{
			return AppQueryRunner.GetQueryString (operations);
		}

		public void Dispose ()
		{
			RemotingServices.Disconnect (this);

			foreach (var node in toDispose) {
				node.Dispose ();
			}
			toDispose = null;
		}

		public override object InitializeLifetimeService () => null;
	}
}

