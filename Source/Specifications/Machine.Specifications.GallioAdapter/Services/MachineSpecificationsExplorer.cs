using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Gallio.Model;
using Gallio.Reflection;
using Machine.Specifications.Explorers;
using Machine.Specifications.GallioAdapter.Model;
using Machine.Specifications.GallioAdapter.Properties;
using Machine.Specifications.Model;

namespace Machine.Specifications.GallioAdapter.Services
{
  public class MachineSpecificationsExplorer : BaseTestExplorer
  {
    private const string MachineSpecificationsAssemblyDisplayName = @"Machine.Specifications";

    public readonly Dictionary<Version, ITest> frameworkTests;
    public readonly Dictionary<IAssemblyInfo, ITest> assemblyTests;
    public readonly Dictionary<ITypeInfo, ITest> typeTests;

    public MachineSpecificationsExplorer(TestModel testModel)
      : base(testModel)
    {
      frameworkTests = new Dictionary<Version, ITest>();
      assemblyTests = new Dictionary<IAssemblyInfo, ITest>();
      typeTests = new Dictionary<ITypeInfo, ITest>();
    }

    public override void ExploreAssembly(IAssemblyInfo assembly, Action<ITest> consumer)
    {
      Version frameworkVersion = GetFrameworkVersion(assembly);

      if (frameworkVersion != null)
      {
        ITest frameworkTest = GetFrameworkTest(frameworkVersion, TestModel.RootTest);
        ITest assemblyTest = GetAssemblyTest(assembly, frameworkTest, true);

        if (consumer != null)
          consumer(assemblyTest);
      }
    }
    private static Version GetFrameworkVersion(IAssemblyInfo assembly)
    {
      AssemblyName frameworkAssemblyName = ReflectionUtils.FindAssemblyReference(assembly, MachineSpecificationsAssemblyDisplayName);
      return frameworkAssemblyName != null ? frameworkAssemblyName.Version : null;
    }

    private ITest GetFrameworkTest(Version frameworkVersion, ITest rootTest)
    {
      ITest frameworkTest;
      if (!frameworkTests.TryGetValue(frameworkVersion, out frameworkTest))
      {
        frameworkTest = CreateFrameworkTest(frameworkVersion);
        rootTest.AddChild(frameworkTest);

        frameworkTests.Add(frameworkVersion, frameworkTest);
      }

      return frameworkTest;
    }

    private ITest GetAssemblyTest(IAssemblyInfo assembly, ITest frameworkTest, bool populateRecursively)
    {
      ITest assemblyTest;
      if (!assemblyTests.TryGetValue(assembly, out assemblyTest))
      {
        assemblyTest = CreateAssemblyTest(assembly);
        frameworkTest.AddChild(assemblyTest);

        assemblyTests.Add(assembly, assemblyTest);
      }

      if (populateRecursively)
      {
        PopulateAssemblyTest(assembly, assemblyTest);
      }

      return assemblyTest;
    }

    private void PopulateAssemblyTest(IAssemblyInfo assembly, ITest assemblyTest)
    {
      AssemblyExplorer explorer = new AssemblyExplorer();
      var specifications = explorer.FindDescriptionsIn(assembly.Resolve());
      foreach (var specification in specifications)
      {
        var specificationTest = new MachineDescriptionTest(specification);
        assemblyTest.AddChild(specificationTest);

        PopulateSpecificationTest(specification, specificationTest);
      }
    }

    private void PopulateSpecificationTest(Description description, MachineDescriptionTest test)
    {
      foreach (var specification in description.Specifications)
      {
        test.AddChild(new MachineSpecificationTest(specification));
      }
    }

    private static ITest CreateFrameworkTest(Version frameworkVersion)
    {
      BaseTest frameworkTest = new BaseTest(String.Format(Resources.MachineSpecificationExplorer_FrameworkNameWithVersionFormat, frameworkVersion), null);
      frameworkTest.BaselineLocalId = Resources.MachineSpecificationFramework_MachineSpecificationFrameworkName;
      frameworkTest.Kind = TestKinds.Framework;

      return frameworkTest;
    }

    private static ITest CreateAssemblyTest(IAssemblyInfo assembly)
    {
      BaseTest assemblyTest = new BaseTest(assembly.Name, assembly);
      assemblyTest.Kind = TestKinds.Assembly;

      ModelUtils.PopulateMetadataFromAssembly(assembly, assemblyTest.Metadata);

      return assemblyTest;
    }
  }
}