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
		public FilePathPropertyInfo (PropertyDescriptor propertyInfo, object propertyProvider, ValueSources valueSources) : base (propertyInfo, propertyProvider, valueSources)
		{
		}

		public override Type Type => typeof (FilePath);

		internal override void SetValue<T> (object target, T value)
		{
			if (value is FilePath filePath) {
				PropertyDescriptor.SetValue (PropertyProvider, new MonoDevelop.Core.FilePath (filePath.Source));
			} else {
				Console.WriteLine ("Value: {0} of type {1} is not a DirectoryPath", value, value.GetType ());
			}
		}

		internal override Task<T> GetValueAsync<T> (object target)
		{
			if (target is Core.FilePath directoryPath) {
				T result = (T)(object)new FilePath (directoryPath.FullPath);
				return Task.FromResult (result);
			}
			Console.WriteLine ("Value: {0} of type {1} is not a DirectoryPath", target, target.GetType ());
			return base.GetValueAsync<T> (target);
		}
	}
}

#endif