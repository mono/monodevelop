// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.ExtractMethod
{
	public enum TriviaLocation
    {
        BeforeBeginningOfSpan = 0,
        AfterBeginningOfSpan,
        BeforeEndOfSpan,
        AfterEndOfSpan
    }

	public struct PreviousNextTokenPair
    {
        public SyntaxToken PreviousToken { get; set; }
        public SyntaxToken NextToken { get; set; }
    }

	public struct LeadingTrailingTriviaPair
    {
        public IEnumerable<SyntaxTrivia> LeadingTrivia { get; set; }
        public IEnumerable<SyntaxTrivia> TrailingTrivia { get; set; }
    }

	public delegate SyntaxToken AnnotationResolver(SyntaxNode root, TriviaLocation location, SyntaxAnnotation annotation);
	public delegate IEnumerable<SyntaxTrivia> TriviaResolver(TriviaLocation location, PreviousNextTokenPair tokenPair, Dictionary<SyntaxToken, LeadingTrailingTriviaPair> triviaMap);

    /// <summary>
    /// contains information to restore trivia later on to the annotated tree
    /// </summary>
	public interface ITriviaSavedResult
    {
        /// <summary>
        /// root node of the annotated tree.
        /// </summary>
        SyntaxNode Root { get; }

        /// <summary>
        /// restore saved trivia to given tree
        /// </summary>
        /// <param name="root">root node to the annotated tree</param>
        /// <param name="annotationResolver">it provides a custom way of resolving annotations to retrieve right tokens to attach trivia</param>
        /// <param name="triviaResolver">it provides a custom way of creating trivia list between two tokens</param>
        /// <returns>root node to a trivia restored tree</returns>
        SyntaxNode RestoreTrivia(SyntaxNode root, AnnotationResolver annotationResolver = null, TriviaResolver triviaResolver = null);
    }

    /// <summary>
    /// syntax trivia related services
    /// </summary>
	public interface ISyntaxTriviaService : ILanguageService
    {
        /// <summary>
        /// save trivia around span and let user restore trivia later
        /// </summary>
        /// <param name="root">root node of a tree</param>
        /// <param name="textSpan">selection whose trivia around its edges will be saved</param>
        /// <returns>object that holds onto enough information to restore trivia later</returns>
        ITriviaSavedResult SaveTriviaAroundSelection(SyntaxNode root, TextSpan textSpan);
    }
}
