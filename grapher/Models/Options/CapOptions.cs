﻿using grapher.Models.Options;
using System;
using System.Windows.Forms;

namespace grapher
{
    public class CapOptions : OptionBase
    {
        #region Constants


        #endregion Constants

        #region Constructors

        public CapOptions(
            ToolStripMenuItem velocityGainCapCheck,
            ToolStripMenuItem legacyCapCheck,
            Option capOption)
        {

            VelocityGainCapCheck = velocityGainCapCheck;
            LegacyCapCheck = legacyCapCheck;
            CapOption = capOption;

            LegacyCapCheck.Click += new System.EventHandler(OnSensitivityCapCheckClick);
            VelocityGainCapCheck.Click += new System.EventHandler(OnVelocityGainCapCheckClick);

            LegacyCapCheck.CheckedChanged += new System.EventHandler(OnSensitivityCapCheckedChange);
            VelocityGainCapCheck.CheckedChanged += new System.EventHandler(OnVelocityGainCapCheckedChange);

            EnableVelocityGainCap();
        }

        #endregion Constructors

        #region Properties

        public ToolStripMenuItem LegacyCapCheck { get; }

        public ToolStripMenuItem VelocityGainCapCheck { get; }

        public Option CapOption { get; }

        public bool IsSensitivityGain { get; private set; }

        public double SensitivityCap { 
            get
            {
                    return CapOption.Field.Data;
            }
        }

        public double VelocityGainCap { 
            get
            {

                {
                    return CapOption.Field.Data;
                }
            }
        }

        public override int Top
        { 
            get 
            {
                return CapOption.Top;
            }
            set
            {
                CapOption.Top = value;
            }
        }

        public override int Height
        {
            get
            {
                return CapOption.Height;
            }
        }

        public override int Left
        { 
            get 
            {
                return CapOption.Left;
            }
            set
            {
                CapOption.Left = value;
            }
        }

        public override int Width
        {
            get
            {
                return CapOption.Width;
            }
            set
            {
                CapOption.Width = value;
            }
        }

        public override bool Visible
        {
            get
            {
                return CapOption.Visible;
            }
        }

        #endregion Properties

        #region Methods

        public override void Hide()
        {
            CapOption.Hide();
        }

        public void Show()
        {
            CapOption.Show();
        }

        public override void Show(string name)
        {
            CapOption.Show(name);
        }

        public void SnapTo(Option option)
        {
            Top = option.Top + option.Height + Constants.OptionVerticalSeperation;
        }


        public void SetActiveValues(double gainCap, double sensCap, bool capGainEnabled)
        {
            if (capGainEnabled)
            {
                CapOption.ActiveValueLabel.FormatString = Constants.GainCapFormatString;
                CapOption.ActiveValueLabel.Prefix = "Gain";
                CapOption.SetActiveValue(gainCap);
                LegacyCapCheck.Checked = false;
                VelocityGainCapCheck.Checked = true;
            }
            else
            {
                CapOption.ActiveValueLabel.FormatString = Constants.DefaultActiveValueFormatString;
                CapOption.ActiveValueLabel.Prefix = string.Empty;
                CapOption.SetActiveValue(sensCap);
                LegacyCapCheck.Checked = true;
                VelocityGainCapCheck.Checked = false;
            }
        }

        public override void AlignActiveValues()
        {
            CapOption.AlignActiveValues();
        }

        void OnSensitivityCapCheckClick(object sender, EventArgs e)
        {
            if (!LegacyCapCheck.Checked)
            {
                VelocityGainCapCheck.Checked = false;
                LegacyCapCheck.Checked = true;
            }
        }

        void OnVelocityGainCapCheckClick(object sender, EventArgs e)
        {
            if (!VelocityGainCapCheck.Checked)
            {
                VelocityGainCapCheck.Checked = true;
                LegacyCapCheck.Checked = false;
            }
        }

        void OnSensitivityCapCheckedChange(object sender, EventArgs e)
        {
            if (LegacyCapCheck.Checked)
            {
                EnableSensitivityCap();
            }
        }

        void OnVelocityGainCapCheckedChange(object sender, EventArgs e)
        {
            if (VelocityGainCapCheck.Checked)
            {
                EnableVelocityGainCap();
            }
        }

        void EnableSensitivityCap()
        {
            IsSensitivityGain = true;
            CapOption.SetName("Sensitivity Cap");
        }

        void EnableVelocityGainCap()
        {
            IsSensitivityGain = false;
            CapOption.SetName("Velocity Gain Cap");
        }

        #endregion Methods
    }
}
