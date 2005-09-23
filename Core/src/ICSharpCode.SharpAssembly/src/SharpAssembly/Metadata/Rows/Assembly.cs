// Assembly.cs
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
	
	public class Assembly : AbstractRow
	{
		public static readonly int TABLE_ID = 0x20;
		
		uint  hashAlgID;
		ushort majorVersion;
		ushort minorVersion;
		ushort buildNumber;
		ushort revisionNumber;
		uint   flags;
		
		uint publicKey; // index into the BLOB heap
		uint name;      // index into the string heap
		uint culture;   // index into the string heap
		
		public uint HashAlgID {
			get {
				return hashAlgID;
			}
			set {
				hashAlgID = value;
			}
		}
		public ushort MajorVersion {
			get {
				return majorVersion;
			}
			set {
				majorVersion = value;
			}
		}
		public ushort MinorVersion {
			get {
				return minorVersion;
			}
			set {
				minorVersion = value;
			}
		}
		public ushort BuildNumber {
			get {
				return buildNumber;
			}
			set {
				buildNumber = value;
			}
		}
		public ushort RevisionNumber {
			get {
				return revisionNumber;
			}
			set {
				revisionNumber = value;
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
		public uint PublicKey {
			get {
				return publicKey;
			}
			set {
				publicKey = value;
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
		
		public override void LoadRow()
		{
			hashAlgID      = binaryReader.ReadUInt32();
			majorVersion   = binaryReader.ReadUInt16();
			minorVersion   = binaryReader.ReadUInt16();
			buildNumber    = binaryReader.ReadUInt16();
			revisionNumber = binaryReader.ReadUInt16();
			flags          = binaryReader.ReadUInt32();
			publicKey      = LoadBlobIndex();
			name           = LoadStringIndex();
			culture        = LoadStringIndex();
		}
	}
}
