// 
// Event.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MonoDevelop.Profiler
{
	public enum EventType
	{
		Alloc      = 0,
		Gc         = 1,
		Metadata   = 2,
		Method     = 3,
		Exception  = 4,
		Monitor    = 5,
		Heap       = 6
	}
	
	public class Backtrace
	{
		public ulong Flags;
		public long[] Frame;
		
		public Backtrace (BinaryReader reader)
		{
			Flags = reader.ReadULeb128 ();
			ulong num = reader.ReadULeb128 ();
			Frame = new long[num];
			for (ulong i = 0; i < num; i++) {
				Frame [i] = reader.ReadSLeb128 ();
			}
		}
	}
	
	public abstract class Event
	{
		/// <summary>
		/// Gets or sets the nanoseconds since last timing.
		/// </summary>
		/// <value>
		/// Nanoseconds since last timing.
		/// </value>
		public ulong TimeDiff {
			get;
			protected set;
		}
		
		public const byte TYPE_GC_EVENT = 1 << 4;
		public const byte TYPE_GC_RESIZE = 2 << 4;
		public const byte TYPE_GC_MOVE = 3 << 4;
		public const byte TYPE_GC_HANDLE_CREATED = 4 << 4;
		public const byte TYPE_GC_HANDLE_DESTROYED = 5 << 4;
		
		public static Event CreateEvent (BinaryReader reader,EventType type, byte extendedInfo)
		{
			switch (type) {
			case EventType.Alloc:
				return AllocEvent.Read (reader, extendedInfo); 
			case EventType.Exception:
				return ExceptionEvent.Read (reader, extendedInfo);
			case EventType.Gc:
				switch (extendedInfo) {
				case TYPE_GC_EVENT:
					return GcEvent.Read (reader);
				case TYPE_GC_RESIZE:
					return ResizeGcEvent.Read (reader);
				case TYPE_GC_MOVE:
					return MoveGcEvent.Read (reader);
				case TYPE_GC_HANDLE_CREATED:
					return HandleCreatedGcEvent.Read (reader);
				case TYPE_GC_HANDLE_DESTROYED:
					return HandleDestroyedGcEvent.Read (reader);
				}
				throw new InvalidOperationException ("unknown gc type:" + extendedInfo);
			case EventType.Heap:
				return HeapEvent.Read (reader, extendedInfo); 
			case EventType.Metadata:
				return MetadataEvent.Read (reader); 
			case EventType.Method:
				return MethodEvent.Read (reader, extendedInfo); 
			case EventType.Monitor:
				return MonitiorEvent.Read (reader, extendedInfo); 
			}
			throw new InvalidOperationException ("invalid event type " + type);	
		}

		public static Event Read (BinaryReader reader)
		{
			byte info = reader.ReadByte ();
			EventType type = (EventType)(info & 0xF);
			byte extendedInfo = (byte)(info & 0xF0);
			return CreateEvent (reader, type, extendedInfo);
		}
		
		public abstract object Accept (EventVisitor visitor);
	}
	
	// type == Alloc
	public class AllocEvent : Event
	{
		public const byte TYPE_ALLOC_BT = 1 << 4;
		public readonly long Ptr; // class as a byte difference from ptr_base
		public readonly long Obj; // object address as a byte difference from obj_base
		public readonly ulong Size; // size of the object in the heap
		public readonly Backtrace Backtrace;
		
		AllocEvent (BinaryReader reader, byte extendedInfo)
		{
			TimeDiff = reader.ReadULeb128 ();
			Ptr = reader.ReadSLeb128 ();
			Obj = reader.ReadSLeb128 ();
			Size = reader.ReadULeb128 ();
			if (extendedInfo == TYPE_ALLOC_BT)
				Backtrace = new Backtrace (reader);
		}

		public static Event Read (BinaryReader reader, byte extendedInfo)
		{
			return new AllocEvent (reader, extendedInfo);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	// type == Gc
	public class ResizeGcEvent : Event
	{
		public readonly ulong HeapSize; // new heap size
		
		ResizeGcEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			HeapSize = reader.ReadULeb128 ();
		}

		public static new Event Read (BinaryReader reader)
		{
			return new ResizeGcEvent (reader);
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class GcEvent : Event
	{
		public readonly ulong GcEventType; //  GC event (MONO_GC_EVENT_* from profiler.h)
		public readonly ulong Generation;  // GC generation event refers to
		
		GcEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			GcEventType = reader.ReadULeb128 ();
			Generation = reader.ReadULeb128 ();
		}

		public static new Event Read (BinaryReader reader)
		{
			return new GcEvent (reader);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class MoveGcEvent : Event
	{
		public readonly long[] ObjAddr; //  num_objects object pointer differences from obj_base
		
		MoveGcEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			ulong num = reader.ReadULeb128 ();
			ObjAddr = new long[num];
			for (ulong i = 0; i < num; i++) {
				ObjAddr [i] = reader.ReadSLeb128 ();
			}
		}

		public static new Event Read (BinaryReader reader)
		{
			return new MoveGcEvent (reader);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class HandleCreatedGcEvent : Event
	{
		public readonly ulong HandleType; // GC handle type (System.Runtime.InteropServices.GCHandleType)
		public readonly ulong Handle; // GC handle value
		public readonly long ObjAddr; // object pointer differences from obj_base
		
		HandleCreatedGcEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			HandleType = reader.ReadULeb128 ();
			Handle = reader.ReadULeb128 ();
			ObjAddr = reader.ReadSLeb128 ();
		}

		public static new Event Read (BinaryReader reader)
		{
			return new HandleCreatedGcEvent (reader);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class HandleDestroyedGcEvent : Event
	{
		public readonly ulong HandleType; // GC handle type (System.Runtime.InteropServices.GCHandleType)
		public readonly ulong Handle; // GC handle value
		
		HandleDestroyedGcEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			HandleType = reader.ReadULeb128 ();
			Handle = reader.ReadULeb128 ();
		}

		public static new Event Read (BinaryReader reader)
		{
			return new HandleDestroyedGcEvent (reader);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	// type == Methadata
	public class MetadataEvent : Event
	{
		public enum MetaDataType : byte
		{
			Class = 1,
			Image = 2,
			Assembly = 3,
			Domain = 4,
			Thread = 5
		}
		
		public readonly MetaDataType MType; //  metadata type, one of: TYPE_CLASS, TYPE_IMAGE, TYPE_ASSEMBLY, TYPE_DOMAINTYPE_THREAD
		public readonly long Pointer; // pointer of the metadata type depending on mtype
		
		public readonly ulong Flags; // must be 0
		public readonly string Name; // full class/image file or thread name 
		
		public readonly long Image; // MonoImage* as a pointer difference from ptr_base
	
		MetadataEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			MType = (MetaDataType)reader.ReadByte ();
			Pointer = reader.ReadSLeb128 ();
			switch (MType) {
			case MetaDataType.Class:
				Image = reader.ReadSLeb128 ();
				Flags = reader.ReadULeb128 ();
				Name = reader.ReadNullTerminatedString ();
				break;
			case MetaDataType.Image:
				Flags = reader.ReadULeb128 ();
				Name = reader.ReadNullTerminatedString ();
				break;
			case MetaDataType.Thread:
				Flags = reader.ReadULeb128 ();
				Name = reader.ReadNullTerminatedString ();
				break;
			}
		}

		public static new Event Read (BinaryReader reader)
		{
			return new MetadataEvent (reader);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	// type == Method
	public class MethodEvent : Event
	{
		public enum MethodType
		{
			Leave = 1 << 4,
			Enter = 2 << 4,
			ExcLeave = 3 << 4,
			Jit = 4 << 4
		};
		
		public readonly long Method; //  MonoMethod* as a pointer difference from the last such pointer or the buffer method_base
		public readonly MethodType Type;
		
		public readonly long CodeAddress; // pointer to the native code as a diff from ptr_base
		public readonly ulong CodeSize; // size of the generated code
		public readonly string Name; // full method name
		
		MethodEvent (BinaryReader reader, byte exinfo)
		{
			TimeDiff = reader.ReadULeb128 ();
			Method = reader.ReadSLeb128 ();
			Type = (MethodType)exinfo;
			if (Type == MethodType.Jit) {
				CodeAddress = reader.ReadSLeb128 ();
				CodeSize = reader.ReadULeb128 ();
				Name = reader.ReadNullTerminatedString ();
			}
		}

		public static Event Read (BinaryReader reader, byte exinfo)
		{
			return new MethodEvent (reader, exinfo);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	// type == Exception
	public class ExceptionEvent : Event
	{
		public const byte TYPE_THROW = 0 << 4;
		public const byte TYPE_CLAUSE = 1 << 4;
		public const byte TYPE_EXCEPTION_BT = 1 << 7;
		
		// Type clause
		public readonly ulong ClauseType; // finally/catch/fault/filter
		public readonly ulong ClauseNum; // the clause number in the method header
		public readonly long Method; //  MonoMethod* as a pointer difference from the last such pointer or the buffer method_base
		
		// Type throw
		public readonly long Object; // the object that was thrown as a difference from obj_base If the TYPE_EXCEPTION_BT flag is set, a backtrace follows.
		
		ExceptionEvent (BinaryReader reader, byte exinfo)
		{
			TimeDiff = reader.ReadULeb128 ();
			exinfo &= TYPE_EXCEPTION_BT - 1;
			if (exinfo == TYPE_CLAUSE) {
				ClauseType = reader.ReadULeb128 ();
				ClauseNum = reader.ReadULeb128 ();
				Method = reader.ReadSLeb128 ();
			} else if (exinfo == TYPE_THROW) {
				Object = reader.ReadSLeb128 ();
			}
		}

		public static Event Read (BinaryReader reader, byte exinfo)
		{
			return new ExceptionEvent (reader, exinfo);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	// type == Monitor
	public class MonitiorEvent : Event
	{
		public const int MONO_PROFILER_MONITOR_CONTENTION = 1;
		public const int MONO_PROFILER_MONITOR_DONE = 2;
		public const int MONO_PROFILER_MONITOR_FAIL = 3;
		
		public readonly long Object; //  the lock object as a difference from obj_base
		public readonly Backtrace Backtrace;
		
		MonitiorEvent (BinaryReader reader, byte exinfo)
		{
			TimeDiff = reader.ReadULeb128 ();
			Object = reader.ReadSLeb128 ();
			if (exinfo == MONO_PROFILER_MONITOR_CONTENTION) {
				Backtrace = new Backtrace (reader);
			}
		}

		public static Event Read (BinaryReader reader, byte exinfo)
		{
			return new MonitiorEvent (reader, exinfo);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	// type == Heap
	public class HeapEvent : Event
	{
		public const byte TYPE_HEAP_START = 0 << 4;
		public const byte TYPE_HEAP_END = 1 << 4;
		public const byte TYPE_HEAP_OBJECT = 2 << 4;
		
		public readonly long Object; // the object as a difference from obj_base
		public readonly long Class; // the object MonoClass* as a difference from ptr_base
		public readonly ulong Size; // size of the object on the heap
		public readonly ulong[] RelOffset;
		public readonly long[] ObjectRefs; // object referenced as a difference from obj_base
		
		HeapEvent (BinaryReader reader, byte exinfo)
		{
			if (exinfo == TYPE_HEAP_START) {
				TimeDiff = reader.ReadULeb128 ();
			} else if (exinfo == TYPE_HEAP_END) {
				TimeDiff = reader.ReadULeb128 ();
			} else if (exinfo == TYPE_HEAP_OBJECT) {
				Object = reader.ReadSLeb128 ();
				Class = reader.ReadSLeb128 ();
				Size = reader.ReadULeb128 ();
				ulong num = reader.ReadULeb128 ();
				ObjectRefs = new long[num];
				RelOffset = new ulong[num];
				for (ulong i = 0; i < num; i++) {
					RelOffset [i] = reader.ReadULeb128 ();
					ObjectRefs [i] = reader.ReadSLeb128 ();
				}
			}
		}

		public static Event Read (BinaryReader reader, byte exinfo)
		{
			return new HeapEvent (reader, exinfo);
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
}
