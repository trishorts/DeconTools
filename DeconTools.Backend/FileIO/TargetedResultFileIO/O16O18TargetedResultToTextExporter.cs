﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend.Core.Results;

namespace DeconTools.Backend.FileIO.TargetedResultFileIO
{
    public class O16O18TargetedResultToTextExporter : TargetedResultToTextExporter
    {

        public O16O18TargetedResultToTextExporter(string filename)
            : base(filename)
        {

        }


        #region Private Methods

        protected override string addAdditionalInfo(Core.Results.TargetedResult result)
        {

            O16O18TargetedResult o16o18result = (O16O18TargetedResult)result;

            StringBuilder sb = new StringBuilder();
            sb.Append(Delimiter);
            sb.Append(o16o18result.IntensityI0);
            sb.Append(Delimiter);
            sb.Append(o16o18result.IntensityI2);
            sb.Append(Delimiter);
            sb.Append(o16o18result.IntensityI4);
            sb.Append(Delimiter);
            sb.Append(o16o18result.IntensityI4Adjusted);
            sb.Append(Delimiter);
            sb.Append(o16o18result.Ratio.ToString("0.0000"));

            return sb.ToString();

        }


        protected override string buildHeaderLine()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.buildHeaderLine());

            sb.Append(Delimiter);
            sb.Append("IntensityTheorI0");
            sb.Append(Delimiter);
            sb.Append("IntensityTheorI2");
            sb.Append(Delimiter);
            sb.Append("IntensityTheorI4");
            sb.Append(Delimiter);

            sb.Append("IntensityI0");
            sb.Append(Delimiter);
            sb.Append("IntensityI2");
            sb.Append(Delimiter);
            sb.Append("IntensityI4");
            sb.Append(Delimiter);
            sb.Append("IntensityI4Adjusted");
            sb.Append(Delimiter);
            sb.Append("Ratio");

            return sb.ToString();

        }

        #endregion

    }
}
