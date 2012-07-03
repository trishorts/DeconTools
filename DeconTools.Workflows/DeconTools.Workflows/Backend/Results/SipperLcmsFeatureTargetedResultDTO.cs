﻿
namespace DeconTools.Workflows.Backend.Results
{
    public class SipperLcmsFeatureTargetedResultDTO : UnlabelledTargetedResultDTO
    {

        #region Constructors
        #endregion

        #region Properties

        public double AreaUnderRatioCurve { get; set; }

        public double AreaUnderDifferenceCurve { get; set; }

        public double RSquaredValForRatioCurve { get; set; }

        public double ChromCorrelationMin { get; set; }

        public double ChromCorrelationMax { get; set; }

        public double ChromCorrelationAverage { get; set; }

        public double ChromCorrelationMedian { get; set; }

        public double AreaUnderRatioCurveRevised { get; set; }

        public double ChromCorrelationStdev { get; set; }

        public double FractionLabelled { get; set; }

        public double AmountC13Labelling { get; set; }

        public bool PassesFilter { get; set; }

        public int NumHighQualityProfilePeaks { get; set; }

        #endregion

         

    }
}
