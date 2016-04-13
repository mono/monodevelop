// 
// FormattingOptionsFactory.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp
{
	/// <summary>
	/// The formatting options factory creates pre defined formatting option styles.
	/// </summary>
	static class FormattingOptionsFactory
	{
		readonly static Workspace defaultWs = new TestWorkspace ();

		internal class TestWorkspace : Workspace
		{
			readonly static HostServices services = Microsoft.CodeAnalysis.Host.Mef.MefHostServices.DefaultHost;
			public TestWorkspace(string workspaceKind = "Test") : base(services , workspaceKind)
			{
			}

		}
//		/// <summary>
//		/// Creates empty CSharpFormatting options.
//		/// </summary>
//		public static CSharpFormattingOptions CreateEmpty()
//		{
//			return new CSharpFormattingOptions();
//		}

		/// <summary>
		/// Creates mono indent style CSharpFormatting options.
		/// </summary>
		public static OptionSet CreateMono()
		{
			var options = defaultWs.Options;
			options = options.WithChangedOption(CSharpFormattingOptions.SpaceAfterMethodCallName, true);
			options = options.WithChangedOption(CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, true);

			options = options.WithChangedOption(CSharpFormattingOptions.NewLineForCatch, false);
			options = options.WithChangedOption(CSharpFormattingOptions.NewLineForFinally, false);
			options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, false);
			options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
			options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, false);

			options = options.WithChangedOption(CSharpFormattingOptions.IndentSwitchSection, false);

			options = options.WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true);
			options = options.WithChangedOption(FormattingOptions.TabSize, LanguageNames.CSharp, 4);
			options = options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\n");

			return options;
		}

		/// <summary>
		/// Creates sharp develop indent style CSharpFormatting options.
		/// </summary>
		public static OptionSet CreateSharpDevelop()
		{
			var baseOptions = CreateKRStyle();
			return baseOptions;
		}

		/// <summary>
		/// The K&amp;R style, so named because it was used in Kernighan and Ritchie's book The C Programming Language,
		/// is commonly used in C. It is less common for C++, C#, and others.
		/// </summary>
		public static OptionSet CreateKRStyle()
		{
			var options = defaultWs.Options;
			options = options.WithChangedOption(CSharpFormattingOptions.NewLineForCatch, false);
			options = options.WithChangedOption(CSharpFormattingOptions.NewLineForFinally, false);
			options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, false);
			options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
			options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, false);

			options = options.WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true);
			options = options.WithChangedOption(FormattingOptions.TabSize, LanguageNames.CSharp, 4);
			options = options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\n");

			return options;
		}

		/// <summary>
		/// Creates allman indent style CSharpFormatting options used in Visual Studio.
		/// </summary>
		public static OptionSet CreateAllman()
		{
			var options = defaultWs.Options;
			options = options.WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true);
			options = options.WithChangedOption(FormattingOptions.TabSize, LanguageNames.CSharp, 4);
			options = options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\n");
			return options;
		}

//		/// <summary>
//		/// The Whitesmiths style, also called Wishart style to a lesser extent, is less common today than the previous three. It was originally used in the documentation for the first commercial C compiler, the Whitesmiths Compiler.
//		/// </summary>
//		public static CSharpFormattingOptions CreateWhitesmiths()
//		{
//			var baseOptions = CreateKRStyle();
//				
//			baseOptions.NamespaceBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.ClassBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.InterfaceBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.StructBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.EnumBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.MethodBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.ConstructorBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.DestructorBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.AnonymousMethodBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.PropertyBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.PropertyGetBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.PropertySetBraceStyle = BraceStyle.NextLineShifted;
//	
//			baseOptions.EventBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.EventAddBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.EventRemoveBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.StatementBraceStyle = BraceStyle.NextLineShifted;
//			baseOptions.IndentBlocksInsideExpressions = true;
//			return baseOptions;
//		}
//
//		/// <summary>
//		/// Like the Allman and Whitesmiths styles, GNU style puts braces on a line by themselves, indented by 2 spaces,
//		/// except when opening a function definition, where they are not indented.
//		/// In either case, the contained code is indented by 2 spaces from the braces.
//		/// Popularised by Richard Stallman, the layout may be influenced by his background of writing Lisp code.
//		/// In Lisp the equivalent to a block (a progn) 
//		/// is a first class data entity and giving it its own indent level helps to emphasize that,
//		/// whereas in C a block is just syntax.
//		/// Although not directly related to indentation, GNU coding style also includes a space before the bracketed 
//		/// list of arguments to a function.
//		/// </summary>
//		public static CSharpFormattingOptions CreateGNU()
//		{
//			var baseOptions = CreateAllman();
//			baseOptions.StatementBraceStyle = BraceStyle.NextLineShifted2;
//			return baseOptions;
//		}
	}
}

