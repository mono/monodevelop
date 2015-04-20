//
// ISelectionSurroundingProvider.cs
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
using MonoDevelop.Components.PropertyGrid.PropertyEditors;

namespace MonoDevelop.Ide.Editor.Extension
{
	/// <summary>
	/// A selection surrounding provider handles a special handling how the text editor behaves when the user
	/// types a key with a selection. The selection can be surrounded instead of beeing replaced.
	/// </summary>
	public abstract class SelectionSurroundingProvider	
	{
		/// <summary>
		/// Gets the selection surroundings for a given unicode key.
		/// </summary>
		/// <returns>
		/// true, if the key is valid for a surrounding action.
		/// </returns>
		/// <param name='unicodeKey'>
		/// The key to handle.
		/// </param>
		/// <param name='start'>
		/// The start of the surrounding
		/// </param>
		/// <param name='end'>
		/// The end of the surrounding
		/// </param>
		public abstract bool GetSelectionSurroundings (uint unicodeKey, out string start, out string end);

		public abstract void HandleSpecialSelectionKey (uint unicodeKey);
	}
}

