// 
// ViMacro.cs
//  
// Author:
//       Sanjoy Das <sanjoy@playingwithpointers.com>
// 
// Copyright (c) 2010 Sanjoy Das
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

namespace Mono.TextEditor.Vi
{
	/// <summary>
	/// Implements a Vi macro. Only the keys pressed need to be stored. Though this class
	/// is not exactly required, it provides a possible place to extend the code.
	/// </summary>
	public class ViMacro {
	
		/// <summary>
		/// One of these determines a complete set of arguments passed to HandleKeyPress. I am
		/// still not sure about whether each of these fields is indepedent in itself or may 
		/// be eliminated and re-constructed later.
		/// </summary>
		public struct KeySet {
			public Gdk.Key Key { get; set;}
			public uint UnicodeKey {get; set;}
			public Gdk.ModifierType Modifiers {get; set;}
		}
		
		/// <summary>
		/// This <see cref="System.Collections.Queue"/> of KeySets determine the ultimate functionality
		/// of the macro this ViMacro object represents.
		/// </summary>
		public Queue<KeySet> KeysPressed {get; set;}
		public char MacroCharacter {get; set;}
		
		public ViMacro (char macroCharacter) {
			MacroCharacter = MacroCharacter;
		}
	
	}
	
}

