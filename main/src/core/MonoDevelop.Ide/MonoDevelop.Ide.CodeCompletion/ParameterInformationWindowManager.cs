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

		static ParameterInformationWindowManager ()
		{
			IdeApp.Workbench.RootWindow.Destroyed += (sender, e) => DestroyWindow ();
		}

		static void DestroyWindow ()
		{
			if (window != null) {
				window.Destroy ();
				window = null;
			}
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
				window.ChangeOverload ();
				UpdateWindow (ext, widget);
				return true;
			} else if (key == Gdk.Key.Up) {
				if (cmd.MethodProvider.Count <= 1)
					return false;
				if (cmd.CurrentOverload > 0)
					cmd.CurrentOverload --;
				else
					cmd.CurrentOverload = cmd.MethodProvider.Count - 1;
				window.ChangeOverload ();
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
		}

		public static void UpdateCursorPosition (CompletionTextEditorExtension ext, ICompletionWidget widget)
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
		
		public static void ShowWindow (CompletionTextEditorExtension ext, ICompletionWidget widget, CodeCompletionContext ctx, ParameterDataProvider provider)
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
			if (window != null)
				window.ChangeOverload ();
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
			if (methods.Count == 0 || window == null)
				return;
			
			// If the user enters more parameters than the current overload has,
			// look for another overload with more parameters.
			MethodData md = methods [methods.Count - 1];

			int bestOverload = ext.GuessBestMethodOverload (md.MethodProvider, md.CurrentOverload);
			if (bestOverload != -1) {
				md.CurrentOverload = bestOverload;
				window.ChangeOverload ();
				UpdateWindow (ext, widget);
			}
		}
		
		public static int X { get; private set; }
		public static int Y { get; private set; }
		public static bool wasAbove = false;
		static bool wasVisi;
		static int lastW = -1, lastH = -1;

		internal static void UpdateWindow (CompletionTextEditorExtension textEditorExtension, ICompletionWidget completionWidget)
		{
			// Updates the parameter information window from the information
			// of the current method overload
			if (methods.Count > 0) {
				if (window == null) {
					window = new ParameterInformationWindow ();
					window.Ext = textEditorExtension;
					window.Widget = completionWidget;
					window.SizeAllocated += delegate(object o, SizeAllocatedArgs args) {
						if (args.Allocation.Width == lastW && args.Allocation.Height == lastH && wasVisi == CompletionWindowManager.IsVisible)
							return;
						PositionParameterInfoWindow (args.Allocation);
					};
					window.Hidden += delegate {
						lastW = -1;
						lastH = -1;
					};
				} else {
					window.Ext = textEditorExtension;
					window.Widget = completionWidget;
				}
				wasAbove = false;
				var lastMethod = methods [methods.Count - 1];
				int curParam = window.Ext != null ? window.Ext.GetCurrentParameterIndex (lastMethod.MethodProvider.StartOffset) : 0;
				var geometry2 = DesktopService.GetUsableMonitorGeometry (window.Screen, window.Screen.GetMonitorAtPoint (X, Y));
				window.ShowParameterInfo (lastMethod.MethodProvider, lastMethod.CurrentOverload, curParam - 1, geometry2.Width);
				PositionParameterInfoWindow (window.Allocation);
				window.Show ();
			}
			
			if (methods.Count == 0) {
				if (window != null) {
//					window.HideParameterInfo ();
					DestroyWindow ();
					wasAbove = false;
					wasVisi = false;
					lastW = -1;
					lastH = -1;
				}
				return;
			}
		}

	
		static void PositionParameterInfoWindow (Rectangle allocation)
		{
			lastW = allocation.Width;
			lastH = allocation.Height;
			wasVisi = CompletionWindowManager.IsVisible;
			var ctx = window.Widget.CurrentCodeCompletionContext;
			var md = methods [methods.Count - 1];
			int cparam = window.Ext != null ? window.Ext.GetCurrentParameterIndex (md.MethodProvider.StartOffset) : 0;

			X = md.CompletionContext.TriggerXCoord;
			if (CompletionWindowManager.IsVisible) {
				// place above
				Y = ctx.TriggerYCoord - ctx.TriggerTextHeight - allocation.Height - 10;
			} else {
				// place below
				Y = ctx.TriggerYCoord;
			}

			var geometry = DesktopService.GetUsableMonitorGeometry (window.Screen, window.Screen.GetMonitorAtPoint (X, Y));

			window.ShowParameterInfo (md.MethodProvider, md.CurrentOverload, cparam - 1, geometry.Width);

			if (X + allocation.Width > geometry.Right)
				X = geometry.Right - allocation.Width;
			if (Y < geometry.Top)
				Y = ctx.TriggerYCoord;
			if (wasAbove || Y + allocation.Height > geometry.Bottom) {
				Y = Y - ctx.TriggerTextHeight - allocation.Height - 4;
				wasAbove = true;
			}

			if (CompletionWindowManager.IsVisible) {
				var completionWindow = new Rectangle (CompletionWindowManager.X, CompletionWindowManager.Y, CompletionWindowManager.Wnd.Allocation.Width, CompletionWindowManager.Wnd.Allocation.Height);
				if (completionWindow.IntersectsWith (new Rectangle (X, Y, allocation.Width, allocation.Height))) {
					X = completionWindow.X;
					Y = completionWindow.Y - allocation.Height - 6;
					if (Y < 0)
						Y = completionWindow.Bottom + 6;
				}
			}

			window.Move (X, Y);
		}		
	}
		
	class MethodData
	{
		public ParameterDataProvider MethodProvider;
		public CodeCompletionContext CompletionContext;
		int currentOverload;
		public int CurrentOverload {
			get {
				return currentOverload;
			}
			set {
				currentOverload = value;
			}
		}
	}
}
