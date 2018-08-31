//
// AbstractRadioButtonViewModel.cs
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
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace MonoDevelop.Refactoring.Options
{
	internal abstract class AbstractRadioButtonViewModel : AbstractNotifyPropertyChanged
	{
		private readonly AbstractOptionPreviewViewModel _info;
		internal readonly string Preview;
		private bool _isChecked;

		public string Description { get; }
		public string GroupName { get; }

		public bool IsChecked {
			get {
				return _isChecked;
			}

			set {
				SetProperty (ref _isChecked, value);

				if (_isChecked) {
					SetOptionAndUpdatePreview (_info, Preview);
				}
			}
		}

		public AbstractRadioButtonViewModel (string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options, bool isChecked, string group)
		{
			Description = description;
			this.Preview = preview;
			_info = info;
			this.GroupName = group;

			SetProperty (ref _isChecked, isChecked);
		}

		internal abstract void SetOptionAndUpdatePreview (AbstractOptionPreviewViewModel info, string preview);
	}
}
