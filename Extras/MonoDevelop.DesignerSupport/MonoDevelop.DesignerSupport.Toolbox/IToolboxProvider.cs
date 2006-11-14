//
// IToolbox(Default|Dynamic)Provider.cs: Interface for extensions that 
//   provide toolbox items.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	
	//Used to fetch or generate the default toolbox items at the beginning of each MD session
	public interface IToolboxDefaultProvider
	{
		IList<ItemToolboxNode> GetItems ();
	}
	
	//Can provide dynamic toolbox items for a specific consumer. 
	public interface IToolboxDynamicProvider
	{
		//This method will be called each time the consumer changes. Return null if not
		//returning any items for a specific consumer.
		//TODO: an event to update the dynamic items in the toolbox at any other time
		IList<ItemToolboxNode> GetDynamicItems (IToolboxConsumer consumer);
	}
}
