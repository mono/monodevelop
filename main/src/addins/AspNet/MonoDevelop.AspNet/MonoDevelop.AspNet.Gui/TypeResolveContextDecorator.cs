//// 
//// TypeResolveContextDecorator.cs
////  
//// Author:
////       Mike Krüger <mkrueger@novell.com>
//// 
//// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
//
//using System;
//using System.Collections.Generic;
//using System.Diagnostics.Contracts;
//
//using ICSharpCode.NRefactory.Utils;
//using ICSharpCode.NRefactory.TypeSystem;
//
//namespace MonoDevelop.AspNet.Gui
//{
//	public class TypeResolveContextDecorator : ITypeResolveContext
//	{
//		protected ITypeResolveContext ctx;
//
//		public TypeResolveContextDecorator (ITypeResolveContext ctx)
//		{
//			this.ctx = ctx;
//		}
//
//		public virtual ITypeDefinition GetKnownTypeDefinition (TypeCode typeCode)
//		{
//			return ctx.GetKnownTypeDefinition (typeCode);
//		}
//
//		public virtual ITypeDefinition GetTypeDefinition(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
//		{
//			return ctx.GetTypeDefinition(nameSpace, name, typeParameterCount, nameComparer);
//		}
//
//		public virtual IEnumerable<ITypeDefinition> GetTypes()
//		{
//			return ctx.GetTypes();
//		}
//
//		public virtual IEnumerable<ITypeDefinition> GetTypes(string nameSpace, StringComparer nameComparer)
//		{
//			return ctx.GetTypes(nameSpace, nameComparer);
//		}
//
//		public virtual IEnumerable<string> GetNamespaces()
//		{
//			return ctx.GetNamespaces();
//		}
//
//		public virtual string GetNamespace(string nameSpace, StringComparer nameComparer)
//		{
//			return ctx.GetNamespace(nameSpace, nameComparer);
//		}
//
//		public virtual CacheManager CacheManager {
//			get {
//				return ctx.CacheManager;
//			}
//		}
//	}
//}
//
