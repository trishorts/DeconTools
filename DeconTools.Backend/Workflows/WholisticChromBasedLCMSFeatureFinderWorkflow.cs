﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend.Core;
using DeconTools.Backend.ProcessingTasks;
using DeconTools.Utilities;
using DeconTools.Backend.DTO;
using DeconTools.Backend.Algorithms;
using DeconTools.Backend.Utilities;
using DeconTools.Backend.ProcessingTasks.PeakDetectors;

namespace DeconTools.Backend.Workflows
{
    public class WholisticChromBasedLCMSFeatureFinderWorkflow : IWorkflow
    {
        #region Constructors

        public WholisticChromBasedLCMSFeatureFinderWorkflow()
        {
            InitializeWorkflow();
            this.Name = this.ToString();

        }

        #endregion

        #region Properties

        public int ChromGenToleranceInPPM { get; set; }


        public ChromatogramGenerator ChromGenerator { get; set; }
        public DeconTools.Backend.ProcessingTasks.Smoothers.DeconToolsSavitzkyGolaySmoother ChromSmoother { get; set; }
        public ChromPeakDetector ChromPeakDetector { get; set; }


        public I_MSGenerator MSgen { get; set; }
        public DeconToolsPeakDetector MSPeakDetector { get; set; }
        public IDeconvolutor Deconvolutor { get; set; }

        #endregion

        #region Public Methods



        #endregion

        #region Private Methods
        #endregion
        #region IWorkflow Members

        public string Name { get; set; }
        public int MinScan { get; set; }
        public int MaxScan { get; set; }

        public void InitializeWorkflow()
        {
            this.ChromGenToleranceInPPM = 20;

            this.ChromGenerator = new ChromatogramGenerator();

            this.ChromSmoother = new DeconTools.Backend.ProcessingTasks.Smoothers.DeconToolsSavitzkyGolaySmoother(11, 11, 2);

            this.ChromPeakDetector = new DeconTools.Backend.ProcessingTasks.PeakDetectors.ChromPeakDetector(0.5, 0.5);

            this.MSPeakDetector = new DeconToolsPeakDetector(1.3, 2, Globals.PeakFitType.QUADRATIC, false);

            this.Deconvolutor = new RapidDeconvolutor();



        }

