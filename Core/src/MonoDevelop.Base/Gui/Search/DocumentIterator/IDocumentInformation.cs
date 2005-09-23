// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez Gual" email="lluis@ximian.com"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;

namespace MonoDevelop.Gui.Search
{
	public interface IDocumentInformation
	{
		string FileName {
			get;
		}
		
		ITextIterator GetTextIterator ();
		
		string GetLineTextAtOffset (int offset);
	}
}
