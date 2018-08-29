//
// AbstractNotifyPropertyChanged.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
	internal class AbstractNotifyPropertyChanged : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected void NotifyPropertyChanged ([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
		}

		/// <returns>True if the property was updated</returns>
		protected bool SetProperty<T> (ref T field, T value, [CallerMemberName] string propertyName = "")
		{
			if (!EqualityComparer<T>.Default.Equals (field, value)) {
				field = value;
				NotifyPropertyChanged (propertyName);
				return true;
			}

			return false;
		}
	}
}
