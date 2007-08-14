
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
						Runtime.LoggingService.Error (ex);
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
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasCompilationUnit) != 0)
					return Wrapped.CompilationUnit;
				else
					return null;
			}
		}
		
		public virtual CombineEntry SourceProject {
			get {
				if (db is ProjectCodeCompletionDatabase)
					return ((ProjectCodeCompletionDatabase)db).Project;
				else
					return null;
			}
		}
		
		public IRegion Region {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasRegion) != 0)
					return Wrapped.Region;
				else
					return null;
			}
		}
		
		public IRegion BodyRegion {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasBodyRegion) != 0)
					return Wrapped.BodyRegion;
				else
					return null;
			}
		}
		
		// For classes composed by several files, returns all parts of the class
		public IClass[] Parts {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasParts) != 0)
					return Wrapped.Parts;
				else
					return new IClass [0];
			}
		}
		
		public GenericParameterList GenericParameters {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasGenericParams) != 0)
					return Wrapped.GenericParameters;
				else
					return null;
			}
		}
		
		public ReturnTypeList BaseTypes {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasBaseTypes) != 0)
					return Wrapped.BaseTypes;
				else
					return new ReturnTypeList ();
			}
		}
		
		public ClassCollection InnerClasses {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasInnerClasses) != 0)
					return Wrapped.InnerClasses;
				else
					return new ClassCollection ();
			}
		}

		public FieldCollection Fields {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasFields) != 0)
					return Wrapped.Fields;
				else
					return new FieldCollection (this);
			}
		}

		public PropertyCollection Properties {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasProperties) != 0)
					return Wrapped.Properties;
				else
					return new PropertyCollection (this);
			}
		}

		public IndexerCollection Indexer {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasIndexers) != 0)
					return Wrapped.Indexer;
				else
					return new IndexerCollection (this);
			}
		}

		public MethodCollection Methods {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasMethods) != 0)
					return Wrapped.Methods;
				else
					return new MethodCollection (this);
			}
		}

		public EventCollection Events {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasEvents) != 0)
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
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasAttributes) != 0)
					return Wrapped.Attributes;
				else
					return new AttributeSectionCollection ();
			}
		}

		public string Documentation {
			get {
				if (Wrapped != null && (entry.ContentFlags & ContentFlags.HasDocumentation) != 0)
					return Wrapped.Documentation;
				else
					return string.Empty;
			}
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
