﻿using grapher.Models.Serialized;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grapher.Layouts
{
    public class MotivityLayout : LayoutBase
    {
         public MotivityLayout()
            : base()
        {
            Name = "Motivity";
            Index = (int)AccelMode.motivity;
            LogarithmicCharts = true;

            AccelLayout = new OptionLayout(true, Acceleration);
            LimitLayout = new OptionLayout(true, Motivity);
            MidpointLayout = new OptionLayout(true, Midpoint);
        }
    }
}
