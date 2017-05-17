//
// DotNetCoreProjectTemplateWizard.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Ide.Templates;

namespace MonoDevelop.DotNetCore.Templating
{
	class DotNetCoreProjectTemplateWizard : TemplateWizard
	{
		public override WizardPage GetPage (int pageNumber)
		{
			return new DotNetCoreProjectTemplateWizardPage (this);
		}

		public override int TotalPages {
			get { return GetTotalPages (); }
		}

		public override string Id {
			get { return "MonoDevelop.DotNetCore.ProjectTemplateWizard"; }
		}

		/// <summary>
		/// When only .NET Core 2.0 is installed there is only one option in the drop down
		/// list for the target framework for .NET Core projects so there is no point in displaying
		/// the wizard since nothing can be changed. If .NET Core 1.0 is installed then there is at
		/// least two options available. If the .NET Standard project template is selected then there
		/// are multiple options available.
		/// </summary>
		int GetTotalPages ()
		{
			if (IsSupportedParameter ("NetStandard"))
				return 1;

			if (IsOnlyDotNetCore2Installed ())
				return 0;

			return 1;
		}

		bool IsOnlyDotNetCore2Installed ()
		{
			return DotNetCoreRuntime.IsNetCore20Installed () &&
				!DotNetCoreRuntime.IsNetCore1xInstalled ();
		}
	}
}
