// 
// CSharpModifierToken.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom;
namespace MonoDevelop.CSharp.Dom
{
	
	public class CSharpModifierToken : CSharpTokenNode
	{
		MonoDevelop.Projects.Dom.Modifiers modifier;
		
		public MonoDevelop.Projects.Dom.Modifiers Modifier {
			get { return modifier; }
			set {
				modifier = value;
				if (!lengthTable.TryGetValue (modifier, out tokenLength))
					throw new InvalidOperationException ("Modifier " + modifier + " is invalid.");
			}
		}
		
		static Dictionary<MonoDevelop.Projects.Dom.Modifiers, int> lengthTable = new Dictionary<MonoDevelop.Projects.Dom.Modifiers, int> ();
		static CSharpModifierToken ()
		{
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.New] = "new".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Public] = "new".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Protected] = "protected".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Private] = "private".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Internal] = "internal".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Abstract] = "abstract".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Virtual] = "virtual".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Sealed] = "sealed".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Static] = "static".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Override] = "override".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Readonly] = "readonly".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Const] = "const".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Partial] = "partial".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Extern] = "extern".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Volatile] = "volatile".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Unsafe] = "unsafe".Length;
			lengthTable[MonoDevelop.Projects.Dom.Modifiers.Overloads] = "override".Length; 
		}
		
		public CSharpModifierToken (DomLocation location, MonoDevelop.Projects.Dom.Modifiers modifier) : base (location, 0)
		{
			this.Modifier = modifier;
		}
	}
}