﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend.Algorithms;
using DeconTools.Backend.Core;
using DeconTools.Backend.Core.Results;
using DeconTools.Backend.ProcessingTasks.ChromatogramProcessing;
using DeconTools.Backend.Utilities;
using DeconTools.Utilities;

namespace DeconTools.Backend.ProcessingTasks.Quantifiers
{
    public class SipperQuantifierOld : Task
    {
        private const double LabeldistCalcIntensityThreshold = 0.1;
        private double _chromScanWindowWidth;

        private readonly LabelingDistributionCalculator _labelingDistributionCalculator;
        private readonly ChromatogramCorrelatorBase _chromatogramCorrelatorTask;

        private readonly PartialLabelingQuantifier _partialLabelingQuantifier;

        #region Properties
        protected double MaximumFitScoreForFurtherProcessing { get; set; }
        protected double MinimumRatioAreaForFurtherProcessing { get; set; }

        public bool IsChromatogramCorrelationPerformed { get; set; }

        protected double ChromToleranceInPPM
        {
            get
            {
                return _chromatogramCorrelatorTask.ChromTolerance;
            }
            set
            {
                _chromatogramCorrelatorTask.ChromTolerance = value;
            }
        }


        public double MinimumRelativeIntensityForChromCorr
        {
            get { return _chromatogramCorrelatorTask.MinimumRelativeIntensityForChromCorr; }
            set { _chromatogramCorrelatorTask.MinimumRelativeIntensityForChromCorr = value; }
        }


        public List<double> ChromatogramRSquaredVals { get; set; }

        public IsotopicProfile NormalizedIso { get; set; }




        /// <summary>
        /// Normalized Isotopic profile but intensities adjusted using ChromatogramCorrelationData
        /// </summary>
        public IsotopicProfile NormalizedAdjustedIso { get; set; }

        public XYData RatioVals { get; set; }

        public XYData RatioLogVals { get; set; }

        protected double MinimumRSquaredValForQuant { get; set; }

        protected double MinimumRelativeIntensityForRatioCalc { get; set; }





        #endregion


        #region Constructors
        public SipperQuantifierOld()
        {
            IsChromatogramCorrelationPerformed = true;
            MaximumFitScoreForFurtherProcessing = 0.50;
            MinimumRatioAreaForFurtherProcessing = 5;

            MinimumRSquaredValForQuant = 0.75;

            MinimumRelativeIntensityForChromCorr = 0.0001;
            ChromToleranceInPPM = 25;

            const int numPointsInSmoother = 5;
            _chromatogramCorrelatorTask = new ChromatogramCorrelator(numPointsInSmoother, MinimumRelativeIntensityForChromCorr, (int)ChromToleranceInPPM);



            ChromatogramRSquaredVals = new List<double>();
            RatioLogVals = new XYData();
            RatioVals = new XYData();

            _labelingDistributionCalculator = new LabelingDistributionCalculator();
            _partialLabelingQuantifier = new PartialLabelingQuantifier("C", 12, 13);
            _partialLabelingQuantifier.MaxLabelAmount = 15;
            _partialLabelingQuantifier.StepAmountForIterator = 0.25;
        }



        #endregion


