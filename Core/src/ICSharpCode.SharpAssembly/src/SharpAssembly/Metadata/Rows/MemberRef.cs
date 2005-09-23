// MemberRef.cs
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
	
	public class MemberRef : AbstractRow
	{
		public static readonly int TABLE_ID = 0x0A;
		
		uint myClass;    // index into the TypeRef, ModuleRef, Method, TypeSpec or TypeDef tables; more precisely, a MemberRefParent coded index
		uint name;      // index into String heap
		uint signature; // index into Blob heap
		
		public uint Class {
			get {
				return myClass;
			}
			set {
				myClass = value;
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
		public uint Signature {
			get {
				return signature;
			}
			set {
				signature = value;
			}
		}
		
		
		public override void LoadRow()
		{
			myClass   = ReadCodedIndex(CodedIndex.MemberRefParent);
			name      = LoadStringIndex();
			signature = LoadBlobIndex();
		}
	}
}
