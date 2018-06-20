//
// DomMethodNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.TypeSystem;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Components;
using ICSharpCode.ILSpy;

namespace MonoDevelop.AssemblyBrowser
{
	class MethodDefinitionNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(MethodDefinition); }
		}
		
		public MethodDefinitionNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var method = (MethodDefinition)dataObject;
			if (method.IsConstructor)
				return method.DeclaringType.Name;
			return method.Name;
		}
		
		public static string FormatPrivate (string label)
		{
			return "<span foreground= \"#666666\">" + label + "</span>";	
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var method = (MethodDefinition)dataObject;

			var ambience = new CSharpAmbience ();
			try {
				nodeInfo.Label = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (GetText (method));
			} catch (Exception) {
				nodeInfo.Label = method.Name;
			}

			if (method.IsPrivate || method.IsAssembly)
				nodeInfo.Label = MethodDefinitionNodeBuilder.FormatPrivate (nodeInfo.Label);
			
			nodeInfo.Icon = Context.GetIcon (GetStockIcon (method));
		}

		public static IconId GetStockIcon (MethodDefinition method)
		{
			var isStatic = (method.Attributes & MethodAttributes.Static) != 0;
			var global = isStatic ? "static-" : "";
			return "md-" + GetAccess (method.Attributes) + global + "method";
		}

		static string GetAccess (MethodAttributes attributes)
		{
			switch (attributes & MethodAttributes.MemberAccessMask) {
			case MethodAttributes.Private:
				return "private-";
			case MethodAttributes.Public:
				return "";
			case MethodAttributes.Family:
				return "protected-";
			case MethodAttributes.Assembly:
				return "internal-";
			case MethodAttributes.FamORAssem:
			case MethodAttributes.FamANDAssem:
				return "ProtectedOrInternal-";
			default:
				return "";
			}
		}

		public static string GetText (MethodDefinition method)
		{
			var b = StringBuilderCache.Allocate ();
			try {
				b.Append ('(');
				for (int i = 0; i < method.Parameters.Count; i++) {
					if (i > 0)
						b.Append (", ");
					b.Append (CSharpLanguage.Instance.TypeToString (method.Parameters [i].ParameterType, false, method.Parameters [i]));
				}
				if (method.CallingConvention == MethodCallingConvention.VarArg) {
					if (method.HasParameters)
						b.Append (", ");
					b.Append ("...");
				}
				if (method.IsConstructor) {
					b.Append (')');
				} else {
					b.Append (") : ");
					b.Append (CSharpLanguage.Instance.TypeToString (method.ReturnType, false, method.MethodReturnType));
				}
				return CSharpLanguage.Instance.FormatMethodName (method) + b;
			} finally {
				StringBuilderCache.Free (b);
			}
		}

		#region IAssemblyBrowserNodeBuilder
		internal static void PrintDeclaringType (StringBuilder result, ITreeNavigator navigator)
		{
			var type = (IType)navigator.GetParentDataItem (typeof (IType), false);
			if (type == null)
				return;
			
			result.Append (GettextCatalog.GetString ("<b>Declaring Type:</b>\t{0}", type.FullName));
			result.AppendLine ();
		}
		
		static string GetInstructionOffset (Instruction instruction)
		{
			return String.Format ("IL_{0:X4}", instruction.Offset);
		}
		
		public static AssemblyLoader GetAssemblyLoader (ITreeNavigator navigator)
		{
			var nav = navigator.Clone ();
			while (!(nav.DataItem is AssemblyLoader)) {
				if (!nav.MoveToParent ())
					return null;
			}
				
			return (AssemblyLoader)nav.DataItem;
		}

		public static DecompilerSettings GetDecompilerSettings (TextEditor data, bool publicOnly = false)
		{
			var types = DesktopService.GetMimeTypeInheritanceChain (data.MimeType);
			var codePolicy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy> (types);
			var settings = TypeDefinitionNodeBuilder.CreateDecompilerSettings (publicOnly, codePolicy);
			return settings;
		}


		public static List<ReferenceSegment> Decompile (TextEditor data, AssemblyLoader assemblyLoader, Func<CSharpDecompiler, SyntaxTree> decompile, DecompilerSettings settings = null, DecompileFlags flags = null)
		{
			settings = settings ?? GetDecompilerSettings (data, publicOnly: flags.PublicOnly);
			var csharpDecompiler = assemblyLoader.CSharpDecompiler;
			try
			{
				var syntaxTree = decompile(csharpDecompiler);
				if (!flags.MethodBodies) {
					MethodBodyRemoveVisitor.RemoveMethodBodies (syntaxTree);
				}

				var output = new ColoredCSharpFormatter(data);
				TokenWriter tokenWriter = new TextTokenWriter(output, settings, csharpDecompiler.TypeSystem) { FoldBraces = settings.FoldBraces };
				var formattingPolicy = settings.CSharpFormattingOptions;
				syntaxTree.AcceptVisitor(new CSharpOutputVisitor(tokenWriter, formattingPolicy));
				output.SetDocumentData();
				return output.ReferencedSegments;
			}
			catch (Exception e)
			{
				data.InsertText(data.Length, "/* decompilation failed: \n" + e + " */");
			}
			return null;
		}

		internal static string GetAttributes (IEnumerable<IAttribute> attributes)
		{
			var result = new StringBuilder ();
			//var ambience = new CSharpAmbience ();

			foreach (var attr in attributes) {
				if (result.Length > 0)
					result.AppendLine ();
				// result.Append (ambience.ConvertSymbol (attr));
			}
			if (result.Length > 0)
				result.AppendLine ();
			return result.ToString ();
		}
		
		public List<ReferenceSegment> Decompile (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			if (HandleSourceCodeEntity (navigator, data)) 
				return null;
			var cecilMethod = (MethodDefinition)navigator.DataItem;
			if (cecilMethod == null)
				return null;
			return MethodDefinitionNodeBuilder.Decompile (data, MethodDefinitionNodeBuilder.GetAssemblyLoader (navigator), b => b.Decompile (cecilMethod), flags: flags);
		}
		
		static void AppendLink (StringBuilder sb, string link, string text)
		{
			sb.Append ("<span style=\"text.link\"><u><a ref=\"");
			sb.Append (AssemblyBrowserWidget.FormatText (link.Replace ("<", "").Replace (">", "")));
			sb.Append ("\">");
			sb.Append (AssemblyBrowserWidget.FormatText (text.Replace ("::", ".").Replace ("<", "").Replace (">", "")));
			sb.Append ("</a></u></span>");
		}
		
		public static List<ReferenceSegment> Disassemble (TextEditor data, Action<ReflectionDisassembler> setData)
		{
			var source = new CancellationTokenSource ();
			var output = new ColoredCSharpFormatter (data);
			var disassembler = new ReflectionDisassembler (output, source.Token);
			setData (disassembler);
			output.SetDocumentData ();
			return output.ReferencedSegments;
		}
		
		internal static bool HandleSourceCodeEntity (ITreeNavigator navigator, TextEditor data)
		{
			/*			if (IsFromAssembly (navigator))
							return false;

						var method = (MethodDefinition)navigator.DataItem;
						var source = StringTextSource.ReadFrom (method.Region.FileName);
						data.Text = source.Text;
						data.CaretLocation = new MonoDevelop.Ide.Editor.DocumentLocation (method.Region.BeginLine, method.Region.BeginColumn);
						return true;*/
			return false;
		}
		
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Disassemble (TextEditor data, ITreeNavigator navigator)
		{
			if (HandleSourceCodeEntity (navigator, data)) 
				return null;
			var cecilMethod = (MethodDefinition)navigator.DataItem;
			if (cecilMethod == null)
				return null;
			return Disassemble (data, rd => rd.DisassembleMethod (cecilMethod));
		}

		#endregion
	}
}
