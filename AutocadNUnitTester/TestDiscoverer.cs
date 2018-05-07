using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JPP.AutocadNUnitTester
{
    class TestDiscoverer : ITestDiscoverer
    {
        //private Dump.DumpXml dumpXml;

        #region ITestDiscoverer Members

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif
            //Initialize(discoveryContext, messageLogger);



            //TestLog.Info($"NUnit Adapter {AdapterVersion}: Test discovery starting");

            // Ensure any channels registered by other adapters are unregistered
            /*CleanUpRegisteredChannels();

            if (Settings.InProcDataCollectorsAvailable && sources.Count() > 1)
            {
                TestLog.Error("Unexpected to discover tests in multiple assemblies when InProcDataCollectors specified in run configuration.");
                Unload();
                return;
            }*/

            foreach (string sourceAssembly in sources)
            {
                var sourceAssemblyPath = Path.IsPathRooted(sourceAssembly) ? sourceAssembly : Path.Combine(Directory.GetCurrentDirectory(), sourceAssembly);
                /*TestLog.Debug("Processing " + sourceAssembly);
                ITestRunner runner = null;

                if (Settings.DumpXmlTestDiscovery)
                {
                    dumpXml = new DumpXml(sourceAssemblyPath);

                }*/

                try
                {
                    runner = GetRunnerFor(sourceAssemblyPath);

                    XmlNode topNode = runner.Explore(TestFilter.Empty);
#if !NETCOREAPP1_0
                    dumpXml?.AddString(topNode.AsString());
#endif
                    // Currently, this will always be the case but it might change
                    if (topNode.Name == "test-run")
                        topNode = topNode.FirstChild;

                    if (topNode.GetAttribute("runstate") == "Runnable")
                    {
                        int cases;
                        using (var testConverter = new TestConverter(TestLog, sourceAssemblyPath, Settings.CollectSourceInformation))
                        {
                            cases = ProcessTestCases(topNode, discoverySink, testConverter);
                        }

                        TestLog.Debug($"Discovered {cases} test cases");
                        // Only save if seed is not specified in runsettings
                        // This allows workaround in case there is no valid
                        // location in which the seed may be saved.
                        if (cases > 0 && !Settings.RandomSeedSpecified)
                            Settings.SaveRandomSeed(Path.GetDirectoryName(sourceAssemblyPath));
                    }
                    else
                    {
                        var msgNode = topNode.SelectSingleNode("properties/property[@name='_SKIPREASON']");
                        if (msgNode != null && (new[] { "contains no tests", "Has no TestFixtures" }).Any(msgNode.GetAttribute("value").Contains))
                            TestLog.Info("Assembly contains no NUnit 3.0 tests: " + sourceAssembly);
                        else
                            TestLog.Info("NUnit failed to load " + sourceAssembly);
                    }

                }
                catch (BadImageFormatException)
                {
                    // we skip the native c++ binaries that we don't support.
                    TestLog.Warning("Assembly not supported: " + sourceAssembly);
                }
                catch (FileNotFoundException ex)
                {
                    // Either the NUnit framework was not referenced by the test assembly
                    // or some other error occured. Not a problem if not an NUnit assembly.
                    TestLog.Warning("Dependent Assembly " + ex.FileName + " of " + sourceAssembly + " not found. Can be ignored if not a NUnit project.");
                }
                catch (FileLoadException ex)
                {
                    // Attempts to load an invalid assembly, or an assembly with missing dependencies
                    TestLog.Warning("Assembly " + ex.FileName + " loaded through " + sourceAssembly + " failed. Assembly is ignored. Correct deployment of dependencies if this is an error.");
                }
                catch (TypeLoadException ex)
                {
                    if (ex.TypeName == "NUnit.Framework.Api.FrameworkController")
                        TestLog.Warning("   Skipping NUnit 2.x test assembly");
                    else
                        TestLog.Warning("Exception thrown discovering tests in " + sourceAssembly, ex);
                }
                catch (Exception ex)
                {
                    TestLog.Warning("Exception thrown discovering tests in " + sourceAssembly, ex);
                }
                finally
                {
#if !NETCOREAPP1_0
                    dumpXml?.Dump4Discovery();
#endif
                    if (runner != null)
                    {
                        if (runner.IsTestRunning)
                            runner.StopRun(true);

                        runner.Unload();
                        runner.Dispose();
                    }
                }
            }

            TestLog.Info($"NUnit Adapter {AdapterVersion}: Test discovery complete");

            Unload();
        }

#endregion

        #region Helper Methods

        private int ProcessTestCases(XmlNode topNode, ITestCaseDiscoverySink discoverySink, TestConverter testConverter)
        {
            int cases = 0;
            foreach (XmlNode testNode in topNode.SelectNodes("//test-case"))
            {
                try
                {
#if LAUNCHDEBUGGER
                    if (!Debugger.IsAttached)
                        Debugger.Launch();
#endif
                    TestCase testCase = testConverter.ConvertTestCase(testNode);
                    discoverySink.SendTestCase(testCase);
                    cases += 1;
                }
                catch (Exception ex)
                {
                    TestLog.Warning("Exception converting " + testNode.GetAttribute("fullname"), ex);
                }
            }

            return cases;
        }

        #endregion
    }
}
