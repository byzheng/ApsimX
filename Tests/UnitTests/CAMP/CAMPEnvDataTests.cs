using APSIM.Core;
using APSIM.Shared.Utilities;
using Models;
using Models.Agroforestry;
using Models.Core;
using Models.PMF;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace UnitTests.PMF.Phenology.CAMP
{
    /// <summary>
    /// Unit tests for the CAMPEnvData class.
    /// </summary>
    [TestFixture]
    public class CAMPEnvDataTests
    {
        /// <summary>
        /// Test [Phenology].CAMP.EnvData.VrnTreatTemp with reasonable value from 1 to 8
        /// </summary>
        [TestCase(4, 1)]
        [TestCase(0, 0)]
        [TestCase(9, 0)]
        public void TestVrnTreatTempValue(double vrnTreatTemp, int expectedRows)
        {
            DataStore storage = RunSimulationWithVrnTreatTemp(vrnTreatTemp);
            var dataTable = storage.Reader.GetData("Report");
            if (dataTable.Rows.Count == 0)
            {
                Console.WriteLine("No rows found in Report table");
            }
            else
            {
                foreach (System.Data.DataRow row in dataTable.Rows)
                {
                    Console.WriteLine(string.Join(", ", dataTable.Columns.Cast<System.Data.DataColumn>().Select(col => row[col])));
                }
            }
            Assert.That(expectedRows, Is.EqualTo(dataTable.Rows.Count),
                $"Expected exactly {expectedRows} row(s) in the Report table.");
        }

        private static DataStore RunSimulationWithVrnTreatTemp(double vrnTreatTemp)
        {
            string path = Path.Combine("%root%", "Examples", "Wheat.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;

            foreach (Soil soil in sims.Node.FindChildren<Soil>(recurse: true))
                soil.Sanitise();

            var replacementsFolder = new Folder()
            {
                Name = "Replacements",
                Children = new List<IModel>()
                {
                    new Cultivar()
                    {
                        Name = "Hartog",
                        Command = [
                            $"[Phenology].CAMP.EnvData.VrnTreatTemp = {vrnTreatTemp.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        ]
                    }
                }
            };

            sims.Children.Add(replacementsFolder);

            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.ClearChildLists();
            storage.UseInMemoryDB = true;
            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);
            Utilities.ResolveLinks(sim);
            Clock clock = sim.Node.FindChild<Clock>(recurse: true);
            clock.EndDate = new DateTime(1900, 12, 31);
            sim.Prepare();
            sim.Run();
            storage.Writer.Stop();
            storage.Reader.Refresh();

            return storage;
        }
    }
}
