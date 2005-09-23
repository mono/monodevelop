// MethodImpl.cs
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
	
	public class MethodImpl : AbstractRow
	{
		public static readonly int TABLE_ID = 0x19;
		
		uint myClass;           // index into TypeDef table
		uint methodBody;        // index into Method or MemberRef table; more precisely, a MethodDefOrRef coded index
		uint methodDeclaration; // index into Method or MemberRef table; more precisely, a MethodDefOrRef coded index
		
		public uint MyClass {
			get {
				return myClass;
			}
			set {
				myClass = value;
			}
		}
		public uint MethodBody {
			get {
				return methodBody;
			}
			set {
				methodBody = value;
			}
		}
		public uint MethodDeclaration {
			get {
				return methodDeclaration;
			}
			set {
				methodDeclaration = value;
			}
		}
		
		public override void LoadRow()
		{
			myClass           = ReadSimpleIndex(TypeDef.TABLE_ID);
			methodBody        = ReadCodedIndex(CodedIndex.MethodDefOrRef);
			methodDeclaration = ReadCodedIndex(CodedIndex.MethodDefOrRef);
		}
	}
}
