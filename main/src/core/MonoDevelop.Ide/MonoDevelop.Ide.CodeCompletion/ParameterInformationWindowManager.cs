// ParameterInformationWindowManager.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Gdk;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class ParameterInformationWindowManager
	{
		static List<MethodData> methods = new List<MethodData> ();
		static ParameterInformationWindow window;
		
		public static bool IsWindowVisible {
			get { return methods.Count > 0; }
		}
		
		// Called when a key is pressed in the editor.
		// Returns false if the key press has to continue normal processing.
		public static bool ProcessKeyEvent (CompletionTextEditorExtension ext, ICompletionWidget widget, Gdk.Key key, Gdk.ModifierType modifier)
		{
			if (methods.Count == 0)
				return false;

			MethodData cmd = methods [methods.Count - 1];
			
			if (key == Gdk.Key.Down) {
				if (cmd.MethodProvider.Count <= 1)
					return false;
				if (cmd.CurrentOverload < cmd.MethodProvider.Count - 1)
					cmd.CurrentOverload ++;
				else
					cmd.CurrentOverload = 0;
				UpdateWindow (ext, widget);
				return true;
			}
			else if (key == Gdk.Key.Up) {
				if (cmd.MethodProvider.Count <= 1)
					return false;
				if (cmd.CurrentOverload > 0)
					cmd.CurrentOverload --;
				else
					cmd.CurrentOverload = cmd.MethodProvider.Count - 1;
				UpdateWindow (ext, widget);
				return true;
			}
			else if (key == Gdk.Key.Escape) {
				HideWindow (ext, widget);
				return true;
			}
			return false;
		}
		
		public static void PostProcessKeyEvent (CompletionTextEditorExtension ext, ICompletionWidget widget, Gdk.Key key, Gdk.ModifierType modifier)
		{
			// Called after the key has been processed by the editor
		
			if (methods.Count == 0)
				return;
				
			for (int n=0; n< methods.Count; n++) {
				// If the cursor is outside of any of the methods parameter list, discard the
				// information window for that method.
				
				MethodData md = methods [n];
				int pos = ext.GetCurrentParameterIndex (md.MethodProvider.StartOffset);
				if (pos == -1) {
					methods.RemoveAt (n);
					n--;
				}
			}
			// If the user enters more parameters than the current overload has,
			// look for another overload with more parameters.
			UpdateOverload (ext, widget);
			
			// Refresh.
			UpdateWindow (ext, widget);
		}
		
		public static void ShowWindow (CompletionTextEditorExtension ext, ICompletionWidget widget, CodeCompletionContext ctx, IParameterDataProvider provider)
		{
			if (provider.Count == 0)
				return;
			
			// There can be several method parameter lists open at the same time, so
			// they have to be queued. The last one of queue is the one being shown
			// in the information window.
			
			MethodData md = new MethodData ();
			md.MethodProvider = provider;
			md.CurrentOverload = 0;
			md.CompletionContext = ctx;
			methods.Add (md);
			UpdateOverload (ext, widget);
			UpdateWindow (ext, widget);
		}
		
		public static void HideWindow (CompletionTextEditorExtension ext, ICompletionWidget widget)
		{
			methods.Clear ();
			UpdateWindow (ext, widget);
		}
		
		public static int GetCurrentOverload ()
		{
			if (methods.Count == 0)
				return -1;
			return methods [methods.Count - 1].CurrentOverload;
		}
		
		public static IParameterDataProvider GetCurrentProvider ()
		{
			if (methods.Count == 0)
				return null;
			return methods [methods.Count - 1].MethodProvider;
		}
		
		static void UpdateOverload (CompletionTextEditorExtension ext, ICompletionWidget widget)
		{
			if (methods.Count == 0)
				return;
			
			// If the user enters more parameters than the current overload has,
			// look for another overload with more parameters.
			
			MethodData md = methods [methods.Count - 1];
			int cparam = ext.GetCurrentParameterIndex (md.MethodProvider.StartOffset);
			
			if (cparam > md.MethodProvider.GetParameterCount (md.CurrentOverload) && !md.MethodProvider.AllowParameterList (md.CurrentOverload)) {
				// Look for an overload which has more parameters
				int bestOverload = -1;
				int bestParamCount = int.MaxValue;
				for (int n=0; n<md.MethodProvider.Count; n++) {
					int pc = md.MethodProvider.GetParameterCount (n);
					if (pc < bestParamCount && pc >= cparam) {
						bestOverload = n;
						bestParamCount = pc;
					}
				}
				if (bestOverload == -1) {
					for (int n=0; n<md.MethodProvider.Count; n++) {
						if (md.MethodProvider.AllowParameterList (n)) {
							bestOverload = n;
							break;
						}
					}
				}
				if (bestOverload != -1)
					md.CurrentOverload = bestOverload;
			}
		}
		
		public static int X { get; private set; }
		public static int Y { get; private set; }
		public static bool wasAbove = false;
		static bool wasVisi;
		static int lastW = -1, lastH = -1;
		
		internal static void UpdateWindow (CompletionTextEditorExtension ext, ICompletionWidget widget)
		{
			// Updates the parameter information window from the information
			// of the current method overload
			if (window == null && methods.Count > 0) {
				window = new ParameterInformationWindow (ext, widget);
				window.SizeAllocated += delegate(object o, SizeAllocatedArgs args) {

					if (args.Allocation.Width == lastW && args.Allocation.Height == lastH && wasVisi == CompletionWindowManager.IsVisible)
						return;
					lastW = args.Allocation.Width;
					lastH = args.Allocation.Height;
					wasVisi = CompletionWindowManager.IsVisible;
					
					var ctx = widget.CurrentCodeCompletionContext;
					MethodData md = methods [methods.Count - 1];
					int cparam = ext != null ? ext.GetCurrentParameterIndex (md.MethodProvider.StartOffset) : 0;
					Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (window.Screen, window.Screen.GetMonitorAtPoint (X, Y));
					window.ShowParameterInfo (md.MethodProvider, md.CurrentOverload, cparam - 1, geometry.Width);
					X = md.CompletionContext.TriggerXCoord;
					if (CompletionWindowManager.IsVisible) {
						// place above
						Y = ctx.TriggerYCoord - ctx.TriggerTextHeight - args.Allocation.Height - 10;
					} else {
						// place below
						Y = ctx.TriggerYCoord;
					}

					if (X + args.Allocation.Width > geometry.Right)
						X = geometry.Right - args.Allocation.Width;
			
					if (Y < geometry.Top)
						Y = ctx.TriggerYCoord;
			
					if (wasAbove || Y + args.Allocation.Height > geometry.Bottom) {
						Y = Y - ctx.TriggerTextHeight - args.Allocation.Height - 4;
						wasAbove = true;
					}
					
					if (CompletionWindowManager.IsVisible) {
						Rectangle completionWindow = new Rectangle (CompletionWindowManager.X, CompletionWindowManager.Y,
				                                            CompletionWindowManager.Wnd.Allocation.Width, CompletionWindowManager.Wnd.Allocation.Height);
						if (completionWindow.IntersectsWith (new Rectangle (X, Y, args.Allocation.Width, args.Allocation.Height))) {
							X = completionWindow.X;
							Y = completionWindow.Y - args.Allocation.Height - 6;
							if (Y < 0)
								Y = completionWindow.Bottom + 6;
						}
					}
					window.Move (X, Y);
				};
				window.Destroyed += delegate {
					lastW = -1;
					lastH = -1;
				};
				window.QueueResize ();
				window.Show ();
				wasAbove = false;
			}
			
			if (methods.Count == 0) {
				if (window != null) {
					window.Destroy ();
					window = null;
					wasAbove = false;
				}
				return;
			}
					
			if (window != null) {
				var ctx = widget.CurrentCodeCompletionContext;
				MethodData md = methods [methods.Count - 1];
				int cparam = ext != null ? ext.GetCurrentParameterIndex (md.MethodProvider.StartOffset) : 0;
				Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (window.Screen, window.Screen.GetMonitorAtPoint (X, Y));
				window.ShowParameterInfo (md.MethodProvider, md.CurrentOverload, cparam - 1, geometry.Width);
				window.QueueResize ();
			}
			
			
		}
	}
	
	class MethodData
	{
		public IParameterDataProvider MethodProvider;
		public CodeCompletionContext CompletionContext;
		public int CurrentOverload;
	}
}
