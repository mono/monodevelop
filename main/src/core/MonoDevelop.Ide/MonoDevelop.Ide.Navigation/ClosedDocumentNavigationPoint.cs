//
// ClosedDocumentNavigationPoint.cs
//
// Author:
//       Therzok <therzok@gmail.com>
//
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Navigation
{
	public class ClosedDocumentNavigationPoint : NavigationPoint
	{
		FilePath fileName;
		int position;

		public ClosedDocumentNavigationPoint (FilePath fileName, int position)
		{
			this.fileName = fileName;
			this.position = position;
		}

		public override void Dispose ()
		{
			if (fileName != null) {
				fileName = null;
			}
			base.Dispose ();
		}

		FilePath FileName {
			get { return fileName; }
		}

		public override void Show ()
		{
			DoShow ();
			Dispose ();
		}

		protected virtual Document DoShow ()
		{
			Document doc = IdeApp.Workbench.OpenDocument (fileName, true);
			IdeApp.Workbench.ReorderTab (IdeApp.Workbench.Documents.IndexOf (doc), position);
			return doc;
		}

		public override string DisplayName {
			get {
				return System.IO.Path.GetFileName ((string) fileName);
			}
		}

		public override bool Equals (object o)
		{
			ClosedDocumentNavigationPoint dp = o as ClosedDocumentNavigationPoint;
			return dp != null && ((FileName != FilePath.Null && FileName == dp.FileName));
		}

		public override int GetHashCode ()
		{
			return (FileName != FilePath.Null ? FileName.GetHashCode () : 0);
		}

		internal bool HandleRenameEvent (string oldName, string newName)
		{
			if (oldName == fileName) {
				fileName = newName;
				return true;
			}
			return false;
		}
	}
}

