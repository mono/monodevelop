//
// Copy of the ICompletionData interface that was
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

namespace MonoDevelop.XmlEditor
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
		
		void InsertAction(ICompletionWidget widget);
	}
	
	public interface ICompletionDataWithMarkup : ICompletionData
	{
		string DescriptionPango {
			get;
		}
	}
}
