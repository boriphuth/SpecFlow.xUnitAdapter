﻿using System.Linq;
using Gherkin.Ast;
using SpecFlow.xUnitAdapter.SpecFlowPlugin.TestArtifacts;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SpecFlow.xUnitAdapter.SpecFlowPlugin.Framework
{
    public class SpecFlowTestDiscoverer : TestFrameworkDiscoverer
    {
        public SpecFlowTestDiscoverer(IAssemblyInfo assemblyInfo, ISourceInformationProvider sourceProvider, IMessageSink diagnosticMessageSink) : 
            base(new SpecFlowProjectAssemblyInfo(assemblyInfo), sourceProvider, diagnosticMessageSink)
        {
        }

        protected override ITestClass CreateTestClass(ITypeInfo typeInfo)
        {
            return (ITestClass)typeInfo;
        }

        protected override bool FindTestsForType(ITestClass testClass, bool includeSourceInformation, IMessageBus messageBus,
            ITestFrameworkDiscoveryOptions discoveryOptions)
        {
            var featureTestClass = (SpecFlowFeatureTestClass)testClass;
            var gherkinDocument = featureTestClass.GetDocument();
            if (gherkinDocument.SpecFlowFeature != null)
            {
                featureTestClass.FeatureName = gherkinDocument.SpecFlowFeature.Name;
                var featureTags = gherkinDocument.SpecFlowFeature.Tags.GetTags().ToArray();
                foreach (var scenarioDefinition in gherkinDocument.SpecFlowFeature.ScenarioDefinitions.Where(sd => !(sd is Background)))
                {
                    var scenario = scenarioDefinition as Scenario;
                    if (scenario != null)
                    {
                        var scenarioTestCase = new ScenarioTestCase(featureTestClass, scenario, featureTags);
                        if (!messageBus.QueueMessage(new TestCaseDiscoveryMessage(scenarioTestCase)))
                            return false;
                    }
                    var scenarioOutline = scenarioDefinition as ScenarioOutline;
                    if (scenarioOutline != null)
                    {
                        foreach (var example in scenarioOutline.Examples)
                        {
                            foreach (var exampleRow in example.TableBody)
                            {
                                var parameters = SpecFlowParserHelper.GetScenarioOutlineParameters(example, exampleRow);
                                var scenarioOutlineTestCase = new ScenarioTestCase(featureTestClass, scenarioOutline, featureTags, parameters, SpecFlowParserHelper.GetExampleRowId(scenarioOutline, exampleRow), exampleRow.Location);
                                if (!messageBus.QueueMessage(new TestCaseDiscoveryMessage(scenarioOutlineTestCase)))
                                    return false;
                            }
                        }
                    }
                    
                }
            }
            return true;
        }
    }
}