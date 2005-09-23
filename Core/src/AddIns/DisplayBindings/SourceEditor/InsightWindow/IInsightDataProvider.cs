// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Reflection;
using System.Collections;

using MonoDevelop.SourceEditor.Gui;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.SourceEditor.InsightWindow
{
	public interface IInsightDataProvider
	{
		void SetupDataProvider(Project project, string fileName, SourceEditorView textArea);
		
		bool CaretOffsetChanged();
		bool CharTyped();
		
		string GetInsightData(int number);
		
		int InsightDataCount {
			get;
		}
	}
}
