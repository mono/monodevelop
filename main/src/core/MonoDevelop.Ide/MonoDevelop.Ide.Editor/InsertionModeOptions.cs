//
// InsertionModeOptions.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// This class contains information the editor needs to initiate the insertion mode.
	/// </summary>
	public sealed class InsertionModeOptions
	{
		/// <summary>
		/// A user visible string describing this operation.
		/// </summary>
		public string Operation {
			get;
			private set;
		}

		/// <summary>
		/// The list of insertion points that are used for the insertion mode. The caret is only able to move between
		/// the insertion points.
		/// </summary>
		public IList<InsertionPoint> InsertionPoints {
			get;
			private set;
		}

		/// <summary>
		/// That's the action that is started after the exit mode ended.
		/// </summary>
		public Action<InsertionCursorEventArgs> ModeExitedAction {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the first selected insertion point. The default value is 0.
		/// </summary>
		public int FirstSelectedInsertionPoint {
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.Ide.Editor.InsertionModeOptions"/> class.
		/// </summary>
		/// <param name="operation">A user visible string describing this operation.</param>
		/// <param name="insertionPoints">The list of insertion points that are used for the insertion mode.</param>
		/// <param name="modeExitedAction">The action that is started after the exit mode ended.</param>
		public InsertionModeOptions (string operation, IList<InsertionPoint> insertionPoints, Action<InsertionCursorEventArgs> modeExitedAction)
		{
			if (operation == null)
				throw new ArgumentNullException ("operation");
			if (insertionPoints == null)
				throw new ArgumentNullException ("insertionPoints");
			if (modeExitedAction == null)
				throw new ArgumentNullException ("modeExitedAction");
			Operation = operation;
			InsertionPoints = insertionPoints;
			ModeExitedAction = modeExitedAction;
		}
	}
}