//
// Function.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2009 Levi Bard
//
// This source code is licenced under The MIT License:
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

namespace MonoDevelop.ValaBinding.Parser
{
	/// <summary>
	/// Representation of a Vala function
	/// </summary>
	public class Function: CodeNode
	{
		public string ReturnType{ get; protected set; }
		public KeyValuePair<string,string>[] Parameters{ get; set; }
		
		public override string Description {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.AppendFormat ("{0} {1} (", ReturnType, Name);
				foreach (KeyValuePair<string,string> param in Parameters) {
					sb.AppendFormat ("{0} {1},", param.Value, param.Key);
				}
				if (',' == sb[sb.Length-1]){ sb = sb.Remove (sb.Length-1, 1); }
				sb.Append(")");
				
				return sb.ToString ();
			}
		}
		
		public Function (string type, string name, string parentname, string file, int first_line, int last_line, AccessModifier access, string returnType, KeyValuePair<string,string>[] parameters): 
			base (type, name, parentname, file, first_line, last_line, access)
		{
			ReturnType = returnType;
			Parameters = parameters;
		}
		
		public override CodeNode Clone ()
		{
			Function clone = new Function (NodeType, Name, string.Empty, File, FirstLine, LastLine, Access, ReturnType, null);
			clone.FullName = FullName;
			KeyValuePair<string,string>[] parameters = new KeyValuePair<string, string>[Parameters.Length];
			Parameters.CopyTo (parameters, 0);
			clone.Parameters = parameters;
			return clone;
		}
	}
}
