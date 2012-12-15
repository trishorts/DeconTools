﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend.Algorithms.ChargeStateDetermination.PattersonAlgorithm;
using DeconTools.Backend.Core;
using DeconTools.Backend.Parameters;
using DeconTools.Backend.ProcessingTasks.FitScoreCalculators;
using DeconTools.Backend.ProcessingTasks.TargetedFeatureFinders;
using DeconTools.Backend.Utilities.IsotopeDistributionCalculation;
using DeconTools.Utilities;

namespace DeconTools.Backend.ProcessingTasks.Deconvoluters.HornDeconvolutor
{
    public class ThrashDeconvolutorV2 : Deconvolutor
    {
        private PattersonChargeStateCalculator _chargeStateCalculator = new PattersonChargeStateCalculator();

        IsotopicDistributionCalculator _isotopicDistCalculator = IsotopicDistributionCalculator.Instance;

        private readonly AreaFitter _areafitter = new AreaFitter();

        private BasicTFF _targetedFeatureFinder = new BasicTFF();

        private Dictionary<int, IsotopicProfile> _averagineProfileLookupTable = null;

        private const int NumPointsPerTheorPeak = 20;


        #region Constructors
        public ThrashDeconvolutorV2(ThrashParameters parameters)
        {
            TagFormula = parameters.TagFormula;
            AveragineFormula = parameters.AveragineFormula;
            MinMSFeatureToBackgroundRatio = parameters.MinMSFeatureToBackgroundRatio;
            MaxFit = parameters.MaxFit;
            MinIntensityForScore = parameters.MinIntensityForScore;
            MaxCharge = parameters.MaxCharge;
            MaxMass = parameters.MaxMass;
            NumPeaksForShoulder = parameters.NumPeaksForShoulder;
            IsO16O18Data = parameters.IsO16O18Data;
            UseAbsoluteIntensity = parameters.UseAbsoluteIntensity;
            AbsolutePeptideIntensity = parameters.AbsolutePeptideIntensity;
            IsThrashUsed = parameters.IsThrashUsed;
            CheckAllPatternsAgainstChargeState1 = parameters.CheckAllPatternsAgainstChargeState1;
            CompleteFit = parameters.CompleteFit;
            ChargeCarrierMass = parameters.ChargeCarrierMass;
            IsotopicProfileFitType = parameters.IsotopicProfileFitType;
            UseMercuryCaching = parameters.UseMercuryCaching;
            LeftFitStringencyFactor = parameters.LeftFitStringencyFactor;
            RightFitStringencyFactor = parameters.RightFitStringencyFactor;
            NumPeaksUsedInAbundance = parameters.NumPeaksUsedInAbundance;
        }

        public ThrashDeconvolutorV2()
            : this(new ThrashParameters())
        {

        }


        #endregion

        #region Properties

        public string TagFormula { get; set; }

        //TODO: adjust IsotopicDistributionCalculator so that averagine isn't hard-coded
        public string AveragineFormula { get; set; }

        public double MinMSFeatureToBackgroundRatio { get; set; }

        public double MaxFit { get; set; }

        public double MinIntensityForScore { get; set; }

        public int MaxCharge { get; set; }

        public double MaxMass { get; set; }

        public int NumPeaksForShoulder { get; set; }

        public bool IsO16O18Data { get; set; }

        public bool UseAbsoluteIntensity { get; set; }

        public double AbsolutePeptideIntensity { get; set; }

        public bool IsThrashUsed { get; set; }

        public bool CheckAllPatternsAgainstChargeState1 { get; set; }

        public bool CompleteFit { get; set; }

        public double ChargeCarrierMass { get; set; }

        public Globals.IsotopicProfileFitType IsotopicProfileFitType { get; set; }

        public bool UseMercuryCaching { get; set; }

        public double LeftFitStringencyFactor { get; set; }

        public double RightFitStringencyFactor { get; set; }

        public int NumPeaksUsedInAbundance { get; set; }



        #endregion

        #region Public Methods

        public Dictionary<int, IsotopicProfile> CreateTheoreticalProfilesForMassRange(int startMass = 400, int stopMass = 5000)
        {

            Dictionary<int, IsotopicProfile> isotopicProfileDictionary = new Dictionary<int, IsotopicProfile>();

            for (int i = startMass; i <= stopMass; i++)
            {
                IsotopicProfile profile = _isotopicDistCalculator.GetAveraginePattern(startMass);

                isotopicProfileDictionary.Add(i, profile);

            }

            return isotopicProfileDictionary;


        }



        #endregion

        #region Private Methods

        #endregion

        public override void Deconvolute(ResultCollection resultList)
        {
            Check.Require(resultList.Run != null, "Cannot deconvolute. Run is null");
            Check.Require(resultList.Run.XYData != null, "Cannot deconvolute. No mass spec XY data found.");
            Check.Require(resultList.Run.PeakList != null, "Cannot deconvolute. Mass spec peak list is empty.");

            if (resultList.Run.PeakList.Count<2)
            {
                return;
            }


            var msFeatures = PerformThrash(resultList.Run.XYData, resultList.Run.PeakList,
                                           resultList.Run.CurrentScanSet.BackgroundIntensity,
                                           this.MinMSFeatureToBackgroundRatio);



        }



