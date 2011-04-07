// 
// SolutionItem.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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


namespace MonoDevelop.Projects
{
	/// <summary>
	/// Specifies a configuration to be used for solution and project operations
	/// </summary>
	public class ConfigurationSelector
	{
		/// <summary>
		/// A configuration selector which selects the default (active) project or solution configuration
		/// </summary>
		public static readonly ConfigurationSelector Default = new DefaultConfigurationSelector ();
		
		/// <summary>
		/// Gets the configuration selected by this instance for a given target
		/// </summary>
		/// <returns>
		/// The configuration.
		/// </returns>
		/// <param name='target'>
		/// A target
		/// </param>
		public virtual ItemConfiguration GetConfiguration (IConfigurationTarget target)
		{
			return null;
		}
	}
}
