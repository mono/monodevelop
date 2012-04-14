//// 
//// AspProjectDom.cs
////  
//// Author:
////       Michael Hutchinson <mhutchinson@novell.com>
//// 
//// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
//// 
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
//// 
//// The above copyright notice and this permission notice shall be included in
//// all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//// THE SOFTWARE.
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using ICSharpCode.NRefactory.TypeSystem;
//using ICSharpCode.NRefactory.TypeSystem.Implementation;
//
//
//namespace MonoDevelop.AspNet.Gui
//{
//	/// <summary>
//	/// This wraps a project dom and adds the compilation information from the ASP.NET page to the DOM to lookup members
//	/// on the page.
//	/// </summary>
//	class AspProjectDomWrapper : TypeResolveContextDecorator
//	{
//		DocumentInfo info;
//		
//		//FIXME: use all the doms
//		//FIXME: merge the items from the members visitor too
//		public AspProjectDomWrapper (DocumentInfo info) : base (info.References[0])
//		{
//			this.info = info;
//		}
//		
//		ITypeDefinition constructedType = null;
//		ITypeDefinition CheckType (ITypeDefinition type)
//		{
//			if (type == null)
//				return null;
//			var cu = info.ParsedDocument;
//			var firstType = cu.TopLevelTypeDefinitions.FirstOrDefault ();
//			if (firstType != null && firstType.FullName == type.FullName) {
//				if (constructedType != null)
//					return constructedType;
//				constructedType = CompoundTypeDefinition.Create (new [] { firstType, type, info.CodeBesideClass });
////				constructedType.GetProjectContent () = this;
//				return constructedType;
//			}
//			return type;
//		}
//		
//		public override ITypeDefinition GetTypeDefinition (string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
//		{
//			return CheckType (base.GetTypeDefinition (nameSpace, name, typeParameterCount, nameComparer));
//		}
//		
//		public override IEnumerable<ITypeDefinition> GetTypes()
//		{
//			return base.GetTypes ().Select (t => CheckType (t));
//		}
//		
//		public override IEnumerable<ITypeDefinition> GetTypes(string nameSpace, StringComparer nameComparer)
//		{
//			return base.GetTypes (nameSpace, nameComparer).Select (t => CheckType (t));
//		}
//		
//	}
//}
//
