//
// Copy of the ICompletionDataProvider interface that was
// a part of MonoDevelop 0.12
//
// The code completion infrastructure was changed
// ready for MonoDevelop 1.0 and a quick fix to
// get the XML Editor working again is to use the old
// completion code.
//
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using Gdk;

namespace MonoDevelop.XmlEditor
{
	public interface ICompletionDataProvider
	{
		ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped);
	}
	
	public interface IMutableCompletionDataProvider: ICompletionDataProvider
	{
		bool IsChanging { get; }
		event EventHandler CompletionDataChanging;
		event EventHandler CompletionDataChanged;
	}
}
