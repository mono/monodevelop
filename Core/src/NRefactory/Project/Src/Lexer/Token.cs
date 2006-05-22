// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.Drawing;

namespace ICSharpCode.NRefactory.Parser
{
	public class Token
	{
		public int kind;
		
		public int col;
		public int line;
		
		public object literalValue;
		public string val;
		public Token  next;
		
		public Point EndLocation {
			get {
				return new Point(val == null ? col + 1 : col + val.Length, line);
			}
		}
		public Point Location {
			get {
				return new Point(col, line);
			}
		}
		
		public Token(int kind) : this(kind, 0, 0)
		{
		}
		
		public Token(int kind, int col, int line) : this (kind, col, line, null)
		{
		}
		
		public Token(int kind, int col, int line, string val) : this(kind, col, line, val, null)
		{
		}
		
		public Token(int kind, int col, int line, string val, object literalValue)
		{
			this.kind         = kind;
			this.col          = col;
			this.line         = line;
			this.val          = val;
			this.literalValue = literalValue;
		}
	}
}