        public void ExecuteWorkflow(DeconTools.Backend.Core.Run run)
        {
            double scanTolerance = 100;


            Check.Require(run != null, String.Format("{0} failed. Run not defined.", this.Name));
            Check.Require(run.ResultCollection != null && run.ResultCollection.MSPeakResultList != null && run.ResultCollection.MSPeakResultList.Count > 0,
                String.Format("{0} failed. Workflow requires MSPeakResults, but these were not defined.", this.Name));

            var sortedList = run.ResultCollection.MSPeakResultList.OrderByDescending(p => p.MSPeak.Height);

            bool msGeneratorNeedsInitializing = (this.MSgen == null);
            if (msGeneratorNeedsInitializing)
            {
                MSGeneratorFactory factoryMSGen = new MSGeneratorFactory();
                this.MSgen = factoryMSGen.CreateMSGenerator(run.MSFileType);

            }


            List<MSPeakResult> sortedMSPeakResultList = sortedList.ToList();

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            List<long> deconTimes = new List<long>();

            StringBuilder sb = new StringBuilder();


            int totalPeaks = sortedMSPeakResultList.Count;

            int counter = -1;

            Dictionary<int, string> whatPeakWentWhere = new Dictionary<int, string>();

            foreach (var peakResult in sortedMSPeakResultList)
            {
                counter++;

                if (counter % 1000==0)
                {
                    Console.WriteLine(DateTime.Now + "\tWorking on peak " + counter + " of " + totalPeaks);
                }

                if (peakResult.PeakID == 396293)
                {
                    Console.WriteLine(DateTime.Now + "\tWorking on peak " + peakResult.PeakID);
                }



                bool peakResultAlreadyIncludedInChromatogram = (peakResult.ChromID != -1);
                if (peakResultAlreadyIncludedInChromatogram)
                {
                    whatPeakWentWhere.Add(peakResult.PeakID, "ChromAlready");
                    continue;
                }

                bool peakResultAlreadyFoundInAnMSFeature = findPeakWithinMSFeatureResults(run.ResultCollection.ResultList, peakResult, scanTolerance);
                if (peakResultAlreadyFoundInAnMSFeature)
                {
                    whatPeakWentWhere.Add(peakResult.PeakID, "MSFeatureAlready");
                    continue;
                }

                whatPeakWentWhere.Add(peakResult.PeakID, "ChromGen");
                //generate chromatogram & tag MSPeakResults
                XYData chromatogram = this.ChromGenerator.GenerateChromatogram(run.ResultCollection.MSPeakResultList, run.MinScan, run.MaxScan, peakResult.MSPeak.XValue, this.ChromGenToleranceInPPM, peakResult.PeakID);

                if (chromatogram == null) continue;


                //remove points from chromatogram due to MS/MS level data
                if (run.ContainsMSMSData)
                {
                    chromatogram = filterOutMSMSRelatedPoints(run, chromatogram);
                }

                //smooth the chromatogram
                chromatogram = this.ChromSmoother.Smooth(chromatogram);

                //detect peaks in chromatogram
                List<IPeak> chromPeakList = this.ChromPeakDetector.FindPeaks(chromatogram, 0, 0);

                //sort chrompeak list so it is decending.
                chromPeakList = chromPeakList.OrderByDescending(p => p.Height).ToList();

                //this is the temporary results that are collected for MSFeatures found over each chromPeak of the ChromPeakList
                List<IsosResult> tempChromPeakMSFeatures = new List<IsosResult>();

                //store each chrom peak (which, here, is an ElutingPeak)
                foreach (var chromPeak in chromPeakList)
                {
                    //ElutingPeak ep = new ElutingPeak();
                    //ep.ChromPeak = chromPeak;
                    //ep.PeakList.Add(peakResult);

                    run.CurrentScanSet = createScanSetFromChromatogramPeak(run, chromPeak);

                    MSgen.Execute(run.ResultCollection);

                    //trim the XYData to help the peak detector and Deconvolutor work faster
                    run.XYData = run.XYData.TrimData(peakResult.MSPeak.XValue - 2, peakResult.MSPeak.XValue + 2);

                    this.MSPeakDetector.Execute(run.ResultCollection);

                    //HACK:  calling 'deconvolute' will write results to 'isosResultBin' but not to 'ResultList';  I will manually add what I want to the official 'ResultList'
                    run.ResultCollection.IsosResultBin.Clear();


                    sw.Reset();
                    sw.Start();

                    this.Deconvolutor.deconvolute(run.ResultCollection);
                    sw.Stop();
                    deconTimes.Add(sw.ElapsedMilliseconds);

                    sb.Append(sw.ElapsedMilliseconds);
                    sb.Append("\t");

                    sb.Append(peakResult.Scan_num);
                    sb.Append("\t");
                    sb.Append(peakResult.MSPeak.XValue.ToString("0.00000"));
                    sb.Append("\t");
                    sb.Append(peakResult.MSPeak.Height);
                    sb.Append("\t");
                    sb.Append(chromPeak.XValue.ToString("0.000"));
                    sb.Append("\t");
                    sb.Append(chromPeak.Width.ToString("0.000"));
                    sb.Append("\t");
                    sb.Append(run.XYData.Xvalues.Length);
                    sb.Append(Environment.NewLine);




                    //Need to find the target peak within the MSFeature.  Then mark other peaks of the MSFeature as being found, so that we don't bother generating a MS and deisotoping

                    findTargetPeakAddResultsToCollectionAndMarkAssociatedPeaks(tempChromPeakMSFeatures, peakResult, run, scanTolerance);


                }

            }

            //foreach (var item in deconTimes)
            //{
            //    Console.WriteLine(item);
            //}

            //Console.WriteLine("Average = " + deconTimes.Average());
            //Console.WriteLine("Top 50 = " + deconTimes.Take(50).Average());
            //Console.WriteLine("Next 50 = " + deconTimes.Skip(50).Take(50).Average());

            StringBuilder peaksb = new StringBuilder();
            foreach (var item in whatPeakWentWhere)
            {
                peaksb.Append(item.Key);
                peaksb.Append("\t");
                peaksb.Append(item.Value);
                peaksb.Append(Environment.NewLine);
            }


            //Console.WriteLine(sb.ToString());



        }

        private bool findPeakWithinMSFeatureResults(List<IsosResult> msFeatureList, MSPeakResult peakResult, double scanTolerance)
        {
            double toleranceInPPM = 5;
            double toleranceInMZ = toleranceInPPM / 1e6 * peakResult.MSPeak.XValue;

            foreach (var msfeature in msFeatureList)
            {
                if (msfeature.IsotopicProfile == null || msfeature.IsotopicProfile.Peaklist == null || msfeature.IsotopicProfile.Peaklist.Count == 0) continue;

                //check target peak is within an allowable scan tolerance
                bool targetPeakIsWithinScanTol = Math.Abs(msfeature.ScanSet.PrimaryScanNumber - peakResult.Scan_num) <= scanTolerance;
                if (!targetPeakIsWithinScanTol) continue;

                List<MSPeak> peaksWithinTol = PeakUtilities.GetMSPeaksWithinTolerance(msfeature.IsotopicProfile.Peaklist, peakResult.MSPeak.XValue, toleranceInMZ);

                if (peaksWithinTol.Count == 0)
                {
                    continue;
                }
                else
                {
                    return true;
                }



            }

            return false;

        }

