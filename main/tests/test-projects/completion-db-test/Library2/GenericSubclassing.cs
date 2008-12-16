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

namespace Library2
{
	public class GenericWidget
	{
	}

	public class GenericWidget<T>
	{
	}

	public class GenericWidget<T1,T2>
	{
	}

	public class GenericBin: GenericWidget
	{
	}

	public class GenericBin<T>: GenericWidget<T>
	{
	}

	public class GenericBin<T1,T2>: GenericWidget<T1,T2>
	{
	}

	public class SpecialGenericBin<T>: GenericWidget<string,T>
	{
	}

	public class GenericBinInt: GenericWidget<int>
	{
	}

	public class GenericBinString: GenericWidget<string>
	{
	}

	public class GenericBinInt<T>: GenericWidget<int>
	{
	}

	public class GenericBinStringInt: GenericWidget<string,int>
	{
	}

	public class GenericBinIntString: GenericWidget<int,string>
	{
	}

	public class GenericBinStringInt<T>: GenericWidget<string,int>
	{
	}

	public class GenericBinStringIntInst: GenericBinStringInt<int>
	{
	}

	public class Container
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
