// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Printing;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using System.CodeDom.Compiler;

using MonoDevelop.Gui;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Services;

using ICSharpCode.SharpRefactory.PrettyPrinter;
using ICSharpCode.SharpRefactory.Parser;

namespace MonoDevelop.Commands
{
	internal class VBConvertBuffer : CommandHandler
	{
		protected override void Run()
		{
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			
			if (window != null && window.ViewContent is IEditable) {
				
				Parser p = new Parser();
				p.Parse(new Lexer(new ICSharpCode.SharpRefactory.Parser.StringReader(((IEditable)window.ViewContent).Text)));
				
				if (p.Errors.count > 0) {
					Runtime.MessageService.ShowError("Correct source code errors first (only compileable C# source code would convert).");
					return;
				}
				VBNetVisitor vbv = new VBNetVisitor();
				vbv.Visit(p.compilationUnit, null);
				
				Runtime.LoggingService.Info(vbv.SourceText.ToString());
				Runtime.FileService.NewFile ("Generated.VB", "VBNET", vbv.SourceText.ToString());
			}
		}
	}
}
