// 
// StatefulCompletionTextEditorExtension.cs
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

//#define DEBUG_DOCUMENT_STATE_ENGINE_TRACKER

using System;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Gui.Content
{
	
	public class DocumentStateTracker<T> : IDisposable where T : IDocumentStateEngine
	{
		T currentEngine;
		Stack<T> cachedEngines = new Stack<T> ();
		
		TextEditor editor;
		
		public DocumentStateTracker (T engine, TextEditor editor)
		{
			this.currentEngine = engine;
			this.editor = editor;
			editor.TextChanged += textChanged;
		}
		
		public void Dispose ()
		{
			editor.TextChanged -= textChanged;
		}
		
		public T Engine {
			get { return currentEngine; }
		}
		
		void textChanged (object sender, TextChangeEventArgs args)
		{
			foreach (var change in args.TextChanges) {
				if (change.Offset < currentEngine.Position)
					ResetEngineToPosition (change.Offset);
			}
		}
		
		public void ResetEngineToPosition (int position)
		{
			bool gotCachedEngine = false;
			while (cachedEngines.Count > 0) {
				T topEngine = cachedEngines.Peek ();
				if (topEngine.Position <= position) {
					currentEngine = (T) topEngine.Clone ();
					gotCachedEngine = true;
					ConsoleWrite ("Recovered state engine #{0}", cachedEngines.Count);
					break;
				} else {
					cachedEngines.Pop ();
				}
			}
			if (!gotCachedEngine) {
				ConsoleWrite ("Did not recover a state engine", cachedEngines.Count);
				currentEngine.Reset ();
			}
		}
		public void UpdateEngine ()
		{
			UpdateEngine (editor.CaretOffset);
		}
		
		//Makes sure that the smart indent engine's cursor has caught up with the 
		//text editor's cursor.
		//The engine can take some time to parse the file, and we need it to be snappy
		//so we keep a stack of old engines (they're fairly lightweight) that we can clone
		//in order to quickly catch up.
		public void UpdateEngine (int position)
		{
			//bigger buffer means fewer saved stacks needed
			const int BUFFER_SIZE = 2000;
			
			
			ConsoleWrite ("moving backwards if currentEngine.Position {0} > position {1}", currentEngine.Position, position);
			
			if (currentEngine.Position == position) {
				//positions match, nothing to be done
				return;
			} else if (currentEngine.Position > position) {
				//moving backwards, so reset from previous saved location
				ResetEngineToPosition (position);
			}
			
			// get the engine caught up
			int nextSave = (cachedEngines.Count == 0)? BUFFER_SIZE : cachedEngines.Peek ().Position + BUFFER_SIZE;
			if (currentEngine.Position + 1 == position) {
				char ch = editor.GetCharAt (currentEngine.Position);
				currentEngine.Push (ch);
				ConsoleWrite ("pushing character '{0}'", ch);
				if (currentEngine.Position == nextSave)
					cachedEngines.Push ((T) currentEngine.Clone ());
			} else {
				//bulk copy characters in case buffer is unmanaged 
				//(faster if we reduce managed/unmanaged transitions)
				while (currentEngine.Position < position) {
					int endCut = currentEngine.Position + BUFFER_SIZE;
					if (endCut > position)
						endCut = position;
					string buffer = editor.GetTextBetween (currentEngine.Position, endCut);
					ConsoleWrite ("getting buffer between {0} and {1}" /* '{2}'"*/, currentEngine.Position, endCut - 1, buffer);
					foreach (char ch in buffer) {
						currentEngine.Push (ch);
						//ConsoleWrite ("pushing character '{0}'", ch);
						if (currentEngine.Position == nextSave) {
							cachedEngines.Push ((T) currentEngine.Clone ());
							nextSave += BUFFER_SIZE;
						}
					}
				}
			}
			ConsoleWrite ("Now state engine is at {0}, doc is at {1}", currentEngine.Position, position);
		}
		
		[System.Diagnostics.Conditional("DEBUG_DOCUMENT_STATE_ENGINE_TRACKER")]
		void ConsoleWrite (string message, params object[] args)
		{
			System.Console.Write ("DocumentStateTracker: ");
			try {
				System.Console.WriteLine (message, args);
			} catch (Exception e) {
				System.Console.WriteLine("\nError: Exception while formatting '{0}' for an array with {1} elements", message, args == null ? 0 : args.Length);
				System.Console.WriteLine(e);
			}
		}
	}
	
	public interface IDocumentStateEngine : ICloneable
	{
		int Position { get; }
		void Push (char c);
		void Reset ();
	}
}
