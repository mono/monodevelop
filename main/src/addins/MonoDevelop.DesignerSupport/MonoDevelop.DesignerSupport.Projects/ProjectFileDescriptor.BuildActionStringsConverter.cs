//
//Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.ComponentModel;

namespace MonoDevelop.DesignerSupport
{
	partial class ProjectFileDescriptor
	{
		[MonoDevelop.Components.PropertyGrid.PropertyEditors.StandardValuesSeparator ("--")]
		class BuildActionStringsConverter : StandardStringsConverter
		{
			public override System.Collections.ICollection GetStandardStrings (ITypeDescriptorContext context)
			{
				ProjectFileDescriptor descriptor = context != null ?
					context.Instance as ProjectFileDescriptor : null;

				if (descriptor != null && descriptor.file != null && descriptor.file.Project != null) {
					return descriptor.file.Project.GetBuildActions (descriptor.file.FilePath);
				} else {
					return new string [] { "Content", "None", "Compile" };
				}
			}

			public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
			{
				return destinationType == typeof (string);
			}

			public override object ConvertTo (ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
			{
				return (string)value;
			}

			public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
			{
				return sourceType == typeof (string);
			}

			public override object ConvertFrom (ITypeDescriptorContext context,
												System.Globalization.CultureInfo culture, object value)
			{
				if (!IsValid (context, value))
					throw new FormatException ("Invalid build target name");

				return (string)value;
			}

			public override bool IsValid (ITypeDescriptorContext context, object value)
			{
				if (!(value is string))
					return false;

				string str = (string)value;
				if (string.IsNullOrEmpty (str) || !char.IsLetter (str [0]))
					return false;

				for (int i = 1; i < str.Length; i++) {
					char c = str [i];
					if (char.IsLetterOrDigit (c) || c == '_')
						continue;
					else
						return false;
				}

				return true;
			}
			public override bool GetStandardValuesExclusive (ITypeDescriptorContext context)
			{
				return false;
			}
		}
	}
}