        public override void Execute(ResultCollection resultList)
        {
            Check.Require(resultList.CurrentTargetedResult is SipperLcmsTargetedResult, "Sipper Quantifier only works on Sipper-type result objects");
            Check.Require(resultList.Run.CurrentMassTag != null, this.Name + " failed; CurrentMassTag is empty");
            Check.Require(resultList.Run.CurrentMassTag.IsotopicProfile != null, this.Name + " failed; Theor isotopic profile is empty. Run a TheorFeatureGenerator");


            var result = (SipperLcmsTargetedResult)resultList.CurrentTargetedResult;
            result.AreaUnderDifferenceCurve = -9999;
            result.AreaUnderRatioCurve = -9999;



            RatioVals.Xvalues = new double[] { 1, 2, 3, 4, 5, 6, 7 };
            RatioVals.Yvalues = new double[] { 0, 0, 0, 0, 0, 0, 0 };

            RatioLogVals.Xvalues = new double[] { 1, 2, 3, 4, 5, 6, 7 };
            RatioLogVals.Yvalues = new double[] { 0, 0, 0, 0, 0, 0, 0 };

            ChromatogramRSquaredVals.Clear();


            Check.Require(result != null, "No MassTagResult has been generated for CurrentMassTag");

            if (result.IsotopicProfile == null || result.IsotopicProfile.Peaklist == null || result.IsotopicProfile.Peaklist.Count < 2)
            {

                return;
            }


            var theorUnlabelledIso = resultList.Run.CurrentMassTag.IsotopicProfile.CloneIsotopicProfile();
            IsotopicProfileUtilities.NormalizeIsotopicProfile(theorUnlabelledIso);

            //PeakUtilities.TrimIsotopicProfile(unlabeledIso, 0.001);



            var sb = new StringBuilder();
            sb.Append(result.Target.ID + "\t" + result.Target.MZ.ToString("0.0000") + "\t" + result.Target.ChargeState +
                      "-----------------------------\n");


            var indexMostAbundantTheorPeak = theorUnlabelledIso.GetIndexOfMostIntensePeak();

            NormalizedIso = result.IsotopicProfile.CloneIsotopicProfile();
            IsotopicProfileUtilities.NormalizeIsotopicProfileToSpecificPeak(NormalizedIso, indexMostAbundantTheorPeak);

            //            return;


            if (result.Flags.Count > 0)
            {

                var flagstring = "";

                foreach (var resultFlag in result.Flags)
                {
                    flagstring += resultFlag.Description + ";";
                }

                result.ErrorDescription = flagstring.TrimEnd(';');
                result.FailedResult = true;
                if (flagstring.Contains("PeakToTheLeft"))
                {
                    result.FailureType = Globals.TargetedResultFailureType.DeisotopingProblemDetected;
                }
                else
                {
                    result.FailureType = Globals.TargetedResultFailureType.QuantifierFailure;
                }

            }


            var resultPassesMinimalCriteria = (result.Score < MaximumFitScoreForFurtherProcessing && result.Flags.Count == 0);




            if (resultPassesMinimalCriteria)
            {
                //------------------------------------- Get chromatogramCorrelation data -------------------------------

                if (IsChromatogramCorrelationPerformed)
                {
                    GetChromatogramCorrelationData(result);
                }




                NormalizedAdjustedIso = NormalizedIso.CloneIsotopicProfile();

                //this is experimental!!  it attempts to correct the problems caused by Orbitraps on isotopic profile intensities
                //UpdateIsoIntensitiesUsingChromCorrData(result.ChromCorrelationData, NormalizedAdjustedIso);


                //----------------- create ratio data -------------------------------------------------------
                var ratioData = NormalizedAdjustedIso.CloneIsotopicProfile();
                for (var i = 0; i < NormalizedIso.Peaklist.Count; i++)
                {

                    if (i < theorUnlabelledIso.Peaklist.Count && theorUnlabelledIso.Peaklist[i].Height > MinimumRelativeIntensityForRatioCalc)
                    {
                        ratioData.Peaklist[i].Height = (NormalizedIso.Peaklist[i].Height / theorUnlabelledIso.Peaklist[i].Height - 1);
                    }
                    else
                    {
                        ratioData.Peaklist[i].Height = 0;
                    }

                }

                //trim off zeros from ratio data
                for (var i = ratioData.Peaklist.Count - 1; i >= 0; i--)
                {
                    if (ratioData.Peaklist[i].Height == 0)
                    {
                        ratioData.Peaklist.RemoveAt(i);
                    }
                    else
                    {
                        break;
                    }

                }

                var xvals = ratioData.Peaklist.Select((p, i) => new { peak = p, index = i }).Select(n => (double)n.index).ToList();
                var yvals = ratioData.Peaklist.Select(p => (double)p.Height).ToList();
                result.AreaUnderRatioCurve = yvals.Sum();

                RatioVals.Xvalues = xvals.ToArray();
                RatioVals.Yvalues = yvals.ToArray();


                GetLinearRegressionData(result, xvals, yvals);



                //------------- subtract unlabelled profile from normalized profile ----------------------
                var subtractedIsoData = NormalizedAdjustedIso.CloneIsotopicProfile();
                for (var i = 0; i < subtractedIsoData.Peaklist.Count; i++)
                {
                    float intensityTheorPeak = 0;
                    if (i < theorUnlabelledIso.Peaklist.Count)
                    {
                        intensityTheorPeak = theorUnlabelledIso.Peaklist[i].Height;
                    }


                    var subtractedIntensity = subtractedIsoData.Peaklist[i].Height - intensityTheorPeak;
                    if (subtractedIntensity < 0)
                    {
                        subtractedIntensity = 0;
                    }

                    subtractedIsoData.Peaklist[i].Height = subtractedIntensity;
                }

              
                result.AreaUnderDifferenceCurve = subtractedIsoData.Peaklist.Select(p => p.Height).Sum();
                result.NumCarbonsLabelled = calculateNumCarbonsFromSubtractedProfile(subtractedIsoData);


                //-------------- calculate Label Distribution ------------------------------------------------
                double[] numLabelVals;
                double[] labelDistributionVals;

                var theorIntensityVals = theorUnlabelledIso.Peaklist.Select(p => (double)p.Height).ToList();
                var normalizedCorrectedIntensityVals = NormalizedAdjustedIso.Peaklist.Select(p => (double)p.Height).ToList();

                var numRightPads = 3;
                _labelingDistributionCalculator.CalculateLabelingDistribution(theorIntensityVals, normalizedCorrectedIntensityVals,
                                                                              LabeldistCalcIntensityThreshold,
                                                                              LabeldistCalcIntensityThreshold,
                                                                              out numLabelVals,
                                                                              out labelDistributionVals, true, true, 0, numRightPads, 0, 0);


                //negative distribution values are zeroed out. And, then the remaining values are adjusted such that they add up to 1. 
                result.LabelDistributionVals = AdjustLabelDistributionVals(labelDistributionVals);


                double distFractionUnlabelled, distFractionLabelled, distAverageLabelsIncorporated;
                _labelingDistributionCalculator.OutputLabelingInfo(result.LabelDistributionVals, out distFractionUnlabelled,
                                                                   out distFractionLabelled,
                                                                   out distAverageLabelsIncorporated);




                //-------------- make calculations using inputs from chrom correlation data -------------------



                HighQualitySubtractedProfile = GetIsoDataPassingChromCorrelation(result.ChromCorrelationData, subtractedIsoData);

                var highQualityRatioProfileData = GetIsoDataPassingChromCorrelation(result.ChromCorrelationData, ratioData);

                if (highQualityRatioProfileData.Peaklist != null && highQualityRatioProfileData.Peaklist.Count > 0)
                {
                    result.AreaUnderDifferenceCurve = HighQualitySubtractedProfile.Peaklist.Select(p => p.Height).Sum();
                }
                else
                {
                    result.AreaUnderDifferenceCurve = 0;
                }

                if (HighQualitySubtractedProfile.Peaklist != null && HighQualitySubtractedProfile.Peaklist.Count > 0)
                {
                    result.AreaUnderRatioCurveRevised = highQualityRatioProfileData.Peaklist.Select(p => p.Height).Sum();
                }
                else
                {
                    result.AreaUnderRatioCurveRevised = 0;
                }


                var numCarbonsLabelled = GetNumCarbonsLabelledUsingAverageMassDifferences(theorUnlabelledIso,
                                                                                             HighQualitySubtractedProfile);



                //result.NumCarbonsLabelled = numCarbonsLabelled;
                result.NumCarbonsLabelled = distAverageLabelsIncorporated;

                var numCarbons = result.Target.GetAtomCountForElement("C");

                result.PercentCarbonsLabelled = (result.NumCarbonsLabelled / numCarbons) * 100;
                result.PercentPeptideLabelled = distFractionLabelled * 100;

                //if (HighQualitySubtractedProfile.Peaklist != null && HighQualitySubtractedProfile.Peaklist.Count > 0)
                //{
                //    result.PercentPeptideLabelled = HighQualitySubtractedProfile.Peaklist.Max(p => p.Height) * 100;
                //}
                //else
                //{
                //    result.PercentPeptideLabelled = 0;
                //}

                result.NumHighQualityProfilePeaks = highQualityRatioProfileData.Peaklist != null
                                                        ? highQualityRatioProfileData.Peaklist.Count
                                                        : 0;

                //Console.WriteLine();
                //Console.WriteLine(result);
                //foreach (var val in ChromatogramRSquaredVals)
                //{
                //    Console.WriteLine(val);
                //}


            }
            else
            {
                result.FailedResult = true;

                if (result.FailureType == Globals.TargetedResultFailureType.None)
                {
                    result.FailureType = Globals.TargetedResultFailureType.MinimalCriteriaNotMet;
                }

            }





        }



