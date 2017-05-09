//
// IIndentEngine.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;

namespace MonoDevelop.Ide.Editor.TextMate
{
	interface IDocumentIndentEngine
	{
		/// <summary>
		///     The indentation string of the current line.
		/// </summary>
		string ThisLineIndent { get; }

		/// <summary>
		///     The indentation string of the next line.
		/// </summary>
		string NextLineIndent { get; }

		/// <summary>
		///     The current line number of the engine.
		/// </summary>
		int LineNumber { get; }

		string CurrentIndent { get; }
		new IDocumentIndentEngine Clone ();

		/// <summary>
		///     Resets the engine.
		/// </summary>
		void Reset();

		void Push (IReadonlyTextDocument sourceText, IDocumentLine line);
	}

	/// <summary>
	///     Represents a decorator of an IStateMachineIndentEngine instance that provides
	///     logic for reseting and updating the engine on text changed events.
	/// </summary>
	/// <remarks>
	///     The decorator is based on periodical caching of the engine's state and
	///     delegating all logic behind indentation to the currently active engine.
	/// </remarks>
	class CacheIndentEngine : IDocumentIndentEngine
	{

		#region Properties

		IDocumentIndentEngine currentEngine;
		Stack<IDocumentIndentEngine> cachedEngines = new Stack<IDocumentIndentEngine> ();
		readonly int cacheRate;

		#endregion

		#region Constructors

		/// <summary>
		///     Creates a new CacheIndentEngine instance.
		/// </summary>
		/// <param name="decoratedEngine">
		///     An instance of <see cref="IDocumentIndentEngine"/> to which the
		///     logic for indentation will be delegated.
		/// </param>
		/// <param name="cacheRate">
		///     The number of lines between caching.
		/// </param>
		public CacheIndentEngine (IDocumentIndentEngine decoratedEngine, int cacheRate = 50)
		{
			this.cacheRate = cacheRate;
			currentEngine = decoratedEngine;
		}

		/// <summary>
		///     Creates a new CacheIndentEngine instance from the given prototype.
		/// </summary>
		/// <param name="prototype">
		///     A CacheIndentEngine instance.
		/// </param>
		public CacheIndentEngine (CacheIndentEngine prototype)
		{
			currentEngine = prototype.currentEngine.Clone ();
		}

		#endregion

		#region IDocumentIndentEngine

		/// <inheritdoc />
		public string ThisLineIndent {
			get { return currentEngine.ThisLineIndent; }
		}

		/// <inheritdoc />
		public string NextLineIndent {
			get { return currentEngine.NextLineIndent; }
		}

		/// <inheritdoc />
		public int LineNumber {
			get { return currentEngine.LineNumber; }
		}

		/// <inheritdoc />
		public string CurrentIndent {
			get { return currentEngine.CurrentIndent; }
		}

		/// <inheritdoc />
		public void Push (IReadonlyTextDocument sourceText, IDocumentLine line)
		{
			currentEngine.Push (sourceText, line);
		}

		/// <inheritdoc />
		public void Reset ()
		{
			currentEngine.Reset ();
			cachedEngines.Clear ();
		}

		/// <summary>
		/// Resets the engine to offset. Clears all cached engines after the given offset.
		/// </summary>
		public void ResetEngineToPosition (int lineNumber)
		{
			// We are already there
			if (currentEngine.LineNumber <= lineNumber)
				return;

			bool gotCachedEngine = false;
			while (cachedEngines.Count > 0) {
				var topEngine = cachedEngines.Peek ();
				if (topEngine.LineNumber <= lineNumber) {
					currentEngine = topEngine.Clone ();
					gotCachedEngine = true;
					break;
				} else {
					cachedEngines.Pop ();
				}
			}
			if (!gotCachedEngine)
				currentEngine.Reset ();
		}

		/// <inheritdoc />
		public void Update (IReadonlyTextDocument sourceText, int lineNumber)
		{
			if (currentEngine.LineNumber == lineNumber) {
				//positions match, nothing to be done
				return;
			} else if (currentEngine.LineNumber > lineNumber) {
				//moving backwards, so reset from previous saved location
				ResetEngineToPosition (lineNumber);
			}

			// get the engine caught up
			int nextSave = (cachedEngines.Count == 0) ? cacheRate : cachedEngines.Peek ().LineNumber + cacheRate;
			if (currentEngine.LineNumber + 1 == lineNumber) {
				var line = sourceText.GetLine (currentEngine.LineNumber + 1);
				currentEngine.Push (sourceText, line);
				if (currentEngine.LineNumber == nextSave)
					cachedEngines.Push (currentEngine.Clone ());
			} else {
				//bulk copy characters in case buffer is unmanaged 
				//(faster if we reduce managed/unmanaged transitions)
				while (currentEngine.LineNumber < lineNumber) {
					var line = sourceText.GetLine (currentEngine.LineNumber + 1);
					if (line == null)
						break;
					int endCut = Math.Min (currentEngine.LineNumber + cacheRate, lineNumber);
					for (int i = currentEngine.LineNumber; line != null && i < endCut; i++) {
						currentEngine.Push (sourceText, line);
						if (currentEngine.LineNumber == nextSave) {
							cachedEngines.Push (currentEngine.Clone ());
							nextSave += cacheRate;
						}
						line = line.NextLine;
					}
				}
			}
		}

		#endregion

		/// <inheritdoc />
		public IDocumentIndentEngine Clone ()
		{
			return new CacheIndentEngine (this);
		}
	}
}
