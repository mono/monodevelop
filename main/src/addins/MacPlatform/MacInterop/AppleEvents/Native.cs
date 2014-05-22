// 
// Structs.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Runtime.InteropServices;

namespace MonoDevelop.MacInterop.AppleEvents
{
	
/*
	//Apple event manager data types
	typedef ResType                         DescType;
	typedef Ptr                             AEDataStorageType;
	typedef AEDataStorageType *             AEDataStorage;
*/
	
	/*
	Useful references:
	
	Apple Event Manager
	http://developer.apple.com/legacy/mac/library/documentation/Carbon/reference/Apple_Event_Manager/apple_event_manager.pdf
	
	AEGizmo, AEBuildAppleEvent and Mac OS X 10.4
	http://www.wincent.com/a/about/wincent/weblog/archives/2005/05/aegizmo_aebuild.php
	
	AEVTBuilder
	http://www.cocoadev.com/index.pl?AEVTBuilder
	
	AEBuildAppleEvent
	http://www.cocoadev.com/index.pl?AEBuildAppleEvent
	
	AppScript
	http://appscript.sourceforge.net/
	
	Scripting Bridge Criticisms
	http://www.cocoadev.com/index.pl?ScriptingBridgeCriticisms
	
	Using Scripting Bridge
	http://developer.apple.com/library/mac/documentation/Cocoa/Conceptual/ScriptingBridgeConcepts/ScriptingBridgeConcepts.pdf
	
	*/
	unsafe class Native
	{
		const string AELib = Carbon.CarbonLib;
		
		//********************* AEDataModel.h ********************** 
		
		//for Mach binaries the UPP is identical to the ProcPtr
		public delegate OSErr AECoerceDescUPP (ref AEDesc fromDesc, DescType toType, uint refConst, out AEDesc toDesc);
		public delegate OSErr AECoercePtrUPP (DescType typeCode, IntPtr dataPtr, Size dataSize, DescType toType,
			uint refConst, out AEDesc result);
		public delegate void AEDisposeExternalUPP (IntPtr dataPtr, Size dataLength, uint refcon);
		public delegate OSErr AEEventHandlerUPP (ref AppleEvent evt, ref AppleEvent reply, uint refConst);
		
