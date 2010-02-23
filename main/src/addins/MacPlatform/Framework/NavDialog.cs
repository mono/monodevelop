// 
// Carbon.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Collections.Generic;

namespace OSXIntegration.Framework
{
	
	class NavDialog : IDisposable
	{
		IntPtr ptr;
		
		public static NavDialog CreateChooseFileDialog (NavDialogCreationOptions options)	
		{
			NavDialog dialog = new NavDialog ();
			CheckReturn (NavCreateChooseFileDialog (ref options.data, IntPtr.Zero, new NavEventUPP (), new NavPreviewUPP (),
			                                                    new NavObjectFilterUPP (), IntPtr.Zero, out dialog.ptr));
			return dialog;
		}
		
		public static NavDialog CreateChooseFolderDialog (NavDialogCreationOptions options)	
		{
			NavDialog dialog = new NavDialog ();
			CheckReturn (NavCreateChooseFolderDialog (ref options.data, new NavEventUPP (),
			                                                    new NavObjectFilterUPP (), IntPtr.Zero, out dialog.ptr));
			return dialog;
		}
		
		public static NavDialog CreatePutFileDialog (NavDialogCreationOptions options)	
		{
			NavDialog dialog = new NavDialog ();
			CheckReturn (NavCreatePutFileDialog (ref options.data, new OSType (), new OSType (),
			                                                 new NavEventUPP (), IntPtr.Zero, out dialog.ptr));
			return dialog;
		}
		
		public static NavDialog CreateNewFolderDialog (NavDialogCreationOptions options)	
		{
			NavDialog dialog = new NavDialog ();
			CheckReturn (NavCreateNewFolderDialog (ref options.data, new NavEventUPP (),
			                                                   IntPtr.Zero, out dialog.ptr));
			return dialog;
		}
		
		public NavUserAction Run ()
		{
			CheckDispose ();
			CheckReturn (NavDialogRun (ptr));
			return NavDialogGetUserAction (ptr);
		}
		
		public NavReplyRecordRef GetReply ()
		{
			CheckDispose ();
			var record = new NavReplyRecordRef ();
			CheckReturn (NavDialogGetReply (ptr, out record.value));
			return record;
		}
		
		public void SetLocation (string location)
		{
			CheckDispose ();
			throw new NotImplementedException ();
		//	AEDesc desc = new AEDesc ();
		//	CheckReturn (NavCustomControl (ptr, NavCustomControlMessage.SetLocation, ref desc)); 
		}
		
		void CheckDispose ()
		{
			if (ptr == IntPtr.Zero)
				throw new ObjectDisposedException ("NavDialog");
		}
		
		public void Dispose ()
		{
			if (ptr != IntPtr.Zero) {
				NavDialogDispose (ptr);
				ptr = IntPtr.Zero;
				GC.SuppressFinalize (this);
			}
		}
		
		~NavDialog ()
		{
			Console.WriteLine ("WARNING: NavDialog not disposed");
		}
		
		[DllImport (Carbon.CarbonLib)]
		static extern NavStatus NavCreateChooseFileDialog (ref NavDialogCreationOptionsData options, IntPtr inTypeList, 
		                                                   NavEventUPP inEventProc, NavPreviewUPP inPreviewProc, 
		                                                   NavObjectFilterUPP inFilterProc, IntPtr inClientData, out IntPtr navDialogRef);
		//intTypeList is a NavTypeListHandle, which apparently is a pointer to  NavTypeListPtr, which is a pointer to a NavTypeList
		
		[DllImport (Carbon.CarbonLib)]
		static extern NavStatus NavCreateChooseFolderDialog (ref NavDialogCreationOptionsData options, 
		                                                     NavEventUPP inEventProc, NavObjectFilterUPP inFilterProc,
		                                                     IntPtr inClientData, out IntPtr navDialogRef);
		
