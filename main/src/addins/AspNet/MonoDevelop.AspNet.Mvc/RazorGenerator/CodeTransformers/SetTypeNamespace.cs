//
// From Razor Generator (http://razorgenerator.codeplex.com/)
// Licensed under the Microsoft Public License (MS-PL)
//

using System.Collections.Generic;

namespace RazorGenerator.Core
{
	class SetTypeNamespace : RazorCodeTransformerBase
	{
		private readonly string _namespace;

		public SetTypeNamespace(string @namespace)
		{
			_namespace = @namespace;
		}

		public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
		{
			razorHost.DefaultNamespace = _namespace;
		}
	}
}