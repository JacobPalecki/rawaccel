﻿using grapher.Models.Options;

namespace grapher.Layouts
{
    public abstract class LayoutBase
    {
        public const string Acceleration = "Acceleration";
        public const string Gamma = "Gamma";
        public const string DecayRate = "Decay Rate";
        public const string Scale = "Scale";
        public const string Exponent = "Exponent";
        public const string OutputOffset = "Output Offset";
        public const string PowerClassic = "Power";
        public const string Limit = "Limit";
        public const string SyncSpeed = "SyncSpeed";
        public const string Motivity = "Motivity";
        public const string InputOffset = "Input Offset";
        public const string CapType = "Cap Type";
        public const string Weight = "Weight";
        public const string Smooth = "Smooth";
        public const string Gain = "Gain";
        public const string Input = "Input";
        public const string Output = "Output";

        public LayoutBase()
        {
            DecayRateLayout = new OptionLayout(false, string.Empty);
            GammaLayout = new OptionLayout(false, string.Empty);
            SmoothLayout = new OptionLayout(false, string.Empty);
            ClassicCapLayout = new OptionLayout(false, string.Empty);
            PowerCapLayout = new OptionLayout(false, string.Empty);
            InputJumpLayout = new OptionLayout(false, string.Empty);
            InputOffsetLayout = new OptionLayout(false, string.Empty);
            LimitLayout = new OptionLayout(false, string.Empty);
            PowerClassicLayout = new OptionLayout(false, string.Empty);
            ExponentLayout = new OptionLayout(false, string.Empty);
            OutputJumpLayout = new OptionLayout(false, string.Empty);
            OutputOffsetLayout = new OptionLayout(false, string.Empty);
            SyncSpeedLayout = new OptionLayout(false, string.Empty);
            LutTextLayout = new OptionLayout(false, string.Empty);
            LutPanelLayout = new OptionLayout(false, string.Empty);
            LutApplyOptionsLayout = new OptionLayout(false, string.Empty);
            GainSwitchOptionLayout = new OptionLayout(false, string.Empty);

            LogarithmicCharts = false;
        }

        public AccelMode Mode { get; protected set; }

        public string Name { get; protected set; }

        public virtual string ActiveName { get => Name; }

        public bool LogarithmicCharts { get; protected set; }

        protected OptionLayout DecayRateLayout { get; set; }

        protected OptionLayout GammaLayout { get; set; }

        protected OptionLayout SmoothLayout { get; set; }

        protected OptionLayout ClassicCapLayout { get; set; }

        protected OptionLayout PowerCapLayout { get; set; }

        protected OptionLayout InputJumpLayout { get; set; }

        protected OptionLayout InputOffsetLayout { get; set; }

        protected OptionLayout LimitLayout { get; set; }

        protected OptionLayout PowerClassicLayout { get; set; }

        protected OptionLayout ExponentLayout { get; set; }

        protected OptionLayout OutputJumpLayout { get; set; }

        protected OptionLayout OutputOffsetLayout { get; set; }

        protected OptionLayout SyncSpeedLayout { get; set; }

        protected OptionLayout LutTextLayout { get; set; }

        protected OptionLayout LutPanelLayout { get; set; }

        protected OptionLayout LutApplyOptionsLayout { get; set; }

        protected OptionLayout GainSwitchOptionLayout { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public void Layout(
            IOption gainSwitchOption,
            IOption classicCapOption,
            IOption powerCapOption,
            IOption decayRateOption,
            IOption gammaOption,
            IOption smoothOption,
            IOption inputJumpOption,
            IOption inputOffsetOption,
            IOption limitOption,
            IOption powerClassicOption,
            IOption expOption,
            IOption outputJumpOption,
            IOption outputOffsetOption,
            IOption syncSpeedOption,
            IOption lutTextOption,
            IOption lutPanelOption,
            IOption lutApplyOption,
            int top)
        {

            IOption previous = null;

            foreach (var option in new (OptionLayout, IOption)[] {
                (GainSwitchOptionLayout, gainSwitchOption),
                (ClassicCapLayout, classicCapOption),
                (PowerCapLayout, powerCapOption),
                (DecayRateLayout, decayRateOption),
                (GammaLayout, gammaOption),
                (SmoothLayout, smoothOption),
                (InputJumpLayout, inputJumpOption),
                (InputOffsetLayout, inputOffsetOption),
                (LimitLayout, limitOption),
                (PowerClassicLayout, powerClassicOption),
                (ExponentLayout, expOption),
                (OutputJumpLayout, outputJumpOption),
                (OutputOffsetLayout, outputOffsetOption),
                (SyncSpeedLayout, syncSpeedOption),
                (LutTextLayout, lutTextOption),
                (LutPanelLayout, lutPanelOption),
                (LutApplyOptionsLayout, lutApplyOption)})
            {
                option.Item1.Layout(option.Item2);

                if (option.Item2.Visible)
                {
                    if (previous != null)
                    {
                        option.Item2.SnapTo(previous);
                    }
                    else
                    {
                        option.Item2.Top = top;
                    }

                    previous = option.Item2;
                }
            }
        }

        public void Layout(
            IOption gainSwitchOption,
            IOption classicCapOption,
            IOption powerCapOption,
            IOption decayRateOption,
            IOption gammaOption,
            IOption smoothOption,
            IOption inputJumpOption,
            IOption inputOffsetOption,
            IOption limitOption,
            IOption powerClassicOption,
            IOption expOption,
            IOption outputJumpOption,
            IOption outputOffsetOption,
            IOption syncSpeedOption,
            IOption lutTextOption,
            IOption lutPanelOption,
            IOption lutApplyOption)
        {
            Layout(gainSwitchOption,
                classicCapOption,
                powerCapOption,
                decayRateOption,
                gammaOption,
                smoothOption,
                inputJumpOption,
                inputOffsetOption,
                limitOption,
                powerClassicOption,
                expOption,
                outputJumpOption,
                outputOffsetOption,
                syncSpeedOption,
                lutTextOption,
                lutPanelOption,
                lutApplyOption,
                gainSwitchOption.Top);
        }
    }
}
