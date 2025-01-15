using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minimal_Text_Editor__Lite_.Model
{
    public enum AutoSaveInterval
    {
        Never = 0,
        OneMinute = 1,
        FiveMinutes = 5,
        FifteenMinutes = 15,
        ThirtyMinutes = 30,
        OneHour = 60,
        TwoHours = 120
    }
}
