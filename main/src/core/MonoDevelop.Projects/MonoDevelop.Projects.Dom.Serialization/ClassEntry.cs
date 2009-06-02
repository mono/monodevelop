//
// ClassEntry.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Dom.Serialization
{
	[Serializable]
	class ClassEntry
	{
		// Position of the complete class information in the pidb file
		long position;
		
		NamespaceEntry namespaceRef;
		string name;
		
		[NonSerialized]
		int lastGetTime;
		
		[NonSerialized]
		IType cls;
		
		int typeParameterCount;
		ArrayList subclasses;
		ContentFlags flags;
		ClassType ctype;
		Modifiers modifiers;
		
		public ClassEntry (IType cls, NamespaceEntry namespaceRef)
		{
			this.cls = cls;
			this.namespaceRef = namespaceRef;
			position = -1;
			UpdateContent (cls);
		}
		
		public long Position
		{
			get { return position; }
			set { position = value; }
		}
		
		public ClassType ClassType {
			get { return ctype; }
		}
		
		public Modifiers Modifiers {
			get { return modifiers; }
		}
		
		public ContentFlags ContentFlags {
			get { return flags; }
		}
		
		public IType Class
		{
			get { 
				return cls; 
			}
			set {
				cls = value; 
				if (cls != null) {
					position = -1;
					UpdateContent (cls);
				}
			}
		}
		
		internal void AttachClass (IType c)
		{
			cls = c;
		}
		
		void UpdateContent (IType cls)
		{
			Name = cls.Name; 
			ctype = cls.ClassType;
			modifiers = cls.Modifiers;
			this.typeParameterCount = cls.TypeParameters.Count;
			flags = (ContentFlags) 0;
			if (this.typeParameterCount > 0)
				flags |= ContentFlags.HasGenericParams;
			if (cls.Attributes.Count () > 0)
				flags |= ContentFlags.HasAttributes;
			if ((cls.BaseType != null && !cls.BaseType.ToInvariantString ().Equals (DomReturnType.Object.ToInvariantString ())) || cls.ImplementedInterfaces.Count () > 0)
				flags |= ContentFlags.HasBaseTypes;
			if (cls.CompilationUnit != null)
				flags |= ContentFlags.HasCompilationUnit;
				
			if (cls.Documentation != null && cls.Documentation.Length > 0)
				flags |= ContentFlags.HasDocumentation;
			if (cls.EventCount > 0)
				flags |= ContentFlags.HasEvents;
			if (cls.FieldCount > 0)
				flags |= ContentFlags.HasFields;
			if (cls.IndexerCount > 0)
				flags |= ContentFlags.HasIndexers;
			if (cls.InnerTypeCount > 0)
				flags |= ContentFlags.HasInnerClasses;
			if (cls.MethodCount > 0)
				flags |= ContentFlags.HasMethods;
			
			if (cls.PropertyCount > 0)
				flags |= ContentFlags.HasProperties;
			if (cls.HasParts)
				flags |= ContentFlags.HasParts;
				
			if (cls.Location != DomLocation.Empty)
				flags |= ContentFlags.HasRegion;
			if (cls.BodyRegion != DomRegion.Empty)
				flags |= ContentFlags.HasBodyRegion;
			if (cls.ConstructorCount > 0)
				flags |= ContentFlags.HasConstructors;
			if (cls.IsObsolete)
				flags |= ContentFlags.IsObsolete;
			if (cls.HasExtensionMethods)
				flags |= ContentFlags.HasExtensionMethods;
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public NamespaceEntry NamespaceRef
		{
			get { return namespaceRef; }
		}
		
		public int LastGetTime
		{
			get { return lastGetTime; }
			set { lastGetTime = value; }
		}
		
		public void RegisterSubclass (object cls)
		{
			if (subclasses == null)
				subclasses = new ArrayList ();
			subclasses.Add (cls);
		}
		
		public void UnregisterSubclass (object cls)
		{
			if (subclasses != null)
				subclasses.Remove (cls);
		}
		
		public ArrayList Subclasses {
			get { return subclasses; }
			set { subclasses = value; }
		}

		public int TypeParameterCount {
			get {
				return typeParameterCount;
			}
			set {
				typeParameterCount = value;
			}
		}
		
		public override string ToString()
		{
			return string.Format("[ClassEntry: Position={0}, ClassType={1}, Modifiers={2}, ContentFlags={3}, Name={4}, NamespaceRef={5}, LastGetTime={6}, Subclasses={7}, TypeParameterCount={8}]", Position, ClassType, Modifiers, ContentFlags, Name, NamespaceRef, LastGetTime, Subclasses, TypeParameterCount);
		}
	}

	[Flags]
	enum ContentFlags: uint
	{
		HasGenericParams    = 0x00000001,
		HasBaseTypes        = 0x00000002,
		HasInnerClasses     = 0x00000004,
		HasFields           = 0x00000008,
		
		HasMethods          = 0x00000010,
		HasProperties       = 0x00000020,
		HasIndexers         = 0x00000040,
		HasEvents           = 0x00000080,
		
		HasParts            = 0x00000100,
		HasRegion           = 0x00000200,
		HasBodyRegion       = 0x00000400,
		HasCompilationUnit  = 0x00000800,
		
		HasAttributes       = 0x00001000,
		HasDocumentation    = 0x00002000,
		HasConstructors     = 0x00004000,
		IsObsolete          = 0x00008000,
		HasExtensionMethods = 0x00010000
	}
}
