﻿using System;
using System.IO;
using DeconTools.Workflows.Backend.Core;
using DeconTools.Workflows.Backend.FileIO;
using DeconTools.Workflows.Backend.Results;
using NUnit.Framework;

namespace DeconTools.Workflows.UnitTesting.WorkflowTests
{
    [TestFixture]
    [Category("Functional")]
    public class BasicTargetedWorkflowExecutorTests
    {

        [Test]
        public void ParameterTest1()
        {
            string outputFileName = @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\Unlabelled\BasicTargetedWorkflowExecutorParameters_autoGenerated.xml";
            var executorParameters = new BasicTargetedWorkflowExecutorParameters();



            executorParameters.SaveParametersToXML(outputFileName);

        }


        [Category("MustPass")]
        [Test]
        public void targetedWorkflow_alignUsingDataFromFiles()
        {
            // https://jira.pnnl.gov/jira/browse/OMCS-714

            BasicTargetedWorkflowExecutorParameters executorParameters = new BasicTargetedWorkflowExecutorParameters();
            executorParameters.TargetsFilePath =
                @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\Unlabelled\Targets\QCShew_Formic_MassTags_Bin10_MT24702_Z3.txt";
            executorParameters.TargetedAlignmentIsPerformed = true;
            executorParameters.TargetsUsedForAlignmentFilePath =
                @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\Unlabelled\Targets\QCShew_Formic_MassTags_Bin10_all.txt";

            executorParameters.TargetedAlignmentWorkflowParameterFile =
                @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\Unlabelled\Parameters\TargetedAlignmentWorkflowParameters1.xml";

            var workflowParameters = new BasicTargetedWorkflowParameters();
            workflowParameters.ChromSmootherNumPointsInSmooth = 9;
            workflowParameters.ChromPeakDetectorPeakBR = 1;
            workflowParameters.ChromPeakDetectorSigNoise = 1;
            workflowParameters.ChromToleranceInPPM = 20;
            workflowParameters.ChromNETTolerance = 0.025;
            workflowParameters.MSToleranceInPPM = 20;

            BasicTargetedWorkflow workflow = new BasicTargetedWorkflow(workflowParameters);

            string testDatasetPath =
                @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\Unlabelled\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.RAW";

            TargetedWorkflowExecutor executor = new BasicTargetedWorkflowExecutor(executorParameters, workflow, testDatasetPath);
            executor.Execute();

            string expectedResultsFilename =
                @"C:\Users\d3x720\Documents\Data\QCShew\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18_results.txt";

            var result = executor.TargetedWorkflow.Result;
            Assert.IsTrue(workflow.Success);
            Assert.IsFalse(result.FailedResult);
            Assert.IsNotNull(result.ScanSet);
            Assert.IsNotNull(result.ChromPeakSelected);

            Assert.IsTrue(result.Score < 0.1);
            Assert.AreEqual(3, result.NumChromPeaksWithinTolerance);
            Assert.AreEqual(8627, (decimal)Math.Round(result.ChromPeakSelected.XValue, 0));
            
            //non-calibrated mass directly from mass spectrum
            Assert.AreEqual(2920.49120m, (decimal) Math.Round(result.IsotopicProfile.MonoIsotopicMass,5));

            //calibrated mass
            Assert.AreEqual(2920.50018m, (decimal) Math.Round(result.GetCalibratedMonoisotopicMass(),5));


            Console.WriteLine("theor monomass= \t" + result.Target.MonoIsotopicMass);
            Console.WriteLine("monomass= \t" + result.IsotopicProfile.MonoIsotopicMass);
            Console.WriteLine("ppmError before= \t" + result.GetMassErrorBeforeAlignmentInPPM());
            Console.WriteLine("ppmError after= \t" + result.GetMassErrorAfterAlignmentInPPM());


            var calibratedMass = -1 * ((result.Target.MonoIsotopicMass * result.GetMassErrorAfterAlignmentInPPM() / 1e6) -
                                  result.Target.MonoIsotopicMass);
            var calibratedMass2 = result.GetCalibratedMonoisotopicMass();


            Console.WriteLine("calibrated mass= \t" + calibratedMass);
            Console.WriteLine("calibrated mass2= \t" + calibratedMass2);
            Console.WriteLine("Database NET= " + result.Target.NormalizedElutionTime);
            Console.WriteLine("Result NET= " + result.GetNET());
            Console.WriteLine("Result NET Error= " + result.GetNETAlignmentError());
            Console.WriteLine("NumChromPeaksWithinTol= " + result.NumChromPeaksWithinTolerance);

        //Dataset	TargetID	Code	EmpiricalFormula	ChargeState	Scan	ScanStart	ScanEnd	NumMSSummed	NET	NETError	NumChromPeaksWithinTol	NumQualityChromPeaksWithinTol	MonoisotopicMass	MonoisotopicMassCalibrated	MassErrorInPPM	MonoMZ	IntensityRep	FitScore	IScore	FailureType	ErrorDescription
        //QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18	24702	LLKEEGYIADYAVADEAKPELEITLK	C133H213N29O44	3	8624	8596	8659	0	0.42916	-0.009395	3	1	2920.49120	2920.50018	13.96	974.50434	7529645	0.0193	0.0000		


        }

