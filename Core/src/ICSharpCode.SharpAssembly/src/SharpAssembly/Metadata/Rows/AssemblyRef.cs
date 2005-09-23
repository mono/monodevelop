// AssemblyRef.cs
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
	
	public class AssemblyRef : AbstractRow
	{
		public static readonly int TABLE_ID = 0x23;
		
		ushort major;
		ushort minor;
		ushort build;
		ushort revision;
		uint flags;
		uint publicKeyOrToken; // index into Blob heap
		uint name;    // index into String heap
		uint culture; // index into String heap
		uint hashValue; // index into Blob heap
		
		public ushort Major {
			get {
				return major;
			}
			set {
				major = value;
			}
		}
		public ushort Minor {
			get {
				return minor;
			}
			set {
				minor = value;
			}
		}
		
		public ushort Build {
			get {
				return build;
			}
			set {
				build = value;
			}
		}
		
		public ushort Revision {
			get {
				return revision;
			}
			set {
				revision = value;
			}
		}

		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}
		public uint PublicKeyOrToken {
			get {
				return publicKeyOrToken;
			}
			set {
				publicKeyOrToken = value;
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
		public uint Culture {
			get {
				return culture;
			}
			set {
				culture = value;
			}
		}
		public uint HashValue {
			get {
				return hashValue;
			}
			set {
				hashValue = value;
			}
		}
		
		public override void LoadRow()
		{
			major			 = binaryReader.ReadUInt16();
			minor			 = binaryReader.ReadUInt16();
			build			 = binaryReader.ReadUInt16();
			revision		 = binaryReader.ReadUInt16();
			flags            = binaryReader.ReadUInt32();
			publicKeyOrToken = LoadBlobIndex();
			name             = LoadStringIndex();
			culture          = LoadStringIndex();
			hashValue        = LoadBlobIndex();
		}
	}
}
