//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace MonoDevelop.SourceEditor.Braces
{
	using Microsoft.VisualStudio.Text;
	using Microsoft.VisualStudio.Text.BraceCompletion;
	using Microsoft.VisualStudio.Text.Editor;
	using Microsoft.VisualStudio.Utilities;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using Microsoft.CodeAnalysis.Text;

	/// <summary>
	/// This class combines IBraceCompletionSessionProvider, IBraceCompletionContextProvider, and IBraceCompletionDefaultProvider
	/// session providers. The aggregator will create a session using the best match with the following priorities.
	/// 
	/// 1. OpeningBrace
	/// 2. ContentType
	/// 3. Provider type: SessionProviders > ContextProviders > DefaultProviders
	/// </summary>
	internal sealed class BraceCompletionAggregator : IBraceCompletionAggregator
	{
		#region Private Members

		private BraceCompletionAggregatorFactory _factory;
		private Dictionary<char, List<IContentType>> _contentTypeCache;
		private Dictionary<char, Dictionary<IContentType, List<ProviderHelper>>> _providerCache;

		private string _openingBraces;
		private string _closingBraces;

		#endregion

		#region Constructors

		public BraceCompletionAggregator (BraceCompletionAggregatorFactory factory)
		{
			_factory = factory;

			Init ();
		}

		#endregion

		#region IBraceCompletionAggregator

		/// <summary>
		/// Attempt to create a session using the provider that best matches the buffer content type for 
		/// the given opening brace. This is called only for appropriate buffers in the view's buffer graph.
		/// </summary>
		public bool TryCreateSession (ITextView textView, SnapshotPoint openingPoint, char openingBrace, out IBraceCompletionSession session)
		{
			ITextBuffer buffer = openingPoint.Snapshot.TextBuffer;
			IContentType bufferContentType = buffer.ContentType;

			List<IContentType> contentTypes;
			if (_contentTypeCache.TryGetValue (openingBrace, out contentTypes)) {
				foreach (IContentType providerContentType in contentTypes) {
					// find a matching content type
					if (bufferContentType.IsOfType (providerContentType.TypeName)) {
						// try all providers for that type
						List<ProviderHelper> providersForBrace;
						if (_providerCache [openingBrace].TryGetValue (providerContentType, out providersForBrace)) {
							foreach (ProviderHelper provider in providersForBrace) {
								IBraceCompletionSession curSession = null;
								if (provider.TryCreate (_factory, textView, openingPoint, openingBrace, out curSession)) {
									session = curSession;
									return true;
								}
							}
						}

						// only try the best match
						break;
					}
				}
			}

			session = null;
			return false;
		}

		/// <summary>
		/// Checks the content type against the providers.
		/// </summary>
		/// <returns>True if providers exist for the given content type.</returns>
		public bool IsSupportedContentType (IContentType contentType, char openingBrace)
		{
			List<IContentType> contentTypes = null;
			if (_contentTypeCache.TryGetValue (openingBrace, out contentTypes)) {
				// check if any types match
				return contentTypes.Any (t => contentType.IsOfType (t.TypeName));
			}

			return false;
		}

		/// <summary>
		/// Gives a string containing all opening brace chars that have providers
		/// </summary>
		public string OpeningBraces {
			get {
				return _openingBraces;
			}
		}

		/// <summary>
		/// Gives a string containing all closing brace chars that have providers
		/// </summary>
		public string ClosingBraces {
			get {
				return _closingBraces;
			}
		}

		#endregion

		#region Private Helpers

		private static char GetClosingBrace (IBraceCompletionMetadata metadata, char openingBrace)
		{
			IEnumerator<char> opening = metadata.OpeningBraces.GetEnumerator ();
			IEnumerator<char> closing = metadata.ClosingBraces.GetEnumerator ();

			while (opening.MoveNext () && closing.MoveNext ()) {
				if (opening.Current == openingBrace) {
					return closing.Current;
				}
			}

			// This should never happen since we find the metadata based on the opening char
			Debug.Fail ("Unable to find the given brace in the provider metadata");
			return ' ';
		}

		// basic checks to avoid incorrect behavior such as char c = '\'''
		private static bool AllowDefaultSession (SnapshotPoint openingPoint, char openingBrace, char closingBrace)
		{
			// avoid opening a new session next to the same char
			if (openingBrace == closingBrace && openingPoint.Position > 0) {
				char prevChar = openingPoint.Subtract (1).GetChar ();
				if (openingBrace.Equals (prevChar)) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Build the _providerCache
		/// Each opening brace is a key with a value of the providers and metadata in a 
		/// sorted list. The list is order from most specific to least specific content
		/// types with the provider type sorted secondary.
		/// </summary>
		private void Init ()
		{
			HashSet<char> closing = new HashSet<char> ();
			_providerCache = new Dictionary<char, Dictionary<IContentType, List<ProviderHelper>>> ();
			_contentTypeCache = new Dictionary<char, List<IContentType>> ();

			List<ProviderHelper> providerHelpers = new List<ProviderHelper> ();
			providerHelpers.AddRange (_factory.SessionProviders.Select (p => new ProviderHelper (p)));
			providerHelpers.AddRange (_factory.ContextProviders.Select (p => new ProviderHelper (p)));
			providerHelpers.AddRange (_factory.DefaultProviders.Select (p => new ProviderHelper (p)));

			// build the cache
			// opening brace -> content type -> provider
			foreach (ProviderHelper providerHelper in providerHelpers) {
				foreach (char closeChar in providerHelper.Metadata.ClosingBraces) {
					closing.Add (closeChar);
				}

				foreach (char openingBrace in providerHelper.Metadata.OpeningBraces) {
					// opening brace level
					Dictionary<IContentType, List<ProviderHelper>> providersForBrace;
					if (!_providerCache.TryGetValue (openingBrace, out providersForBrace)) {
						providersForBrace = new Dictionary<IContentType, List<ProviderHelper>> ();
						_providerCache.Add (openingBrace, providersForBrace);
					}

					// convert the type names into IContentTypes for the cache
					foreach (IContentType contentType in providerHelper.Metadata.ContentTypes.Select ((typeName)
											 => _factory.ContentTypeRegistryService.GetContentType (typeName))
											.Where ((t) => t != null)) {
						// content type level
						List<ProviderHelper> curProviders;
						if (!providersForBrace.TryGetValue (contentType, out curProviders)) {
							curProviders = new List<ProviderHelper> ();
							providersForBrace.Add (contentType, curProviders);
						}

						curProviders.Add (providerHelper);
					}

					Debug.Assert (providersForBrace != null && providersForBrace.Count > 0, "providersForBrace should not be empty");
				}
			}

			_openingBraces = String.Join (String.Empty, _providerCache.Keys);
			_closingBraces = String.Join (String.Empty, closing);

			// sort caches
			foreach (KeyValuePair<char, Dictionary<IContentType, List<ProviderHelper>>> cache in _providerCache) {
				// sort the list of content types so the most specific ones are first
				_contentTypeCache.Add (cache.Key, SortContentTypes (cache.Value.Keys.ToList ()));

				Debug.Assert (!_contentTypeCache [cache.Key].Any (t =>
					   _contentTypeCache [cache.Key].Where (tt =>
							 tt.TypeName.Equals (t.TypeName, StringComparison.OrdinalIgnoreCase)).Count () != 1),
						"duplicate content types");

				// sort the providers by type
				foreach (IContentType t in cache.Value.Keys) {
					cache.Value [t].Sort ();
				}
			}
		}

		/// <summary>
		/// Sorts content types by most specific to least specific.
		/// This checks the type against all others until it finds one that it is
		/// a type of. List.Sort() does not work here since most types are unrelated.
		/// </summary>
		private List<IContentType> SortContentTypes (List<IContentType> contentTypes)
		{
			List<IContentType> sorted = new List<IContentType> (contentTypes.Count);

			foreach (IContentType contentType in contentTypes) {
				int i; // sorted pos
				bool added = false;

				for (i = 0; i < sorted.Count; i++) {
					if (contentType.IsOfType (sorted [i].TypeName)) {
						sorted.Insert (i, contentType);
						added = true;
						break;
					}
				}

				if (!added) {
					// the type was unrelated to all others, add it at the end
					sorted.Add (contentType);
				}
			}

			return sorted;
		}

		/// <summary>
		/// A private helper class to wrap lazy instances of Session, Context, and Default providers into one type.
		/// </summary>
		private class ProviderHelper : IComparable<ProviderHelper>
		{
			private Lazy<IBraceCompletionSessionProvider, IBraceCompletionMetadata> _sessionPair;
			private Lazy<IBraceCompletionContextProvider, IBraceCompletionMetadata> _contextPair;
			private Lazy<IBraceCompletionDefaultProvider, IBraceCompletionMetadata> _defaultPair;

			public ProviderHelper (Lazy<IBraceCompletionSessionProvider, IBraceCompletionMetadata> sessionPair)
			{
				_sessionPair = sessionPair;
			}

			public ProviderHelper (Lazy<IBraceCompletionContextProvider, IBraceCompletionMetadata> contextPair)
			{
				_contextPair = contextPair;
			}

			public ProviderHelper (Lazy<IBraceCompletionDefaultProvider, IBraceCompletionMetadata> defaultPair)
			{
				_defaultPair = defaultPair;
			}

			public bool IsSession {
				get {
					return _sessionPair != null;
				}
			}

			public bool IsContext {
				get {
					return _contextPair != null;
				}
			}

			public bool IsDefault {
				get {
					return _defaultPair != null;
				}
			}

			public IBraceCompletionMetadata Metadata {
				get {
					if (IsSession) {
						return _sessionPair.Metadata;
					}

					if (IsContext) {
						return _contextPair.Metadata;
					}

					return _defaultPair.Metadata;
				}
			}

			// Create the session
			public bool TryCreate (BraceCompletionAggregatorFactory factory, ITextView textView, SnapshotPoint openingPoint, char openingBrace, out IBraceCompletionSession session)
			{
				char closingBrace = GetClosingBrace (Metadata, openingBrace);

				if (IsSession) {
					bool created = false;
					IBraceCompletionSession currentSession = null;

					factory.GuardedOperations.CallExtensionPoint (() => {
						created = _sessionPair.Value.TryCreateSession (textView, openingPoint, openingBrace, closingBrace, out currentSession);
					});

					if (created) {
						session = currentSession;
						return true;
					}

					session = null;
					return false;
				} else if (IsContext) {
					// Get a language specific context and add it to a default session.
					IBraceCompletionContext context = null;

					// check AllowDefaultSession to avoid starting a new "" session next to a "
					if (AllowDefaultSession (openingPoint, openingBrace, closingBrace)) {
						bool created = false;

						factory.GuardedOperations.CallExtensionPoint (() => {
							created = _contextPair.Value.TryCreateContext (textView, openingPoint, openingBrace, closingBrace, out context);
						});

						if (created) {
							session = new BraceCompletionDefaultSession (textView, openingPoint.Snapshot.TextBuffer, openingPoint, openingBrace,
								closingBrace, factory.UndoManager, factory.EditorOperationsFactoryService, context);

							return true;
						}
					}

					session = null;
					return false;
				} else if (IsDefault) {
					// perform some basic checks on the buffer before going in
					if (AllowDefaultSession (openingPoint, openingBrace, closingBrace)) {
						session = new BraceCompletionDefaultSession (textView, openingPoint.Snapshot.TextBuffer, openingPoint, openingBrace,
							closingBrace, factory.UndoManager, factory.EditorOperationsFactoryService);

						return true;
					}
				}

				session = null;
				return false;
			}

			// Sort order: Session -> Context -> Default
			public int CompareTo (ProviderHelper other)
			{
				if (IsSession && !other.IsSession) {
					return -1;
				} else if (other.IsSession) {
					return 1;
				} else if (IsContext && !other.IsContext) {
					return -1;
				} else if (other.IsContext) {
					return 1;
				}

				// both providers are the same type
				return 0;
			}
		}

		#endregion
	}
}
