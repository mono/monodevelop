//
// DescriptorPropertyInfo.cs
//
// Author:
//       jmedrano <josmed@microsoft.com>
//
// Copyright (c) 2018 
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

#if MAC

using System;
using System.Threading.Tasks;
using Xamarin.PropertyEditing;
using System.ComponentModel;

namespace MonoDevelop.DesignerSupport
{
	class FilePathPropertyInfo
		: DescriptorPropertyInfo, IEquatable<DescriptorPropertyInfo>
	{
		public FilePathPropertyInfo (TypeDescriptorContext typeDescriptorContext, ValueSources valueSources) : base (typeDescriptorContext, valueSources)
		{
		}

		public override Type Type => typeof (Xamarin.PropertyEditing.Common.FilePath);

		internal override void SetValue<T> (object target, T value)
		{
			try {
				if (value is Xamarin.PropertyEditing.Common.FilePath filePath) {
					PropertyDescriptor.SetValue (PropertyProvider, new MonoDevelop.Core.FilePath (filePath.Source));
				} else {
					throw new Exception (string.Format ("Value: {0} of type {1} is not a DirectoryPath", value, value.GetType ()));
				}
			} catch (Exception ex) {
				LogInternalError ($"Error trying to get and convert value:'{value}' T:{typeof (T).FullName} ", ex);
			}
		}

		internal override T GetValue<T> (object target)
		{
			try {
				var currentObject = PropertyDescriptor.GetValue (PropertyProvider);
				if (currentObject is Core.FilePath filePath) {
					T result = (T)(object)new Xamarin.PropertyEditing.Common.FilePath (filePath.FullPath);
					return result;
				}
			} catch (Exception ex) {
				LogInternalError ($"Error trying to get and convert value:'{target}' T:{typeof (T).FullName} ", ex);
			}
			return base.GetValue<T> (target);
		}
	}
}

#endif