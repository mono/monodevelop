//
// PendingPackageActionsInformation.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;

namespace MonoDevelop.PackageManagement
{
	class PendingPackageActionsInformation
	{
		public bool IsInstallPending { get; private set; }
		public bool IsUninstallPending { get; private set; }
		public bool IsRestorePending { get; private set; }

		public void Add (IEnumerable<IPackageAction> actions)
		{
			foreach (var action in actions)
				Add (action);
		}

		public void Add (IPackageAction action)
		{
			switch (action.ActionType) {
			case PackageActionType.Install:
				IsInstallPending = true;
				break;
			case PackageActionType.Uninstall:
				IsUninstallPending = true;
				break;
			case PackageActionType.Restore:
				IsRestorePending = true;
				break;
			}
		}
	}
}