        private void findTargetPeakAddResultsToCollectionAndMarkAssociatedPeaks(List<IsosResult> chromPeakBasedMSFeatures, MSPeakResult peakResult, Run run, double scanTolerance)
        {
            double toleranceInPPM = 5;

            double toleranceInMZ = toleranceInPPM / 1e6 * peakResult.MSPeak.XValue;
            bool foundPeakWithinMSFeature = false;

            IList<IsosResult> msFeatureList = run.ResultCollection.IsosResultBin;    //this is the small list if features found within a small m/z range, based on the targeted peak. 


            for (int i = 0; i < msFeatureList.Count; i++)
            {
                IsosResult msfeature = msFeatureList[i];
                if (msfeature.IsotopicProfile == null || msfeature.IsotopicProfile.Peaklist == null || msfeature.IsotopicProfile.Peaklist.Count == 0) continue;

                List<MSPeak> peaksWithinTol = DeconTools.Backend.Utilities.PeakUtilities.GetMSPeaksWithinTolerance(msfeature.IsotopicProfile.Peaklist, peakResult.MSPeak.XValue, toleranceInMZ);

                if (peaksWithinTol.Count == 0)
                {
                    foundPeakWithinMSFeature = false;
                }
                else if (peaksWithinTol.Count == 1)
                {
                    foundPeakWithinMSFeature = true;

                    bool peakResultAlreadyFoundInAnMSFeature = findPeakWithinMSFeatureResults(chromPeakBasedMSFeatures, peakResult, scanTolerance);
                    if (!peakResultAlreadyFoundInAnMSFeature)
                    {
                        run.ResultCollection.ResultList.Add(msfeature);   // add it to the big long list of MSFeatures found
                    }

                    chromPeakBasedMSFeatures.Add(msfeature);       //also add it to a temporary list.  This list is much smaller and can be searched more quickly. 


                }
                else
                {
                    Console.WriteLine("Not sure what to do with this case!");
                }

                if (foundPeakWithinMSFeature) break;
            }
        }

        private ScanSet createScanSetFromChromatogramPeak(Run run, IPeak chromPeak)
        {
            double sigma = chromPeak.Width / 2.35;      //   width@half-height =  2.35σ   (Gaussian peak theory)
            double centerXVal = chromPeak.XValue;

            int leftMostScan = (int)Math.Floor(centerXVal - sigma);
            int rightMostScan = (int)Math.Ceiling(centerXVal + sigma);
            int centerScan = (int)Math.Round(centerXVal);

            //TODO:  note that I am only summing 3 MS here... for speed sake
            centerScan = run.GetClosestMSScan(centerScan, Globals.ScanSelectionMode.CLOSEST);
            leftMostScan = run.GetClosestMSScan(leftMostScan, Globals.ScanSelectionMode.DESCENDING);
            rightMostScan = run.GetClosestMSScan(rightMostScan, Globals.ScanSelectionMode.ASCENDING);

            List<int> scanIndexList = new List<int>();

            for (int scanIndex = leftMostScan; scanIndex <= rightMostScan; scanIndex++)
            {
                if (run.GetMSLevel(scanIndex) == 1)
                {
                    scanIndexList.Add(scanIndex);
                }
            }

            ScanSet scanSet = new ScanSet(centerScan, scanIndexList.ToArray());
            return scanSet;



        }

        private XYData filterOutMSMSRelatedPoints(Run run, XYData chromatogram)
        {
            XYData output = new XYData();
            Dictionary<int, double> filteredChromVals = new Dictionary<int, double>();

            for (int i = 0; i < chromatogram.Xvalues.Length; i++)
            {
                int currentScanVal = (int)chromatogram.Xvalues[i];

                if (run.GetMSLevel(currentScanVal) == 1)
                {
                    filteredChromVals.Add(currentScanVal, chromatogram.Yvalues[i]);
                }
            }

            output.Xvalues = XYData.ConvertIntsToDouble(filteredChromVals.Keys.ToArray());
            output.Yvalues = filteredChromVals.Values.ToArray();

            return output;
        }

        #endregion





        #region IWorkflow Members



        #endregion
    }
}
