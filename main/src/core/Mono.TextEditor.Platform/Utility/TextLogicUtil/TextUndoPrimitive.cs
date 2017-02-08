namespace Microsoft.VisualStudio.Text.Operations
{
    internal abstract class TextUndoPrimitive : ITextUndoPrimitive
    {
        private ITextUndoTransaction parent;

        public virtual ITextUndoTransaction Parent
        {
            get { return this.parent; }
            set { this.parent = value; }
        }

        public virtual bool CanRedo
        {
            get { return true; }
        }

        public virtual bool CanUndo
        {
            get { return true; }
        }

        protected virtual void Toggle()
        {
        }

        public virtual void Do()
        {
            Toggle();
        }

        public virtual void Undo()
        {
            Toggle();
        }

        public virtual bool CanMerge(ITextUndoPrimitive older)
        {
            return false;
        }

        public virtual ITextUndoPrimitive Merge(ITextUndoPrimitive older)
        {
            throw new System.NotSupportedException();
        }
    }
}