        public List<IsotopicProfile> PerformThrash(XYData originalXYData, List<Peak> mspeakList, double backgroundIntensity = 0, double minPeptideIntensity = 0)
        {

            List<IsotopicProfile> isotopicProfiles = new List<IsotopicProfile>();
            
            if (_averagineProfileLookupTable == null)
            {
                _averagineProfileLookupTable = CreateTheoreticalProfilesForMassRange();
            }

            double minMSFeatureIntensity = backgroundIntensity*MinMSFeatureToBackgroundRatio;
           

            XYData xyData = new XYData();
            xyData.Xvalues = originalXYData.Xvalues;
            xyData.Yvalues = originalXYData.Yvalues;


            Dictionary<Peak, bool> peaksThatWereProcessedInfo = new Dictionary<Peak, bool>();

            List<Peak> sortedPeaklist = new List<Peak>(mspeakList).OrderByDescending(p => p.Height).ToList();

            List<Peak> peaksAlreadyProcessed = new List<Peak>();



            StringBuilder stringBuilder = new StringBuilder();

            int peakCounter = -1;
            foreach (var msPeak in sortedPeaklist)
            {

                if (peaksAlreadyProcessed.Contains(msPeak))
                {
                    continue;

                }

                bool peakIsBelowIntensityThreshold = (msPeak.Height < minMSFeatureIntensity);
                if (peakIsBelowIntensityThreshold) break;



                peakCounter++;

                if (peakCounter == 465)
                {
                    // Console.WriteLine(peakCounter);
                }

                int chargeState = _chargeStateCalculator.GetChargeState(xyData, mspeakList, msPeak as MSPeak);

                double bestFitVal = 1.0;   // 1.0 is worst fit value. Start with 1.0 and see if we can find better fit value
                if (chargeState == -1)
                {

                }
                else
                {
                    double obsPeakMass = (msPeak.XValue - Globals.PROTON_MASS) * chargeState;

                    int massUsedForLookup = (int)Math.Round(obsPeakMass, 0);

                    IsotopicProfile theorIso;
                    if (_averagineProfileLookupTable.ContainsKey(massUsedForLookup))
                    {
                        theorIso = _averagineProfileLookupTable[massUsedForLookup].CloneIsotopicProfile();
                    }
                    else
                    {
                        theorIso = _isotopicDistCalculator.GetAveraginePattern(obsPeakMass);
                        _averagineProfileLookupTable.Add(massUsedForLookup, theorIso);
                    }

                    theorIso.ChargeState = chargeState;
                    theorIso.MostAbundantIsotopeMass = obsPeakMass;

                    //PeakUtilities.TrimIsotopicProfile(theorIso, 0.05);
                   
                    CalculateMassesForIsotopicProfile(theorIso);
                    XYData theorXYData = GetTheoreticalIsotopicProfileXYData(theorIso, msPeak.Width);

                    PerformIterativeFittingAndGetAlignedProfile(xyData, theorXYData, chargeState, ref theorIso, ref bestFitVal);

                    //TODO: ppm tolerance is hard-coded
                    var ppmTolerance = 10;
                    var msFeature = _targetedFeatureFinder.FindMSFeature(mspeakList, theorIso, ppmTolerance, false);

                    string reportstring;
                    if (msFeature != null)
                    {
                        peaksAlreadyProcessed.AddRange(msFeature.Peaklist);
                        msFeature.Score = bestFitVal;
                        msFeature.IntensityMostAbundant = msFeature.getMostIntensePeak().Height;
                       
                        
                        isotopicProfiles.Add(msFeature);

                        reportstring = msPeak.XValue.ToString("0.00000") + "\t" +
                                       msFeature.MonoIsotopicMass.ToString("0.0000") + "\t" +
                                       msFeature.ChargeState + "\t" + msFeature.Score;
                    }
                    else
                    {
                        reportstring = msPeak.XValue.ToString("0.00000") + "\tNo profile found.";
                    }


                    //Console.WriteLine(reportstring);

                    stringBuilder.Append(reportstring + "\n");




                }


            }

            //Console.WriteLine(stringBuilder.ToString());

            return isotopicProfiles;

        }

