﻿
namespace DeconTools.Backend.Core
{
    public class ChromPeak : Peak
    {

        public ChromPeak()
            : base()
        {
            NETValue = -1;
        }

        public ChromPeak(double xValue, float intensity, float width, float signalToNoise)
            : base(xValue, intensity, width)
        {
            SignalToNoise = signalToNoise;
        }


        #region Properties

        public double NETValue { get; set; }

        public float SignalToNoise { get; set; }

        #endregion


    }
}
