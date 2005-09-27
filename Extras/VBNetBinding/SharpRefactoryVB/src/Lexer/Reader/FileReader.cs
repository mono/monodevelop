// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	public class FileReader : IReader
	{
		string file = null;
		int    ptr  = 0;
		
		public FileReader(string filename)
		{
			StreamReader sreader = File.OpenText(filename);
			file = sreader.ReadToEnd();
			sreader.Close();
		}
		
		public char GetNext()
		{
			if (Eos()) {
				return '\0';
//				throw new ParserException("warning : FileReader.GetNext : Read char over eos.", 0, 0);
			}
			return file[ptr++];
		}
		
		public char Peek()
		{
			if (Eos()) {
				return '\0';
//				throw new ParserException("warning : FileReader.Peek : Read char over eos.", 0, 0);
			}
			return file[ptr];
		}
		
		public void UnGet()
		{
			ptr = Math.Max(0, ptr -1);
		}
		
		public bool Eos()
		{
			return ptr >= file.Length;
		}
	}
}
