// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 4482 $</version>
// </file>

using System;
using ICSharpCode.OldNRefactory.Parser;
using ICSharpCode.OldNRefactory.Ast;

namespace ICSharpCode.OldNRefactory.PrettyPrinter
{
	/// <summary>
	/// Description of IOutputASTVisitor.
	/// </summary>
	public interface IOutputAstVisitor : IAstVisitor
	{
		event Action<INode> BeforeNodeVisit;
		event Action<INode> AfterNodeVisit;
		
		string Text {
			get;
		}
		
		Errors Errors {
			get;
		}
		
		AbstractPrettyPrintOptions Options {
			get;
		}
		IOutputFormatter OutputFormatter {
			get;
		}
	}
	public interface IOutputFormatter
	{
		int IndentationLevel {
			get;
			set;
		}
		string Text {
			get;
		}
		bool IsInMemberBody {
			get;
			set;
		}
		bool LastCharacterIsNewLine {
			get;
		}
		void NewLine();
		void Indent();
		void PrintComment(Comment comment, bool forceWriteInPreviousBlock);
		void PrintPreprocessingDirective(PreprocessingDirective directive, bool forceWriteInPreviousBlock);
		void PrintBlankLine(bool forceWriteInPreviousBlock);
	}
}
