// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	public interface IReader
	{
		char GetNext();
		char Peek();
		
		void UnGet();
		
		bool Eos();
	}
}
