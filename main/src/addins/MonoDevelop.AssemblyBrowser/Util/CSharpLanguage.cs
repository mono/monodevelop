//// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
//// 
//// Permission is hereby granted, free of charge, to any person obtaining a copy of this
//// software and associated documentation files (the "Software"), to deal in the Software
//// without restriction, including without limitation the rights to use, copy, modify, merge,
//// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
//// to whom the Software is furnished to do so, subject to the following conditions:
//// 
//// The above copyright notice and this permission notice shall be included in all copies or
//// substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
//// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
//// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
//// DEALINGS IN THE SOFTWARE.

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Resources;

//using ICSharpCode.Decompiler;
//using ICSharpCode.Decompiler.CSharp;
//using ICSharpCode.Decompiler.CSharp.OutputVisitor;
//using ICSharpCode.Decompiler.CSharp.Syntax;
//using ICSharpCode.Decompiler.TypeSystem;
//using ICSharpCode.Decompiler.CSharp.Transforms;
//using ICSharpCode.Decompiler.Metadata;
//using System.Reflection.PortableExecutable;
//using System.Reflection.Metadata;

//namespace ICSharpCode.ILSpy
//{
//	/// <summary>
//	/// C# decompiler integration into ILSpy.
//	/// Note: if you're interested in using the decompiler without the ILSpy UI,
//	/// please directly use the CSharpDecompiler class.
//	/// </summary>
//	class CSharpLanguage
//	{
//		public static readonly CSharpLanguage Instance = new CSharpLanguage ();
//		string name = "C#";
//		int transformCount = int.MaxValue;

//		public string Name {
//			get { return name; }
//		}

//		public string FileExtension {
//			get { return ".cs"; }
//		}

//		public string ProjectFileExtension {
//			get { return ".csproj"; }
//		}


//		CSharpDecompiler CreateDecompiler (PEFile module, DecompilerSettings options)
//		{
//			var assemblyResolver = new UniversalAssemblyResolver (module.FileName, options.ThrowOnAssemblyResolveErrors, module.Reader.DetectTargetFrameworkId (null), options.LoadInMemory ? PEStreamOptions.PrefetchMetadata : 0);
//			CSharpDecompiler decompiler = new CSharpDecompiler (module, assemblyResolver, options);
//			//decompiler.CancellationToken = options.CancellationToken;
//			while (decompiler.AstTransforms.Count > transformCount)
//				decompiler.AstTransforms.RemoveAt (decompiler.AstTransforms.Count - 1);
//			return decompiler;
//		}

//		void WriteCode (ITextOutput output, DecompilerSettings settings, SyntaxTree syntaxTree, IDecompilerTypeSystem typeSystem)
//		{
//			syntaxTree.AcceptVisitor (new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
//			TokenWriter tokenWriter = new TextTokenWriter (output, settings, typeSystem) { FoldBraces = settings.FoldBraces, /*ExpandMemberDefinitions = settings.ExpandMemberDefinitions */};
//			syntaxTree.AcceptVisitor (new CSharpOutputVisitor (tokenWriter, settings.CSharpFormattingOptions));
//		}

//		public void DecompileMethod (IMethod method, ITextOutput output, DecompilerSettings options)
//		{
//			WriteCommentLine (output, TypeToString (method.DeclaringType, includeNamespace: true));
//			CSharpDecompiler decompiler = CreateDecompiler (method.ParentModule.PEFile, options);
//			if (method.IsConstructor && method.DeclaringType.IsReferenceType == false) {
//				List<EntityHandle> members = CollectFieldsAndCtors (method.DeclaringTypeDefinition, method.IsStatic);
//				decompiler.AstTransforms.Add (new SelectCtorTransform (method));
//				WriteCode (output, options, decompiler.Decompile (members), decompiler.TypeSystem);
//			} else {
//				WriteCode (output, options, decompiler.Decompile (method.MetadataToken), decompiler.TypeSystem);
//			}
//		}

//		class SelectCtorTransform : IAstTransform
//		{
//			readonly IMethod ctor;
//			readonly HashSet<ISymbol> removedSymbols = new HashSet<ISymbol> ();

//			public SelectCtorTransform (IMethod ctor)
//			{
//				this.ctor = ctor;
//			}

//			public void Run (AstNode rootNode, TransformContext context)
//			{
//				ConstructorDeclaration ctorDecl = null;
//				foreach (var node in rootNode.Children) {
//					switch (node) {
//					case ConstructorDeclaration ctor:
//						if (ctor.GetSymbol () == this.ctor) {
//							ctorDecl = ctor;
//						} else {
//							// remove other ctors
//							ctor.Remove ();
//							removedSymbols.Add (ctor.GetSymbol ());
//						}
//						break;
//					case FieldDeclaration fd:
//						// Remove any fields without initializers
//						if (fd.Variables.All (v => v.Initializer.IsNull)) {
//							fd.Remove ();
//							removedSymbols.Add (fd.GetSymbol ());
//						}
//						break;
//					}
//				}
//				if (ctorDecl?.Initializer.ConstructorInitializerType == ConstructorInitializerType.This) {
//					// remove all fields
//					foreach (var node in rootNode.Children) {
//						switch (node) {
//						case FieldDeclaration fd:
//							fd.Remove ();
//							removedSymbols.Add (fd.GetSymbol ());
//							break;
//						}
//					}
//				}
//				foreach (var node in rootNode.Children) {
//					if (node is Comment && removedSymbols.Contains (node.GetSymbol ()))
//						node.Remove ();
//				}
//			}
//		}

//		public void DecompileProperty (IProperty property, ITextOutput output, DecompilerSettings options)
//		{
//			WriteCommentLine (output, TypeToString (property.DeclaringTypeDefinition, includeNamespace: true));
//			CSharpDecompiler decompiler = CreateDecompiler (property.ParentModule.PEFile, options);
//			WriteCode (output, options, decompiler.Decompile (property.MetadataToken), decompiler.TypeSystem);
//		}