        private void GetChromatogramCorrelationData(SipperLcmsTargetedResult result)
        {
            _chromScanWindowWidth = result.ChromPeakSelected.Width * 2;

            var startScan = result.ScanSet.PrimaryScanNumber - (int)Math.Round(_chromScanWindowWidth / 2, 0);
            var stopScan = result.ScanSet.PrimaryScanNumber + (int)Math.Round(_chromScanWindowWidth / 2, 0);

            result.ChromCorrelationData = _chromatogramCorrelatorTask.CorrelatePeaksWithinIsotopicProfile(result.Run,
                                                                                                          NormalizedIso,
                                                                                                          startScan,
                                                                                                          stopScan);

            ChromatogramRSquaredVals.AddRange(
                result.ChromCorrelationData.CorrelationDataItems.Select(p => p.CorrelationRSquaredVal.GetValueOrDefault(-1)).
                    ToList());

            //trim off zeros
            for (var i = ChromatogramRSquaredVals.Count - 1; i >= 0; i--)
            {
                if (ChromatogramRSquaredVals[i] <= 0)
                {
                    ChromatogramRSquaredVals.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }

            result.ChromCorrelationMin = ChromatogramRSquaredVals.Min();

            if (ChromatogramRSquaredVals.Count > 1)
            {
                //get the best rsquared value other than the base peak's rsquared value (which is always 1)
                result.ChromCorrelationMax =
                    (from n in ChromatogramRSquaredVals orderby n descending select n).ToList().ElementAt(1);

                result.ChromCorrelationMedian = MathUtils.GetMedian(ChromatogramRSquaredVals);
                result.ChromCorrelationAverage = ChromatogramRSquaredVals.Average();

                if (ChromatogramRSquaredVals.Count > 2)
                {
                    result.ChromCorrelationStdev = MathUtils.GetStDev(ChromatogramRSquaredVals);
                }
                else
                {
                    result.ChromCorrelationStdev = -1;
                }
            }
            else
            {
                result.ChromCorrelationMax = 1;
            }
        }

        private double calculateNumCarbonsFromSubtractedProfile(IsotopicProfile subtractedIsoData)
        {
            var intensityVals = subtractedIsoData.Peaklist.Select(p => p.Height).ToList();

            var sumIntensities = intensityVals.Sum();

            double sumDotProducts = 0;
            for (var peakNum = 0; peakNum < intensityVals.Count; peakNum++)
            {
                var dotProduct = intensityVals[peakNum] * peakNum;
                sumDotProducts += dotProduct;
            }

            return sumDotProducts / sumIntensities;
        }

        private double GetNumCarbonsLabelledUsingAverageMassDifferences(IsotopicProfile theorUnlabelledIso, IsotopicProfile highQualitySubtractedProfile)
        {
            var averageMassTheor = GetAverageMassIso(theorUnlabelledIso);

            var averageMassLabelled = GetAverageMassIso(HighQualitySubtractedProfile);

            if (double.IsNaN(averageMassLabelled))
            {
                return 0;
            }

            return averageMassLabelled - averageMassTheor;
        }


        private List<double> AdjustLabelDistributionVals(double[] labelDistributionVals)
        {
            if (labelDistributionVals == null || labelDistributionVals.Length == 0) return new List<double>();

            //first, will set the negative values to zero. (cannot have a negative label distribution)
            for (var i = 0; i < labelDistributionVals.Length; i++)
            {
                if (labelDistributionVals[i] < 0)
                {
                    labelDistributionVals[i] = 0;
                }

            }


            //because values were zeroed, the distribution now adds up to greater than 1. We need to adjust things so the distribution adds up to 1

            var sumVals = labelDistributionVals.Sum();

            //re-normalize.  
            for (var i = 0; i < labelDistributionVals.Length; i++)
            {
                labelDistributionVals[i] = labelDistributionVals[i] / sumVals;
            }

            return labelDistributionVals.ToList();


        }

        public IsotopicProfile HighQualitySubtractedProfile { get; set; }

        private void UpdateIsoIntensitiesUsingChromCorrData(ChromCorrelationData chromCorrelationData, IsotopicProfile iso)
        {
            var totalChromCorrDataItems = chromCorrelationData.CorrelationDataItems.Count;

            double heightMaxPeak = iso.getMostIntensePeak().Height;

            for (var i = 0; i < iso.Peaklist.Count; i++)
            {
                if (i < totalChromCorrDataItems)
                {

                    var correctedRelIntensity = chromCorrelationData.CorrelationDataItems[i].CorrelationSlope;

                    if (correctedRelIntensity.HasValue)
                    {
                        iso.Peaklist[i].Height = (float)(heightMaxPeak * correctedRelIntensity);
                    }



                }

            }


        }


        public float FractionLabelled { get; set; }

        public double AmountC13Labelling { get; set; }

        private double GetAverageMassIso(IsotopicProfile isotopicProfile)
        {
            if (isotopicProfile.Peaklist == null || isotopicProfile.Peaklist.Count == 0)
            {
                return 0;
            }

            double sumIntensities = isotopicProfile.Peaklist.Sum(p => p.Height);

            double averageMZ = 0;

            for (var i = 0; i < isotopicProfile.Peaklist.Count; i++)
            {
                averageMZ += isotopicProfile.Peaklist[i].XValue * isotopicProfile.Peaklist[i].Height / sumIntensities;
            }


            var averageMass = (averageMZ - Globals.PROTON_MASS) * isotopicProfile.ChargeState;


            return averageMass;

        }

        private IsotopicProfile GetIsoDataPassingChromCorrelation(ChromCorrelationData chromCorrelationData, IsotopicProfile iso)
        {

            var returnedIso = iso.CloneIsotopicProfile();

            returnedIso.Peaklist.Clear();


            for (var i = 0; i < iso.Peaklist.Count; i++)
            {
                if (i < chromCorrelationData.CorrelationDataItems.Count)
                {
                    if (chromCorrelationData.CorrelationDataItems[i].CorrelationRSquaredVal > MinimumRSquaredValForQuant)
                    {
                        returnedIso.Peaklist.Add(iso.Peaklist[i]);

                    }
                    else
                    {
                        break;
                    }
                }

            }

            return returnedIso;


        }



        private void GetLinearRegressionData(SipperLcmsTargetedResult result, List<double> xvals, List<double> yvals)
        {
            var logRatioXVals = new List<double>();
            var logRatioYVals = new List<double>();

            var foundYvalAboveZero = false;
            for (var i = 0; i < xvals.Count; i++)
            {

                if (yvals[i] > 0)
                {
                    foundYvalAboveZero = true;
                    logRatioXVals.Add(xvals[i]);
                    logRatioYVals.Add(Math.Log(yvals[i]));
                }
                else
                {
                    //if we have already found a yval above 0, and then we found one below 0, this
                    //indicates a problem which we will penalize. 
                    //if (foundYvalAboveZero)
                    //{
                    //    logRatioXVals.Add(xvals[i]);
                    //    logRatioYVals.Add(0);
                    //}
                }
            }

            double slope = -9999;
            double intercept = -9999;
            double rsquaredVal = -1;


            RatioLogVals.Xvalues = logRatioXVals.ToArray();
            RatioLogVals.Yvalues = logRatioYVals.ToArray();


            if (logRatioYVals.Count > 2)
            {
                try
                {
                    MathUtils.GetLinearRegression(RatioLogVals.Xvalues, RatioLogVals.Yvalues, out slope, out intercept,
                                                  out rsquaredVal);
                }
                catch (Exception)
                {
                    Console.WriteLine(result.Target.ID + "; Error getting linear regression in Sipper quantifier");
                }
            }
            else
            {
                rsquaredVal = 0;
            }
            
        }


        private double CalculateAreaUnderCubicSplineFit(List<double> xvals, List<double> yvals)
        {
            // var interp = Interpolate.RationalWithoutPoles(xvals, yvals);

            // this was valid in MathNet.Numerics v2.5, but is no longer supported in v3.x
            // var interp = new MathNet.Numerics.Interpolation.Algorithms.CubicSplineInterpolation(xvals, yvals);
            // double area = interp.Integrate(xvals.Max());
            // return area;

            throw new NotImplementedException("No longer compatible with MathNet.Numerics v3.x");

            
        }

    }
}
