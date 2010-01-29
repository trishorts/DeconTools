﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeconTools.Backend.Core
{
    public abstract class IMassTagResult : IsosResult
    {
        #region Constructors
        #endregion

        #region Properties
        
        public abstract List<ChromPeak> ChromPeaks { get; set; }

        public abstract ChromPeak ChromPeakSelected { get; set; }

        public abstract MassTag MassTag { get; set; }

        public abstract double Score { get; set; }


        public abstract XYData ChromValues { get; set; }
        #endregion

        #region Public Methods
        public void DisplayToConsole()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("****** Match ******\n");
            sb.Append("NET = \t"+ MassTag.NETVal.ToString("0.000")+"\n");
            sb.Append("ChromPeak ScanNum = " + ChromPeakSelected.XValue.ToString() + "\n");
            sb.Append("ChromPeak NETVal = " + ChromPeakSelected.NETValue.ToString("0.000") + "\n");
            sb.Append("ScanSet = { ");
            foreach (int scanNum in ScanSet.IndexValues)
            {
                sb.Append(scanNum);
                sb.Append(", ");
                
            }
            sb.Append("} \n");
            if (this.IsotopicProfile != null)
            {
                sb.Append("Observed MZ and intensity = " + this.IsotopicProfile.getMonoPeak().XValue + "\t" + this.IsotopicProfile.getMonoPeak().Height + "\n");
            }
            sb.Append("FitScore = " + this.Score.ToString("0.0000")+"\n");
            Console.Write(sb.ToString());
        }

        #endregion

        #region Private Methods
        #endregion


        public double GetNET()
        {
            if (ChromPeakSelected == null) return -1;
            else
            {
                return ChromPeakSelected.NETValue;
            }

        }
    }
}