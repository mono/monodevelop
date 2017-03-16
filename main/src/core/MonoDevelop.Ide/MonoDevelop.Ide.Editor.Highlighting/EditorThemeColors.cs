//
// EditorThemeColors.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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

using System;
using MonoDevelop.Components;
using Xwt.Drawing;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Linq;
using Cairo;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public static class EditorThemeColors
	{
		public static readonly string Background = "background";
		public static readonly string Foreground = "foreground";
		public static readonly string Caret = "caret";
		public static readonly string Invisibles = "invisibles";
		public static readonly string LineHighlight = "lineHighlight";
		public static readonly string InactiveLineHighlight = "lineHighlight_inactive";
		public static readonly string Selection = "selection";
		public static readonly string InactiveSelection = "selection_inactive";

		public static readonly string FindHighlight = "findHighlight";
		public static readonly string FindHighlightForeground = "findHighlightForeground";
		public static readonly string SelectionBorder = "selectionBorder";
		public static readonly string BracketsForeground = "bracketsForeground";
		public static readonly string BracketsOptions = "bracketsOptions";

		// CUSTOM

		#region Defined in VS.NET

		public static readonly string UsagesRectangle = "usagesRectangle_nonchanging";
		public static readonly string ChangingUsagesRectangle = "usagesRectangle_changing";

		#endregion

		public static readonly string TooltipPager = "tooltipPager";
		public static readonly string TooltipPagerTriangle = "tooltipPagerTriangle";
		public static readonly string TooltipPagerText = "tooltipPagerText";

		public static readonly string TooltipBackground = "tooltipBackground";
		public static readonly string NotificationText = "notificationText";
		public static readonly string NotificationTextBackground = "notificationTextBackground";
		public static readonly string NotificationBorder = "notificationBorder";

		public static readonly string MessageBubbleErrorMarker = "messageBubbleErrorMarker";
		public static readonly string MessageBubbleErrorTag = "messageBubbleErrorTag";
		public static readonly string MessageBubbleErrorTag2 = "messageBubbleErrorTag2";
		public static readonly string MessageBubbleErrorTooltip = "messageBubbleErrorTooltip";
		public static readonly string MessageBubbleErrorLine = "messageBubbleErrorLine";
		public static readonly string MessageBubbleErrorLine2 = "messageBubbleErrorLine2";
		public static readonly string MessageBubbleErrorBorderLine = "messageBubbleErrorBorderLine";
		public static readonly string MessageBubbleErrorCounter = "messageBubbleErrorCounter";
		public static readonly string MessageBubbleErrorCounter2 = "messageBubbleErrorCounter2";
		public static readonly string MessageBubbleErrorIconMargin = "messageBubbleErrorIconMargin";
		public static readonly string MessageBubbleErrorIconMarginBorder = "messageBubbleErrorIconMarginBorder";

		public static readonly string MessageBubbleWarningMarker = "messageBubbleWarningMarker";
		public static readonly string MessageBubbleWarningTag = "messageBubbleWarningTag";
		public static readonly string MessageBubbleWarningTag2 = "messageBubbleWarningTag2";
		public static readonly string MessageBubbleWarningTooltip = "messageBubbleWarningTooltip";
		public static readonly string MessageBubbleWarningLine = "messageBubbleWarningLine";
		public static readonly string MessageBubbleWarningLine2 = "messageBubbleWarningLine2";
		public static readonly string MessageBubbleWarningBorderLine = "messageBubbleWarningBorderLine";
		public static readonly string MessageBubbleWarningCounter = "messageBubbleWarningCounter";
		public static readonly string MessageBubbleWarningCounter2 = "messageBubbleWarningCounter2";
		public static readonly string MessageBubbleWarningIconMargin = "messageBubbleIconMargin";
		public static readonly string MessageBubbleWarningIconMarginBorder = "messageBubbleIconMarginBorder";

		public static readonly string UnderlineError = "underlineError";
		public static readonly string UnderlineWarning = "underlineWarning";
		public static readonly string UnderlineSuggestion = "underlineSuggestion";
		public static readonly string Link = "link";
		public static readonly string LineNumbersBackground = "gutter";
		public static readonly string CollapsedText = "collapsedText";
		public static readonly string FoldLine = "foldLine";
		public static readonly string FoldCross = "foldCross";
		public static readonly string FoldCrossBackground = "foldCrossBackground";
		public static readonly string QuickDiffChanged = "quickdiffChanged";
		public static readonly string QuickDiffDirty = "quickdiffDirty";
		public static readonly string LineNumbers = "gutterForeground";
		public static readonly string IndicatorMargin = "indicatorMargin";
		public static readonly string IndicatorMarginSeparator = "indicatorSeparator";
		public static readonly string BreakpointMarker = "breakpointMarker";
		public static readonly string BreakpointText = "breakpointText";
		public static readonly string BreakpointMarkerDisabled = "breakpointMarkerDisabled";
		public static readonly string BreakpointMarkerInvalid = "breakpointMarkerInvalid";
		public static readonly string DebuggerStackLineMarker = "debuggerStackLineMarker";
		public static readonly string DebuggerCurrentLineMarker = "debuggerCurrentLineMarker";
		public static readonly string DebuggerCurrentLine = "debuggerCurrentLine";
		public static readonly string DebuggerStackLine = "debuggerStackLine";
		public static readonly string IndentationGuide = "indentationGuide";
		public static readonly string Ruler = "ruler";

		public static readonly string PrimaryTemplate2 = "primaryTemplate2";
		public static readonly string PrimaryTemplateHighlighted2 = "primaryTemplateHighlighted2";
		public static readonly string SecondaryTemplate = "secondaryTemplate";
		public static readonly string SecondaryTemplateHighlighted = "secondaryTemplateHighlighted";
		public static readonly string SecondaryTemplateHighlighted2 = "secondaryTemplateHighlighted2";
		public static readonly string SecondaryTemplate2 = "secondaryTemplate2";
		public static readonly string PreviewDiffRemovedBackground = "previewDiffRemovedBackground";
		public static readonly string PreviewDiffRemoved = "previewDiffRemoved";
		public static readonly string PreviewDiffAddedBackground = "previewDiffAddedBackground";
		public static readonly string PreviewDiffAdded = "previewDiffAdded";

		// Semantic

		public static readonly string UserTypes = "entity.name.class";
		public static readonly string UserTypesValueTypes = "entity.name.struct";
		public static readonly string UserTypesInterfaces = "entity.name.interface";
		public static readonly string UserTypesEnums = "entity.name.enum";
		public static readonly string UserTypesTypeParameters = "entity.name.typeparameter";
		public static readonly string UserTypesDelegates = "entity.name.delegate";
		public static readonly string UserTypesMutable = "entity.name.mutable";

		public static readonly string UserFieldDeclaration = "entity.name.field";
		public static readonly string UserFieldUsage = "entity.name.field.usage";

		public static readonly string UserPropertyDeclaration = "entity.name.property";
		public static readonly string UserPropertyUsage = "entity.name.property.usage";

		public static readonly string UserEventDeclaration = "entity.name.event";
		public static readonly string UserEventUsage = "entity.name.event.usage";

		public static readonly string UserMethodDeclaration = "entity.name.function";
		public static readonly string UserMethodUsage = "entity.name.function.usage";

		public static readonly string UserParameterDeclaration = "entity.name.parameter";
		public static readonly string UserParameterUsage = "entity.name.parameter.usage";

		public static readonly string UserVariableDeclaration = "entity.name.local";
		public static readonly string UserVariableUsage = "entity.name.local.usage";
	}
	
}