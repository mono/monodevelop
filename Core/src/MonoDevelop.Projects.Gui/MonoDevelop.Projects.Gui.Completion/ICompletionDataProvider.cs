// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using Gdk;

namespace MonoDevelop.Projects.Gui.Completion
{
	public interface ICompletionDataProvider: IDisposable
	{
		ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped);
		string DefaultCompletionString { get; }
	}
	
	public interface IMutableCompletionDataProvider: ICompletionDataProvider
	{
		bool IsChanging { get; }
		event EventHandler CompletionDataChanging;
		event EventHandler CompletionDataChanged;
	}
}
