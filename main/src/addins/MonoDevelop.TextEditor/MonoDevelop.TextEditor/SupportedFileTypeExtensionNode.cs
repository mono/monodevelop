//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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

using Mono.Addins;

namespace MonoDevelop.TextEditor
{
	sealed class SupportedFileTypeExtensionNode : ExtensionNode
	{
		[NodeAttribute ("extensions", "Comma separated list of file extensions. The file must match one of these or one of the mime types.")]
		public string [] Extensions { get; private set; }

		[NodeAttribute ("mimeTypes", "Comma separated list of mime types. The file must match one of these or one of the extensions.")]
		public string [] MimeTypes { get; private set; }

		[NodeAttribute ("buildAction", Description = "If specified, the file must have this build action")]
		public string BuildAction { get; private set; }

		[NodeAttribute ("featureFlag", Description = "ID of a feature flag that can be used to enable/disable editing of this file type in the new editor")]
		public string FeatureFlag { get; private set; }

		[NodeAttribute ("featureFlagDefault", Description = "Default value of the feature flag")]
		public bool FeatureFlagDefault { get; private set; }
	}
}