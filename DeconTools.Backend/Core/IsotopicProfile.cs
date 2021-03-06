﻿using System;
using System.Collections.Generic;
using DeconTools.Utilities;

namespace DeconTools.Backend.Core
{
    [Serializable]
    public class IsotopicProfile
    {

        public IsotopicProfile()
        {
            _peaklist = new List<MSPeak>();
        }

        private List<MSPeak> _peaklist;
        public List<MSPeak> Peaklist
        {
            get { return _peaklist; }
            set { _peaklist = value; }
        }

        /// <summary>
        /// Zero-based index value that points to which peak of the PeakList is the monoisotopic peak.  (it isn't always the first one)
        /// </summary>
        public int MonoIsotopicPeakIndex { get; set; }


        public bool IsSaturated { get; set; }


        private int _chargeState;
        public int ChargeState
        {
            get { return _chargeState; }
            set { _chargeState = value; }
        }

        /// <summary>
        /// The adjusted intensity of the isotopic profile. Currently used for correcting saturated profiles in IMS workflows
        /// </summary>
        public double IntensityAggregateAdjusted { get; set; }

        private double _originalIntensity;   // the unsummed intensity;  
        public double OriginalIntensity
        {
            get { return _originalIntensity; }
            set { _originalIntensity = value; }
        }

        private double _score;
        public double Score
        {
            get { return _score; }
            set { _score = value; }
        }

        private int _scoreCountBasis;
        public int ScoreCountBasis
        {
            get { return _scoreCountBasis; }
            set { _scoreCountBasis = value; }
        }

        private double _monoIsotopicMass;
        public double MonoIsotopicMass
        {
            get { return _monoIsotopicMass; }
            set { _monoIsotopicMass = value; }
        }

        private double _mostAbundantIsotopeMass;
        public double MostAbundantIsotopeMass
        {
            get { return _mostAbundantIsotopeMass; }
            set { _mostAbundantIsotopeMass = value; }
        }

        private float _monoPlusTwoAbundance;
        public float MonoPlusTwoAbundance
        {
            get { return _monoPlusTwoAbundance; }
            set { _monoPlusTwoAbundance = value; }
        }

        private double _averageMass;
        public double AverageMass
        {
            get { return _averageMass; }
            set { _averageMass = value; }
        }

        public double MonoPeakMZ { get; set; }


        public bool IsFlagged { get; set; }

       
        /// <summary>
        /// Intensity of the most abundant peak of the isotopic profile
        /// </summary>
        public float IntensityMostAbundant { get; set; }

        /// <summary>
        /// Intensity of the peak that relates to the most abundant peak of the theoretical profile
        /// </summary>
        public float IntensityMostAbundantTheor { get; set; }


        public int GetNumOfIsotopesInProfile()
        {
            return _peaklist.Count;
        }

        public double GetMZofMostAbundantPeak()
        {
            var mostIntensePeak = getMostIntensePeak();
            if (mostIntensePeak == null) return -1;
            return mostIntensePeak.XValue;
        }

        public double GetFWHM()
        {
            var mostIntensePeak = getMostIntensePeak();
            if (mostIntensePeak == null)
            {
                return -1;
            }
            return mostIntensePeak.Width;
        }


        public int GetIndexOfMostIntensePeak()
        {
            if (_peaklist == null || _peaklist.Count == 0) return -1;

            var indexOfMaxPeak = -1;
            float maxIntensity = 0;

            for (var i = 0; i < _peaklist.Count; i++)
            {
                if (_peaklist[i].Height > maxIntensity)
                {
                    maxIntensity = _peaklist[i].Height;
                    indexOfMaxPeak = i;
                }
            }
            return indexOfMaxPeak;

        }

        public MSPeak getMostIntensePeak()
        {
            if (_peaklist == null || _peaklist.Count == 0) return null;

            var maxPeak = new MSPeak();
            foreach (var peak in _peaklist)
            {
                if (peak.Height >= maxPeak.Height)
                {
                    maxPeak = peak;
                }

            }
            return maxPeak;

        }

        public double GetSignalToNoise()
        {
            var mostIntensePeak = getMostIntensePeak();
            if (mostIntensePeak == null)
            {
                return -1;
            }
            return mostIntensePeak.SignalToNoise;
        }

        public double GetMonoAbundance()
        {
            if (_peaklist == null || Peaklist.Count == 0) return 0;
            return _peaklist[0].Height;
        }

        public float GetMonoPlusTwoAbundance()
        {
            if (_peaklist == null || _peaklist.Count < 3) return 0;
            return _peaklist[2].Height;
        }

        public double GetMZ()
        {
            if (_peaklist == null || Peaklist.Count == 0) return -1;
            return _peaklist[0].XValue;
        }



        public double GetAbundance()
        {
            return getMostIntensePeak().Height;
        }

        public double GetScore()
        {
            return _score;
        }

        public MSPeak getMonoPeak()
        {
            if (_peaklist != null && _peaklist.Count > 0)
            {
                return _peaklist[0];
            }
            else
            {
                return null;
            }
        }

        public double GetSummedIntensity()
        {
            if (_peaklist == null) return -1;
            double summedIntensity = 0;
            foreach (var peak in _peaklist)
            {
                summedIntensity += (double)peak.Height;

            }
            return summedIntensity;
        }


        public IsotopicProfile CloneIsotopicProfile()
        {
            var iso = new IsotopicProfile();
            iso.AverageMass = AverageMass;
            iso.ChargeState = ChargeState;
            iso.IntensityMostAbundant = IntensityMostAbundant;
            iso.IntensityMostAbundantTheor = IntensityMostAbundantTheor;
            iso.MonoIsotopicMass = MonoIsotopicMass;
            iso.MonoIsotopicPeakIndex = MonoIsotopicPeakIndex;
            iso.MonoPeakMZ = MonoPeakMZ;
            iso.MonoPlusTwoAbundance = MonoPlusTwoAbundance;
            iso.MostAbundantIsotopeMass = MostAbundantIsotopeMass;
            iso.IsSaturated = IsSaturated;
            iso.OriginalIntensity = OriginalIntensity;
            iso.Peaklist = new List<MSPeak>();

            foreach (var mspeak in Peaklist)
            {
                var peak = new MSPeak(mspeak.XValue, mspeak.Height, mspeak.Width, mspeak.SignalToNoise);
                iso.Peaklist.Add(peak);
            }

            iso.Score = Score;
            iso.ScoreCountBasis = ScoreCountBasis;

            return iso;

        }


        public XYData GetTheoreticalIsotopicProfileXYData(double fwhm)
        {
            Check.Require(this != null && Peaklist != null &&
                          Peaklist.Count > 0, "Cannot get theor isotopic profile. Input isotopic profile is empty.");

            var xydata = new XYData();
            var xvals = new List<double>();
            var yvals = new List<double>();

            for (var i = 0; i < Peaklist.Count; i++)
            {
                var tempXYData = Peaklist[i].GetTheorPeakData(fwhm);
                xvals.AddRange(tempXYData.Xvalues);
                yvals.AddRange(tempXYData.Yvalues);

            }
            xydata.Xvalues = xvals.ToArray();
            xydata.Yvalues = yvals.ToArray();



            return xydata;
        }

        public XYData GetTheoreticalIsotopicProfileXYData(double fwhm, double mzOffset)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return MonoPeakMZ.ToString("0.0000") + "; " + ChargeState + "; " + MonoIsotopicMass.ToString("0.0000");
        }
    }
}