        [Test]
        public void targetedWorkflow_alignUsingDataFromFiles_localVersion()
        {
            //TODO: figure out result is correct
            //TODO: get MS and Chrom in Jira
            BasicTargetedWorkflowExecutorParameters executorParameters = new BasicTargetedWorkflowExecutorParameters();
            executorParameters.TargetsFilePath =
                @"C:\Users\d3x720\Documents\Data\QCShew\IQ\QCShew_Formic_MassTags_Bin10_MT24702_Z3.txt";
            executorParameters.TargetedAlignmentIsPerformed = true;
            executorParameters.TargetsUsedForAlignmentFilePath =
                @"C:\Users\d3x720\Documents\Data\QCShew\IQ\QCShew_Formic_MassTags_Bin10_all.txt";

            executorParameters.TargetedAlignmentWorkflowParameterFile =
                @"C:\Users\d3x720\Documents\Data\QCShew\IQ\TargetedAlignmentWorkflowParameters1.xml";


            BasicTargetedWorkflowParameters workflowParameters = new BasicTargetedWorkflowParameters();
            workflowParameters.ChromSmootherNumPointsInSmooth = 9;
            workflowParameters.ChromPeakDetectorPeakBR = 1;
            workflowParameters.ChromPeakDetectorSigNoise = 1;
            workflowParameters.ChromToleranceInPPM = 20;
            workflowParameters.ChromNETTolerance = 0.025;
            workflowParameters.MSToleranceInPPM = 20;

            BasicTargetedWorkflow workflow = new BasicTargetedWorkflow(workflowParameters);

            string testDatasetPath =
                @"C:\Users\d3x720\Documents\Data\QCShew\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.RAW";

            TargetedWorkflowExecutor executor = new BasicTargetedWorkflowExecutor(executorParameters, workflow, testDatasetPath);
            executor.Execute();

            string expectedResultsFilename =
                @"C:\Users\d3x720\Documents\Data\QCShew\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18_results.txt";


            var result = executor.TargetedWorkflow.Result;

            Console.WriteLine("theor monomass= \t" + result.Target.MonoIsotopicMass);
            Console.WriteLine("monomass= \t" + result.IsotopicProfile.MonoIsotopicMass);
            Console.WriteLine("ppmError before= \t" + result.GetMassErrorBeforeAlignmentInPPM());
            Console.WriteLine("ppmError after= \t" + result.GetMassErrorAfterAlignmentInPPM());


            var calibratedMass = -1 * ((result.Target.MonoIsotopicMass * result.GetMassErrorAfterAlignmentInPPM() / 1e6) -
                                  result.Target.MonoIsotopicMass);


            var calibratedMass2 = result.GetCalibratedMonoisotopicMass();


            Console.WriteLine("calibrated mass= \t" + calibratedMass);
            Console.WriteLine("calibrated mass2= \t" + calibratedMass2);


            Console.WriteLine("Database NET= " + result.Target.NormalizedElutionTime);
            Console.WriteLine("Result NET= " + result.GetNET());
            Console.WriteLine("Result NET Error= " + result.GetNETAlignmentError());
            Console.WriteLine("NumChromPeaksWithinTol= " + result.NumChromPeaksWithinTolerance);

            //Dataset	MassTagID	ChargeState	Scan	ScanStart	ScanEnd	NET	NumChromPeaksWithinTol	NumQualityChromPeaksWithinTol	MonoisotopicMass	MonoMZ	IntensityRep	FitScore	IScore	FailureType

            //QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18	24702	3	8119	8112	8124	0.4172	2	1	2920.53082	974.51755	1379489	0.1136	0.0000	

        }

