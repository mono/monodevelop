// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

namespace ICSharpCode.SharpRefactory.Parser
{
	public class StringReader : IReader
	{
		string data = null;
		int    ptr  = 0;
		
		public StringReader(string data)
		{
			this.data = data;
		}
		
		public char GetNext()
		{
			if (Eos()) {
				return '\0';
			}
			return data[ptr++];
		}
		
		public char Peek()
		{
			if (Eos()) {
				return '\0';
			}
			return data[ptr];
		}
		
		public void UnGet()
		{
			ptr = Math.Max(0, ptr - 1);
		}
		
		public bool Eos()
		{
			return ptr >= data.Length;
		}
	}
}
