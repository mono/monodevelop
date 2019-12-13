//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
//
// Authors:
//   Aaron Bockover <abock@microsoft.com>
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

using Foundation;
using ObjCRuntime;

using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MacPlatform
{
	sealed class MacIdeAppleEvents : NSObject
	{
		// From AE.framework AppleEvents.h
		const uint keyDirectObject = 757935405; // '----'
		const uint keyErrorNumber = 1701999214; // 'errn'
		const uint keyErrorString = 1701999219; // 'errs'

		// Our own private event FourCCs; note that all-lowercase FourCCs are reserved by Apple;
		// any FourCC that has at least one capital letter is considered private to the application.
		const AEEventClass WorkspaceEventClass = (AEEventClass)1448302419; // 'VSWS' FourCC
		const AEEventID CurrentSelectedSolutionPathEventID = (AEEventID)1129534288; // 'CSSP' FourCC

		public MacIdeAppleEvents ()
		{
			NSAppleEventManager.SharedAppleEventManager.SetEventHandler (
				this,
				sel_getCurrentSelectedSolutionPath_withReply_,
				WorkspaceEventClass,
				CurrentSelectedSolutionPathEventID);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				NSAppleEventManager.SharedAppleEventManager.RemoveEventHandler (
					WorkspaceEventClass,
					CurrentSelectedSolutionPathEventID);
			}

			base.Dispose (disposing);
		}

		const string getCurrentSelectedSolutionPath_withReply_
			= "getCurrentSelectedSolutionPath:withReply:";

		static readonly Selector sel_getCurrentSelectedSolutionPath_withReply_
			= new Selector (getCurrentSelectedSolutionPath_withReply_);

		[Export (getCurrentSelectedSolutionPath_withReply_)]
		void GetCurrentSelectedSolutionPath (NSAppleEventDescriptor @event, NSAppleEventDescriptor reply)
		{
			LoggingService.LogInfo ($"{nameof (GetCurrentSelectedSolutionPath)}: received AppleEvent {@event}");

			var solutionPath = IdeApp.Workspace.CurrentSelectedSolution?.FileName.FullPath.ToString ();
			if (!string.IsNullOrEmpty (solutionPath)) {
				reply.SetParamDescriptorforKeyword (
					NSAppleEventDescriptor.DescriptorWithString (solutionPath),
					keyDirectObject);

				LoggingService.LogInfo ($"{nameof (GetCurrentSelectedSolutionPath)}: replying to AppleEvent {@event} with {solutionPath}");
			}
		}
	}
}