//		public void DecompileField (IField field, ITextOutput output, DecompilerSettings options)
//		{
//			WriteCommentLine (output, TypeToString (field.DeclaringType, includeNamespace: true));
//			CSharpDecompiler decompiler = CreateDecompiler (field.ParentModule.PEFile, options);
//			if (field.IsLiteral) {
//				WriteCode (output, options, decompiler.Decompile (field.MetadataToken), decompiler.TypeSystem);
//			} else {
//				List<EntityHandle> members = CollectFieldsAndCtors (field.DeclaringTypeDefinition, field.IsStatic);
//				decompiler.AstTransforms.Add (new SelectFieldTransform (field));
//				WriteCode (output, options, decompiler.Decompile (members), decompiler.TypeSystem);
//			}
//		}

//		private static List<EntityHandle> CollectFieldsAndCtors (ITypeDefinition type, bool isStatic)
//		{
//			var members = new List<EntityHandle> ();
//			foreach (var field in type.Fields) {
//				if (field.IsStatic == isStatic)
//					members.Add (field.MetadataToken);
//			}
//			foreach (var ctor in type.Methods) {
//				if (ctor.IsConstructor && ctor.IsStatic == isStatic)
//					members.Add (ctor.MetadataToken);
//			}

//			return members;
//		}

//		/// <summary>
//		/// Removes all top-level members except for the specified fields.
//		/// </summary>
//		sealed class SelectFieldTransform : IAstTransform
//		{
//			readonly IField field;

//			public SelectFieldTransform (IField field)
//			{
//				this.field = field;
//			}

//			public void Run (AstNode rootNode, TransformContext context)
//			{
//				foreach (var node in rootNode.Children) {
//					switch (node) {
//					case EntityDeclaration ed:
//						if (node.GetSymbol () != field)
//							node.Remove ();
//						break;
//					case Comment c:
//						if (c.GetSymbol () != field)
//							node.Remove ();
//						break;
//					}
//				}
//			}
//		}

//		public void DecompileEvent (IEvent ev, ITextOutput output, DecompilerSettings options)
//		{
//			WriteCommentLine (output, TypeToString (ev.DeclaringType, includeNamespace: true));
//			CSharpDecompiler decompiler = CreateDecompiler (ev.ParentModule.PEFile, options);
//			WriteCode (output, options, decompiler.Decompile (ev.MetadataToken), decompiler.TypeSystem);
//		}

//		public void DecompileType (ITypeDefinition type, ITextOutput output, DecompilerSettings options)
//		{
//			WriteCommentLine (output, TypeToString (type, includeNamespace: true));
//			CSharpDecompiler decompiler = CreateDecompiler (type.ParentModule.PEFile, options);
//			WriteCode (output, options, decompiler.Decompile (type.MetadataToken), decompiler.TypeSystem);
//		}

	
//		public string TypeToString (IType type, bool includeNamespace)
//		{
//			ConversionFlags options = ConversionFlags.ShowTypeParameterList | ConversionFlags.ShowReturnType;
//			if (includeNamespace)
//				options |= ConversionFlags.UseFullyQualifiedTypeNames;

//			return TypeToString (options, type);
//		}

//		string TypeToString (ConversionFlags options, IType type)
//		{
//			AstType astType =  (type, options);

//			StringWriter w = new StringWriter ();
//			if (type.IsByReference) {
//				IParameter pd = typeAttributes as IParameter;
//				if (pd != null && (!pd.IsIn && pd.IsOut))
//					w.Write ("out ");
//				else
//					w.Write ("ref ");

//				if (astType is ComposedType && ((ComposedType)astType).PointerRank > 0)
//					((ComposedType)astType).PointerRank--;
//			}

//			astType.AcceptVisitor (new CSharpOutputVisitor (w, TypeToStringFormattingOptions));
//			return w.ToString ();
//		}

//		static readonly CSharpFormattingOptions TypeToStringFormattingOptions = FormattingOptionsFactory.CreateEmpty ();

//		public string FormatPropertyName (IProperty property, bool? isIndexer)
//		{
//			if (property == null)
//				throw new ArgumentNullException (nameof (property));

//			if (!isIndexer.HasValue) {
//				isIndexer = property.IsIndexer;
//			}
//			if (isIndexer.Value) {
//				var buffer = new System.Text.StringBuilder ();
//				var accessor = property.Getter ?? property.Setter;
//				if (accessor.IsOverride) {
//					var declaringType = accessor.Overrides [0].DeclaringType;
//					buffer.Append (TypeToString (declaringType, includeNamespace: true));
//					buffer.Append (@".");
//				}
//				buffer.Append (@"this[");
//				bool addSeparator = false;
//				foreach (var p in property.Parameters) {
//					if (addSeparator)
//						buffer.Append (@", ");
//					else
//						addSeparator = true;
//					buffer.Append (TypeToString (p.ParameterType, includeNamespace: true));
//				}
//				buffer.Append (@"]");
//				return buffer.ToString ();
//			} else
//				return property.Name;
//		}

//		public string FormatMethodName (IMethod method)
//		{
//			if (method == null)
//				throw new ArgumentNullException ("method");

//			return (method.IsConstructor) ? FormatTypeName (method.DeclaringType) : method.Name;
//		}

//		public string FormatTypeName (IType type)
//		{
//			if (type == null)
//				throw new ArgumentNullException ("type");

//			return TypeToString (ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions, type);
//		}

//		public void WriteCommentLine (ITextOutput output, string comment)
//		{
//			output.WriteLine ("// " + comment);
//		}
//	}
//}
