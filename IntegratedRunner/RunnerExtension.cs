using Autodesk.AutoCAD.Runtime;
using NUnitLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: ExtensionApplication(typeof(JPP.AutocadNUnitTester.IntegratedRunner.RunnerExtension))]

namespace JPP.AutocadNUnitTester.IntegratedRunner
{
    public class RunnerExtension : IExtensionApplication
    {
        public void Initialize()
        {
            
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("RunTests", CommandFlags.Session)]
        public void RunTets()
        {
            string[] args = new List<string>
            {
                "--verbose"
            }.ToArray();

            new AutoRun().Execute(args);
        }
    }
}
