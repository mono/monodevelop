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

namespace MonoDevelop.MacInterop
{
	public static class AppleScript
	{
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
		static extern OsaError OSALoad (ComponentInstance scriptingComponent, ref AEDesc sourceData,
		                                OsaMode modeFlags, ref OsaId previousAndResultingScriptID);
		
		[DllImport (Carbon.CarbonLib)]
		static extern OsaError OSACompileExecute (ComponentInstance scriptingComponent, ref AEDesc sourceData,
		                                           OsaId contextID, OsaMode modeFlags, ref OsaId resultingScriptValueID);
		
		[DllImport (Carbon.CarbonLib)]
		static extern OsaError OSALoadExecute (ComponentInstance scriptingComponent, ref AEDesc scriptData,
		                                       OsaId contextID, OsaMode modeFlags, ref OsaId resultingScriptValueID);
		
		[DllImport (Carbon.CarbonLib)]
		static extern OsaError OSAScriptError (ComponentInstance scriptingComponent, OsaErrorSelector selector,
		                                       DescType desiredType, out AEDesc resultingErrorDescription);
		
		public static string Run (string scriptSourceFormat, params object[] args)
		{
			return Run (string.Format (scriptSourceFormat, args));
		}
		
		public static string Run (string scriptSource)
		{
			AEDesc sourceData = new AEDesc ();

			try {
				AppleEvent.AECreateDescUtf8 (scriptSource, out sourceData);
				return Run (true, ref sourceData);
			} catch (AppleScriptException ex) {
				MonoDevelop.Core.LoggingService.LogWarning (
					"Applescript failure: {0}\n[[\n{1}\n]]",
					ex.Message,
					scriptSource);
				throw;
			} finally {
				AppleEvent.AEDisposeDesc (ref sourceData);
			}
		}
		
		public static string Run (byte[] compiledBytes)
		{
			AEDesc sourceData = new AEDesc ();
			try {
				AppleEvent.AECreateDesc ((OSType)(int)OsaType.OsaGenericStorage, compiledBytes, out sourceData);
				return Run (false, ref sourceData);
			} finally {
				AppleEvent.AEDisposeDesc (ref sourceData);
			}
		}
		
		static string Run (bool compile, ref AEDesc scriptData)
		{
			string value;
			var ret = Run (compile, ref scriptData, out value);
			
			switch (ret) {
			case OsaError.Success:
				return value;
			case OsaError.Timeout:
				throw new TimeoutException ("The AppleScript command timed out.");
			default:
				throw new AppleScriptException (ret, value);
			}
		}
		
		static OsaError Run (bool compile, ref AEDesc scriptData, out string value)
		{
			var component = ComponentManager.OpenDefaultComponent ((OSType)(int)OsaType.OsaComponent, (OSType)(int)OsaType.AppleScript);
			if (component.IsNull)
				throw new AppleScriptException (OsaError.GeneralError, "Could not load component");
			AEDesc resultData = new AEDesc ();
			OsaId contextId = new OsaId (), scriptId = new OsaId (), resultId = new OsaId ();
			try {
				AppleEvent.AECreateDescNull (out resultData);
				//apparently UnicodeText doesn't work
				var resultType = new DescType () { Value = (OSType)"TEXT" };
				
				//var result = OSADoScript (component, ref sourceData, contextId, resultType, OsaMode.Default, ref resultData);
				
				OsaError result;
				if (compile)
					result = OSACompile (component, ref scriptData, OsaMode.Default, ref scriptId);
				else
					result = OSALoad (component, ref scriptData, OsaMode.Default, ref scriptId);
				
				if (result == OsaError.Success) {
					result = OSAExecute (component, scriptId, contextId, OsaMode.Default, out resultId);
					if (result == OsaError.Success) {
						result = OSADisplay (component, resultId, resultType, OsaMode.Default, out resultData);
						if (result == OsaError.Success) {
							value = AppleEvent.GetStringFromAEDesc (ref resultData);
							return result;
						}
					}
				}
				var errorDesc = new AEDesc ();
				try {
					AppleEvent.AECreateDescNull (out resultData);
					if (OsaError.Success == OSAScriptError (component, OsaErrorSelector.Message, resultType, out errorDesc)) {
						value = AppleEvent.GetStringFromAEDesc (ref errorDesc);
						return result;
					}
					throw new AppleScriptException (result, null);
				} finally {
					AppleEvent.AEDisposeDesc (ref errorDesc);
				}
			} finally {
				AppleEvent.AEDisposeDesc (ref scriptData);
				AppleEvent.AEDisposeDesc (ref resultData);
				if (!contextId.IsZero)
					OSADispose (component, contextId);
				if (!scriptId.IsZero)
					OSADispose (component, scriptId);
				if (!resultId.IsZero)
					OSADispose (component, resultId);
				if (!component.IsNull)
					ComponentManager.CloseComponent (component);
			}
		}
	}
	
