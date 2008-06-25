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

using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Xml.StateEngine
{
	public class Parser<T> : IDocumentStateEngine
		where T : State, new ()
	{
		State currentState;
		int position;
		Indent currentLineExpectedIndent, previousLineExpectedIndent;

		public Parser ()
		{
			position = 0;
			currentState = new T ();
		}
		
		private Parser (Parser<T> old)
		{
			currentState = old.CurrentState.StackCopy ();
			position = old.position;
		}
		
		public State CurrentState {
			get { return currentState; }
		}
		
		public int Position {
			get { return position; }
		}
		
		public Indent CurrentLineExpectedIndent {
			get { return currentLineExpectedIndent; }
		}
		
		public Indent PreviousLineExpectedIndent {
			get { return previousLineExpectedIndent; }
		}
		
		public void Reset ()
		{
			position = 0;
			currentState = new T ();
		}

		public void Push (char c)
		{
			if (c == '\n')
				previousLineExpectedIndent = currentLineExpectedIndent;
			
			position++;
			
			State newState = null;
			
			int loopLimit = 200;
			do {
				System.Diagnostics.Debug.Assert (currentState != null);
				
				bool reject;
				newState = currentState.PushChar (c, position, out reject);
				
				if (newState == currentState) {
					newState = null;
				} else if (newState != null) {
					currentState = newState;
					System.Console.WriteLine(ToString ());
				}
				
				if (!reject)
					break;
				
				loopLimit--;
				if (loopLimit <= 0)
					throw new InvalidOperationException ("Too many state changes for char '" + c +"'. Current state is " + currentState.ToString () + ".");
				
			} while (newState != null);
		}
		
		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder ();
			builder.AppendFormat ("[Parser Location={0} Stack=", position);
			State s = currentState;
			while (s != null) {
				builder.Append ("\n\t");
				builder.Append (s.ToString ());
				s = s.Parent;
			}
			builder.Append ("\n]");
			return builder.ToString ();
		}
		
		object ICloneable.Clone ()
		{
			return new Parser<T> (this);
		}
	}
}
