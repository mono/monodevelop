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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class ParameterInformationWindowManager
	{
		static MethodData currentMethodGroup;

		static ParameterInformationWindow window;
		
		public static bool IsWindowVisible {
			get { return currentMethodGroup != null; }
		}

		static ParameterInformationWindowManager ()
		{
			if (IdeApp.Workbench != null)
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
		internal static bool ProcessKeyEvent (CompletionTextEditorExtension ext, ICompletionWidget widget, KeyDescriptor descriptor)
		{
			if (currentMethodGroup == null)
				return false;

			if (descriptor.SpecialKey == SpecialKey.Down) {
				return OverloadDown (ext, widget);
			} else if (descriptor.SpecialKey == SpecialKey.Up) {
				return OverloadUp (ext, widget);
			}
			else if (descriptor.SpecialKey == SpecialKey.Escape) {
				HideWindow (ext, widget);
				return true;
			}
			return false;
		}

		internal static bool OverloadDown (CompletionTextEditorExtension ext, ICompletionWidget widget)
		{
			if (currentMethodGroup == null)
				return false;
			if (currentMethodGroup.MethodProvider.Count <= 1)
				return false;
			if (currentMethodGroup.CurrentOverload < currentMethodGroup.MethodProvider.Count - 1)
				currentMethodGroup.CurrentOverload ++;
			else
				currentMethodGroup.CurrentOverload = 0;
			window.ChangeOverload ();
			UpdateWindow (ext, widget);
			return true;
		}

		internal static bool OverloadUp (CompletionTextEditorExtension ext, ICompletionWidget widget)
		{
			if (currentMethodGroup == null)
				return false;
			if (currentMethodGroup.MethodProvider.Count <= 1)
				return false;
			if (currentMethodGroup.CurrentOverload > 0)
				currentMethodGroup.CurrentOverload --;
			else
				currentMethodGroup.CurrentOverload = currentMethodGroup.MethodProvider.Count - 1;
			window.ChangeOverload ();
			UpdateWindow (ext, widget);
			return true;
		}

		internal static void PostProcessKeyEvent (CompletionTextEditorExtension ext, ICompletionWidget widget, KeyDescriptor descriptor)
		{
		}

		static CancellationTokenSource updateSrc = new CancellationTokenSource ();
		internal static async void UpdateCursorPosition (CompletionTextEditorExtension ext, ICompletionWidget widget)
		{
			updateSrc.Cancel ();
			updateSrc = new CancellationTokenSource ();
			var token = updateSrc.Token;
			// Called after the key has been processed by the editor
			if (currentMethodGroup == null)
				return;

			var actualMethodGroup = new MethodData ();
			actualMethodGroup.CompletionContext = widget.CurrentCodeCompletionContext;
			actualMethodGroup.MethodProvider = await ext.ParameterCompletionCommand (widget.CurrentCodeCompletionContext);
			if (actualMethodGroup.MethodProvider != null && (currentMethodGroup == null || !actualMethodGroup.MethodProvider.Equals (currentMethodGroup.MethodProvider)))
				currentMethodGroup = actualMethodGroup;
			try {
				int pos = await ext.GetCurrentParameterIndex (currentMethodGroup.MethodProvider.StartOffset, token);
				if (pos == -1) {
					if (actualMethodGroup.MethodProvider == null) {
						currentMethodGroup = null;
					} else {
						pos = await ext.GetCurrentParameterIndex (actualMethodGroup.MethodProvider.StartOffset, token);
						currentMethodGroup = pos >= 0 ? actualMethodGroup : null;
					}
				}

				// If the user enters more parameters than the current overload has,
				// look for another overload with more parameters.
				UpdateOverload (ext, widget, token);

				// Refresh.
				UpdateWindow (ext, widget);
			} catch (OperationCanceledException) { }
		}

		internal static void RepositionWindow (CompletionTextEditorExtension ext, ICompletionWidget widget)
		{
			UpdateWindow (ext, widget);
		}
		
		internal static void ShowWindow (CompletionTextEditorExtension ext, ICompletionWidget widget, CodeCompletionContext ctx, ParameterHintingResult provider)
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
			currentMethodGroup = md;
			UpdateOverload (ext, widget, default (CancellationToken));
			UpdateWindow (ext, widget);
		}
		
		internal static void HideWindow (CompletionTextEditorExtension ext, ICompletionWidget widget)
		{
			currentMethodGroup = null;
			if (window != null)
				window.ChangeOverload ();
			UpdateWindow (ext, widget);
		}
		
		public static int GetCurrentOverload ()
		{
			if (currentMethodGroup == null)
				return -1;
			return currentMethodGroup.CurrentOverload;
		}
		
		public static ParameterHintingResult GetCurrentProvider ()
		{
			if (currentMethodGroup == null)
				return null;
			return currentMethodGroup.MethodProvider;
		}
		
		static async void UpdateOverload (CompletionTextEditorExtension ext, ICompletionWidget widget, CancellationToken token)
		{
			if (currentMethodGroup == null || window == null)
				return;

			// If the user enters more parameters than the current overload has,
			// look for another overload with more parameters.

			int bestOverload = await ext.GuessBestMethodOverload (currentMethodGroup.MethodProvider, currentMethodGroup.CurrentOverload, token);
			if (bestOverload != -1) {
				currentMethodGroup.CurrentOverload = bestOverload;
				window.ChangeOverload ();
				UpdateWindow (ext, widget);
			}
		}
		
		public static int X { get; private set; }
		public static int Y { get; private set; }
		public static bool wasAbove = false;
		/// <summary>
		/// This stores information about code completion window(not about Parameter information window)
		/// at time of last showing of Parameter window, so we know if we have reposition
		/// </summary>
		static bool wasCompletionWindowVisible;
		static int lastW = -1, lastH = -1;

		internal static async void UpdateWindow (CompletionTextEditorExtension textEditorExtension, ICompletionWidget completionWidget)
		{
			// Updates the parameter information window from the information
			// of the current method overload
			if (currentMethodGroup != null) {
				if (window == null) {
					window = new ParameterInformationWindow ();
					window.Ext = textEditorExtension;
					window.Widget = completionWidget;
					window.BoundsChanged += (o, args) => {
						if (window.Size.Width == lastW && window.Size.Height == lastH && wasCompletionWindowVisible == (CompletionWindowManager.Wnd?.Visible ?? false))
							return;
						PositionParameterInfoWindow (window.ScreenBounds);
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
				int curParam = window.Ext != null ? await window.Ext.GetCurrentParameterIndex (currentMethodGroup.MethodProvider.StartOffset) : 0;
				var geometry2 = window.Visible ? window.Screen.VisibleBounds.Width : 480;
				window.ShowParameterInfo (currentMethodGroup.MethodProvider, currentMethodGroup.CurrentOverload, curParam - 1, (int)geometry2);
				PositionParameterInfoWindow (window.ScreenBounds);
			}
			
			if (currentMethodGroup == null) {
				if (window != null) {
					window.HideParameterInfo ();
//					DestroyWindow ();
					wasAbove = false;
					wasCompletionWindowVisible = false;
					lastW = -1;
					lastH = -1;
				}
				return;
			}
		}

	
		static async void PositionParameterInfoWindow (Xwt.Rectangle allocation)
		{
			lastW = (int)allocation.Width;
			lastH = (int)allocation.Height;
			var isCompletionWindowVisible = wasCompletionWindowVisible = (CompletionWindowManager.Wnd?.Visible ?? false);
			var ctx = window.Widget.CurrentCodeCompletionContext;
			int cparam = window.Ext != null ? await window.Ext.GetCurrentParameterIndex (currentMethodGroup.MethodProvider.StartOffset) : 0;

			X = currentMethodGroup.CompletionContext.TriggerXCoord;
			if (isCompletionWindowVisible) {
				// place above
				Y = ctx.TriggerYCoord - ctx.TriggerTextHeight - (int)allocation.Height - 10;
			} else {
				// place below
				Y = ctx.TriggerYCoord;
			}

			var geometry = window.Visible ? window.Screen.VisibleBounds : Xwt.MessageDialog.RootWindow.Screen.VisibleBounds;

			window.ShowParameterInfo (currentMethodGroup.MethodProvider, currentMethodGroup.CurrentOverload, cparam - 1, (int)geometry.Width);

			if (X + allocation.Width > geometry.Right)
				X = (int)geometry.Right - (int)allocation.Width;
			if (Y < geometry.Top)
				Y = ctx.TriggerYCoord;
			if (wasAbove || Y + allocation.Height > geometry.Bottom) {
				Y = Y - ctx.TriggerTextHeight - (int)allocation.Height - 4;
				wasAbove = true;
			}

			if (isCompletionWindowVisible) {
				var completionWindow = new Xwt.Rectangle (CompletionWindowManager.X, CompletionWindowManager.Y, CompletionWindowManager.Wnd.Allocation.Width, CompletionWindowManager.Wnd.Allocation.Height);
				if (completionWindow.IntersectsWith (new Xwt.Rectangle (X, Y, allocation.Width, allocation.Height))) {
					X = (int) completionWindow.X;
					Y = (int)completionWindow.Y - (int)allocation.Height - 6;
					if (Y < 0)
						Y = (int)completionWindow.Bottom + 6;
				}
			}

			window.Location = new Xwt.Point(X, Y);
		}		
	}
		
	class MethodData
	{
		public ParameterHintingResult MethodProvider;
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
