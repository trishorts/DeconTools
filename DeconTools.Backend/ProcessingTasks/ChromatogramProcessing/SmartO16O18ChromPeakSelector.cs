﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend.Core;
using DeconTools.Backend.ProcessingTasks.FitScoreCalculators;
using DeconTools.Backend.ProcessingTasks;

namespace DeconTools.Backend.ProcessingTasks.ChromatogramProcessing
{

    //TODO: abstract out a SmartChromPeakSelector and inherit from that
    //TODO: update constructor like the other SmartChromPeakSelector

    public class SmartO16O18ChromPeakSelector : Task
    {
        private const double DEFAULT_MSPEAKDETECTOR_PEAKBR = 1.3;
        private const double DEFAULT_MSPEAKDETECTOR_SIGNOISERATIO = 2;
        private const double DEFAULT_TARGETEDMSFEATUREFINDERTOLERANCE_PPM = 20;

        private DeconTools.Backend.ProcessingTasks.I_MSGenerator msgen;
        private DeconTools.Backend.ProcessingTasks.ResultValidators.ResultValidatorTask resultValidator;
        private MassTagFitScoreCalculator fitScoreCalc;


        private double p;
        private int p_2;

        internal class PeakQualityData
        {

            internal PeakQualityData(ChromPeak peak)
            {
                this.i_score = 1;     // worst possible
                this.fitScore = 1;   // worst possible
                this.abundance = 0;
                this.peak = peak;
                this.isotopicProfileFound = false;
            }

            internal ChromPeak peak;
            internal bool isotopicProfileFound;
            internal double fitScore;
            internal double i_score;
            internal double abundance;

            internal bool isIsotopicProfileFlagged;


            internal void Display()
            {
                Console.WriteLine(peak.XValue.ToString("0.00") + "\t" + peak.NETValue.ToString("0.0000") + "\t" + abundance + "\t" + fitScore.ToString("0.0000") + "\t" + i_score.ToString("0.000"));
            }
        }




        public SmartO16O18ChromPeakSelector()
        {
            MSPeakDetector = new DeconToolsPeakDetector(DEFAULT_MSPEAKDETECTOR_PEAKBR, DEFAULT_MSPEAKDETECTOR_SIGNOISERATIO, DeconTools.Backend.Globals.PeakFitType.QUADRATIC, true);
            TargetedMSFeatureFinder = new DeconTools.Backend.ProcessingTasks.TargetedFeatureFinders.BasicTFF(DEFAULT_TARGETEDMSFEATUREFINDERTOLERANCE_PPM);
            resultValidator = new DeconTools.Backend.ProcessingTasks.ResultValidators.ResultValidatorTask();
            fitScoreCalc = new MassTagFitScoreCalculator();

            this.NETTolerance = 0.025f;
            this.NumScansToSum = 1;

        }

        public SmartO16O18ChromPeakSelector(double netTolerance, int numScansToSum)
            : this()
        {
            this.NETTolerance = netTolerance;
            this.NumScansToSum = numScansToSum;
        }


        public double NETTolerance { get; set; }

        public int NumScansToSum { get; set; }


