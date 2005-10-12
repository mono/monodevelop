// Event.cs
// Copyright (C) 2003 Mike Krueger
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.IO;

namespace MonoDevelop.SharpAssembly.Metadata.Rows {
	
	public class Event : AbstractRow
	{
		public static readonly int TABLE_ID = 0x14;
		
		public static readonly ushort FLAG_SPECIALNAME   = 0x0200;
		public static readonly ushort FLAG_RTSPECIALNAME = 0x0400;
		
		ushort eventFlags;
		uint   name;      // index into String heap
		uint   eventType; // index into TypeDef, TypeRef or TypeSpec tables; more precisely, a TypeDefOrRef coded index
		
		public ushort EventFlags {
			get {
				return eventFlags;
			}
			set {
				eventFlags = value;
			}
		}
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		public uint EventType {
			get {
				return eventType;
			}
			set {
				eventType = value;
			}
		}
		
		public override void LoadRow()
		{
			eventFlags = binaryReader.ReadUInt16();
			name       = LoadStringIndex();
			eventType  = ReadCodedIndex(CodedIndex.TypeDefOrRef);
		}
	}
}
