// EventMap.cs
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
	
	public class EventMap : AbstractRow
	{
		public static readonly int TABLE_ID = 0x12;
		
		uint   parent;    // index into the TypeDef table
		uint   eventList; // index into Event table
		
		public uint Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}
		public uint EventList {
			get {
				return eventList;
			}
			set {
				eventList = value;
			}
		}
		
		public override void LoadRow()
		{
			parent    = ReadSimpleIndex(TypeDef.TABLE_ID);
			eventList = ReadSimpleIndex(Event.TABLE_ID);
		}
	}
}
