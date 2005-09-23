// MethodSemantics.cs
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
	
	public class MethodSemantics : AbstractRow
	{
		public static readonly int TABLE_ID = 0x18;
		
		public static readonly ushort SEM_SETTER   = 0x0001;
		public static readonly ushort SEM_GETTER   = 0x0002;
		public static readonly ushort SEM_OTHER    = 0x0004;
		public static readonly ushort SEM_ADDON    = 0x0008;
		public static readonly ushort SEM_REMOVEON = 0x0010;
		public static readonly ushort SEM_FIRE     = 0x0020;

		ushort semantics;
		uint   method;      // index into the Method table
		uint   association; // index into the Event or Property table; more precisely, a HasSemantics coded index
		
		public ushort Semantics {
			get {
				return semantics;
			}
			set {
				semantics = value;
			}
		}
		
		public uint Method {
			get {
				return method;
			}
			set {
				method = value;
			}
		}
		
		public uint Association {
			get {
				return association;
			}
			set {
				association = value;
			}
		}
		
		
		public override void LoadRow()
		{
			semantics   = binaryReader.ReadUInt16();
			method      = ReadSimpleIndex(MonoDevelop.SharpAssembly.Metadata.Rows.Method.TABLE_ID);
			association = ReadCodedIndex(CodedIndex.HasSemantics);
		}
	}
}
