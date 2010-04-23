// 
// AppleEvent.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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

namespace OSXIntegration.Framework
{
	public static class AppleEvent
	{
		const string AELib = Carbon.CarbonLib;
		
		//[DllImport (AELib)]
		//OSErr AECreateDesc (DescType typeCode, IntPtr dataPtr, Size dataSize, out AEDesc result);
	}
	
	enum AESendMode {
		NoReply = 0x00000001,
		QueueReply = 0x00000002,
		WaitReply = 0x00000003,
		DontReconnect = 0x00000080,
		WantReceipt = 0x00000200,
		NeverInteract = 0x00000010,
		CanInteract = 0x00000020,
		AlwaysInteract = 0x00000030,
		CanSwitchLayer = 0x00000040,
		DontRecord = 0x00001000,
		DontExecute = 0x00002000,
		ProcessNonReplyEvents = 0x00008000,
	}
}