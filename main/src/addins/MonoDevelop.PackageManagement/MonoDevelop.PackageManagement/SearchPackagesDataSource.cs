//
// SearchPackagesDataSource.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using Xwt.Drawing;
using ICSharpCode.PackageManagement;
using ICSharpCode.NRefactory.Utils;
using MonoDevelop.Core.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.PackageManagement
{
	public class SearchPackagesDataSource : ISearchDataSource
	{
		readonly SearchPopupSearchPattern searchPattern;

		public SearchPackagesDataSource (SearchPopupSearchPattern searchPattern)
		{
			this.searchPattern = searchPattern;
		}

		Image ISearchDataSource.GetIcon (int item)
		{
			return null;
		}

		string ISearchDataSource.GetMarkup (int item, bool isSelected)
		{
			return GettextCatalog.GetString ("Search Packages...");
		}

		string ISearchDataSource.GetDescriptionMarkup (int item, bool isSelected)
		{
			return null;
		}

		Task<TooltipInformation> ISearchDataSource.GetTooltip (CancellationToken token, int item)
		{
			return null;
		}

		double ISearchDataSource.GetWeight (int item)
		{
			return 0;
		}

		ISegment ISearchDataSource.GetRegion (int item)
		{
			return MonoDevelop.Core.Text.TextSegment.Invalid;
		}

		string ISearchDataSource.GetFileName (int item)
		{
			return null;
		}

		bool ISearchDataSource.CanActivate (int item)
		{
			return IsProjectSelected ();
		}

		bool IsProjectSelected ()
		{
			return PackageManagementServices.ProjectService.CurrentProject != null;
		}

		void ISearchDataSource.Activate (int item)
		{
			var runner = new AddPackagesDialogRunner ();
			runner.Run (searchPattern.UnparsedPattern);
		}

		int ISearchDataSource.ItemCount {
			get {
				if (IsProjectSelected ()) {
					return 1;
				}
				return 0;
			}
		}
	}
}

