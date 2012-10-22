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

using Mono.Cecil;
using Mono.Cecil.Cil;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler;
using System.Threading;
using ICSharpCode.Decompiler.Disassembler;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.IO;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects;

namespace MonoDevelop.AssemblyBrowser
{
	class DomMethodNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IUnresolvedMethod); }
		}
		
		public DomMethodNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var method = (IUnresolvedMethod)dataObject;
			if (method.EntityType == EntityType.Constructor || method.EntityType == EntityType.Destructor)
				return method.DeclaringTypeDefinition.Name;
			return method.Name;
		}
		
		public static string FormatPrivate (string label)
		{
			return "<span foreground= \"#666666\">" + label + "</span>";	
		}
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			var method = (IUnresolvedMethod)dataObject;
			var dt = new DefaultResolvedTypeDefinition (GetContext (treeBuilder), method.DeclaringTypeDefinition);
			var resolved = (DefaultResolvedMethod)Resolve (treeBuilder, method, dt);
			label = Ambience.GetString (resolved, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.CompletionListFomat);

			if (method.IsPrivate || method.IsInternal)
				label = DomMethodNodeBuilder.FormatPrivate (label);
			
			icon = ImageService.GetPixbuf (resolved.GetStockIcon (), Gtk.IconSize.Menu);
		}
		
		#region IAssemblyBrowserNodeBuilder
		internal static void PrintDeclaringType (StringBuilder result, ITreeNavigator navigator)
		{
			var type = (IType)navigator.GetParentDataItem (typeof (IType), false);
			if (type == null)
				return;
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Declaring Type:</b>\t{0}"), type.FullName));
			result.AppendLine ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			var method = (IUnresolvedMethod)navigator.DataItem;
			var resolved = Resolve (navigator, method);
			StringBuilder result = new StringBuilder ();
			result.Append ("<span font_family=\"monospace\">");
			result.Append (Ambience.GetString (resolved, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</span>");
			result.AppendLine ();
			PrintDeclaringType (result, navigator);
			DomTypeNodeBuilder.PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		static string GetInstructionOffset (Instruction instruction)
		{
			return String.Format ("IL_{0:X4}", instruction.Offset);
		}
		
		public static ModuleDefinition GetModule (ITreeNavigator navigator)
		{
			var nav = navigator.Clone ();
			while (!(nav.DataItem is ModuleDefinition) && !(nav.DataItem is Tuple<AssemblyDefinition, IProjectContent>)) {
				if (!nav.MoveToParent ())
					return ModuleDefinition.CreateModule ("empty", ModuleKind.Console);
			}
			if (nav.DataItem is Tuple<AssemblyDefinition, IProjectContent>)
				return ((Tuple<AssemblyDefinition, IProjectContent>)nav.DataItem).Item1.MainModule;
				
			return (ModuleDefinition)nav.DataItem;
		}

		public static List<ReferenceSegment> Decompile (TextEditorData data, ModuleDefinition module, TypeDefinition currentType, Action<AstBuilder> setData)
		{
			var types = DesktopService.GetMimeTypeInheritanceChain (data.Document.MimeType);
			var codePolicy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy> (types);
			DecompilerSettings settings = new DecompilerSettings () {
				AnonymousMethods = true,
				AutomaticEvents  = true,
				AutomaticProperties = true,
				ForEachStatement = true,
				LockStatement = true,
				ShowXmlDocumentation = true,
				CSharpFormattingOptions = codePolicy.CreateOptions ()
			};
			return Decompile (data, module, currentType, setData, settings);
		}


		public static List<ReferenceSegment> Decompile (TextEditorData data, ModuleDefinition module, TypeDefinition currentType, Action<AstBuilder> setData, DecompilerSettings settings)
		{
			try {

				var context = new DecompilerContext (module);
				var source = new CancellationTokenSource ();
				
				context.CancellationToken = source.Token;
				context.CurrentType = currentType;
				
				context.Settings = settings;
				
				AstBuilder astBuilder = new AstBuilder (context);
				
				setData (astBuilder);
				
				astBuilder.RunTransformations (o => false);
				GeneratedCodeSettings.Default.Apply (astBuilder.CompilationUnit);
				var output = new ColoredCSharpFormatter (data.Document);
				astBuilder.GenerateCode (output);
				output.SetDocumentData ();
				return output.ReferencedSegments;
			} catch (Exception e) {
				data.Text = "Decompilation failed: \n" + e;
			}
			return null;
		}
		
		internal static string GetAttributes (Ambience ambience, IEnumerable<IAttribute> attributes)
		{
			StringBuilder result = new StringBuilder ();
			foreach (var attr in attributes) {
				if (result.Length > 0)
					result.AppendLine ();
	//			result.Append (ambience.GetString (attr, OutputFlags.AssemblyBrowserDescription));
			}
			if (result.Length > 0)
				result.AppendLine ();
			return result.ToString ();
		}
		
		public List<ReferenceSegment> Decompile (TextEditorData data, ITreeNavigator navigator, bool publicOnly)
		{
			var method = (IUnresolvedMethod)navigator.DataItem;
			if (HandleSourceCodeEntity (navigator, data)) 
				return null;
			var cecilMethod = CecilLoader.GetCecilObject (method);
			if (cecilMethod == null)
				return null;
			return DomMethodNodeBuilder.Decompile (data, DomMethodNodeBuilder.GetModule (navigator), cecilMethod.DeclaringType, b => b.AddMethod (cecilMethod));
		}
		
		static void AppendLink (StringBuilder sb, string link, string text)
		{
			sb.Append ("<span style=\"text.link\"><u><a ref=\"");
			sb.Append (AssemblyBrowserWidget.FormatText (link.Replace ("<", "").Replace (">", "")));
			sb.Append ("\">");
			sb.Append (AssemblyBrowserWidget.FormatText (text.Replace ("::", ".").Replace ("<", "").Replace (">", "")));
			sb.Append ("</a></u></span>");
		}
		
		public static List<ReferenceSegment> Disassemble (TextEditorData data, Action<ReflectionDisassembler> setData)
		{
			var source = new CancellationTokenSource ();
			var output = new ColoredCSharpFormatter (data.Document);
			var disassembler = new ReflectionDisassembler (output, true, source.Token);
			setData (disassembler);
			output.SetDocumentData ();
			return output.ReferencedSegments;
		}
		
		internal static bool HandleSourceCodeEntity (ITreeNavigator navigator, TextEditorData data)
		{
			if (IsFromAssembly (navigator))
				return false;
			
			var method = (IUnresolvedEntity)navigator.DataItem;
			data.Text = Mono.TextEditor.Utils.TextFileUtility.ReadAllText (method.Region.FileName);
			data.Caret.Location = method.Region.Begin;
			data.CenterToCaret ();
			return true;
		}
		
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
			var method = (IUnresolvedMethod)navigator.DataItem;
			if (HandleSourceCodeEntity (navigator, data)) 
				return null;
			var cecilMethod = CecilLoader.GetCecilObject (method);
			if (cecilMethod == null)
				return null;
			return Disassemble (data, rd => rd.DisassembleMethod (cecilMethod));
		}
		
		string IAssemblyBrowserNodeBuilder.GetDocumentationMarkup (ITreeNavigator navigator)
		{
			var method = (IUnresolvedMethod)navigator.DataItem;
			var resolved = Resolve (navigator, method);
			if (GetMainAssembly (navigator) == null) {
				return Mono.TextEditor.Utils.TextFileUtility.ReadAllText (method.Region.FileName);
			}
			StringBuilder result = new StringBuilder ();
			result.Append ("<big>");
			result.Append (Ambience.GetString (resolved, OutputFlags.AssemblyBrowserDescription | OutputFlags.IncludeConstraints));
			result.Append ("</big>");
			result.AppendLine ();
			
			AmbienceService.DocumentationFormatOptions options = new AmbienceService.DocumentationFormatOptions ();
			options.MaxLineLength = -1;
			options.BigHeadings = true;
			options.Ambience = Ambience;
			result.AppendLine ();
			
			result.Append (AmbienceService.GetDocumentationMarkup (resolved, AmbienceService.GetDocumentation (resolved), options));
			
			return result.ToString ();
		}
		#endregion

	}
}
