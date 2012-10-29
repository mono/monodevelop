// 
// WebServiceEngineWCF.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System.Xml.Serialization;
using System.Xml;
using System.Collections.Generic;

namespace MonoDevelop.WebReferences.WCF
{
	public class ClientOptions
	{
		public ClientOptions ()
		{
			EnableDataBinding = true;
			GenerateSerializableTypes = true;
			UseSerializerForFaults = true;
			ReferenceAllAssemblies = true;
			Serializer = "Auto";
			CollectionMappings = new List<CollectionMapping> ();
			ReferencedAssemblies = new List<ReferencedAssembly> ();
		}
		
		[XmlElement]
	    public bool GenerateAsynchronousMethods { get; set; }
		[XmlElement]
	    public bool EnableDataBinding { get; set; }
//	    string[] ExcludedTypes;
	    public bool ImportXmlTypes { get; set; }
	    public bool GenerateInternalTypes { get; set; }
	    public bool GenerateMessageContracts { get; set; }
//	    string[] NamespaceMappings;
	    public List<CollectionMapping> CollectionMappings { get; set; }
	    public bool GenerateSerializableTypes { get; set; }
	    public string Serializer { get; set; }
	    public bool UseSerializerForFaults { get; set; }
	    public bool ReferenceAllAssemblies { get; set; }
	    List<ReferencedAssembly> ReferencedAssemblies { get; set; }
//	    string[] ReferencedDataContractTypes;
//	    string[] ServiceContractMappings;
	}
}
