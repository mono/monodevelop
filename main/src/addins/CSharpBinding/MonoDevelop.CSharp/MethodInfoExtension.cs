/*

MethodInfoExtension.cs
 
Author:
      Jose Medrano <jose.medrano@xamarin.com>

Copyright (c) 2016 Jose Medrano

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Addins;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.CSharp
{
	public class MethodInfoExtension : TextEditorExtension
	{
		public MethodInfoExtension ()
		{
		}
		CancellationTokenSource src = new CancellationTokenSource ();
		protected override void Initialize ()
		{
			base.Initialize ();
			DocumentContext.DocumentParsed += HandleDocumentParsed;
		}

		public override void Dispose ()
		{
			src.Cancel ();
			DocumentContext.DocumentParsed -= HandleDocumentParsed;
			base.Dispose ();
		}

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
			var token = src.Token;
			//var unitTestMarker = TextMarkerFactory.CreateUnitTestMarker (Editor, new UnitTestMarkerHostImpl (this), foundTest);
			//newMarkers.Add (unitTestMarker);
			//var line = Editor.GetLineByOffset (foundTest.Offset);
			//if (line != null) {

			//var unitTestMarkers = AddinManager.GetExtensionNodes (TestMarkersPath).OfType<IUnitTestMarkers> ().ToArray ();
			foreach (var item in new int[] { 1, 2 }) {
				var line = Editor.GetLine (item);
				if (line != null) {

					var bubble = TextMarkerFactory.CreateCurrentMethodInfoMarker (Editor);
					Editor.AddMarker (line, bubble);
				}
			}

			//};
			//ThreadPool.QueueUserWorkItem (delegate {
			//	if (token.IsCancellationRequested || DocumentContext == null)
			//		return;
			//	try {
			//		GatherUnitTests (unitTestMarkers, token).ContinueWith (task => {
			//			var foundTests = task.Result;
			//			if (foundTests == null || DocumentContext == null)
			//				return;
			//			Application.Invoke (delegate {
			//				if (token.IsCancellationRequested || DocumentContext == null)
			//					return;
			//				foreach (var oldMarker in currentMarker)
			//					Editor.RemoveMarker (oldMarker);
			//				var newMarkers = new List<IUnitTestMarker> ();
			//				foreach (var foundTest in foundTests) {
			//					if (foundTest == null)
			//						continue;
			//					var unitTestMarker = TextMarkerFactory.CreateUnitTestMarker (Editor, new UnitTestMarkerHostImpl (this), foundTest);
			//					newMarkers.Add (unitTestMarker);
			//					var line = Editor.GetLineByOffset (foundTest.Offset);
			//					if (line != null) {
			//						Editor.AddMarker (line, unitTestMarker);
			//					}
			//				}
			//				currentMarker = newMarkers;
			//			});

			//		}, TaskContinuationOptions.ExecuteSynchronously |
			//			TaskContinuationOptions.NotOnCanceled |
			//			TaskContinuationOptions.NotOnFaulted);
			//	} catch (OperationCanceledException) {
			//	}
			//});
		}

		class NUnitVisitor : CSharpSyntaxWalker
		{

		}
	}
}