        public override void Execute(ResultCollection resultColl)
        {
            DeconTools.Utilities.Check.Require(resultColl.Run.CurrentMassTag != null, this.Name + " failed. MassTag was not defined.");

            if (msgen == null)
            {
                MSGeneratorFactory msgenFactory = new MSGeneratorFactory();
                msgen = msgenFactory.CreateMSGenerator(resultColl.Run.MSFileType);
                msgen.IsTICRequested = false;
            }

            MassTag mt = resultColl.Run.CurrentMassTag;

            //collect Chrom peaks that fall within the NET tolerance
            List<ChromPeak> peaksWithinTol = new List<ChromPeak>(); // 
            foreach (ChromPeak peak in resultColl.Run.PeakList)
            {
                if (Math.Abs(peak.NETValue - mt.NETVal) <= NETTolerance)     //peak.NETValue was determined by the ChromPeakDetector or a future ChromAligner Task
                {
                    peaksWithinTol.Add(peak);
                }
            }


            List<PeakQualityData> peakQualityList = new List<PeakQualityData>();

            MassTagResultBase currentResult = resultColl.GetMassTagResult(resultColl.Run.CurrentMassTag);

            //iterate over peaks within tolerance and score each peak according to MSFeature quality
            //Console.WriteLine("MT= " + currentResult.MassTag.ID + ";z= " + currentResult.MassTag.ChargeState + "; mz= " + currentResult.MassTag.MZ.ToString("0.000") + ";  ------------------------- PeaksWithinTol = " + peaksWithinTol.Count);

            currentResult.NumChromPeaksWithinTolerance = peaksWithinTol.Count;


            foreach (var peak in peaksWithinTol)
            {
                ScanSet scanset = createNonSummedScanSet(peak, resultColl.Run);
                PeakQualityData pq = new PeakQualityData(peak);
                peakQualityList.Add(pq);

                resultColl.Run.CurrentScanSet = scanset;

                //This resets the flags and the scores on a given result
                currentResult.ResetResult();

                //generate a mass spectrum
                msgen.Execute(resultColl);

                //detect peaks
                MSPeakDetector.Execute(resultColl);

                //find isotopic profile
                TargetedMSFeatureFinder.Execute(resultColl);

                //take only first two peaks of the unlabelled O16 isotopic profile and use these for getting the fit value. 
                if (currentResult.IsotopicProfile != null && currentResult.IsotopicProfile.Peaklist != null && currentResult.IsotopicProfile.Peaklist.Count > 2)
                {
                    currentResult.IsotopicProfile.Peaklist = currentResult.IsotopicProfile.Peaklist.Take(4).ToList();
                }

                //get fit score
                fitScoreCalc.Execute(resultColl);

                //get i_score and flag bad data
                resultValidator.Execute(resultColl);

                //collect the results together
                addScoresToPeakQualityData(pq, currentResult);

                //pq.Display();

            }

            

            //run a algorithm that decides, based on fit score mostly. 
            ChromPeak bestChromPeak = determineBestChromPeak(peakQualityList);

            ScanSet bestScanset = createSummedScanSet(bestChromPeak, resultColl.Run);
            resultColl.Run.CurrentScanSet = bestScanset;   // maybe good to set this here so that the MSGenerator can operate on it...  

            currentResult.AddSelectedChromPeakAndScanSet(bestChromPeak, bestScanset);

            DeconTools.Utilities.Check.Ensure(currentResult.ChromPeakSelected != null && currentResult.ChromPeakSelected.XValue != 0, "ChromPeakSelector failed. No chromatographic peak found within tolerances.");
        }

        public DeconTools.Backend.ProcessingTasks.TargetedFeatureFinders.BasicTFF TargetedMSFeatureFinder { get; set; }

        public DeconToolsPeakDetector MSPeakDetector { get; set; }

        private ChromPeak determineBestChromPeak(List<PeakQualityData> peakQualityList)
        {
            var filteredList1 = (from n in peakQualityList
                                 where n.isotopicProfileFound == true &&
                                 n.fitScore < 1 && n.i_score < 1 &&
                                 n.isIsotopicProfileFlagged == false
                                 select n).ToList();

            ChromPeak bestpeak;

            if (filteredList1.Count == 0)
            {
                bestpeak = null;
            }
            else if (filteredList1.Count == 1)
            {
                bestpeak = filteredList1[0].peak;
            }
            else
            {
                filteredList1 = filteredList1.OrderBy(p => p.fitScore).ToList();
                bestpeak = filteredList1[0].peak;
            }

            return bestpeak;


        }

        private void addScoresToPeakQualityData(PeakQualityData pq, MassTagResultBase currentResult)
        {
            if (currentResult.IsotopicProfile == null)
            {
                pq.isotopicProfileFound = false;
                return;
            }
            else
            {
                pq.isotopicProfileFound = true;
                pq.abundance = currentResult.IsotopicProfile.IntensityAggregate;
                pq.fitScore = currentResult.Score;
                pq.i_score = currentResult.InterferenceScore;

                bool resultHasFlags = (currentResult.Flags != null && currentResult.Flags.Count > 0);
                pq.isIsotopicProfileFlagged = resultHasFlags;
            }
        }

        private ScanSet createSummedScanSet(ChromPeak chromPeak, Run run)
        {
            if (chromPeak == null || chromPeak.XValue == 0) return null;

            int bestScan = (int)chromPeak.XValue;
            bestScan = run.GetClosestMSScan(bestScan, DeconTools.Backend.Globals.ScanSelectionMode.CLOSEST);

            return new ScanSetFactory().CreateScanSet(run, bestScan, this.NumScansToSum);
        }

        private ScanSet createNonSummedScanSet(ChromPeak chromPeak, Run run)
        {
            if (chromPeak == null || chromPeak.XValue == 0) return null;

            int bestScan = (int)chromPeak.XValue;
            bestScan = run.GetClosestMSScan(bestScan, DeconTools.Backend.Globals.ScanSelectionMode.CLOSEST);

            int numScansToSum = 1;
            return new ScanSetFactory().CreateScanSet(run, bestScan, numScansToSum);

        }


    }
}