	[StructLayout (LayoutKind.Sequential,Pack=2)]
	struct OsaId : IEquatable<OsaId>
	{
		IntPtr value;
		
		public IntPtr Value {
			get { return value; }
		}
		
		public bool Equals (OsaId other)
		{
			return other.value == value;
		}
		
		public bool IsZero {
			get { return value == IntPtr.Zero; }
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
	
	public enum OsaError : int //this is a ComponentResult typedef - is it long on int64? Many of these values can be gotten from MacErrors.h
	{
		Success = 0,
		CantCoerce = -1700,	
		MissingParameter = -1701,
		CorruptData = -1702,	
		TypeError = -1703,
		MessageNotUnderstood = -1708,
		Timeout = -1712,
		UndefinedHandler = -1717,
		IllegalIndex	 = -1719,
		IllegalRange	 = -1720,
		ParameterMismatch = -1721,
		IllegalAccess = -1723,
		CantAccess = -1728,
		RecordingIsAlreadyOn = -1732,
		SystemError = -1750,
		InvalidID = -1751,
		BadStorageType = -1752,
		ScriptError = -1753,
		BadSelector = -1754,
		SourceNotAvailable = -1756,
		NoSuchDialect = -1757,
		DataFormatObsolete = -1758,
		DataFormatTooNew = -1759,
		ComponentMismatch = -1761,
		CantOpenComponent = -1762,
		GeneralError	 = -2700,
		DivideByZero	 = -2701,
		NumericOverflow = -2702,
		CantLaunch = -2703,
		AppNotHighLevelEventAware = -2704,
		CorruptTerminology = -2705,
		StackOverflow = -2706,
		InternalTableOverflow = -2707,
		DataBlockTooLarge = -2708,
		CantGetTerminology = -2709,
		CantCreate = -2710,
		SyntaxError = -2740,
		SyntaxTypeError = -2741,
		TokenTooLong	 = -2742,
		DuplicateParameter = -2750,
		DuplicateProperty = -2751,
		DuplicateHandler = -2752,
		UndefinedVariable = -2753,
		InconsistentDeclarations = -2754,
		ControlFlowError = -2755,
		IllegalAssign = -10003,
		CantAssign = -10006,
	}
	
	enum OsaType : int
	{
		OsaComponent	 = 0x6f736120, // 'osa '
		OsaGenericScriptingComponent = 0x73637074, // 'scpt'
		OsaFile = 0x6f736173, // 'osas'
		AppleScript = 1634952050, // 'ascr'
		OsaGenericStorage = OsaGenericScriptingComponent,
		OsaResource = OsaGenericScriptingComponent,
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
	
	public class AppleScriptException : Exception
	{
		public AppleScriptException (OsaError error, string returnValue)
			: base (GetFullMessage (error, returnValue))
		{
			ErrorCode = error;
			ReturnValue = returnValue;
		}

		static string GetFullMessage (OsaError error, string returnValue)
		{
			if (!string.IsNullOrEmpty (returnValue)) {
				return string.Format ("{0}: {1}", error, returnValue);
			}
			return error.ToString ();
		}
		
		public OsaError ErrorCode {
			get; private set;
		}

		public string ReturnValue {
			get; private set;
		}
	}
}

