﻿using DeconTools.Backend.Core;
using DeconTools.Utilities;

namespace DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator
{
    public abstract class ITheorFeatureGenerator:Task
    {
        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Public Methods
        public abstract void LoadRunRelatedInfo(ResultCollection results);
        public abstract void GenerateTheorFeature(TargetBase mt);
        #endregion

        #region Private Methods
        #endregion
        public override void Execute(ResultCollection resultList)
        {
            Check.Require(resultList.Run.CurrentMassTag != null, "Theoretical feature generator failed. No target mass tag was provided");

            LoadRunRelatedInfo(resultList);
            
            GenerateTheorFeature(resultList.Run.CurrentMassTag);
        }
    }
}
