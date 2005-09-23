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
using MonoDevelop.Internal.Project;

using Gdk;

namespace MonoDevelop.Gui.Completion
{
	public interface ICompletionDataProvider
	{
		ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped);
	}
}
