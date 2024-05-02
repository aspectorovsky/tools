using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace DBImportExport
{
    internal static class Tracer
    {
        #region Constants and Fields

        public static TraceSwitch Trace = new TraceSwitch("DBImportExport", "DBImportExport trace switch");

        #endregion
    }
}