        [Test]
        public void targetedWorkflow_noAlignment()
        {
            //TODO: fix test - it is using previous data for alignment
            //TODO: make JIRA issue so we can see chromatogram, etc.

            string executorParameterFile = @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\QCShew_OrbiStandard_workflowExecutorParameters.xml";
            BasicTargetedWorkflowExecutorParameters executorParameters = new BasicTargetedWorkflowExecutorParameters();
            executorParameters.LoadParameters(executorParameterFile);




            string resultsFolderLocation = executorParameters.ResultsFolder;
            string testDatasetPath = @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.RAW";
            string testDatasetName = "QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18";

            string expectedResultsFilename = resultsFolderLocation + "\\" + testDatasetName + "_results.txt";
            if (File.Exists(expectedResultsFilename))
            {
                File.Delete(expectedResultsFilename);
            }


            FileInfo rawFileInfo = new FileInfo(testDatasetPath);
            executorParameters.AlignmentInfoFolder = rawFileInfo.DirectoryName;


            //delete alignment files
            string mzalignmentFile = rawFileInfo.DirectoryName + "\\" + testDatasetName + "_mzAlignment.txt";
            string netAlignmentFile = rawFileInfo.DirectoryName + "\\" + testDatasetName + "_netAlignment.txt";
            if (File.Exists(mzalignmentFile)) File.Delete(mzalignmentFile);
            if (File.Exists(netAlignmentFile)) File.Delete(netAlignmentFile);


            executorParameters.TargetedAlignmentIsPerformed = false;    //no targeted alignment

            TargetedWorkflowExecutor executor = new BasicTargetedWorkflowExecutor(executorParameters, testDatasetPath);
            executor.Execute();

            Assert.IsTrue(File.Exists(expectedResultsFilename));

            var importer = new UnlabelledTargetedResultFromTextImporter(expectedResultsFilename);
            TargetedResultRepository repository = importer.Import();

            Assert.AreEqual(10, repository.Results.Count);

            TargetedResultDTO result1 = repository.Results[2];
            Assert.AreEqual(24702, result1.TargetID);
            Assert.AreEqual(3, result1.ChargeState);
            Assert.AreEqual(8112, result1.ScanLC);

            //TODO: confirm/fix this NET value
            Assert.AreEqual(0.41724m, (decimal)Math.Round(result1.NET, 5));
            // Assert.AreEqual(0.002534m, (decimal)Math.Round(result1.NETError, 6));
            Assert.AreEqual(974.52068m, (decimal)Math.Round(result1.MonoMZ, 5));
            Assert.AreEqual(2920.53082m, (decimal)Math.Round(result1.MonoMass, 5));
            //Assert.AreEqual(2920.53733m, (decimal)Math.Round(result1.MonoMassCalibrated, 5));
            //Assert.AreEqual(-1.83m, (decimal)Math.Round(result1.MassErrorInPPM, 2));


        }


        [Test]
        public void AlternateConstructor_targetedWorkflowNoAlignment()
        {
            string executorParameterFile = @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\QCShew_OrbiStandard_workflowExecutorParameters.xml";
            BasicTargetedWorkflowExecutorParameters executorParameters = new BasicTargetedWorkflowExecutorParameters();
            executorParameters.LoadParameters(executorParameterFile);
            string resultsFolderLocation = executorParameters.ResultsFolder;
            string testDatasetPath = @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.RAW";
            string testDatasetName = "QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18";

            string expectedResultsFilename = resultsFolderLocation + "\\" + testDatasetName + "_results.txt";
            if (File.Exists(expectedResultsFilename))
            {
                File.Delete(expectedResultsFilename);
            }

            var basicTargetedWorkflowParameters = new BasicTargetedWorkflowParameters();
            BasicTargetedWorkflow workflow = new BasicTargetedWorkflow(basicTargetedWorkflowParameters);

            TargetedWorkflowExecutor executor = new BasicTargetedWorkflowExecutor(executorParameters,
                workflow, testDatasetPath);
            executor.Execute();

            Assert.IsTrue(File.Exists(expectedResultsFilename));
        }

