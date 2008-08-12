// 
// Parser2.cs
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
using System.Collections.Generic;

using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Xml.StateEngine
{
	
	
	public abstract class Parser2 : IDocumentStateEngine, ICloneable
	{
		bool buildTree;
		
		int position;
		int currentState;
		int currentStateLength;
		StringBuilder keywordBuilder;
		
		NodeStack nodes;
		
		List<Error> errors;
		
		public Parser2 () : this (false) {}
		
		public Parser2 (bool buildTree)
		{
			this.buildTree = buildTree;
			Reset ();
		}
		
		public void Reset ()
		{
			position = 0;
			currentState = 0;
			keywordBuilder = new StringBuilder ();
			currentStateLength = 0;
			nodes = new NodeStack ();
			nodes.Push (CreateDocument ());
			errors = buildTree? new List<Error> () : null;
		}
		
		public void Push (char c)
		{
			position++;

			int newState = Dispatch (c);

			if (newState == currentState)
				currentStateLength++;
			else
				currentStateLength = 0;

			currentState = newState;
		}
		
		object ICloneable.Clone ()
		{
			if (buildTree)
				throw new InvalidOperationException ("Parser can only be cloned when in stack mode");
			
			Parser2 copy = (Parser2) CreateNew ();
			Debug.Assert (copy != null && copy.GetType () == this.GetType ());
			copy.CopyFrom (this);
			return copy;
		}
		
		void CopyFrom (Parser2 copyFrom)
		{
			buildTree = false;
			
			position = copyFrom.position;
			currentState = copyFrom.currentState;
			currentStateLength = copyFrom.currentStateLength;
			keywordBuilder = new StringBuilder (copyFrom.keywordBuilder.ToString ());
			
			nodes = ShallowCloneNodes (copyFrom.nodes);
		}
		
		public int Position { get { return position; } }
		public int StateLength { get { return currentStateLength; } }
		public int CurrentState { get { return currentState; } }
		public bool BuildTree { get { return buildTree; } }
		public IEnumerable<Error> Errors { get { return errors; } }
		public NodeStack Nodes { get { return nodes; } }
		
		#region Protected API
		
		protected StringBuilder KeywordBuilder { get { return keywordBuilder; } }
		
		protected abstract int Dispatch (char c);
		
		protected abstract XDocument CreateDocument ();
		
		protected abstract object CreateNew ();
		
		protected void LogError (string message)
		{
			Error err = new Error (Position, message, ErrorSeverity.Error);
			System.Console.WriteLine (err.ToString ());
			if (errors != null)
				errors.Add (err);
			Console.WriteLine (ToString ());
		}
		
		protected void LogWarning (string message)
		{
			Error err = new Error (Position, message, ErrorSeverity.Warning);
			System.Console.WriteLine (err.ToString ());
			if (errors != null)
				errors.Add (err);
			Console.WriteLine (ToString ());
		}
		
		#endregion
		
		#region Move to NodeStack
		
		protected void ConnectAll ()
		{
			XNode prev = null;
			foreach (XObject o in Nodes) {
				XContainer container = o as XContainer;
				if (prev != null && container != null && prev.IsComplete)
					container.AddChildNode (prev);
				if (o.Parent != null)
					break;
				prev = o as XNode;
			}
		}
		
		protected void EndAll (bool pop)
		{
			int popCount = 0;
			foreach (XObject ob in Nodes) {
				if (ob.Position.End < 0 && !(ob is XDocument)) {
					ob.End (this.Position);
					popCount++;
				} else {
					break;
				}
			}
			if (pop)
				for (; popCount > 0; popCount--)
					Nodes.Pop ();
		}
		
		IEnumerable<XObject> CopyXObjects (IEnumerable<XObject> src)
		{
			foreach (XObject o in src)
				yield return o.ShallowCopy ();
		}
		
		NodeStack ShallowCloneNodes (NodeStack source)
		{
			List<XObject> l = new List<XObject> (CopyXObjects (source));
			l.Reverse ();
			return new NodeStack (l);
		}
		
		#endregion
	}
	
	/*
	//NOTE: this is immutable so that collections of it can be cloned safely
	public class Error
	{
		int position;
		string message;
		public ErrorSeverity severity;
		
		public Error (int position, string message, ErrorSeverity severity)
		{
			this.position = position;
			this.message = message;
			this.severity = severity;
		}
		
		public int Position { get { return position; } }
		public string Message { get { return message; } }
		public ErrorSeverity Severity { get { return severity; } }
		
		public override string ToString ()
		{
			return string.Format ("[{0}@{1}: {2}]", severity, position, message);
		}
	}
	
	public enum ErrorSeverity {
		Error,
		Warning
	}
	
	public class NodeStack : Stack<XObject>
	{
		public NodeStack (IEnumerable<XObject> collection) : base (collection) {}
		public NodeStack () {}
		
		public XObject Peek (int down)
		{
			int i = 0;
			foreach (XObject o in this) {
				if (i == down)
					return o;
				i++;
			}
			return null;
		}
	}*/
}
