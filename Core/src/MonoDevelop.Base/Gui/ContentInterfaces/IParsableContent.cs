// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing.Printing;

namespace MonoDevelop.Ide.Gui.Content
{
	/// <summary>
	/// </summary>
	public interface IParseableContent
	{
		/// <summary>
		/// </summary>
		string ParseableContentName {
			get;
		}
	}
}

