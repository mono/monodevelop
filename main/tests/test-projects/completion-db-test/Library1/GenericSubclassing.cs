// GenericSubclassing.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using Library2;

namespace Library1
{
	public class SubGenericWidget: GenericWidget
	{
	}

	public class SubGenericWidget<T>: GenericWidget<T>
	{
	}

	public class SubGenericWidget<T1,T2>: GenericWidget<T1,T2>
	{
	}

	public class SubGenericBin: GenericBin
	{
	}

	public class SubGenericBin<T>: GenericBin<T>
	{
	}

	public class SubGenericBin<T1,T2>: GenericBin<T1,T2>
	{
	}

	public class SubGenericBinInt1: GenericWidget<int>
	{
	}

	public class SubGenericBinInt2: GenericBinInt
	{
	}

	public class SubGenericBinInt<T>: GenericBinInt<T>
	{
	}

	public class SubGenericBinStringInt1: GenericBinStringInt
	{
	}

	public class SubGenericBinStringInt2: GenericWidget<string,int>
	{
	}

	public class SubGenericBinStringIntGen1<T>: GenericBinStringInt<T>
	{
	}

	public class SubGenericBinStringIntGen1: GenericBinStringInt<int>
	{
	}
	
	public class SubInnerClass<T>: Container.InnerClass1<T>
	{
	}

	public class SubGenericWidgetStringNull<T>: GenericWidget<string,T>
	{
	}

	public class SubGenericWidgetNullInt<T>: GenericWidget<T,int>
	{
	}

	public class SubGenericWidgetSwapped<T1,T2>: GenericWidget<T2,T1>
	{
	}

	public class SubContainer
	{
		public class InnerClass1<T>: GenericWidget<T>
		{
		}

		public class InnerClass2: GenericWidget<int>
		{
		}

		public class InnerClass3: InnerClass1<int>
		{
		}

		public class InnerClass4: InnerClass2
		{
		}
	}
}