		[DllImport (Carbon.CarbonLib)]
		static extern NavStatus NavCreatePutFileDialog (ref NavDialogCreationOptionsData options, OSType inFileType,
		                                                OSType inFileCreator, NavEventUPP inEventProc,
		                                                IntPtr inClientData, out IntPtr navDialogRef);

		[DllImport (Carbon.CarbonLib)]
		static extern NavStatus NavCreateNewFolderDialog (ref NavDialogCreationOptionsData options, 
		                                                  NavEventUPP inEventProc, IntPtr inClientData, 
		                                                  out IntPtr navDialogRef);
		
		[DllImport (Carbon.CarbonLib)]
		public static extern NavStatus NavDialogRun (IntPtr navDialogRef);
		
		[DllImport (Carbon.CarbonLib)]
		static extern NavStatus NavDialogGetReply (IntPtr navDialogRef, out NavReplyRecord outReply);
		
		[DllImport (Carbon.CarbonLib)]
		static extern NavUserAction NavDialogGetUserAction (IntPtr navDialogRef);
		
		[DllImport (Carbon.CarbonLib)]
		static extern void NavDialogDispose (IntPtr navDialogRef);
		
		[DllImport (Carbon.CarbonLib)]
		static extern NavStatus NavCustomControl (IntPtr dialog, NavCustomControlMessage selector, IntPtr parms);
		
		[DllImport (Carbon.CarbonLib)]
		static extern NavStatus NavCustomControl (IntPtr dialog, NavCustomControlMessage selector, ref AEDesc parm);
		
		public static void CheckReturn (NavStatus status)
		{
			CheckReturn (status);
		}
	}
	
	struct NavEventUPP { IntPtr ptr; }
	struct NavObjectFilterUPP { IntPtr ptr; }
	struct NavPreviewUPP { IntPtr ptr; }
	struct OSType { int value; }
	
	class NavDialogCreationOptions : IDisposable
	{
		internal NavDialogCreationOptionsData data;
		
		public static NavDialogCreationOptions NewFromDefaults ()
		{
			var options = new NavDialogCreationOptions ();
			NavDialog.CheckReturn (NavGetDefaultDialogCreationOptions (out options.data));
			return options;
		}
		
		[DllImport (Carbon.CarbonLib)]
		static extern NavStatus NavGetDefaultDialogCreationOptions (out NavDialogCreationOptionsData options);
		
		public NavDialogOptionFlags OptionFlags {
			get { return data.optionFlags; }
			set { data.optionFlags = value; }
		}
		
		public Point Location {
			get { return data.location; }
			set { data.location = value; }
		}
		
		public string ClientName {
			get { return CoreFoundation.FetchString (data.clientName); }
			set { data.clientName = AddCFString (value); }
		}
		
		public string WindowTitle {
			get { return CoreFoundation.FetchString (data.windowTitle); }
			set { data.windowTitle = AddCFString (value); }
		}
		
		public string ActionButtonLabel {
			get { return CoreFoundation.FetchString (data.actionButtonLabel); }
			set { data.actionButtonLabel = AddCFString (value); }
		}
		
		public string CancelButtonLabel {
			get { return CoreFoundation.FetchString (data.cancelButtonLabel); }
			set { data.cancelButtonLabel = AddCFString (value); }
		}
		
		public string SaveFileName {
			get { return CoreFoundation.FetchString (data.saveFileName); }
			set { data.saveFileName = AddCFString (value); }
		}
		
		public string Message {
			get { return CoreFoundation.FetchString (data.message); }
			set { data.message = AddCFString (value); }
		}
		
		public uint PreferenceKey {
			get { return data.preferenceKey; }
			set { data.preferenceKey = value; }
		}
		
		public IntPtr PopupExtension {
			get { return data.popupExtension; }
			set { data.popupExtension = value; }
		}
		
		public WindowModality Modality {
			get { return data.modality; }
			set { data.modality = value; }
		}
		
		public IntPtr ParentWindow {
			get { return data.parentWindow; }
			set { data.parentWindow = value; }
		}
		