        private void PerformIterativeFittingAndGetAlignedProfile(XYData xyData, XYData theorXYData, int chargeState, ref IsotopicProfile theorIso, ref double bestFitVal)
        {
            if (xyData == null || xyData.Xvalues.Length == 0)
            {
                bestFitVal = 1;
                return;
            }

            double relIntensityUseForFitting = 0;



            double fitval = _areafitter.GetFit(theorXYData, xyData, relIntensityUseForFitting);

            if (fitval < bestFitVal)
            {
                bestFitVal = fitval;
            }

            double bestOffsetForTheorProfile = 0;

            // move fitting window to the left
            for (int numPeaksToTheLeft = 1; numPeaksToTheLeft < 10; numPeaksToTheLeft++)
            {
                double offsetForTheorProfile = -1 * numPeaksToTheLeft * Globals.MASS_DIFF_BETWEEN_ISOTOPICPEAKS / chargeState;
                //negative offset

                fitval = _areafitter.GetFit(theorXYData, xyData, relIntensityUseForFitting, offsetForTheorProfile);

                if (fitval > bestFitVal || fitval >= 1 || double.IsNaN(fitval))
                {
                    break;
                }

                bestFitVal = fitval;
                bestOffsetForTheorProfile = offsetForTheorProfile;
            }

            //move fitting window to the right
            for (int numPeaksToTheRight = 1; numPeaksToTheRight < 10; numPeaksToTheRight++)
            {
                double offsetForTheorProfile = numPeaksToTheRight * Globals.MASS_DIFF_BETWEEN_ISOTOPICPEAKS / chargeState;



                fitval = _areafitter.GetFit(theorXYData, xyData, relIntensityUseForFitting, offsetForTheorProfile);

                if (fitval >= bestFitVal || fitval >= 1 || double.IsNaN(fitval))
                {
                    break;
                }

                bestFitVal = fitval;
                bestOffsetForTheorProfile = offsetForTheorProfile;
            }


            foreach (var theorMSPeak in theorIso.Peaklist)
            {
                theorMSPeak.XValue = theorMSPeak.XValue + bestOffsetForTheorProfile;
            }

            theorIso.MonoPeakMZ = theorIso.getMonoPeak().XValue;

            theorIso.MonoIsotopicMass = (theorIso.MonoPeakMZ - Globals.PROTON_MASS) * chargeState;
            theorIso.MostAbundantIsotopeMass = (theorIso.getMostIntensePeak().XValue - Globals.PROTON_MASS) * chargeState;
        }


        public void CalculateMassesForIsotopicProfile(IsotopicProfile iso)
        {
            if (iso == null || iso.Peaklist == null) return;

            //start with most abundant peak.

            int indexMostAbundantPeak = iso.GetIndexOfMostIntensePeak();


            double mzMostAbundantPeak = iso.MostAbundantIsotopeMass / iso.ChargeState + Globals.PROTON_MASS;

            //start with most abundant peak and move to the LEFT and calculate m/z values
            for (int peakIndex = indexMostAbundantPeak; peakIndex >= 0; peakIndex--)
            {

                int numPeaksToLeft = indexMostAbundantPeak - peakIndex;
                double calcMZ = mzMostAbundantPeak - numPeaksToLeft * 1.00235 / iso.ChargeState;

                iso.Peaklist[peakIndex].XValue = calcMZ;

            }


            //move to the RIGHT and calculate m/z values
            for (int peakIndex = indexMostAbundantPeak + 1; peakIndex < iso.Peaklist.Count; peakIndex++)
            {

                int numPeaksToRight = peakIndex - indexMostAbundantPeak;
                double calcMZ = mzMostAbundantPeak + numPeaksToRight * 1.00235 / iso.ChargeState;

                iso.Peaklist[peakIndex].XValue = calcMZ;

            }

            iso.MonoPeakMZ = iso.getMonoPeak().XValue;
            iso.MonoIsotopicMass = (iso.MonoPeakMZ - Globals.PROTON_MASS) * iso.ChargeState;




        }

        private XYData GetTheoreticalIsotopicProfileXYData(IsotopicProfile iso, double fwhm, double minRelIntensity = 0.1)
        {
            
            var xydata = new XYData();
            var xvals = new List<double>();
            var yvals = new List<double>();


            List<MSPeak> mspeaks = new List<MSPeak>(iso.Peaklist);

            MSPeak zeroIntensityPeakToTheLeft = new MSPeak();
            zeroIntensityPeakToTheLeft.XValue = iso.Peaklist[0].XValue - 1 * 1.00235 / iso.ChargeState;
            zeroIntensityPeakToTheLeft.Height = 0;

            mspeaks.Insert(0, zeroIntensityPeakToTheLeft);

            //TheorXYDataCalculationUtilities.GetTheoreticalIsotopicProfileXYData()


            for (int peakIndex = 0; peakIndex < mspeaks.Count; peakIndex++)
            {
                MSPeak msPeak = mspeaks[peakIndex];
                XYData tempXYData = TheorXYDataCalculationUtilities.GetTheorPeakData(msPeak, fwhm,NumPointsPerTheorPeak);

                for (int j = 0; j < tempXYData.Xvalues.Length; j++)
                {
                    //First peak is a zero-intensity peak. We always want to add that one. For the others,
                    //add intensity points that are above a certain intensity
                    if (peakIndex>0)
                    {
                        if (tempXYData.Yvalues[j] >= minRelIntensity)
                        {
                            xvals.Add(tempXYData.Xvalues[j]);
                            yvals.Add(tempXYData.Yvalues[j]);
                        }
                        
                    }
                    else
                    {
                        xvals.Add(tempXYData.Xvalues[j]);
                        yvals.Add(tempXYData.Yvalues[j]);
                    }
                    
                }
            }
            xydata.Xvalues = xvals.ToArray();
            xydata.Yvalues = yvals.ToArray();



            return xydata;
        }

    }
}
