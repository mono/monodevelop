// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;

using MonoDevelop.Core.AddIns;

using MonoDevelop.Internal.Parser;
using MonoDevelop.Internal.Project;

using MonoDevelop.Gui;

namespace MonoDevelop.Services
{
	internal class ClassProxy : AbstractNamedEntity, IComparable, IClass
	{
		uint offset = 0;
		ClassType classType;
		
		public uint Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}
		
		public ClassType ClassType {
			get {
				return classType;
			}
			set {
				classType = value;
			}
		}
		
		/// <value>
		/// Class Proxies clases don't have a compilation unit.
		/// </value>
		public ICompilationUnit CompilationUnit {
			get {
				return null;
			}
		}
		
		public int CompareTo(object obj)
		{
			return FullyQualifiedName.CompareTo(((ClassProxy)obj).FullyQualifiedName);
		}
		
		public ClassProxy(BinaryReader reader)
		{
			FullyQualifiedName = reader.ReadString();
			Documentation      = reader.ReadString();
			offset             = reader.ReadUInt32();
			modifiers          = (ModifierEnum)reader.ReadUInt32();
			classType          = (ClassType)reader.ReadInt16();
		}
		
		public void WriteTo(BinaryWriter writer)
		{
			writer.Write(FullyQualifiedName);
			writer.Write(Documentation);
			writer.Write(offset);
			writer.Write((uint)modifiers);
			writer.Write((short)classType);
		}
		
		public ClassProxy(IClass c)
		{
			this.FullyQualifiedName  = c.FullyQualifiedName;
			this.Documentation       = c.Documentation;
			this.modifiers           = c.Modifiers;
			this.classType           = c.ClassType;
		}
		
		/// unnecessary stuff
		public IRegion Region {
			get {
				return new DefaultRegion(Point.Empty, Point.Empty);
			}
		}
		
		public IRegion BodyRegion {
			get {
				return new DefaultRegion(Point.Empty, Point.Empty);
			}
		}
		
		public StringCollection BaseTypes {
			get {
				return new StringCollection();
			}
		}
		
		public ClassCollection InnerClasses {
			get {
				return new ClassCollection();
			}
		}

		public FieldCollection Fields {
			get {
				return new FieldCollection();
			}
		}

		public PropertyCollection Properties {
			get {
				return new PropertyCollection();
			}
		}

		public IndexerCollection Indexer {
			get {
				return new IndexerCollection();
			}
		}

		public MethodCollection Methods {
			get {
				return new MethodCollection();
			}
		}

		public EventCollection Events {
			get {
				return new EventCollection();
			}
		}

		public IEnumerable ClassInheritanceTree {
			get {
				return null;
			}
		}
		
		public object DeclaredIn {
			get {
				return null;
			}
		}
		///
		
	}
}
