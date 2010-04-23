// 
// AppleScript.cs
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
	internal static class AppleScript
	{
		//IntPtr defaultComponent = ;

		static AppleScript ()
		{
			
		}

		[DllImport (Carbon.CarbonLib)]
		static extern OsaError OSADoScript (ComponentInstance scriptingComponent, ref AEDesc sourceData,
		                                    OsaId contextID, DescType desiredType, OsaMode modeFlags,
		                                    ref AEDesc resultingText);

		[DllImport (Carbon.CarbonLib)]
		static extern OsaError OSAExecute (ComponentInstance scriptingComponent, OsaId compiledScriptID,
		                                   OsaId contextID, OsaMode modeFlags, out OsaId resultingScriptValueID);

		[DllImport (Carbon.CarbonLib)]
		static extern OsaError OSADispose (ComponentInstance scriptingComponent, OsaId scriptID);

		[DllImport (Carbon.CarbonLib)]
		static extern OsaError OSADisplay (ComponentInstance scriptingComponent, OsaId scriptValueID,
		                                   DescType desiredType, OsaMode modeFlags, out AEDesc resultingText);

		[DllImport (Carbon.CarbonLib)]
		static extern OsaError OSACompile (ComponentInstance scriptingComponent, ref AEDesc sourceData,
		                                   OsaMode modeFlags, ref OsaId previousAndResultingScriptID);
		
		[DllImport (Carbon.CarbonLib)]
		static extern OsaError  OSACompileExecute (ComponentInstance scriptingComponent, ref AEDesc sourceData,
		                                           OsaId contextID, OsaMode modeFlags, ref OsaId resultingScriptValueID);
		
		[DllImport (Carbon.CarbonLib)]
		static extern OsaError OSAScriptError (ComponentInstance scriptingComponent, OsaErrorSelector selector,
		                                       DescType desiredType, out AEDesc resultingErrorDescription);
		
		public static string RunScript (string script)
		{
			throw new NotImplementedException ();
			
			var component = ComponentManager.OpenDefaultComponent ((OSType)"osa ", (OSType)"ascr");
			try {
				//
				AEDesc sourceData = new AEDesc (); //FIXME initialize with real data
				AEDesc resultData = new AEDesc (); //FIXME initialize with real data
				DescType resultType = new DescType ();
				OsaId contextId = new OsaId (); // 0 = default
				var result = OSADoScript (component, ref sourceData, contextId, resultType, OsaMode.Default, ref resultData);
				if (result != OsaError.Success)
					throw new InvalidOperationException (string.Format ("Unexpected result {0}", (long)result));
			} finally {
				if (!component.IsNull)
					ComponentManager.CloseComponent (component);
			}
		}
	}
	
	struct OsaId : IEquatable<OsaId>
	{
		IntPtr value;
		
		public bool Equals (OsaId other)
		{
			return other.value == value;
		}
	}
	
	enum OsaErrorSelector
	{
		Number = 1701999214, // = keyErrorNumber = 'errn'
		Message = 1701999219, // = keyErrorString = 'errs'
		BriefMessage = 1701999202, //'errb'
		App = 1701994864, // 'erap'
		PartialResult = 1886678130, // 'ptlr'
		OffendingObject = 1701998434, // 'erob'
		ExpectedType = 1701999220, // 'errt'
		Range = 1701998183, // 'erng'
	}

	struct DescType
	{
		public OSType Value;
	}
	
	enum OsaError : long //this is a ComponentResult typedef
	{
		Success = 0,
	}
	
	enum OsaType : int
	{
		OsaComponent	 = 0x6f736120, // 'osa '
		OsaGenericScriptingComponent = 0x73637074, // 'scpt'
		OsaFile = 0x6f736173, // 'osas'
	}
	
	[Flags]
	enum OsaMode {
		Default = 0,
		PreventGetSource = 0x00000001,
		NeverInteract = 0x00000010,
		CanInteract = 0x00000020,
		AlwaysInteract = 0x00000030,
		DontReconnect = 0x00000080,
		CantSwitchLayer = 0x00000040,
		DoRecord = 0x00001000,
		CompileIntoContext = 0x00000002,
		AugmentContext = 0x00000004,
		DisplayForHumans = 0x00000008,
		DontStoreParent = 0x00010000,
		DispatchToDirectObject = 0x00020000,
		DontGetDataForArguments = 0x00040000,
		FullyQualifyDescriptors = 0x00080000,
	}
}

