// ClassWrapper.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	// This IClass implementation gets its information from a ClassEntry.
	// However, ClassEntry does not have all information of a class, it
	// only has the name, namespace and some flags. All the other information
	// (fields, methods, etc) is stored in the pidb file at a position
	// stored in the ClassEntry. However, in some cases the information
	// in ClassEntry is enough. For example, for displaying the contents
	// of the code completion list (which only shows the type name and kind).
	// For those cases, the code completion database will create a ClassWrapper,
	// which already has the basic information from ClassEntry and which
	// will lazily load all the missing information from the pidb file.
	
	class ClassWrapper: IClass
	{
		ClassEntry entry;
		CodeCompletionDatabase db;
		string ns;
		string name;
		IClass wrapped;
		bool loaded;
		
		public ClassWrapper (CodeCompletionDatabase db, ClassEntry entry)
		{
			this.entry = entry;
			this.db = db;
			this.name = entry.Name;
			this.ns = entry.NamespaceRef.FullName;
		}
		
		IClass Wrapped {
			get {
				// If the wrapper is required, it means that the user is trying to get some
				// information not available in ClassEntry. In this case, read the full
				// clas from the database.
				if (!loaded) {
					loaded = true;
					try {
						wrapped = db.ReadClass (entry);
						entry.AttachClass (wrapped);
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				}
				return wrapped;
			}
		}
		
		public string FullyQualifiedName {
			get {
				if (ns != null && ns.Length > 0)
					return string.Concat (ns, ".", name);
				else
					return name;
			}
		}
		
		public string Name {
			get { return name; }
		}
		
		public string Namespace {
			get { return ns; }
		}
		
		public ClassType ClassType {
			get { return entry.ClassType; }
		}		
		
		public ICompilationUnit CompilationUnit {
			get {
				if (HasContent (ContentFlags.HasCompilationUnit))
					return Wrapped.CompilationUnit;
				else
					return null;
			}
		}
		
		public virtual SolutionItem SourceProject {
			get {
				if (db is ProjectCodeCompletionDatabase)
					return ((ProjectCodeCompletionDatabase)db).Project;
				else
					return null;
			}
		}
		
		public IRegion Region {
			get {
				if (HasContent (ContentFlags.HasRegion))
					return Wrapped.Region;
				else
					return null;
			}
		}
		
		public IRegion BodyRegion {
			get {
				if (HasContent (ContentFlags.HasBodyRegion))
					return Wrapped.BodyRegion;
				else
					return null;
			}
		}
		
		// For classes composed by several files, returns all parts of the class
		public IClass[] Parts {
			get {
				if (HasContent (ContentFlags.HasParts))
					return Wrapped.Parts;
				else
					return new IClass [0];
			}
		}
		
		public GenericParameterList GenericParameters {
			get {
				if (HasContent (ContentFlags.HasGenericParams))
					return Wrapped.GenericParameters;
				else
					return null;
			}
		}
		
		public ReturnTypeList BaseTypes {
			get {
				if (HasContent (ContentFlags.HasBaseTypes))
					return Wrapped.BaseTypes;
				else
					return new ReturnTypeList ();
			}
		}
		
		public ClassCollection InnerClasses {
			get {
				if (HasContent (ContentFlags.HasInnerClasses))
					return Wrapped.InnerClasses;
				else
					return new ClassCollection ();
			}
		}

		public FieldCollection Fields {
			get {
				if (HasContent (ContentFlags.HasFields))
					return Wrapped.Fields;
				else
					return new FieldCollection (this);
			}
		}

		public PropertyCollection Properties {
			get {
				if (HasContent (ContentFlags.HasProperties))
					return Wrapped.Properties;
				else
					return new PropertyCollection (this);
			}
		}

		public IndexerCollection Indexer {
			get {
				if (HasContent (ContentFlags.HasIndexers))
					return Wrapped.Indexer;
				else
					return new IndexerCollection (this);
			}
		}

		public MethodCollection Methods {
			get {
				if (HasContent (ContentFlags.HasMethods))
					return Wrapped.Methods;
				else
					return new MethodCollection (this);
			}
		}

		public EventCollection Events {
			get {
				if (HasContent (ContentFlags.HasEvents))
					return Wrapped.Events;
				else
					return new EventCollection (this);
			}
		}

		public object DeclaredIn {
			get { return null; } // Inner classes are never wrapped
		}
		
		public AttributeSectionCollection Attributes {
			get {
				if (HasContent (ContentFlags.HasAttributes))
					return Wrapped.Attributes;
				else
					return new AttributeSectionCollection ();
			}
		}

		public string Documentation {
			get {
				if (HasContent (ContentFlags.HasDocumentation))
					return Wrapped.Documentation;
				else
					return string.Empty;
			}
		}
		
		bool HasContent (ContentFlags cf)
		{
			return (entry.ContentFlags & cf) != 0 && Wrapped != null;
		}
		
		public ModifierEnum Modifiers {
			get { return entry.Modifiers; }
		}
		
		public bool IsAbstract {
			get {
				return (Modifiers & ModifierEnum.Abstract) == ModifierEnum.Abstract;
			}
		}

		public bool IsSealed {
			get {
				return (Modifiers & ModifierEnum.Sealed) == ModifierEnum.Sealed;
			}
		}

		public bool IsStatic {
			get {
				return (Modifiers & ModifierEnum.Static) == ModifierEnum.Static;
			}
		}

		public bool IsVirtual {
			get {
				return (Modifiers & ModifierEnum.Virtual) == ModifierEnum.Virtual;
			}
		}

		public bool IsPublic {
			get {
				return (Modifiers & ModifierEnum.Public) == ModifierEnum.Public;
			}
		}

		public bool IsProtected {
			get {
				return (Modifiers & ModifierEnum.Protected) == ModifierEnum.Protected;
			}
		}

		public bool IsPrivate {
			get {
				return (Modifiers & ModifierEnum.Private) == ModifierEnum.Private;
			}
		}

		public bool IsInternal {
			get {
				return (Modifiers & ModifierEnum.Internal) == ModifierEnum.Internal;
			}
		}

		public bool IsProtectedAndInternal {
			get {
				return (Modifiers & (ModifierEnum.Internal | ModifierEnum.Protected)) == (ModifierEnum.Internal | ModifierEnum.Protected);
			}
		}

		public bool IsProtectedOrInternal {
			get {
				return (Modifiers & ModifierEnum.ProtectedOrInternal) == ModifierEnum.ProtectedOrInternal;
			}
		}

		public bool IsLiteral {
			get {
				return (Modifiers & ModifierEnum.Const) == ModifierEnum.Const;
			}
		}

		public bool IsReadonly {
			get {
				return (Modifiers & ModifierEnum.Readonly) == ModifierEnum.Readonly;
			}
		}

		public bool IsOverride {
			get {
				return (Modifiers & ModifierEnum.Override) == ModifierEnum.Override;
			}
		}

		public bool IsFinal {
			get {
				return (Modifiers & ModifierEnum.Final) == ModifierEnum.Final;
			}
		}

		public bool IsSpecialName {
			get {
				return (Modifiers & ModifierEnum.SpecialName) == ModifierEnum.SpecialName;
			}
		}

		public bool IsNew {
			get {
				return (Modifiers & ModifierEnum.New) == ModifierEnum.New;
			}
		}
		
		public int CompareTo (object ob)
		{
			if (Wrapped != null)
				return Wrapped.CompareTo (ob);
			else
				return 1;
		}
	}
}
