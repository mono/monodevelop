// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;

using ICSharpCode.Decompiler;
using Mono.Cecil;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.CSharp.Transforms;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// C# decompiler integration into ILSpy.
	/// Note: if you're interested in using the decompiler without the ILSpy UI,
	/// please directly use the CSharpDecompiler class.
	/// </summary>
	class CSharpLanguage
	{
		public static readonly CSharpLanguage Instance = new CSharpLanguage ();
		string name = "C#";
		int transformCount = int.MaxValue;

#if DEBUG
		internal static IEnumerable<CSharpLanguage> GetDebugLanguages ()
		{
			var decompiler = new CSharpDecompiler (ModuleDefinition.CreateModule ("Dummy", ModuleKind.Dll), new DecompilerSettings ());
			string lastTransformName = "no transforms";
			int transformCount = 0;
			foreach (var transform in decompiler.AstTransforms) {
				yield return new CSharpLanguage {
					transformCount = transformCount,
					name = "C# - " + lastTransformName,
				};
				lastTransformName = "after " + transform.GetType ().Name;
				transformCount++;
			}
			yield return new CSharpLanguage {
				name = "C# - " + lastTransformName,
			};
		}
#endif

		public string Name {
			get { return name; }
		}

		public string FileExtension {
			get { return ".cs"; }
		}

		public string ProjectFileExtension {
			get { return ".csproj"; }
		}


		CSharpDecompiler CreateDecompiler (ModuleDefinition module, DecompilerSettings options)
		{
			CSharpDecompiler decompiler = new CSharpDecompiler (module, options);
			//decompiler.CancellationToken = options.CancellationToken;
			while (decompiler.AstTransforms.Count > transformCount)
				decompiler.AstTransforms.RemoveAt (decompiler.AstTransforms.Count - 1);
			return decompiler;
		}

		void WriteCode (ITextOutput output, DecompilerSettings settings, SyntaxTree syntaxTree, IDecompilerTypeSystem typeSystem)
		{
			syntaxTree.AcceptVisitor (new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
			TokenWriter tokenWriter = new TextTokenWriter (output, settings, typeSystem) { FoldBraces = settings.FoldBraces, /*ExpandMemberDefinitions = settings.ExpandMemberDefinitions */};
			syntaxTree.AcceptVisitor (new CSharpOutputVisitor (tokenWriter, settings.CSharpFormattingOptions));
		}

		public void DecompileMethod (MethodDefinition method, ITextOutput output, DecompilerSettings options)
		{
			AddReferenceWarningMessage (method.Module.Assembly, output);
			WriteCommentLine (output, TypeToString (method.DeclaringType, includeNamespace: true));
			CSharpDecompiler decompiler = CreateDecompiler (method.Module, options);
			if (method.IsConstructor && !method.DeclaringType.IsValueType) {
				List<IMemberDefinition> members = CollectFieldsAndCtors (method.DeclaringType, method.IsStatic);
				decompiler.AstTransforms.Add (new SelectCtorTransform (decompiler.TypeSystem.Resolve (method)));
				WriteCode (output, options, decompiler.Decompile (members), decompiler.TypeSystem);
			} else {
				WriteCode (output, options, decompiler.Decompile (method), decompiler.TypeSystem);
			}
		}

		class SelectCtorTransform : IAstTransform
		{
			readonly IMethod ctor;
			readonly HashSet<ISymbol> removedSymbols = new HashSet<ISymbol> ();

			public SelectCtorTransform (IMethod ctor)
			{
				this.ctor = ctor;
			}

			public void Run (AstNode rootNode, TransformContext context)
			{
				ConstructorDeclaration ctorDecl = null;
				foreach (var node in rootNode.Children) {
					switch (node) {
					case ConstructorDeclaration ctor:
						if (ctor.GetSymbol () == this.ctor) {
							ctorDecl = ctor;
						} else {
							// remove other ctors
							ctor.Remove ();
							removedSymbols.Add (ctor.GetSymbol ());
						}
						break;
					case FieldDeclaration fd:
						// Remove any fields without initializers
						if (fd.Variables.All (v => v.Initializer.IsNull)) {
							fd.Remove ();
							removedSymbols.Add (fd.GetSymbol ());
						}
						break;
					}
				}
				if (ctorDecl?.Initializer.ConstructorInitializerType == ConstructorInitializerType.This) {
					// remove all fields
					foreach (var node in rootNode.Children) {
						switch (node) {
						case FieldDeclaration fd:
							fd.Remove ();
							removedSymbols.Add (fd.GetSymbol ());
							break;
						}
					}
				}
				foreach (var node in rootNode.Children) {
					if (node is Comment && removedSymbols.Contains (node.GetSymbol ()))
						node.Remove ();
				}
			}
		}

		public void DecompileProperty (PropertyDefinition property, ITextOutput output, DecompilerSettings options)
		{
			AddReferenceWarningMessage (property.Module.Assembly, output);
			WriteCommentLine (output, TypeToString (property.DeclaringType, includeNamespace: true));
			CSharpDecompiler decompiler = CreateDecompiler (property.Module, options);
			WriteCode (output, options, decompiler.Decompile (property), decompiler.TypeSystem);
		}

		public void DecompileField (FieldDefinition field, ITextOutput output, DecompilerSettings options)
		{
			AddReferenceWarningMessage (field.Module.Assembly, output);
			WriteCommentLine (output, TypeToString (field.DeclaringType, includeNamespace: true));
			CSharpDecompiler decompiler = CreateDecompiler (field.Module, options);
			if (field.IsLiteral) {
				WriteCode (output, options, decompiler.Decompile (field), decompiler.TypeSystem);
			} else {
				List<IMemberDefinition> members = CollectFieldsAndCtors (field.DeclaringType, field.IsStatic);
				decompiler.AstTransforms.Add (new SelectFieldTransform (decompiler.TypeSystem.Resolve (field)));
				WriteCode (output, options, decompiler.Decompile (members), decompiler.TypeSystem);
			}
		}

		private static List<IMemberDefinition> CollectFieldsAndCtors (TypeDefinition type, bool isStatic)
		{
			var members = new List<IMemberDefinition> ();
			foreach (var field in type.Fields) {
				if (field.IsStatic == isStatic)
					members.Add (field);
			}
			foreach (var ctor in type.Methods) {
				if (ctor.IsConstructor && ctor.IsStatic == isStatic)
					members.Add (ctor);
			}

			return members;
		}

		/// <summary>
		/// Removes all top-level members except for the specified fields.
		/// </summary>
		sealed class SelectFieldTransform : IAstTransform
		{
			readonly IField field;

			public SelectFieldTransform (IField field)
			{
				this.field = field;
			}

			public void Run (AstNode rootNode, TransformContext context)
			{
				foreach (var node in rootNode.Children) {
					switch (node) {
					case EntityDeclaration ed:
						if (node.GetSymbol () != field)
							node.Remove ();
						break;
					case Comment c:
						if (c.GetSymbol () != field)
							node.Remove ();
						break;
					}
				}
			}
		}

		public void DecompileEvent (EventDefinition ev, ITextOutput output, DecompilerSettings options)
		{
			AddReferenceWarningMessage (ev.Module.Assembly, output);
			WriteCommentLine (output, TypeToString (ev.DeclaringType, includeNamespace: true));
			CSharpDecompiler decompiler = CreateDecompiler (ev.Module, options);
			WriteCode (output, options, decompiler.Decompile (ev), decompiler.TypeSystem);
		}

		public void DecompileType (TypeDefinition type, ITextOutput output, DecompilerSettings options)
		{
			AddReferenceWarningMessage (type.Module.Assembly, output);
			WriteCommentLine (output, TypeToString (type, includeNamespace: true));
			CSharpDecompiler decompiler = CreateDecompiler (type.Module, options);
			WriteCode (output, options, decompiler.Decompile (type), decompiler.TypeSystem);
		}

		public static string GetPlatformDisplayName (ModuleDefinition module)
		{
			switch (module.Architecture) {
			case TargetArchitecture.I386:
				if ((module.Attributes & ModuleAttributes.Preferred32Bit) == ModuleAttributes.Preferred32Bit)
					return "AnyCPU (32-bit preferred)";
				else if ((module.Attributes & ModuleAttributes.Required32Bit) == ModuleAttributes.Required32Bit)
					return "x86";
				else
					return "AnyCPU (64-bit preferred)";
			case TargetArchitecture.AMD64:
				return "x64";
			case TargetArchitecture.IA64:
				return "Itanium";
			default:
				return module.Architecture.ToString ();
			}
		}

		public static string GetRuntimeDisplayName (ModuleDefinition module)
		{
			switch (module.Runtime) {
			case TargetRuntime.Net_1_0:
				return ".NET 1.0";
			case TargetRuntime.Net_1_1:
				return ".NET 1.1";
			case TargetRuntime.Net_2_0:
				return ".NET 2.0";
			case TargetRuntime.Net_4_0:
				return ".NET 4.0";
			}
			return null;
		}

		void AddReferenceWarningMessage (AssemblyDefinition assembly, ITextOutput output)
		{
			/*var loadedAssembly = MainWindow.Instance.CurrentAssemblyList.GetAssemblies ().FirstOrDefault (la => la.GetAssemblyDefinitionOrNull () == assembly);
			if (loadedAssembly == null || !loadedAssembly.LoadedAssemblyReferencesInfo.HasErrors)
				return;
			const string line1 = "Warning: Some assembly references could not be loaded. This might lead to incorrect decompilation of some parts,";
			const string line2 = "for ex. property getter/setter access. To get optimal decompilation results, please manually add the references to the list of loaded assemblies.";
			if (output is ISmartTextOutput fancyOutput) {
				fancyOutput.AddUIElement (() => new StackPanel {
					Margin = new Thickness (5),
					Orientation = Orientation.Horizontal,
					Children = {
						new Image {
							Width = 32,
							Height = 32,
							Source = Images.LoadImage(this, "Images/Warning.png")
						},
						new TextBlock {
							Margin = new Thickness(5, 0, 0, 0),
							Text = line1 + Environment.NewLine + line2
						}
					}
				});
				fancyOutput.WriteLine ();
				fancyOutput.AddButton (Images.ViewCode, "Show assembly load log", delegate {
					MainWindow.Instance.SelectNode (MainWindow.Instance.FindTreeNode (assembly).Children.OfType<ReferenceFolderTreeNode> ().First ());
				});
				fancyOutput.WriteLine ();
			} else {
				WriteCommentLine (output, line1);
				WriteCommentLine (output, line2);
			}*/
		}

	
		public string TypeToString (TypeReference type, bool includeNamespace, ICustomAttributeProvider typeAttributes = null)
		{
			ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
			if (includeNamespace)
				options |= ConvertTypeOptions.IncludeNamespace;

			return TypeToString (options, type, typeAttributes);
		}

		string TypeToString (ConvertTypeOptions options, TypeReference type, ICustomAttributeProvider typeAttributes = null)
		{
			AstType astType = CSharpDecompiler.ConvertType (type, typeAttributes, options);

			StringWriter w = new StringWriter ();
			if (type.IsByReference) {
				ParameterDefinition pd = typeAttributes as ParameterDefinition;
				if (pd != null && (!pd.IsIn && pd.IsOut))
					w.Write ("out ");
				else
					w.Write ("ref ");

				if (astType is ComposedType && ((ComposedType)astType).PointerRank > 0)
					((ComposedType)astType).PointerRank--;
			}

			astType.AcceptVisitor (new CSharpOutputVisitor (w, TypeToStringFormattingOptions));
			return w.ToString ();
		}

		static readonly CSharpFormattingOptions TypeToStringFormattingOptions = FormattingOptionsFactory.CreateEmpty ();

		public string FormatPropertyName (PropertyDefinition property, bool? isIndexer)
		{
			if (property == null)
				throw new ArgumentNullException (nameof (property));

			if (!isIndexer.HasValue) {
				isIndexer = property.IsIndexer ();
			}
			if (isIndexer.Value) {
				var buffer = new System.Text.StringBuilder ();
				var accessor = property.GetMethod ?? property.SetMethod;
				if (accessor.HasOverrides) {
					var declaringType = accessor.Overrides [0].DeclaringType;
					buffer.Append (TypeToString (declaringType, includeNamespace: true));
					buffer.Append (@".");
				}
				buffer.Append (@"this[");
				bool addSeparator = false;
				foreach (var p in property.Parameters) {
					if (addSeparator)
						buffer.Append (@", ");
					else
						addSeparator = true;
					buffer.Append (TypeToString (p.ParameterType, includeNamespace: true));
				}
				buffer.Append (@"]");
				return buffer.ToString ();
			} else
				return property.Name;
		}

		public string FormatMethodName (MethodDefinition method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			return (method.IsConstructor) ? FormatTypeName (method.DeclaringType) : method.Name;
		}

		public string FormatTypeName (TypeDefinition type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");

			return TypeToString (ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions, type);
		}

		public void WriteCommentLine (ITextOutput output, string comment)
		{
			output.WriteLine ("// " + comment);
		}
	}
}
