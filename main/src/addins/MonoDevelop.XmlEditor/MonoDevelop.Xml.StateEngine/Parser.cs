// 
// Parser.cs
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
using System.Text;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Xml.StateEngine
{
	public class Parser : IDocumentStateEngine, IParseContext
	{
		RootState rootState;
		State currentState;
		State previousState;
		bool buildTree;
		
		int position;
		int line, col;
		int stateTag;
		StringBuilder keywordBuilder;
		int currentStateLength;
		NodeStack nodes;
		List<Error> errors;
		
		public Parser (RootState rootState, bool buildTree)
		{
			this.rootState = rootState;
			this.currentState = rootState;
			this.previousState = rootState;
			this.buildTree = buildTree;
			Reset ();
		}
		
		Parser (Parser copyFrom)
		{
			buildTree = false;
			
			rootState = copyFrom.rootState;
			currentState = copyFrom.currentState;
			previousState = copyFrom.previousState;
			
			position = copyFrom.position;
			line = copyFrom.line;
			col = copyFrom.col;
			stateTag = copyFrom.stateTag;
			keywordBuilder = new StringBuilder (copyFrom.keywordBuilder.ToString ());
			currentStateLength = copyFrom.currentStateLength;
			
			//clone the node stack
			List<XObject> l = new List<XObject> (CopyXObjects (copyFrom.nodes));
			l.Reverse ();
			nodes = new NodeStack (l);
		}
		
		IEnumerable<XObject> CopyXObjects (IEnumerable<XObject> src)
		{
			foreach (XObject o in src)
				yield return o.ShallowCopy ();
		}
		
		public RootState RootState { get { return rootState; } }
		
		#region IDocumentStateEngine
		
		public int Position { get { return position; } }
		
		public void Reset ()
		{
			currentState = rootState;
			previousState = rootState;
			position = 0;
			stateTag = 0;
			line = 1;
			col = 0;
			keywordBuilder = new StringBuilder ();
			currentStateLength = 0;
			nodes = new NodeStack ();
			nodes.Push (rootState.CreateDocument ());
			
			if (buildTree)
				errors = new List<Error> ();
			else
				errors = null;
		}
		
		public void Parse (System.IO.TextReader reader)
		{
			int i = reader.Read ();
			while (i >= 0) {
				char c = (char) i;
				Push (c);
				i = reader.Read ();
			}
		}

		public void Push (char c)
		{
			try {
				//track line, column
				if (c == '\n') {
					line++;
					col = 0;
				} else {
					col++;
				}
				
				position++;
				for (int loopLimit = 0; loopLimit < 10; loopLimit++) {
					currentStateLength++;
					string rollback = null;
					State nextState = currentState.PushChar (c, this, ref rollback);
					
					// no state change
					if (nextState == currentState || nextState == null)
						return;
					
					// state changed; reset stuff
					previousState = currentState;
					currentState = nextState;
					stateTag = 0;
					currentStateLength = 0;
					if (keywordBuilder.Length < 50)
						keywordBuilder.Length = 0;
					else
						keywordBuilder = new StringBuilder ();
					
					
					// only loop if the same char should be run through the new state
					if (rollback == null)
						return;
					
					//"complex" rollbacks require actually skipping backwards.
					//Note the previous state is invalid for this operation.
					else if (rollback.Length > 0) {
						position -= (rollback.Length + 1);
						foreach (char rollChar in rollback)
							Push (rollChar);
						position++;
					}
				}
				throw new InvalidOperationException ("Too many state changes for char '" + c + "'. Current state is " + currentState.ToString () + ".");
			} catch (Exception ex)  {
				//attach parser state to exceptions
				throw new Exception (ToString (), ex);
			}
		}
		
		object ICloneable.Clone ()
		{
			if (buildTree)
				throw new InvalidOperationException ("Parser can only be cloned when in stack mode");
			return new Parser (this);
		}
		
		#endregion
		
		public Parser GetTreeParser ()
		{
			if (buildTree)
				throw new InvalidOperationException ("Parser can only be cloned when in stack mode");
			Parser p = new Parser (this);
			
			p.buildTree = true;
			
			//reconnect the node tree
			((IParseContext)p).ConnectAll ();
			
			p.errors = new List<Error> ();
			
			return p;
		}
		
		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder ();
			builder.AppendFormat ("[Parser Location={0} CurrentStateLength={1}", position, currentStateLength);
			builder.AppendLine ();
			
			builder.Append (' ', 2);
			builder.AppendLine ("Stack=");
			
			XObject rootOb = null;
			foreach (XObject ob in nodes) {
				rootOb = ob;
				builder.Append (' ', 4);
				builder.Append (ob.ToString ());
				builder.AppendLine ();
			}
			
			builder.Append (' ', 2);
			builder.AppendLine ("States=");
			State s = currentState;
			while (s != null) {
				builder.Append (' ', 4);
				builder.Append (s.ToString ());
				builder.AppendLine ();
				s = s.Parent;
			}
			
			if (buildTree && rootOb != null) {
				builder.Append (' ', 2);
				builder.AppendLine ("Tree=");
				rootOb.BuildTreeString (builder, 3);
			}
			
			builder.AppendLine ("]");
			return builder.ToString ();
		}
		
		#region IParseContext
				
		int IParseContext.StateTag {
			get { return stateTag; }
			set { stateTag = value; }
		}
		
		StringBuilder IParseContext.KeywordBuilder {
			get { return keywordBuilder; }
		}
		
		State IParseContext.PreviousState {
			get { return previousState; }
		}
		
		public int CurrentStateLength { get { return currentStateLength; } }
		public NodeStack Nodes { get { return nodes; } }
		
		public bool BuildTree { get { return buildTree; } }
		
		void IParseContext.LogError (string message)
		{
			InternalLogError (message, ErrorType.Error);
		}
		
		void IParseContext.LogWarning (string message)
		{
			InternalLogError (message, ErrorType.Warning);
		}
		
		void InternalLogError (string message, ErrorType type)
		{
			if (ErrorLogged == null && errors == null)
				return;
			Error err = new Error (type, line, col, message);
			if (errors != null)
				errors.Add (err);
			if (ErrorLogged != null)
				ErrorLogged (err);
		}
		
		void IParseContext.ConnectAll ()
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
		
		void IParseContext.EndAll (bool pop)
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
		
		#endregion
		
		public State CurrentState { get { return currentState; } }
		
		public IList<Error> Errors { get { return errors; } }
		
		public event Action<Error> ErrorLogged;
	}
	
	public interface IParseContext
	{
		int StateTag { get; set; }
		StringBuilder KeywordBuilder { get; }
		int CurrentStateLength { get; }
		int Position { get; }
		State PreviousState { get; }
		NodeStack Nodes { get; }
		bool BuildTree { get; }
		void LogError (string message);
		void LogWarning (string message);
		void EndAll (bool pop);
		void ConnectAll ();
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
		
		public XDocument GetRoot ()
		{
			XObject last = null;
			foreach (XObject o in this)
				last = o;
			return last as XDocument;
		}
	}
}
