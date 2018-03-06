//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Ide.Composition;

namespace Microsoft.VisualStudio.Platform
{
    public class RoslynCommandTarget
    {
        internal ICommandHandlerService CurrentHandlers { get; set; }

        internal ITextBuffer _languageBuffer;
        internal ITextView _textView;

        static RoslynCommandTarget()
        {
            var defaultForegroundThreadData = ForegroundThreadData.CreateDefault(
                defaultKind: ForegroundThreadDataKind.ForcedByPackageInitialize);
            ForegroundThreadAffinitizedObject.CurrentForegroundThreadData = defaultForegroundThreadData;
        }

        private RoslynCommandTarget(ITextView textView, ITextBuffer languageBuffer)
        {
            ICommandHandlerServiceFactory commandHandlerServiceFactory = CompositionManager.GetExportedValue<ICommandHandlerServiceFactory>();

            if (commandHandlerServiceFactory != null)
            {
                commandHandlerServiceFactory.Initialize (languageBuffer.ContentType.TypeName);

                CurrentHandlers = commandHandlerServiceFactory.GetService (languageBuffer);
            }

            _languageBuffer = languageBuffer;
            _textView = textView;
        }

        public static RoslynCommandTarget FromViewAndBuffer(ITextView textView, ITextBuffer languageBuffer)
        {
            return languageBuffer.Properties.GetOrCreateSingletonProperty<RoslynCommandTarget>(() => new RoslynCommandTarget(textView, languageBuffer));
        }

        public void ExecuteTypeCharacter(char typedChar, Action lastHandler)
        {
            CurrentHandlers?.Execute (_languageBuffer.ContentType,
                args: new TypeCharCommandArgs (_textView, _languageBuffer, typedChar),
                lastHandler: lastHandler);
        }

        public void ExecuteTab (Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute (_languageBuffer.ContentType,
                args: new TabKeyCommandArgs (_textView, _languageBuffer),
                lastHandler: executeNextCommandTarget);
        }

        public void ExecuteBackspace(Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute (_languageBuffer.ContentType,
                args: new BackspaceKeyCommandArgs (_textView, _languageBuffer),
                lastHandler: executeNextCommandTarget);
        }

        public void ExecuteDelete(Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute (_languageBuffer.ContentType,
                args: new DeleteKeyCommandArgs (_textView, _languageBuffer),
                lastHandler: executeNextCommandTarget);
        }

        public void ExecuteReturn(Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute (_languageBuffer.ContentType,
                args: new ReturnKeyCommandArgs (_textView, _languageBuffer),
                lastHandler: executeNextCommandTarget);
        }

        public void ExecuteUp (Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute (_languageBuffer.ContentType,
                args: new UpKeyCommandArgs (_textView, _languageBuffer),
                lastHandler: executeNextCommandTarget);
        }

        public void ExecuteDown (Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute (_languageBuffer.ContentType,
                args: new DownKeyCommandArgs (_textView, _languageBuffer),
                lastHandler: executeNextCommandTarget);
        }

        public void ExecuteUncommentBlock(Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute (_languageBuffer.ContentType,
                args: new UncommentSelectionCommandArgs (_textView, _languageBuffer),
                lastHandler: executeNextCommandTarget);
        }

        public void ExecuteCommentBlock(Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute (_languageBuffer.ContentType,
                args: new CommentSelectionCommandArgs (_textView, _languageBuffer),
                lastHandler: executeNextCommandTarget);
        }

        public void ExecuteInvokeCompletionList (Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute (_languageBuffer.ContentType,
                args: new InvokeCompletionListCommandArgs (_textView, _languageBuffer),
                lastHandler: executeNextCommandTarget);
        }
    }
}