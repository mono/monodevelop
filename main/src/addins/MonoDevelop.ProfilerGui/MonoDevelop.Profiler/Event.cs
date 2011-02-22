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
		Heap       = 6,
		Sample     = 7
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
		
		// extended type for TYPE_HEAP
		public const byte TYPE_HEAP_START = 0 << 4;
		public const byte TYPE_HEAP_END = 1 << 4;
		public const byte TYPE_HEAP_OBJECT = 2 << 4;
		public const byte TYPE_HEAP_ROOT   = 3 << 4;
		
		// extended type for TYPE_GC
		public const byte TYPE_GC_EVENT = 1 << 4;
		public const byte TYPE_GC_RESIZE = 2 << 4;
		public const byte TYPE_GC_MOVE = 3 << 4;
		public const byte TYPE_GC_HANDLE_CREATED = 4 << 4;
		public const byte TYPE_GC_HANDLE_DESTROYED = 5 << 4;
		
		// extended type for TYPE_METHOD
		public const byte TYPE_LEAVE     = 1 << 4;
		public const byte TYPE_ENTER     = 2 << 4;
		public const byte TYPE_EXC_LEAVE = 3 << 4;
		public const byte TYPE_JIT       = 4 << 4;
		
		// extended type for TYPE_EXCEPTION 
		public const byte TYPE_THROW = 0 << 4;
		public const byte TYPE_CLAUSE = 1 << 4;
		public const byte TYPE_EXCEPTION_BT = 1 << 7;
		
		// extended type for TYPE_SAMPLE
		public const byte TYPE_SAMPLE_HIT  = 0 << 4;
		public const byte TYPE_SAMPLE_USYM = 1 << 4;
		public const byte TYPE_SAMPLE_UBIN = 2 << 4;
		
		public static Event CreateEvent (BinaryReader reader,EventType type, byte extendedInfo)
		{
			switch (type) {
			case EventType.Alloc:
				return AllocEvent.Read (reader, extendedInfo); 
			case EventType.Exception:
				switch (extendedInfo & (TYPE_EXCEPTION_BT - 1)) {
				case TYPE_CLAUSE:
					return new ExceptionClauseEvent (reader);
				case TYPE_THROW:
					return new ExceptionThrowEvent (reader);
				default:
					throw new InvalidOperationException ("Unknown exception event type:" + (extendedInfo & (TYPE_EXCEPTION_BT - 1)));
				}
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
				switch (extendedInfo) {
				case TYPE_HEAP_START:
					return new HeapStartEvent (reader);
				case TYPE_HEAP_END:
					return new HeapEndEvent (reader);
				case TYPE_HEAP_OBJECT:
					return new HeapObjectEvent (reader);
				case TYPE_HEAP_ROOT:
					return new HeapRootEvent (reader);
				default:
					throw new InvalidOperationException ("Unknown heap event type:" + extendedInfo);
				}
			case EventType.Metadata:
				return MetadataEvent.Read (reader); 
			case EventType.Method:
				switch (extendedInfo) {
				case TYPE_LEAVE:
					return new MethodLeaveEvent (reader);
				case TYPE_ENTER:
					return new MethodEnterEvent (reader);
				case TYPE_EXC_LEAVE:
					return new MethodExcLeaveEvent (reader);
				case TYPE_JIT:
					return new MethodJitEvent (reader);
				default:
					throw new InvalidOperationException ("Unknown method event type:" + extendedInfo);
				}
			case EventType.Monitor:
				return MonitiorEvent.Read (reader, extendedInfo); 
			case EventType.Sample:
				switch (extendedInfo) {
				case TYPE_SAMPLE_HIT:
					return new SampleHitEvent (reader);
				case TYPE_SAMPLE_USYM:
					return new SampleUSymEvent (reader);
				case TYPE_SAMPLE_UBIN:
					return new SampleUBinEvent (reader);
				default:
					throw new InvalidOperationException ("Unknown sample event:" + extendedInfo);
				}
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
	
	#region Alloc
	public class AllocEvent : Event
	{
		public const byte TYPE_ALLOC_BT = 1 << 4;
		
		/// <summary>
		/// The class as a byte difference from ptr_base.
		/// </summary>
		public readonly long Ptr;
		
		/// <summary>
		/// The object address as a byte difference from obj_base.
		/// </summary>
		public readonly long Obj; 
		
		/// <summary>
		/// The size of the object in the heap.
		/// </summary>
		public readonly ulong Size;
		
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
	#endregion
	
	#region Gc
	public class ResizeGcEvent : Event
	{
		/// <summary>
		/// The new heap size.
		/// </summary>
		public readonly ulong HeapSize;
		
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
		public enum MonoGCEvent
		{
			Start,
			MarkStart,
			MarkEnd,
			ReclaimStart,
			ReclaimEnd,
			End,
			PreStopWorld,
			PostStopWorld,
			PreStartWorld,
			PostStartWorld
		}
		
		/// <summary>
		/// The GC event.
		/// </summary>
		public readonly MonoGCEvent GcEventType;
		public readonly ulong Generation;  // GC generation event refers to
		
		GcEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			GcEventType = (MonoGCEvent)reader.ReadULeb128 ();
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
		/// <summary>
		/// The num_objects object pointer differences from obj_base.
		/// </summary>
		public readonly long[] ObjAddr;
		
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
		/// <summary>
		/// The GC handle type (System.Runtime.InteropServices.GCHandleType).
		/// </summary>
		public readonly System.Runtime.InteropServices.GCHandleType HandleType;
		
		/// <summary>
		/// The GC handle value.
		/// </summary>
		public readonly ulong Handle;
		
		/// <summary>
		/// The object pointer differences from obj_base.
		/// </summary>
		public readonly long ObjAddr;
		
		HandleCreatedGcEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			HandleType = (System.Runtime.InteropServices.GCHandleType)reader.ReadULeb128 ();
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
		/// <summary>
		/// GC handle type (System.Runtime.InteropServices.GCHandleType).
		/// </summary>
		public readonly System.Runtime.InteropServices.GCHandleType HandleType;
		
		/// <summary>
		/// GC handle value
		/// </summary>
		public readonly ulong Handle;
		
		HandleDestroyedGcEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			HandleType = (System.Runtime.InteropServices.GCHandleType)reader.ReadULeb128 ();
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
	#endregion
	
	#region MetaData
	public abstract class MetadataEvent : Event
	{
		const byte TYPE_CLASS    = 1;
		const byte TYPE_IMAGE    = 2;
		const byte TYPE_ASSEMBLY = 3;
		const byte TYPE_DOMAIN   = 4;
		const byte TYPE_THREAD   = 5;
		
		/// <summary>
		/// The pointer of the metadata type depending on mtype.
		/// </summary>
		public long Pointer { get; private set; }
		
		public static new Event Read (BinaryReader reader)
		{
			ulong timeDiff = reader.ReadULeb128 ();
			byte type = reader.ReadByte ();
			long pointer = reader.ReadSLeb128 ();
			
			MetadataEvent result;
			switch (type) {
			case TYPE_CLASS:
				result = new MetaDataClassEvent (reader);
				break;
			case TYPE_IMAGE:
				result = new MetaDataImageEvent (reader);
				break;
			case TYPE_ASSEMBLY:
				result = new MetaDataAssemblyEvent ();
				break;
			case TYPE_DOMAIN:
				result = new MetaDataDomainEvent ();
				break;
			case TYPE_THREAD:
				result = new MetaDataThreadEvent (reader);
				break;
			default:
				throw new InvalidOperationException ("Unknown metadata event type:" + type);
			}
			result.TimeDiff = timeDiff;
			result.Pointer = pointer;
			return result;
		}
	}
	
	public class MetaDataClassEvent : MetadataEvent
	{
		/// <summary>
		/// MonoImage* as a pointer difference from ptr_base
		/// </summary>
		public readonly long Image;
		
		/// <summary>
		/// must be 0
		/// </summary>
		public readonly ulong Flags;
		
		/// <summary>
		/// full class/image file or thread name 
		/// </summary>
		public readonly string Name; 
		
		internal MetaDataClassEvent (BinaryReader reader)
		{
			Image = reader.ReadSLeb128 ();
			Flags = reader.ReadULeb128 ();
			Name = reader.ReadNullTerminatedString ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class MetaDataAssemblyEvent : MetadataEvent
	{
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class MetaDataDomainEvent : MetadataEvent
	{
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class MetaDataImageEvent : MetadataEvent
	{
		/// <summary>
		/// must be 0
		/// </summary>
		public readonly ulong Flags;
		
		/// <summary>
		/// full class/image file or thread name 
		/// </summary>
		public readonly string Name;
		
		internal MetaDataImageEvent (BinaryReader reader)
		{
			Flags = reader.ReadULeb128 ();
			Name = reader.ReadNullTerminatedString ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class MetaDataThreadEvent : MetadataEvent
	{
		/// <summary>
		/// must be 0
		/// </summary>
		public readonly ulong Flags;
		
		/// <summary>
		/// full class/image file or thread name 
		/// </summary>
		public readonly string Name;
		
		internal MetaDataThreadEvent (BinaryReader reader)
		{
			Flags = reader.ReadULeb128 ();
			Name = reader.ReadNullTerminatedString ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	#endregion
	
	#region Method
	public class MethodLeaveEvent : Event 
	{
		/// <summary>
		/// The MonoMethod* as a pointer difference from the last such pointer or the buffer method_base.
		/// </summary>
		public readonly long Method;
		
		public MethodLeaveEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			Method = reader.ReadSLeb128 ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class MethodEnterEvent : Event 
	{
		/// <summary>
		/// The MonoMethod* as a pointer difference from the last such pointer or the buffer method_base.
		/// </summary>
		public readonly long Method;
		
		public MethodEnterEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			Method = reader.ReadSLeb128 ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class MethodExcLeaveEvent : Event 
	{
		/// <summary>
		/// The MonoMethod* as a pointer difference from the last such pointer or the buffer method_base.
		/// </summary>
		public readonly long Method;
		
		public MethodExcLeaveEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			Method = reader.ReadSLeb128 ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class MethodJitEvent : Event 
	{
		/// <summary>
		/// The MonoMethod* as a pointer difference from the last such pointer or the buffer method_base.
		/// </summary>
		public readonly long Method;
		
		/// <summary>
		/// The pointer to the native code as a diff from ptr_base.
		/// </summary>
		public readonly long CodeAddress;
		
		/// <summary>
		/// The size of the generated code.
		/// </summary>
		public readonly ulong CodeSize;
		
		/// <summary>
		/// The full method name.
		/// </summary>
		public readonly string Name;
		
		public MethodJitEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			Method = reader.ReadSLeb128 ();
			CodeAddress = reader.ReadSLeb128 ();
			CodeSize = reader.ReadULeb128 ();
			Name = reader.ReadNullTerminatedString ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	#endregion
	
	#region Exception
	public class ExceptionClauseEvent : Event
	{
		/// <summary>
		/// finally/catch/fault/filter
		/// </summary>
		public readonly ulong ClauseType;
		
		/// <summary>
		/// the clause number in the method header
		/// </summary>
		public readonly ulong ClauseNum;
		
		/// <summary>
		/// MonoMethod* as a pointer difference from the last such pointer or the buffer method_base
		/// </summary>
		public readonly long Method;
		
		internal ExceptionClauseEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			ClauseType = reader.ReadULeb128 ();
			ClauseNum = reader.ReadULeb128 ();
			Method = reader.ReadSLeb128 ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class ExceptionThrowEvent : Event
	{
		/// <summary>
		/// the object that was thrown as a difference from obj_base If the TYPE_EXCEPTION_BT flag is set, a backtrace follows.
		/// </summary>
		public readonly long Object;
		
		internal ExceptionThrowEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			Object = reader.ReadSLeb128 ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	#endregion
	
	#region Monitor
	public class MonitiorEvent : Event
	{
		public const int MONO_PROFILER_MONITOR_CONTENTION = 1;
		public const int MONO_PROFILER_MONITOR_DONE = 2;
		public const int MONO_PROFILER_MONITOR_FAIL = 3;
		
		/// <summary>
		/// the lock object as a difference from obj_base
		/// </summary>
		public readonly long Object;
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
	#endregion
	
	#region Heap
	public class HeapStartEvent : Event
	{
		public HeapStartEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class HeapEndEvent : Event
	{
		public HeapEndEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class HeapObjectEvent : Event
	{
		/// <summary>
		/// the object as a difference from obj_base
		/// </summary>
		public readonly long Object;
		
		/// <summary>
		/// the object MonoClass* as a difference from ptr_base
		/// </summary>
		public readonly long Class;
		
		/// <summary>
		/// size of the object on the heap
		/// </summary>
		public readonly ulong Size;
		
		/// <summary>
		/// The relative offsets.
		/// </summary>
		public readonly ulong[] RelOffset;
		
		/// <summary>
		/// object referenced as a difference from obj_base
		/// </summary>
		public readonly long[] ObjectRefs;
		
		public HeapObjectEvent (BinaryReader reader)
		{
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
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	
	public class HeapRootEvent : Event
	{
		/// <summary>
		/// The number of major gcs.
		/// </summary>
		public readonly ulong NumGc;
		
		/// <summary>
		/// The object as a difference from obj_base.
		/// </summary>
		public readonly long[] Object;
		
		/// <summary>
		/// The root_type: MonoProfileGCRootType (profiler.h).
		/// </summary>
		public readonly ulong[] RootType;
		
		/// <summary>
		/// The extra info value.
		/// </summary>
		public readonly ulong[] ExtraInfo;
		
		public HeapRootEvent (BinaryReader reader)
		{
			ulong numRoots = reader.ReadULeb128 ();
			NumGc = reader.ReadULeb128 ();
			
			Object = new long [numRoots];
			RootType = new ulong [numRoots];
			ExtraInfo = new ulong [numRoots];
			
			for (ulong i = 0; i < numRoots; i++) {
				Object[i] = reader.ReadSLeb128 ();
				RootType[i] = reader.ReadULeb128 ();
				ExtraInfo[i] = reader.ReadULeb128 ();
			}
		}
		
		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	
	#endregion
	
	#region Sample
	public class SampleHitEvent : Event
	{
		public enum SampleType {
			Cycles = 1,
			Instructions = 2,
			CacheMisses = 3,
			CacheRefs = 4,
			Branches = 5,
			BranchMisses = 6
		}
		
		/// <summary>
		/// The type of sample (SAMPLE_*).
		/// </summary>
		public SampleType Type { get; private set; }
		
		/// <summary>
		/// Nanoseconds since startup (note: different from other timestamps!).
		/// </summary>
		public readonly ulong TimeStamp;
		
		/// <summary>
		/// The instruction pointer as difference from ptr_base.
		/// </summary>
		public readonly long[] Ip;
		
		
		public SampleHitEvent (BinaryReader reader)
		{
			Type = (SampleType)reader.ReadULeb128 ();
			TimeStamp = reader.ReadULeb128 ();
			ulong num = reader.ReadULeb128 ();
			Ip = new long[num];
			for (ulong i = 0; i < num; i++) {
				Ip[i] = reader.ReadSLeb128 ();
			}
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class SampleUSymEvent : Event
	{
		/// <summary>
		/// The symbol address as a difference from ptr_base.
		/// </summary>
		public readonly long Address;
		
		/// <summary>
		/// The symbol size (may be 0 if unknown).
		/// </summary>
		public readonly ulong Size;
		
		/// <summary>
		/// The symbol name.
		/// </summary>
		public readonly string Name;
		
		public SampleUSymEvent (BinaryReader reader)
		{
			Address = reader.ReadSLeb128 ();
			Size = reader.ReadULeb128 ();
			Name = reader.ReadNullTerminatedString ();
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	public class SampleUBinEvent : Event
	{
		/// <summary>
		/// The address where binary has been loaded.
		/// </summary>
		public readonly long Address;
		
		/// <summary>
		/// The file offset of mapping (the same file can be mapped multiple times).
		/// </summary>
		public readonly ulong Offset;
		
		/// <summary>
		/// The memory size.
		/// </summary>
		public readonly ulong Size;
		
		/// <summary>
		/// The binary name.
		/// </summary>
		public readonly string Name;
		
		public SampleUBinEvent (BinaryReader reader)
		{
			TimeDiff = reader.ReadULeb128 ();
			Address = reader.ReadSLeb128 ();
			Offset = reader.ReadULeb128 ();
			Size = reader.ReadULeb128 ();
			Name = reader.ReadNullTerminatedString ();
		}

		public override object Accept (EventVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	#endregion
}