		List<IntPtr> toDispose;
		IntPtr AddCFString (string value)
		{
			var ptr = CoreFoundation.CreateString (value);
			if (toDispose == null)
				toDispose = new List<IntPtr> ();
			toDispose.Add (ptr);
			return ptr;
		}
		
		public void Dispose ()
		{
			if (toDispose != null) {
				foreach (IntPtr ptr in toDispose)
					CoreFoundation.Release (ptr);
				toDispose = null;
				GC.SuppressFinalize (this);
			}
		}
		
		~NavDialogCreationOptions ()
		{
			Console.WriteLine ("WARNING: Did not dispose NavDialogCreationOptions");
		}
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 66)]
	struct NavDialogCreationOptionsData
	{
		public ushort version;
		public NavDialogOptionFlags optionFlags;
		public Point location;
		public IntPtr clientName; //CFStringRef
		public IntPtr windowTitle; //CFStringRef
		public IntPtr actionButtonLabel; // CFStringRef
		public IntPtr cancelButtonLabel; // CFStringRef
		public IntPtr saveFileName; // CFStringRef
		public IntPtr message; // CFStringRef
		public uint preferenceKey;
		public IntPtr popupExtension; //CFArrayRef
		public WindowModality modality;
		public IntPtr parentWindow; //WindowRef
		public char reserved; //char[16]
	}
	
	[Flags]
	enum NavDialogOptionFlags : uint
	{
		Default = DontAddTranslateItems & AllowStationery & AllowPreviews & AllowMultipleFiles,
		NoTypePopup = 1,
		DontAutoTranslate = 1 << 1,
		DontAddTranslateItems = 1 << 2,
		AllFilesInPopup = 1 << 4,
		AllowStationery = 1 << 5,
		AllowPreviews = 1 << 6,
		AllowMultipleFiles = 1 << 7,
		AllowInvisibleFiles = 1 << 8,
		DontResolveAliases = 1 << 9,
 		SelectDefaultLocation = 1 << 10,
		SelectAllReadableItem = 1 << 11,
		SupportPackages = 1 << 12,
		AllowOpenPackages = 1 << 13,
		DontAddRecents = 1 << 14,
		DontUseCustomFrame = 1 << 15,
		DontConfirmReplacement = 1 << 16,
		PreserveSaveFileExtension = 1 << 17
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	struct Point
	{
		short v;
		short h;
		
		public Point (short v, short h)
		{
			this.v = v;
			this.h = h;
		}
		
		public short V { get { return v; } }
		public short H { get { return h; } }
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	struct Rect
	{
		short top;
		short left;
		short bottom;
		short right;
		
		public short Top { get { return top; } }
		public short Left { get { return left; } }
		public short Bottom { get { return bottom; } }
		public short Right { get { return right; } }
	}
	
	enum WindowModality : uint
	{
		None = 0,
		SystemModal = 1,
		AppModal = 2,
		WindowModal = 3,
	}
	
	enum WindowPositionMethod : uint
	{
		CenterOnMainScreen = 1,
		CenterOnParentWindow = 2,
		CenterOnParentWindowScreen = 3,
		CascadeOnMainScreen = 4,
		CascadeOnParentWindow = 5,
		CascadeOnParentWindowScreen = 6,
		CascadeStartAtParentWindowScreen = 10,
		AlertPositionOnMainScreen = 7,
		AlertPositionOnParentWindow = 8,
		AlertPositionOnParentWindowScreen = 9,
	}
	
	enum NavStatus : int
	{
		Ok = 0,
		WrongDialogStateErr = -5694,
		WrongDialogClassErr = -5695,
		InvalidSystemConfigErr = -5696,
		CustomControlMessageFailedErr = -5697,
		InvalidCustomControlMessageErr = -5698,
		MissingKindStringErr = -5699,
	}
	
	enum NavEventCallbackMessage : int
	{
		Event = 0,
		Customize = 1,
		Start = 2,
		Terminate = 3,
		AdjustRect = 4,
		NewLocation = 5,
		ShowDesktop = 6,
		SelectEntry = 7,
		PopupMenuSelect = 8,
		Accept = 9,
		Cancel = 10,
		AdjustPreview = 11,
		UserAction = 12,
		OpenSelection = -2147483648, // unchecked 0x80000000
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 2, Size=254)]
	struct NavCBRec
	{
		ushort version;
		IntPtr context; // NavDialogRef
		IntPtr window; // WindowRef
		Rect customRect;
		Rect previewRect;
		NavEventData eventData;
		NavUserAction userAction;
		char reserved; //[218];
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	struct NavEventData
	{
		IntPtr eventDataParms; // NavEventDataInfo union, usually a pointer to either a EventRecord or an AEDescList
		short itemHit;
	}
	
	enum NavUserAction : uint
	{
		None = 0,
		Cancel = 1,
		Open = 2,
		SaveAs = 3,
		Choose = 4,
		NewFolder = 5,
		SaveChanges = 6,
		DontSaveChanges = 7,
		DiscardChanges = 8,
		ReviewDocuments = 9,
		DiscardDocuments = 10
	}
	
	enum NavFilterModes : short
	{
		BrowserList = 0,
		Favorites = 1,
		Recents = 2,
		ShortCutVolumes = 3,
		LocationPopup = 4
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 2, Size=255)]
	struct NavReplyRecord
	{
		ushort version;
		[MarshalAs(UnmanagedType.U1)]
		bool validRecord;
		[MarshalAs(UnmanagedType.U1)]
		bool replacing;
		[MarshalAs(UnmanagedType.U1)]
		bool isStationery;
		[MarshalAs(UnmanagedType.U1)]
		bool translationNeeded;
		AEDesc selection; //actually an AEDescList
		short keyScript;
		//fileTranslation is a FileTranslationSpecArrayHandle, which apparently is a pointer to a FileTranslationSpecArrayPtr,
		//which is a pointer to a FileTranslationSpec
		IntPtr fileTranslation;
		uint reserved1;
		IntPtr saveFileName; //CFStringRef
		[MarshalAs(UnmanagedType.U1)]
		bool saveFileExtensionHidden;
		byte reserved2;
		char reserved; //size [225];
	}
	
	class NavReplyRecordRef : IDisposable
	{
		bool disposed;
		internal NavReplyRecord value;
		
		public void Dispose ()
		{
			if (!disposed) {
				disposed = true;
				NavDisposeReply (ref value);
				GC.SuppressFinalize (this);
			}
		}
		
		~NavReplyRecordRef ()
		{
			Console.WriteLine ("WARNING: NavReplyRecordRef not disposed");
		}
		
		[DllImport (Carbon.CarbonLib)]
		static extern NavStatus NavDisposeReply (ref NavReplyRecord record);
	}
	
	enum NavCustomControlMessage : int
	{
		ShowDesktop = 0,
		SortBy = 1,
		SortOrder = 2,
		ScrollHome = 3,
		ScrollEnd = 4,
		PageUp = 5,
		PageDown = 6,
		GetLocation = 7,
		SetLocation = 8,
		GetSelection = 9,
		SetSelection = 10,
		ShowSelection = 11,
		OpenSelection = 12,
		EjectVolume = 13,
		NewFolder = 14,
		Cancel = 15,
		Accept = 16,
		IsPreviewShowing = 17,
		AddControl = 18,
		AddControlList = 19,
		GetFirstControlID = 20,
		SelectCustomType = 21,
		SelectAllType = 22,
		GetEditFileName = 23,
		SetEditFileName = 24,
		SelectEditFileName = 25,
		BrowserSelectAll = 26,
		GotoParent = 27,
		SetActionState = 28,
		BrowserRedraw = 29,
		Terminate = 30
	}
}
