// CompletionListWindow.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components;
using System.Linq;
using MonoDevelop.Ide.Editor.Extension;
using System.ComponentModel;
using System.Threading;
using Xwt.Drawing;
using Xwt;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class CompletionListWindow
	{
		ICompletionView window;
		CompletionController controller;

		public CompletionData SelectedItem {
			get {
				return controller.SelectedItem;
			}
		}

		public int SelectedItemIndex {
			get { return controller.SelectedItemIndex; }
		}

		public event EventHandler SelectionChanged {
			add { controller.SelectionChanged += value; }
			remove { controller.SelectionChanged -= value; }
		}

		public CompletionListWindow ()
		{
			window = new CompletionListWindowGtk ();
			controller = new CompletionController (this, window);
		}

		internal CompletionListWindow (ICompletionView w)
		{
			window = w;
			controller = new CompletionController (this, w);
		}

		internal static CompletionListWindow CreateAsDialog ()
		{
			var gtkWindow = new CompletionListWindowGtk (Gtk.WindowType.Toplevel);
			gtkWindow.TypeHint = Gdk.WindowTypeHint.Dialog;
			gtkWindow.Decorated = false;
			return new CompletionListWindow (gtkWindow);
		}

		public Xwt.Rectangle Allocation {
			get {
				return window.Allocation;
			}
		}

		public CodeCompletionContext CodeCompletionContext {
			get { return controller.CodeCompletionContext; }
			set { controller.CodeCompletionContext = value; }
		} 

		internal int StartOffset {
			get { return controller.StartOffset; }
			set { controller.StartOffset = value; }
		}

		public int EndOffset {
			get { return controller.EndOffset; }
			set { controller.EndOffset = value; }
		}

		internal ICompletionWidget CompletionWidget {
			get { return controller.CompletionWidget; }
			set { controller.CompletionWidget = value; }
		}

		public bool Visible {
			get { return controller.Visible; }
		}

		public int X {
			get { return window.X; }
		}

		public int Y {
			get { return window.Y; }
		}

		public bool AutoSelect {
			get { return controller.AutoSelect; }
			set { controller.AutoSelect = value; }
		}

		public bool SelectionEnabled {
			get { return controller.SelectionEnabled; }
		}

		public bool InCategoryMode {
			get { return controller.ShowCategories; }
			set { controller.ShowCategories = value; }
		}

		public bool AutoCompleteEmptyMatch {
			get { return controller.AutoCompleteEmptyMatch; }
			set { controller.AutoCompleteEmptyMatch = value; }
		}

		public bool AutoCompleteEmptyMatchOnCurlyBrace {
			get { return controller.AutoCompleteEmptyMatchOnCurlyBrace; }
			set { controller.AutoCompleteEmptyMatchOnCurlyBrace = value; }
		}

		public string CompletionString {
			get { return controller.CompletionString; }
			set { controller.CompletionString = value; }
		}

		public string DefaultCompletionString {
			get { return controller.DefaultCompletionString; }
			set { controller.DefaultCompletionString = value; }
		}

		public bool CloseOnSquareBrackets {
			get { return controller.CloseOnSquareBrackets; }
			set { controller.CloseOnSquareBrackets = value; }
		}

		public int InitialWordLength {
			get { return controller.InitialWordLength; }
		}

		public event EventHandler<CodeCompletionContextEventArgs> WordCompleted {
			add { controller.WordCompleted += value; }
			remove { controller.WordCompleted -= value; }
		}

		public event EventHandler VisibleChanged {
			add { controller.VisibleChanged += value; }
			remove { controller.VisibleChanged -= value; }
		}

		internal Gtk.Window TransientFor {
			get { return window.TransientFor; }
			set { window.TransientFor = value; }
		}

		public CompletionTextEditorExtension Extension {
			get { return controller.Extension; }
			set { controller.Extension = value; }
		}

		internal void InitializeListWindow (ICompletionWidget completionWidget, CodeCompletionContext completionContext)
		{
			controller.InitializeSession (completionWidget, completionContext);
		}

		internal bool ShowListWindow (char firstChar, ICompletionDataList list, ICompletionWidget completionWidget, CodeCompletionContext completionContext)
		{
			controller.InitializeSession (completionWidget, completionContext);
			return ShowListWindow (list);
		}

		internal bool ShowListWindow (ICompletionDataList list)
		{
			Counters.ProcessCodeCompletion.Trace ("Begin show completion window");

			var r = controller.ShowListWindow (list);

			Counters.ProcessCodeCompletion.Trace ("End show completion window");

			return r;
		}

		public void Show ()
		{
			controller.ShowWindow ();
		}

		public void Destroy ()
		{
			controller.Dispose ();
		}

		public string PartialWord {
			get {
				return controller.PartialWord;
			}
		}

		public string CurrentPartialWord {
			get {
				return controller.CurrentPartialWord;
			}
		}

		public bool IsUniqueMatch {
			get {
				return controller.IsUniqueMatch;
			}
		}

		public bool PreProcessKeyEvent (KeyDescriptor descriptor)
		{
			return controller.PreProcessKeyEvent (descriptor);
		}

		public void PostProcessKeyEvent (KeyDescriptor descriptor)
		{
			controller.PostProcessKeyEvent (descriptor);
		}

		internal bool IsInCompletion {
			get {
				return controller.IsInCompletion;
			}
		}

		public void UpdateWordSelection ()
		{
			controller.UpdateWordSelection ();
		}

		public void RepositionWindow (Xwt.Rectangle? newCaret = null)
		{
			var r = newCaret != null ? new Xwt.Rectangle ((int)newCaret.Value.X, (int)newCaret.Value.Y, (int)newCaret.Value.Width, (int)newCaret.Value.Height) : (Xwt.Rectangle?)null;
			window.RepositionWindow (r);
		}

		public void HideWindow ()
		{
			controller.HideWindow ();
		}

		[Obsolete("Use CompletionWindowManager.ToggleCategoryMode")]
		public void ToggleCategoryMode ()
		{
			controller.ToggleCategoryMode ();
		}

		/// <summary>
		/// Gets or sets a value indicating that shift was pressed during enter.
		/// </summary>
		/// <value>
		/// <c>true</c> if was shift pressed; otherwise, <c>false</c>.
		/// </value>
		public bool WasShiftPressed {
			get { return controller.WasShiftPressed; }
		}

		public void ResetSizes ()
		{
			controller.ResetSizes ();
		}

		public List<int> FilteredItems {
			get {
				return controller.FilteredItems;
			}
		}

		internal ICompletionDataList CompletionDataList => controller.CompletionDataList;

		internal void ClearMruCache ()
		{
			controller.ClearMruCache ();
		}

		public bool CompleteWord ()
		{
			return controller.CompleteWord ();
		}

		internal List<CompletionData> GetFilteredItems ()
		{
			var result = new List<CompletionData> ();
			foreach (var i in controller.FilteredItems)
				result.Add (controller.CompletionDataList [i]);
			return result;
		}
	}
}
