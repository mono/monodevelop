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
//			if (Eos()) {
//				throw new ParserException("warning : FileReader.GetNext : Read char over eos.", 0, 0);
//			}
			return file[ptr++];
		}
		
		public char Peek()
		{
//			if (Eos()) {
//				throw new ParserException("warning : FileReader.Peek : Read char over eos.", 0, 0);
//			}
			return file[ptr];
		}
		
		public void UnGet()
		{
			--ptr;
//			if (ptr < 0) 
//				throw new Exception("error : FileReader.UnGet : ungetted first char");
		}
		
		public bool Eos()
		{
			return ptr >= file.Length;
		}
	}
}
