// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Projects.Gui.Completion
{
	public interface ICompletionData
	{
		string Image {
			get;
		}
		
		string[] Text {
			get;
		}
		
		string Description {
			get;
		}

		string CompletionString 
		{
			get;
		}
	}
	
	public interface ICompletionDataWithMarkup : ICompletionData
	{
		string DescriptionPango {
			get;
		}
	}
	
	public interface IActionCompletionData : ICompletionData
	{
		void InsertAction (ICompletionWidget widget, ICodeCompletionContext context);
	}
}