		[DllImport (AELib)]
		public static extern OSErr AEInstallCoercionHandler (DescType fromType, DescType toType, AECoerceDescUPP handler,
			uint handlerRefcon, bool fromTypeIsDesc, bool isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AEInstallCoercionHandler (DescType fromType, DescType toType, AECoercePtrUPP handler,
			uint handlerRefcon, bool fromTypeIsDesc, bool isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AERemoveCoercionHandler (DescType fromType, DescType toType, AECoerceDescUPP handler,
			bool isSysHandler);

		[DllImport (AELib)]
		public static extern OSErr AERemoveCoercionHandler (DescType fromType, DescType toType, AECoercePtrUPP handler,
			bool isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AEGetCoercionHandler (DescType fromType, DescType toType, out IntPtr handler,
			out uint handlerRefcon, out bool fromTypeIsDesc, bool isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AECoercePtr (DescType typeCode, IntPtr data, Size dataSize, DescType toType,
			out AEDesc result);
		
		[DllImport (AELib)]
		public static extern OSErr AECoerceDesc (ref AEDesc desc, DescType toType, out AEDesc result);
		
		[DllImport (AELib)]
		public static extern void AEInitializeDesc (ref AEDesc desc);
		
		public static void AEInitializeDescInline (ref AEDesc desc)
		{
			desc.descriptorType = (int) AEDescriptorType.Null;
			desc.dataHandle = IntPtr.Zero;
		}
		
		[DllImport (AELib)]
		public static extern OSErr AECreateDesc (DescType typeCode, IntPtr dataPtr, Size dataSize, out AEDesc result);

		[DllImport (AELib)]
		public static extern OSErr AEDisposeDesc (ref AEDesc desc);
		
		[DllImport (AELib)]
		public static extern OSErr AEDuplicateDesc (ref AEDesc desc, out AEDesc result);
		
		[DllImport (AELib)]
		public static extern OSStatus AECreateDescFromExternalPtr (OSType descriptorType, IntPtr dataPtr, Size dataLength,
			AEDisposeExternalUPP disposeCallback, uint disposeRefcon, out AEDesc result);
		
		[DllImport (AELib)]
		public static extern OSErr AECreateList (IntPtr factoringPtr, Size factoredSize, bool isRecord, out AEDescList result);
		
		[DllImport (AELib)]
		public static extern OSErr AECountItems (ref AEDescList descList, out Size count);

		[DllImport (AELib)]
		public static extern OSErr AEPutPtr (ref AEDescList descList, Size index, DescType typeCode, IntPtr dataPtr,
			Size dataSize);
		
		[DllImport (AELib)]
		public static extern OSErr AEPutDesc (ref AEDescList descList, Index index, out AEDesc desc);
		
		[DllImport (AELib)]
		public static extern OSErr AEGetNthPtr (ref AEDescList descList, Index index, DescType desiredType,
			ref AEKeyword keyword, ref DescType typeCode, IntPtr dataPtr, Size maximumSize, out Size actualSize);
		
		[DllImport (AELib)]
		public static extern OSErr AEGetNthDesc (ref AEDescList descList, Index index, DescType desiredType,
			ref AEKeyword keyword, out AEDesc result);
		
		[DllImport (AELib)]
		public static extern OSErr SizeOfNthItem (ref AEDescList descList, Index index, ref DescType typeCode,
			out Size dataSize);
		
		[DllImport (AELib)] static extern OSErr AEGetArray (ref AEDescList descList, AEArrayType arrayType,
			ref AEArrayData array, Size maximumSize, ref DescType itemType, Size itemSize, out Size itemCount);

		[DllImport (AELib)]
		public static extern OSErr 
		AEPutArray (ref AEDescList descList, AEArrayType arrayType, ref AEArrayData array, DescType itemType,
				Size itemSize, Size itemCount);

		[DllImport (AELib)]
		public static extern OSErr AEDeleteItem (ref AEDescList descList, Index index);
		
		[DllImport (AELib)]
		public static extern bool AECheckIsRecord (ref AEDesc desc);
		
		[DllImport (AELib, EntryPoint="AEPutParamPtr")]
		public static extern OSErr AEPutKeyPtr (ref AERecord record, AEKeyword keyword, DescType typeCode,
			IntPtr dataPtr, Size dataSize);
		
		[DllImport (AELib, EntryPoint="AEPutParamDesc")]
		public static extern OSErr AEPutKeyDesc (ref AERecord record, AEKeyword keyword, ref AEDesc desc);
		
		[DllImport (AELib, EntryPoint="AEGetParamPtr")]
		public static extern OSErr AEGetKeyPtr (ref AERecord record, AEKeyword keyword, DescType desiredType,
			ref DescType actualType, IntPtr dataPtr, Size maximumSize, out Size actualSize);
		
		[DllImport (AELib, EntryPoint="AEGetParamDesc")]
		public static extern OSErr AEGetKeyDesc (ref AERecord record, AEKeyword keyword, DescType desiredType,
			out AEDesc result);

		[DllImport (AELib, EntryPoint="SizeOfParam")]
		public static extern OSErr SizeOfKeyDesc (ref AERecord record, AEKeyword keyword,
			ref DescType typeCode, out Size dataSize);
		
		[DllImport (AELib, EntryPoint="AEDeleteParam")]
		public static extern OSErr AEDeleteKeyDesc (ref AERecord record, AEKeyword keyword);
		
		[DllImport (AELib)]
		public static extern OSErr AECreateAppleEvent (AEEventClass eventClass, AEEventID eventID, ref AEAddressDesc target,
			AEReturnID returnID, AETransactionID transactionID, out AppleEvent result);

		[DllImport (AELib)]
		public static extern OSErr AEPutParamPtr (ref AppleEvent evt, AEKeyword keyword, DescType typeCode,
			IntPtr dataPtr, Size dataSize);
		
		[DllImport (AELib)]
		public static extern OSErr AEPutParamDesc (ref AppleEvent evt, AEKeyword keyword, ref AEDesc desc);
		
		[DllImport (AELib)]
		public static extern OSErr AEGetParamPtr ( ref AppleEvent evt, AEKeyword keyword, DescType desiredType, 
			out DescType actualType, IntPtr dataPtr, Size maximumSize, out Size actualSize);
		
		[DllImport (AELib)]
		public static extern OSErr AEGetParamDesc (ref AppleEvent evt, AEKeyword keyword, DescType desiredType,
			out AEDesc result);

		[DllImport (AELib)]
		public static extern OSErr SizeOfParam (ref AppleEvent evt, AEKeyword keyword,
			out DescType typeCode, out Size dataSize);

		[DllImport (AELib)]
		public static extern OSErr AEDeleteParam (ref AppleEvent evt, AEKeyword keyword);

		[DllImport (AELib)]
		public static extern OSErr AEGetAttributePtr (ref AppleEvent evt, AEKeyword keyword, DescType desiredType,
			out DescType typeCode, IntPtr dataPtr, Size maximumSize, out Size actualSize);
		
		[DllImport (AELib)]
		public static extern OSErr AEGetAttributeDesc (ref AppleEvent evt, AEKeyword keyword, DescType desiredType,
			out AEDesc result);
		
		[DllImport (AELib)]
		public static extern OSErr SizeOfAttribute (ref AppleEvent evt, AEKeyword keyword,
			out DescType typeCode, out Size dataSize);
		
		[DllImport (AELib)]
		public static extern OSErr AEPutAttributePtr (ref AppleEvent evt, AEKeyword keyword, DescType typeCode,
			IntPtr dataPtr, Size dataSize);
		
		[DllImport (AELib)]
		public static extern OSErr AEPutAttributeDesc (ref AppleEvent evt, AEKeyword keyword, ref AEDesc desc);
		
		[DllImport (AELib)]
		public static extern Size SizeOfFlattenedDesc (ref AEDesc desc);
		
		[DllImport (AELib)]
		public static extern OSStatus AEFlattenDesc (ref AEDesc desc, IntPtr buffer, Size bufferSize, out Size actualSize);
		
		[DllImport (AELib)]
		public static extern OSStatus AEUnflattenDesc (IntPtr buffer, out AEDesc result);
		
		[DllImport (AELib)]
		public static extern OSErr AEGetDescData (ref AEDesc desc, IntPtr dataPtr, Size maximumSize);
		
		[DllImport (AELib)]
		public static extern Size AEGetDescDataSize (ref AEDesc desc);
		
		[DllImport (AELib)]
		public static extern OSErr AEReplaceDescData (DescType typeCode, IntPtr dataPtr, Size dataSize, ref AEDesc desc);
		
		[DllImport (AELib)]
		public static extern OSStatus AEGetDescDataRange (ref AEDesc dataDesc, IntPtr buffer, Size offset, Size length);
		
		public struct AEDesc
		{
			public int  descriptorType; //DescType
			public IntPtr dataHandle; //AEDataStorage
		}
		
		//is an AEDesc with address data
		public struct AEAddressDesc
		{
			public int  descriptorType; //DescType
			public IntPtr dataHandle; //AEDataStorage
			
			public static implicit operator AEDesc (AEAddressDesc a) { return *((AEDesc*)&a); }
			public static explicit operator AEAddressDesc (AEDesc a) { return *((AEAddressDesc*)&a); }
		}
		
		//is a special AEDesc
		public struct AEDescList
		{
			public int  descriptorType; //DescType
			public IntPtr dataHandle; //AEDataStorage
			
			public static implicit operator AEDesc (AEDescList a) { return *((AEDesc*)&a); }
			public static explicit operator AEDescList (AEDesc a) { return *((AEDescList*)&a); }
		}
		
		//is an AEDescList with keys
		public struct AERecord
		{
			public int  descriptorType; //DescType
			public IntPtr dataHandle; //AEDataStorage
			
			public static implicit operator AEDescList (AERecord a) { return *((AEDescList*)&a); }
			public static explicit operator AERecord (AEDescList a) { return *((AERecord*)&a); }
		}
		
		//is an AERecord with event
		public struct AppleEvent
		{
			public int  descriptorType; //DescType
			public IntPtr dataHandle; //AEDataStorage
			
			public static implicit operator AERecord (AppleEvent a) { return *((AERecord*)&a); }
			public static explicit operator AppleEvent (AERecord a) { return *((AppleEvent*)&a); }
		}
		
		public struct AEKeyDesc
		{
			public int descKey; //AEKeyword
			public AEDesc descContent;
		}
		
		public enum AEArrayType : sbyte
		{
			Data = 0,
			Packed = 1,
			Handle = 2,
			Desc = 3,
			KeyDesc = 4,
		}
		
		public struct AEArrayData
		{
			public IntPtr Ptr;
			
			public short AsData {
				get {
					unsafe { return *((short*)Ptr); }
				}
			}
			
			public byte AsPacked {
				get {
					unsafe { return *((byte*)Ptr); }
				}
			}
			
			public IntPtr AsHandle {
				get {
					unsafe { return *((IntPtr*)Ptr); }
				}
			}
			
			public AEDesc AsDesc {
				get {
					unsafe { return *((AEDesc*)Ptr); }
				}
			}
			
			public AEKeyDesc AsKeyDesc {
				get {
					unsafe { return *((AEKeyDesc*)Ptr); }
				}
			}
		}
		
		public struct Size
		{
			IntPtr value;
			
			public Size (int a)
			{
				value = (IntPtr) a;
			}
			
			public Size (long a)
			{
				value = (IntPtr) a;
			}
			
			public static implicit operator long (Size a)
			{
				return a.value.ToInt64 ();
			}
			
			public static implicit operator int (Size a)
			{
				return a.value.ToInt32 ();
			}
			
			public static explicit operator Size (int a)
			{
				return new Size (a);
			}
			
			public static explicit operator Size (long a)
			{
				return new Size (a);
			}
		}
			
		public struct Index
		{
			IntPtr value;
			
			public Index (int a)
			{
				value = (IntPtr) a;
			}
			
			public Index (long a)
			{
				value = (IntPtr) a;
			}
			
			public static implicit operator long (Index a)
			{
				return a.value.ToInt64 ();
			}
			
			public static implicit operator int (Index a)
			{
				return a.value.ToInt32 ();
			}
			
			public static explicit operator Index (int a)
			{
				return new Index (a);
			}
			
			public static explicit operator Index (long a)
			{
				return new Index (a);
			}
		}
		
		public struct AEKeyword
		{
			public int FourCC;
		}
		
		public struct AEEventClass
		{
			public int FourCC;
			
			public AEEventClass (int fourcc)
			{
				this.FourCC = fourcc;
			}
			
			public static implicit operator AEEventClass (int a)
			{
				return new AEEventClass (a);
			}
			
			public static AEEventClass Core { get { return 0; } } //'aevt'
		}
		
		public struct AEEventID
		{
			public int FourCC;
			
			public AEEventID (int fourcc)
			{
				this.FourCC = fourcc;
			}
			
			public static implicit operator AEEventID (int a)
			{
				return new AEEventID (a);
			}
			
			public static AEEventID OpenApplication { get { return 0; } } // 'oapp',
			public static AEEventID OpenDocuments   { get { return 0; } } // 'odoc',
			public static AEEventID PrintDocuments  { get { return 0; } } // 'pdoc',
			public static AEEventID OpenContents    { get { return 0; } } // 'ocon',
			public static AEEventID QuitApplication { get { return 0; } } // 'quit',
			public static AEEventID Answer          { get { return 0; } } // 'ansr',
			public static AEEventID ApplicationDied { get { return 0; } } // 'obit',
			public static AEEventID ShowPreferences { get { return 0; } } // 'pref'
		}
		
		public struct AEReturnID
		{
			public short Value;
			
			public AEReturnID (short value) { this.Value = value; }
			
			///<summary>Make AECreateAppleEvent create session-unique value for returnID</summary>
			public AEReturnID AutoGenerate { get { return new AEReturnID (-1); } }
		}
		
		public struct AETransactionID
		{
			public int Value;
			
			public AETransactionID (int value) { this.Value = value; }
			
			/// <summary>Not using transactions</summary>
			public AETransactionID Any { get { return new AETransactionID (0); } }
		}
		
		public enum OSErr
		{
			Ok = 0,
		}
		
		public enum OSStatus
		{
			Ok = 0,
		}

		public enum AEDescriptorType
		{
			Boolean = 0, //'bool',
			
			///<summary>Big-endian UTF-16 with optional BOM, or little-endian UTF16 with required BOM</summary>
			///<remarks>No length byte or null termination</remarks>
			Utf16ExternalRepresentation = 0,//'ut16',
			
			/// <summary>UTF8</summary>
			///<remarks>No length byte or null termination</remarks>
			Utf8Text = 0,//'utf8'
			
			SInt16 = 0,//'shor',
			UInt16 = 0,//'ushr',
			SInt32 = 0,//'long',
			UInt32 = 0,//'magn',
			SInt64 = 0,//'comp',
			UInt64 = 0,//'ucom',
			FloatingPointIeee32Bit = 0,//'sing',
			FloatingPointIeee64Bit = 0,//'doub',
			FloatingPoint128Bit = 0,//'ldbl',
			DecimalStruct = 0,//'decm'
			
			AEList                    = 0,//'list',
			AERecord                  = 0,//'reco',
			AppleEvent                = 0,//'aevt',
			EventRecord               = 0,//'evrc',
			True                      = 0,//'true',
			False                     = 0,//'fals',
			/// <summary>AliasPtr, from a valid AliasHandle</summary>
			Alias                     = 0,//'alis',
			Enumerated                = 0,//'enum',
			/// <summary>OSType</summary>
			Type                      = 0,//'type',
			AppParameters             = 0,//'appa',
			Property                  = 0,//'prop',
			FSRef                     = 0,//'fsrf',
			/// <summary>UTF8-encoded full path with native path separators</summary>
			FileURL                   = 0,//'furl',
			/// <summary>Bytes of a CFURLBookmarkData</summary>
			BookmarkData              = 0,//'bmrk',
			/// <summary>OSType</summary>
			Keyword                   = 0,//'keyw',
			SectionH                  = 0,//'sect',
			WildCard                  = 0,//'****',
			/// <summary>OSType</summary>
			ApplSignature             = 0,//'sign',
			QDRectangle               = 0,//'qdrt',
			Fixed                     = 0,//'fixd',
			ProcessSerialNumber       = 0,//'psn ',
			ApplicationUrl            = 0,//'aprl',
			/// <summary>Null or nonexistent data</summary>
			Null                      = 0,//'null'
			
			CFAttributedStringRef     = 0,//'cfas',
			CFMutableAttributedStringRef = 0,//'cfaa',
			CFStringRef               = 0,//'cfst',
			CFMutableStringRef        = 0,//'cfms',
			CFArrayRef                = 0,//'cfar',
			CFMutableArrayRef         = 0,//'cfma',
			CFDictionaryRef           = 0,//'cfdc',
			CFMutableDictionaryRef    = 0,//'cfmd',
			CFNumberRef               = 0,//'cfnb',
			CFBooleanRef              = 0,//'cftf',
			CFTypeRef                 = 0,//'cfty'
			
			KernelProcessID           = 0,//'kpid',
			MachPort                  = 0,//'port'
			
			ApplicationBundleID       = 0,//'bund'
		}
		
		public enum AEAttribute
		{
			TransactionID          = 0,//'tran',
			ReturnID               = 0,//'rtid',
			EventClass             = 0,//'evcl',
			EventID                = 0,//'evid',
			Address                = 0,//'addr',
			OptionalKeyword        = 0,//'optk',
			///<summary>AEDescriptorType.SInt32</summary>
			Timeout                = 0,//'timo',
			///<summary>Read-only, set in AESend</summary>
			InteractLevel          = 0,//'inte',
			///<summary>Read-only, AEDescriptorType.SInt16</summary>
			EventSource            = 0,//'esrc'
			///<summary>Read-only</summary>
			MissedKeyword          = 0,//'miss'
			OriginalAddress        = 0,//'from'
			AcceptTimeout          = 0,//'actm'
			///<summary>Whether reply was requested, Boolean</summary>
			ReplyRequested         = 0,//'repq'
			///<summary>Read-only, AEDescriptorType.SInt32</summary>
			SenderEuid             = 0,//'seid'
			///<summary>Read-only, AEDescriptorType.SInt32</summary>
			SenderEgid             = 0,//'sgid'
			///<summary>Read-only, AEDescriptorType.SInt32</summary>
			SenderUid              = 0,//'uids'
			///<summary>Read-only, AEDescriptorType.SInt32</summary>
			SenderGid              = 0,//'gids'
			///<summary>Read-only, AEDescriptorType.SInt32</summary>
			SenderPid              = 0,//'spid'
		}
		
		public enum AEDescListFactor
		{
			None = 0,
			Type         = 4,
			TypeAndSize  = 8
		}
		
		public enum AESendPriority
		{
			/// <summary>Post message at end of event queue</summary>
			Normal = 0x00000000,
			/// <summary>Post message at front of event queue</summary>
			High = 0x00000001
		}
		
		[Flags]
		public enum AESendMode
		{
			/// <summary>Sender doesn't want reply</summary>
			NoReply                    = 0x00000001,
			/// <summary>Sender wants reply but won't wait</summary>
			QueueReply                 = 0x00000002,
			/// <summary>Sender wants reply and will wait</summary>
			WaitReply                  = 0x00000003,
			/// <summary>Don't reconnect on sessClosedErr</summary>
			DontReconnect              = 0x00000080,
			/// <summary>Sender wants receipt</summary>
			WantReceipt                = 0x00000200,
			/// <summary>Server shouldn't interact with user</summary>
			NeverInteract              = 0x00000010,
			/// <summary>Server may interact with user</summary>
			CanInteract                = 0x00000020,
			/// <summary>Server should interact with user if appropriate</summary>
			AlwaysInteract             = 0x00000030,
			/// <summary></summary>
			CanSwitchLayer             = 0x00000040,
			/// <summary></summary>
			DontRecord                 = 0x00001000,
			/// <summary></summary>
			DontExecute                = 0x00002000,
			/// <summary>Allow processing other events while waiting for reply</summary>
			ProcessNonReplyEvents      = 0x00008000,
			/// <summary></summary>
			DoNotAutomaticallyAddAnnotationsToEvent = 0x00010000,
		}
		
		// ********************** AppleEvents.h ********************
		
		public enum AEEventParameterKeys
		{
			DirectObject               = 0,//'----',
			ErrorNumber                = 0,//'errn',
			ErrorString                = 0,//'errs',
			ProcessSerialNumber        = 0,//'psn ',
			PreDispatch                = 0,//'phac',
			SelectProc                 = 0,//'selh',
			AERecorderCount            = 0,//'recr',
			AEVersion                  = 0,//'vers',
		}
		
		public enum AERecording
		{
			Start       = 0,//'reca',
			Stop        = 0,//'recc',
			NotifyStart = 0,//'rec1',
			NotifyStop  = 0,//'rec0',
			Notify      = 0,//'recr',
		}
		
		// keyEventSourceAttr treats this as a short
		enum AEEventSource : sbyte
		{
			UnknownSource = 0,
			DirectCall    = 1,
			SameProcess   = 2,
			LocalProcess  = 3,
			RemoteProcess = 4
		}
		
		[DllImport (AELib)]
		public static extern OSErr AEInstallEventHandler (AEEventClass eventClass, AEEventID eventID,
			AEEventHandlerUPP handler, uint handlerRefcon, bool isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AERemoveEventHandler (AEEventClass eventClass, AEEventID eventID,
			AEEventHandlerUPP handler, bool isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AEGetEventHandler (AEEventClass eventClass, AEEventID eventID,
			out AEEventHandlerUPP handler, out uint handlerRefcon, Boolean isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AEInstallSpecialHandler (AEKeyword functionClass, AEEventHandlerUPP handler,
			Boolean isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AERemoveSpecialHandler (AEKeyword functionClass, AEEventHandlerUPP handler,
			Boolean isSysHandler);

		[DllImport (AELib)]
		public static extern OSErr AEGetSpecialHandler (AEKeyword functionClass, out AEEventHandlerUPP handler,
			Boolean isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AEManagerInfo (AEKeyword keyWord, out IntPtr /* long */ result);
		
		//extern const CFStringRef kAERemoteProcessURLKey;
		//extern const CFStringRef kAERemoteProcessNameKey;
		//extern const CFStringRef kAERemoteProcessUserIDKey;
		//extern const CFStringRef kAERemoteProcessProcessIDKey;
		
		public struct AERemoteProcessResolverContext
		{
			public Index Version; //CFIndex
			public IntPtr Info;
			public IntPtr Retain; // CFAllocatorRetainCallBack
			public IntPtr Release; //CFAllocatorReleaseCallBack
			public IntPtr CopyDescription; //CFAllocatorCopyDescriptionCallBack
		}
		
		public struct AERemoteProcessResolverRef { public IntPtr Ptr; }
		public struct CFAllocatorRef { public IntPtr Ptr; }
		public struct CFURLRef { public IntPtr Ptr; }
		public struct CFArrayRef { public IntPtr Ptr; }
		public struct CFStringRef { public IntPtr Ptr; }
		public struct CFRunLoopRef { public IntPtr Ptr; }
		
		public struct CFStreamError
		{
			public Index Domain; //CFIndex
    		public int Error;
		}
		
		[DllImport (AELib)]
		public static extern AERemoteProcessResolverRef AECreateRemoteProcessResolver (CFAllocatorRef allocator,
			CFURLRef url);
		
		[DllImport (AELib)]
		public static extern void AEDisposeRemoteProcessResolver (AERemoteProcessResolverRef resRef);
		
		[DllImport (AELib)]
		public static extern CFArrayRef AERemoteProcessResolverGetProcesses (AERemoteProcessResolverRef resRef,
			out CFStreamError outError);
		
		public delegate void AERemoteProcessResolverCallback (AERemoteProcessResolverRef resRef, IntPtr info);

		[DllImport (AELib)]
		public static extern void AERemoteProcessResolverScheduleWithRunLoop (AERemoteProcessResolverRef resRef,
			CFRunLoopRef runLoop, CFStringRef runLoopMode, AERemoteProcessResolverCallback callback,
			ref AERemoteProcessResolverContext ctx);
		
		//******************** AEHelpers.h **********************
		
		public class AppleEventTimeout
		{
			/// <summary>The default timeout of the Event Manager</summary>
			public const int Default = -1;
			
			/// <summary>Wait indefinitely</summary>
			public const int NoTimeOut = -2;
		}
		
		public enum AEBuildErrorCode : uint
		{
			NoErr            = 0,
			BadToken         = 1,
			BadEOF           = 2,
			NoEOF            = 3,
			BadNegative      = 4,
			MissingQuote     = 5,
			BadHex           = 6,
			OddHex           = 7,
			NoCloseHex       = 8,
			UncoercedHex     = 9,
			NoCloseString    = 10,
			BadDesc          = 11,
			BadData          = 12,
			NoCloseParen     = 13,
			NoCloseBracket   = 14,
			NoCloseBrace     = 15,
			NoKey            = 16,
			NoColon          = 17,
			CoercedList      = 18,
			UncoercedDoubleAt = 19
		}
		
		public struct AEBuildError
		{
			public AEBuildErrorCode Error;
			public uint ErrorPos;
		}
		
		[DllImport (AELib)]
		public static extern OSStatus AEBuildDesc (ref AEDesc dst, out AEBuildError error, string src, __arglist);
		
		[DllImport (AELib)]
		public static extern OSStatus 
		AEBuildParameters (ref AppleEvent evt, out AEBuildError error, string format, __arglist);
		
		[DllImport (AELib)]
		public static extern OSStatus AEBuildAppleEvent (AEEventClass theClass, AEEventID theID, DescType addressType,
			IntPtr addressData, Size addressLength, AEReturnID returnID, AETransactionID transactionID,
			out AppleEvent result, out AEBuildError error, string paramsFmt, __arglist);
		
		[DllImport (AELib)]
		public static extern OSStatus AEPrintDescToHandle (ref AEDesc desc, out IntPtr resultHandle);
		
		public struct AEStreamRef
		{
			public IntPtr Ptr;
		}
		
		[DllImport (AELib)]
		public static extern AEStreamRef AEStreamOpen ();
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamClose (AEStreamRef streamRef, out AEDesc desc);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamOpenDesc (AEStreamRef streamRef, DescType newType);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamWriteData (AEStreamRef streamRef, IntPtr data, Size length);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamCloseDesc (AEStreamRef streamRef);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamWriteDesc (AEStreamRef streamRef, DescType newType, IntPtr data,
			Size length);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamWriteAEDesc (AEStreamRef streamRef, ref AEDesc desc);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamOpenList (AEStreamRef streamRef);

		[DllImport (AELib)]
		public static extern OSStatus AEStreamCloseList (AEStreamRef streamRef);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamOpenRecord (AEStreamRef streamRef, DescType newType);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamSetRecordType (AEStreamRef streamRef, DescType newType);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamCloseRecord (AEStreamRef streamRef);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamWriteKeyDesc (AEStreamRef streamRef, AEKeyword key, DescType newType,
			IntPtr data, Size length);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamOpenKeyDesc (AEStreamRef streamRef, AEKeyword key, DescType newType);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamWriteKey (AEStreamRef streamRef, AEKeyword key);
		
		[DllImport (AELib)]
		public static extern AEStreamRef AEStreamCreateEvent (AEEventClass clazz, AEEventID id, DescType targetType,
			IntPtr targetData, Size targetLength, AEReturnID returnID, AETransactionID transactionID);
		
		[DllImport (AELib)]
		public static extern AEStreamRef AEStreamOpenEvent (ref AppleEvent evt);
		
		[DllImport (AELib)]
		public static extern OSStatus AEStreamOptionalParam (AEStreamRef streamRef, AEKeyword key);
		
		
		//*********************** AEMach.h ********************
		
		public const int keyReplyPortAttr = 0;//'repp';
		
		[DllImport (AELib)]
		public static extern uint /* mach_port_t */ AEGetRegisteredMachPort ();
		
		[DllImport (AELib)]
		public static extern OSStatus AEDecodeMessage (IntPtr /* ref mach_msg_header_t */  header, ref AppleEvent evt,
			out AppleEvent reply);
		
		[DllImport (AELib)]
		public static extern OSStatus AEProcessMessage (IntPtr /* ref mach_msg_header_t */ header);
		
		[DllImport (AELib)]
		public static extern OSStatus AESendMessage (ref AppleEvent evnt, out AppleEvent reply, AESendMode sendMode, 
			Size timeOutInTicks);
		
		
		//******************* AEObjects.h **************
		
		public static class AEOperator
		{
			public static readonly int And = 0x414e4420;//'AND '
			public static readonly int Or  = 0x4f522020;//'OR  '
			public static readonly int Not = 0x4e4f5420;//'NOT '
		}
		
		public static class AEOrdinal
		{
			public static readonly int First    = 0x66697273;//'firs'
			public static readonly int Last     = 0x6c617374;//'last'
			public static readonly int Middle   = 0x6d696464;//'midd'
			public static readonly int Any      = 0x616e7920;//'any '
			public static readonly int All      = 0x616c6c20;//'all '
			public static readonly int Next     = 0x6e657874;//'next'
			public static readonly int Previous = 0x70726576;//'prev'
		}
		
		public static class AEObjectKeywords
		{
			// KEYWORD CONSTANT
			public static readonly int keyAECompOperator             = 0x72656c6f;//'relo'
			public static readonly int keyAELogicalTerms             = 0x7465726d;//'term'
			public static readonly int keyAELogicalOperator          = 0x6c6f6763;//'logc'
			public static readonly int keyAEObject1                  = 0x6f626a31;//'obj1'
			public static readonly int keyAEObject2                  = 0x6f626a32;//'obj2'
			// for getting fields out of object specifier records
			public static readonly int keyAEDesiredClass             = 0x77616e74;//'want'
			public static readonly int keyAEContainer                = 0x66726f6d;//'from'
			public static readonly int keyAEKeyForm                  = 0x666f726d;//'form'
			public static readonly int keyAEKeyData                  = 0x73656c64;//'seld'
			
			// for getting fields out of Range specifier records
			public static readonly int keyAERangeStart               = 0x73746172;//'star'
			public static readonly int keyAERangeStop                = 0x73746f70;//'stop'
			// special handler selectors for OSL Callbacks.
			public static readonly int keyDisposeTokenProc           = 0x78746f6b;//'xtok'
			public static readonly int keyAECompareProc              = 0x636d7072;//'cmpr'
			public static readonly int keyAECountProc                = 0x636f6e74;//'cont'
			public static readonly int keyAEMarkTokenProc            = 0x6d6b6964;//'mkid'
			public static readonly int keyAEMarkProc                 = 0x6d61726b;//'mark'
			public static readonly int keyAEAdjustMarksProc          = 0x61646a6d;//'adjm'
			public static readonly int keyAEGetErrDescProc           = 0x696e6463;//'indc'
			
			// VALUE and TYPE CONSTANTS
			// possible values for the keyAEKeyForm field of an object specifier
			public static readonly int formAbsolutePosition          = 0x696e6478;//'indx'
			public static readonly int formRelativePosition          = 0x72656c65;//'rele'
			public static readonly int formTest                      = 0x74657374;//'test'
			public static readonly int formRange                     = 0x72616e67;//'rang'
			public static readonly int formPropertyID                = 0x70726f70;//'prop'
			public static readonly int formName                      = 0x6e616d65;//'name'
			public static readonly int formUniqueID                  = 0x49442020;//'ID  '
            // relevant types (some of these are often paired with forms above).
			public static readonly int typeObjectSpecifier           = 0x6f626a20;//'obj '
			public static readonly int typeObjectBeingExamined       = 0x65786d6e;//'exmn'
			public static readonly int typeCurrentContainer          = 0x63636e74;//'ccnt'
			public static readonly int typeToken                     = 0x746f6b65;//'toke'
			public static readonly int typeRelativeDescriptor        = 0x72656c20;//'rel '
			public static readonly int typeAbsoluteOrdinal           = 0x6162736f;//'abso'
			public static readonly int typeIndexDescriptor           = 0x696e6465;//'inde'
			public static readonly int typeRangeDescriptor           = 0x72616e67;//'rang'
			public static readonly int typeLogicalDescriptor         = 0x6c6f6769;//'logi'
			public static readonly int typeCompDescriptor            = 0x636d7064;//'cmpd'
			public static readonly int typeOSLTokenList              = 0x6F73746C;//'ostl'
			
			//SPECIAL CONSTANTS FOR CUSTOM WHOSE-CLAUSE RESOLUTION
			public static readonly int typeWhoseDescriptor           = 0x77686f73;//'whos'
			public static readonly int formWhose                     = 0x77686f73;//'whos'
			public static readonly int typeWhoseRange                = 0x77726e67;//'wrng'
			public static readonly int keyAEWhoseRangeStart          = 0x77737472;//'wstr'
			public static readonly int keyAEWhoseRangeStop           = 0x77737470;//'wstp'
			public static readonly int keyAEIndex                    = 0x6b696478;//'kidx'
			public static readonly int keyAETest                     = 0x6b747374;//'ktst'
		}
	
		[Flags]
		public enum AEResolveFlags
		{
			IDoMinimum                 = 0x0000,
			IDoWhose                   = 0x0001,
			IDoMarking                 = 0x0004,
			PassSubDescs               = 0x0008,
			ResolveNestedLists         = 0x0010,
			HandleSimpleRanges         = 0x0020,
			UseRelativeIterators       = 0x0040
		}
	
		public struct ccntTokenRecord
		{
			public DescType tokenClass;
			public AEDesc token;
		}
		
		public struct ccntTokenRecPtr
		{
			public ccntTokenRecord *Ptr;
		}
		
		public struct ccntTokenRecHandle
		{
			public ccntTokenRecPtr *Handle;
		}
		
		public delegate OSErr OSLAccessorUPP (DescType desiredClass, ref AEDesc container,
			DescType containerClass, DescType form, ref AEDesc selectionData, out AEDesc value, uint accessorRefcon);
		public delegate OSErr OSLCompareUPP (DescType oper, ref AEDesc obj1, ref AEDesc obj2, out bool result);
		public delegate OSErr OSLCountUPP (DescType desiredType, DescType containerClass, ref AEDesc container, out Size result);
		public delegate OSErr OSLDisposeTokenUPP (ref AEDesc unneededToken);
		public delegate OSErr OSLGetMarkTokenUPP (ref AEDesc dContainerToken, DescType containerClass, out AEDesc result);
		public delegate OSErr OSLGetErrDescUPP (AEDesc ** appDescPtr);
		public delegate OSErr OSLMarkUPP (ref AEDesc dToken, ref AEDesc markToken, Index index);
		public delegate OSErr OSLAdjustMarksUPP (Index newStart, Index newStop, ref AEDesc markToken);
		
		[DllImport (AELib)]
		public static extern OSErr AEObjectInit ();
		
		[DllImport (AELib)]
		public static extern OSErr AESetObjectCallbacks (OSLCompareUPP compare, OSLCountUPP count,
			OSLDisposeTokenUPP disposeToken, OSLGetMarkTokenUPP getMarkToken, OSLMarkUPP mark,
			OSLAdjustMarksUPP adjustMarks, OSLGetErrDescUPP getErrDesc);
		
		[DllImport (AELib)]
		public static extern OSErr AEResolve (ref AEDesc objectSpecifier, short callbackFlags, out AEDesc theToken);
		
		[DllImport (AELib)]
		public static extern OSErr AEInstallObjectAccessor (DescType desiredClass, DescType containerType,
			OSLAccessorUPP accessor, uint accessorRefcon, bool isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AERemoveObjectAccessor (DescType desiredClass, DescType containerType, 
			OSLAccessorUPP theAccessor, Boolean isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AEGetObjectAccessor (DescType desiredClass, DescType containerType,
			out OSLAccessorUPP accessor, out uint accessorRefcon, bool isSysHandler);
		
		[DllImport (AELib)]
		public static extern OSErr AEDisposeToken (ref AEDesc token);
		
		[DllImport (AELib)]
		public static extern OSErr AECallObjectAccessor (DescType desiredClass, ref AEDesc containerToken,
			DescType containerClass, DescType keyForm, ref AEDesc keyData, ref AEDesc token);
		
		//**************** AEPackObject.h ***************
		
		[DllImport (AELib)]
		public static extern OSErr CreateOffsetDescriptor (Index theOffset, out AEDesc theDescriptor);
		
		[DllImport (AELib)]
		public static extern OSErr CreateCompDescriptor (DescType comparisonOperator, ref AEDesc operand1,
			ref AEDesc operand2, bool disposeInputs, out AEDesc theDescriptor);
		
		[DllImport (AELib)]
		public static extern OSErr CreateLogicalDescriptor (ref AEDescList theLogicalTerms,
			DescType theLogicOperator, bool disposeInputs, out AEDesc theDescriptor);

		[DllImport (AELib)]
		public static extern OSErr CreateObjSpecifier (DescType desiredClass, ref AEDesc theContainer,
			DescType keyForm, ref AEDesc keyData, bool disposeInputs, out AEDesc objSpecifier);
		
		[DllImport (AELib)]
		public static extern OSErr CreateRangeDescriptor (ref AEDesc rangeStart, ref AEDesc rangeStop,
			bool disposeInputs, out AEDesc theDescriptor);
		
		//**************** AERegistry.h, AEUserTermTypes.h,  ***************
		
		//about 1000 lines of constants, didn't figure out how to sanely structure them
	}
}