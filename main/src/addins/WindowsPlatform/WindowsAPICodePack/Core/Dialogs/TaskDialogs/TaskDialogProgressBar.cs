//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Resources;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Provides a visual representation of the progress of a long running operation.
    /// </summary>
    public class TaskDialogProgressBar : TaskDialogBar
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public TaskDialogProgressBar() { }

        /// <summary>
        /// Creates a new instance of this class with the specified name.
        /// And using the default values: Min = 0, Max = 100, Current = 0
        /// </summary>
        /// <param name="name">The name of the control.</param>        
        public TaskDialogProgressBar(string name) : base(name) { }

        /// <summary>
        /// Creates a new instance of this class with the specified 
        /// minimum, maximum and current values.
        /// </summary>
        /// <param name="minimum">The minimum value for this control.</param>
        /// <param name="maximum">The maximum value for this control.</param>
        /// <param name="value">The current value for this control.</param>        
        public TaskDialogProgressBar(int minimum, int maximum, int value)
        {
            Minimum = minimum;
            Maximum = maximum;
            Value = value;
        }

        private int _minimum;
        private int _value;
        private int _maximum = TaskDialogDefaults.ProgressBarMaximumValue;

        /// <summary>
        /// Gets or sets the minimum value for the control.
        /// </summary>                
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                CheckPropertyChangeAllowed("Minimum");

                // Check for positive numbers
                if (value < 0)
                {
                    throw new System.ArgumentException(LocalizedMessages.TaskDialogProgressBarMinValueGreaterThanZero, "value");
                }

                // Check if min / max differ
                if (value >= Maximum)
                {
                    throw new System.ArgumentException(LocalizedMessages.TaskDialogProgressBarMinValueLessThanMax, "value");
                }

                _minimum = value;
                ApplyPropertyChange("Minimum");
            }
        }
        /// <summary>
        /// Gets or sets the maximum value for the control.
        /// </summary>
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                CheckPropertyChangeAllowed("Maximum");

                // Check if min / max differ
                if (value < Minimum)
                {
                    throw new System.ArgumentException(LocalizedMessages.TaskDialogProgressBarMaxValueGreaterThanMin, "value");
                }
                _maximum = value;
                ApplyPropertyChange("Maximum");
            }
        }
        /// <summary>
        /// Gets or sets the current value for the control.
        /// </summary>
        public int Value
        {
            get { return this._value; }
            set
            {
                CheckPropertyChangeAllowed("Value");
                // Check for positive numbers
                if (value < Minimum || value > Maximum)
                {
                    throw new System.ArgumentException(LocalizedMessages.TaskDialogProgressBarValueInRange, "value");
                }
                this._value = value;
                ApplyPropertyChange("Value");
            }
        }

        /// <summary>
        /// Verifies that the progress bar's value is between its minimum and maximum.
        /// </summary>
        internal bool HasValidValues
        {
            get { return _minimum <= _value && _value <= _maximum; }
        }

        /// <summary>
        /// Resets the control to its minimum value.
        /// </summary>
        protected internal override void Reset()
        {
            base.Reset();
            _value = _minimum;
        }
    }
}