        [Test]
        [Category("LongRunning")]
        public void targetedWorkflow_withTargetedAlignment_test()
        {

            string executorParameterFile = @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\QCShew_OrbiStandard_workflowExecutorParameters.xml";
            BasicTargetedWorkflowExecutorParameters executorParameters = new BasicTargetedWorkflowExecutorParameters();
            executorParameters.LoadParameters(executorParameterFile);

            string resultsFolderLocation = executorParameters.ResultsFolder;
            string testDatasetPath = @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.RAW";
            string testDatasetName = "QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18";

            string expectedResultsFilename = resultsFolderLocation + "\\" + testDatasetName + "_results.txt";
            if (File.Exists(expectedResultsFilename))
            {
                File.Delete(expectedResultsFilename);
            }


            FileInfo rawFileInfo = new FileInfo(testDatasetPath);
            executorParameters.AlignmentInfoFolder = rawFileInfo.DirectoryName;

            string mzalignmentFile = rawFileInfo.DirectoryName + "\\" + testDatasetName + "_mzAlignment.txt";
            string netAlignmentFile = rawFileInfo.DirectoryName + "\\" + testDatasetName + "_netAlignment.txt";
            if (File.Exists(mzalignmentFile)) File.Delete(mzalignmentFile);
            if (File.Exists(netAlignmentFile)) File.Delete(netAlignmentFile);




            TargetedWorkflowExecutor executor = new BasicTargetedWorkflowExecutor(executorParameters, testDatasetPath);
            executor.Execute();

            Assert.IsTrue(File.Exists(expectedResultsFilename));

            UnlabelledTargetedResultFromTextImporter importer = new UnlabelledTargetedResultFromTextImporter(expectedResultsFilename);
            Backend.Results.TargetedResultRepository repository = importer.Import();

            Assert.AreEqual(10, repository.Results.Count);

            TargetedResultDTO result1 = repository.Results[2];


            Assert.AreEqual(24702, result1.TargetID);
            Assert.AreEqual(3, result1.ChargeState);
            Assert.AreEqual(8112, result1.ScanLC);
            //Assert.AreEqual(0.41724m, (decimal)Math.Round(result1.NET, 5));
            //Assert.AreEqual(0.002534m, (decimal)Math.Round(result1.NETError, 6));
            Assert.AreEqual(2920.53082m, (decimal)Math.Round(result1.MonoMass, 5));
            Assert.AreEqual(2920.53879m, (decimal)Math.Round(result1.MonoMassCalibrated, 5));
            Assert.AreEqual(-2.33m, (decimal)Math.Round(result1.MassErrorInPPM, 2));

            //Dataset	MassTagID	ChargeState	Scan	ScanStart	ScanEnd	NET	NumChromPeaksWithinTol	NumQualityChromPeaksWithinTol	MonoisotopicMass	MonoMZ	IntensityRep	FitScore	IScore	FailureType

            //QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18	24702	3	8119	8112	8124	0.4172	2	1	2920.53082	974.51755	1379489	0.1136	0.0000	

        }



        [Test]
        [Category("LongRunning")]
        public void copyToLocalTest1()
        {

            string executorParameterFile =
                @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\basicTargetedWorkflowExecutorParameters_CopyToLocalTestCase2.xml";
            string datasetPath = @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.RAW";
            string testDatasetName = "QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18";



            BasicTargetedWorkflowExecutorParameters executorParameters = new BasicTargetedWorkflowExecutorParameters();
            executorParameters.LoadParameters(executorParameterFile);



            string expectedResultsFilename = executorParameters.ResultsFolder + "\\" + testDatasetName + "_results.txt";
            if (File.Exists(expectedResultsFilename))
            {
                File.Delete(expectedResultsFilename);
            }

            TargetedWorkflowExecutor executor = new BasicTargetedWorkflowExecutor(executorParameters, datasetPath);
            executor.Execute();

            Assert.IsTrue(File.Exists(expectedResultsFilename));

            UnlabelledTargetedResultFromTextImporter importer = new UnlabelledTargetedResultFromTextImporter(expectedResultsFilename);
            Backend.Results.TargetedResultRepository repository = importer.Import();

            Assert.AreEqual(10, repository.Results.Count);

            TargetedResultDTO result1 = repository.Results[2];


            Assert.AreEqual(24702, result1.TargetID);
            Assert.AreEqual(3, result1.ChargeState);
            Assert.AreEqual(8112, result1.ScanLC);




            //Dataset	MassTagID	ChargeState	Scan	ScanStart	ScanEnd	NET	NumChromPeaksWithinTol	NumQualityChromPeaksWithinTol	MonoisotopicMass	MonoMZ	IntensityRep	FitScore	IScore	FailureType

            //QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18	24702	3	8119	8112	8124	0.4172	2	1	2920.53082	974.51755	1379489	0.1136	0.0000	

        }


    }
}
