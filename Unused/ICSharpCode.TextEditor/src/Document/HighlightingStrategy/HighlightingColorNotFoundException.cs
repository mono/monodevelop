// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;

namespace MonoDevelop.TextEditor.Document
{
	public class HighlightingColorNotFoundException : Exception
	{
		public HighlightingColorNotFoundException(string name) : base(name)
		{
		}
	}
}
