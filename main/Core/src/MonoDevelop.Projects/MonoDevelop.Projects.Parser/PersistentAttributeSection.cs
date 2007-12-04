//
// PersistentAttributeSection.cs
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
using System.IO;

using System.Reflection;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	class PersistentAttributeSectionCollection
	{
		public static AttributeSectionCollection Resolve (AttributeSectionCollection collection, ITypeResolver typeResolver)
		{
			if (collection == null)
				return null;
			AttributeSectionCollection rcol = new AttributeSectionCollection ();
			foreach (IAttributeSection ats in collection)
				rcol.Add (PersistentAttributeSection.Resolve (ats, typeResolver));
			return rcol;
		}
		
		public static AttributeSectionCollection Read (BinaryReader reader, INameDecoder nameTable)
		{
			if (PersistentHelper.ReadNull (reader)) return null;
			
			AttributeSectionCollection collection = new AttributeSectionCollection ();
			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				collection.Add (PersistentAttributeSection.Read (reader, nameTable));
			}
			return collection;
		}
		
		public static void WriteTo (AttributeSectionCollection collection, BinaryWriter writer, INameEncoder nameTable)
		{
			if (PersistentHelper.WriteNull (collection, writer)) return;
			
			writer.Write ((uint)collection.Count);
			foreach (IAttributeSection ats in collection)
				PersistentAttributeSection.WriteTo (ats, writer, nameTable);
		}
	}

	[Serializable]
	internal sealed class PersistentAttributeSection
	{
		public static DefaultAttributeSection Resolve (IAttributeSection source, ITypeResolver typeResolver)
		{
			DefaultAttributeSection ats = new DefaultAttributeSection (source.AttributeTarget, source.Region);
			
			foreach (IAttribute at in source.Attributes)
				ats.Attributes.Add (PersistentAttribute.Resolve (at, typeResolver));

			return ats;
		}
		
		public static DefaultAttributeSection Read (BinaryReader reader, INameDecoder nameTable)
		{
			AttributeTarget tar = (AttributeTarget) reader.ReadInt32 ();
			IRegion reg = PersistentRegion.Read (reader, nameTable);
			DefaultAttributeSection ats = new DefaultAttributeSection (tar, reg);
			
			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				ats.Attributes.Add (PersistentAttribute.Read (reader, nameTable));
			}
			return ats;
		}
		
		public static void WriteTo (IAttributeSection ats, BinaryWriter writer, INameEncoder nameTable)
		{
			writer.Write ((int) ats.AttributeTarget);
			PersistentRegion.WriteTo (ats.Region, writer, nameTable);
			
			writer.Write (ats.Attributes != null ? (uint)ats.Attributes.Count : (uint)0);
			foreach (IAttribute at in ats.Attributes)
				PersistentAttribute.WriteTo (at, writer, nameTable);
		}
	}
}
