// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.Document;
using MonoDevelop.SourceEditor.FormattingStrategy;
using MonoDevelop.Core;
using MonoDevelop.Core;

namespace NemerleBinding.FormattingStrategy {
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class NemerleFormattingStrategy : DefaultFormattingStrategy {
		// Simply uses the default formatting scheme for now
	}
}
