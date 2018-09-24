﻿namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using Models.Factorial;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// Converts the .apsim file from one version to the next
    /// </summary>
    public class Converter
    {
        /// <summary>Gets the latest .apsimx file format version.</summary>
        public static int LatestVersion { get { return 42; } }

        /// <summary>Converts to file to the latest version.</summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>Returns true if something was changed.</returns>
        public static Stream ConvertToLatestVersion(string fileName)
        {
            return ConvertToVersion(fileName, LatestVersion);
        }

        /// <summary>Converts XML to the latest version.</summary>
        /// <param name="rootNode">The root node.</param>
        /// <param name="fileName">The name of the .apsimx file</param>
        /// <returns>Returns true if something was changed.</returns>
        public static bool ConvertToLatestVersion(XmlNode rootNode, string fileName)
        {
            return ConvertToVersion(rootNode, fileName, LatestVersion);
        }

        /// <summary>
        /// Converts XML to a given version.
        /// </summary>
        /// <param name="fileName">Filename (including path) of the file to be converted.</param>
        /// <param name="version">Version to which the file will be converted.</param>
        /// <returns></returns>
        public static Stream ConvertToVersion(string fileName, int version)
        {
            // Load the file.
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            // Apply converter.
            bool changed = ConvertToVersion(doc.DocumentElement, fileName, version);

            if (changed)
            {
                MemoryStream memStream = new MemoryStream();
                doc.Save(memStream);
                memStream.Seek(0, SeekOrigin.Begin);
                return memStream;
            }
            else
                return File.OpenRead(fileName);
        }

        /// <summary>Converts XML to the latest version.</summary>
        /// <param name="rootNode">The root node.</param>
        /// <param name="fileName">The name of the .apsimx file</param>
        /// <param name="toVersion">Version number to convert to</param>
        /// <returns>Returns true if something was changed.</returns>
        public static bool ConvertToVersion(XmlNode rootNode, string fileName, int toVersion)
        {
            string fileVersionString = XmlUtilities.Attribute(rootNode, "Version");
            int fileVersion = 0;
            if (fileVersionString != string.Empty)
                fileVersion = Convert.ToInt32(fileVersionString);

            // Update the xml if not at the latest version.
            bool changed = false;
            while (fileVersion < toVersion)
            {
                changed = true;

                // Find the method to call to upgrade the file by one version.
                int versionFunction = fileVersion + 1;
                MethodInfo method = typeof(Converter).GetMethod("UpgradeToVersion" + versionFunction, BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                    throw new Exception("Cannot find converter to go to version " + versionFunction);

                // Found converter method so call it.
                method.Invoke(null, new object[] { rootNode, fileName });

                fileVersion++;
            }

            if (changed)
                XmlUtilities.SetAttribute(rootNode, "Version", fileVersion.ToString());
            return changed;
        }

        /// <summary>Upgrades to version 1. Change xml structure of graph series</summary>
        private static void UpgradeToVersion1(XmlNode node, string fileName)
        {
            foreach (XmlNode seriesNode in XmlUtilities.FindAllRecursivelyByType(node, "Series"))
            {
                XmlUtilities.Rename(seriesNode, "Title", "Name");
                XmlUtilities.Move(seriesNode, "X/TableName", seriesNode, "TableName");
                XmlUtilities.Move(seriesNode, "X/FieldName", seriesNode, "XFieldName");
                XmlUtilities.Move(seriesNode, "Y/FieldName", seriesNode, "YFieldName");
                XmlUtilities.Move(seriesNode, "X2/FieldName", seriesNode, "X2FieldName");
                XmlUtilities.Move(seriesNode, "Y2/FieldName", seriesNode, "Y2FieldName");

                bool showRegression = XmlUtilities.Value(seriesNode.ParentNode, "ShowRegressionLine") == "true";
                if (showRegression)
                    seriesNode.AppendChild(seriesNode.OwnerDocument.CreateElement("Regression"));

                string seriesType = XmlUtilities.Value(seriesNode, "Type");
                if (seriesType == "Line")
                    XmlUtilities.SetValue(seriesNode, "Type", "Scatter");

                XmlUtilities.DeleteValue(seriesNode, "X");
                XmlUtilities.DeleteValue(seriesNode, "Y");

            }
        }

        /// <summary>Upgrades to version 2. Change xml structure for cultivar aliases</summary>
        private static void UpgradeToVersion2(XmlNode node, string fileName)
        {
            foreach (XmlNode cultivarNode in XmlUtilities.FindAllRecursivelyByType(node, "Cultivar"))
            {
                List<string> aliases = XmlUtilities.Values(cultivarNode, "Alias");

                // Delete all alias children.
                foreach (XmlNode alias in XmlUtilities.ChildNodes(cultivarNode, "Alias"))
                    alias.ParentNode.RemoveChild(alias);

                foreach (string alias in aliases)
                {
                    XmlNode aliasNode = cultivarNode.AppendChild(cultivarNode.OwnerDocument.CreateElement("Alias"));
                    XmlUtilities.SetValue(aliasNode, "Name", alias);
                }
            }
        }

        /// <summary>Upgrades to version 3. Make sure all area elements are greater than zero.</summary>
        private static void UpgradeToVersion3(XmlNode node, string fileName)
        {
            foreach (XmlNode zoneNode in XmlUtilities.FindAllRecursivelyByType(node, "Zone"))
            {
                string areaString = XmlUtilities.Value(zoneNode, "Area");

                try
                {
                    double area = Convert.ToDouble(areaString,
                                                   System.Globalization.CultureInfo.InvariantCulture);
                    if (area <= 0)
                        XmlUtilities.SetValue(zoneNode, "Area", "1");
                }
                catch (Exception)
                {
                    XmlUtilities.SetValue(zoneNode, "Area", "1");
                }
            }
        }

        /// <summary>Upgrades to version 4. Make sure all zones have a SoluteManager model.</summary>
        private static void UpgradeToVersion4(XmlNode node, string fileName)
        {
            foreach (XmlNode zoneNode in XmlUtilities.FindAllRecursivelyByType(node, "Zone"))
                XmlUtilities.EnsureNodeExists(zoneNode, "SoluteManager");
            foreach (XmlNode zoneNode in XmlUtilities.FindAllRecursivelyByType(node, "RectangularZone"))
                XmlUtilities.EnsureNodeExists(zoneNode, "SoluteManager");
            foreach (XmlNode zoneNode in XmlUtilities.FindAllRecursivelyByType(node, "CircularZone"))
                XmlUtilities.EnsureNodeExists(zoneNode, "SoluteManager");
            foreach (XmlNode constantNode in XmlUtilities.FindAllRecursivelyByType(node, "Constant"))
                XmlUtilities.Rename(constantNode, "Value", "FixedValue");
        }

        /// <summary>Upgrades to version 5. Make sure all zones have a CERESSoilTemperature model.</summary>
        private static void UpgradeToVersion5(XmlNode node, string fileName)
        {
            foreach (XmlNode soilNode in XmlUtilities.FindAllRecursivelyByType(node, "Soil"))
                XmlUtilities.EnsureNodeExists(soilNode, "CERESSoilTemperature");
        }

        /// <summary> Upgrades to version 6. Make sure all KLModifier, KNO3, KNH4 nodes have value XProperty values. </summary>
        private static void UpgradeToVersion6(XmlNode node, string fileName)
        {
            foreach (XmlNode n in XmlUtilities.FindAllRecursivelyByType(node, "XProperty"))
            {
                if (n.InnerText == "[Root].RootLengthDensity" ||
                    n.InnerText == "[Root].RootLengthDenisty" ||
                    n.InnerText == "[Root].LengthDenisty")
                    n.InnerText = "[Root].LengthDensity";
            }
        }

        /// <summary> Upgrades to version 7. Find all occurrences of ESW XProperty values. </summary>
        private static void UpgradeToVersion7(XmlNode node, string fileName)
        {
            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, @"([\[\]\.\w]+\.ESW)", "MathUtilities.Sum($1)", "using APSIM.Shared.Utilities;");
            foreach (XmlNode report in XmlUtilities.FindAllRecursivelyByType(node, "report"))
                ConverterUtilities.SearchReplaceReportCodeUsingRegEx(report, @"([\[\]\.\w]+\.ESW)", "sum($1)");
        }

        /// <summary>Upgrades to version 8. Create ApexStandard node. </summary>
        private static void UpgradeToVersion8(XmlNode node, string fileName)
        {
            XmlNode apex = XmlUtilities.CreateNode(node.OwnerDocument, "ApexStandard", "");
            XmlNode stemSen = XmlUtilities.CreateNode(node.OwnerDocument, "Constant", "");
            XmlElement name = node.OwnerDocument.CreateElement("Name");
            XmlElement element = node.OwnerDocument.CreateElement("FixedValue");
            name.InnerText = "StemSenescenceAge";
            element.InnerText = "0";
            stemSen.AppendChild(name);
            stemSen.AppendChild(element);

            foreach (XmlNode n in XmlUtilities.FindAllRecursivelyByType(node, "Leaf"))
            {
                n.AppendChild(apex);
            }
            foreach (XmlNode n in XmlUtilities.FindAllRecursivelyByType(node, "Structure"))
            {
                n.AppendChild(stemSen);
            }
        }

        /// <summary> Add a DMDemandFunction constant function to all Root nodes that don't have one</summary>
        private static void UpgradeToVersion9(XmlNode node, string fileName)
        {
            foreach (XmlNode root in XmlUtilities.FindAllRecursivelyByType(node, "Root"))
            {
                XmlNode partitionFraction = ConverterUtilities.FindModelNode(root, "PartitionFraction");
                if (partitionFraction != null)
                {
                    root.RemoveChild(partitionFraction);
                    XmlNode demandFunction = root.AppendChild(root.OwnerDocument.CreateElement("PartitionFractionDemandFunction"));
                    XmlUtilities.SetValue(demandFunction, "Name", "DMDemandFunction");
                    demandFunction.AppendChild(partitionFraction);
                }
            }
        }

        /// <summary>Add default values for generic organ parameters that were previously optional</summary>
        private static void UpgradeToVersion10(XmlNode node, string fileName)
        {
            List<XmlNode> organs = XmlUtilities.FindAllRecursivelyByType(node, "GenericOrgan");
            organs.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "SimpleLeaf"));
            organs.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "Nodule"));

            foreach (XmlNode organ in organs)
            {
                ConverterUtilities.AddConstantFuntionIfNotExists(organ, "NRetranslocationFactor", "0.0");
                ConverterUtilities.AddConstantFuntionIfNotExists(organ, "NitrogenDemandSwitch", "1.0");
                ConverterUtilities.AddConstantFuntionIfNotExists(organ, "DMReallocationFactor", "0.0");
                ConverterUtilities.AddConstantFuntionIfNotExists(organ, "DMRetranslocationFactor", "0.0");
                ConverterUtilities.AddVariableReferenceFuntionIfNotExists(organ, "CriticalNConc", "[" + organ.FirstChild.InnerText + "].MinimumNConc.Value()");
            }
        }

        /// <summary> Rename NonStructural to Storage in Biomass organs</summary>
        private static void UpgradeToVersion11(XmlNode node, string fileName)
        {
            ConverterUtilities.RenameVariable(node, ".NonStructural", ".Storage");
            ConverterUtilities.RenameVariable(node, ".NonStructuralDemand", ".StorageDemand");
            ConverterUtilities.RenameVariable(node, ".TotalNonStructuralDemand", ".TotalStorageDemand");
            ConverterUtilities.RenameVariable(node, ".NonStructuralAllocation", ".StorageAllocation");
            ConverterUtilities.RenameVariable(node, ".NonStructuralFraction", ".StorageFraction");
            ConverterUtilities.RenameVariable(node, ".NonStructuralWt", ".StorageWt");
            ConverterUtilities.RenameVariable(node, ".NonStructuralN", ".StorageN");
            ConverterUtilities.RenameVariable(node, ".NonStructuralNConc", ".StorageNConc");
            ConverterUtilities.RenameVariable(node, "NonStructuralFraction", "StorageFraction");
            ConverterUtilities.RenameVariable(node, "LeafStartNonStructuralNReallocationSupply", "LeafStartStorageFractionNReallocationSupply");
            ConverterUtilities.RenameVariable(node, "LeafStartNonStructuralNRetranslocationSupply", "LeafStartStorageNRetranslocationSupply");
            ConverterUtilities.RenameVariable(node, "LeafStartNonStructuralDMReallocationSupply", "LeafStartStorageDMReallocationSupply");
            ConverterUtilities.RenameVariable(node, "NonStructuralDMDemand", "StorageDMDemand");
            ConverterUtilities.RenameVariable(node, "NonStructuralNDemand", "StorageNDemand");

            // renames
            ConverterUtilities.RenamePMFFunction(node, "LeafCohortParameters", "NonStructuralFraction", "StorageFraction");
            ConverterUtilities.RenameNode(node, "NonStructuralNReallocated", "StorageNReallocated");
            ConverterUtilities.RenameNode(node, "NonStructuralWtReallocated", "StorageWtReallocated");
            ConverterUtilities.RenameNode(node, "NonStructuralNRetrasnlocated", "StorageNRetrasnlocated");
        }

        /// <summary> Rename MainStemNodeAppearanceRate to Phyllochron AND 
        ///        MainStemFinalNodeNumber to FinalLeafNumber in Structure </summary>
        private static void UpgradeToVersion12(XmlNode node, string fileName)
        {
            ConverterUtilities.RenamePMFFunction(node, "Structure", "MainStemNodeAppearanceRate", "Phyllochron");
            ConverterUtilities.RenameVariable(node, ".MainStemNodeAppearanceRate", ".Phyllochron");

            ConverterUtilities.RenamePMFFunction(node, "Structure", "MainStemFinalNodeNumber", "FinalLeafNumber");
            ConverterUtilities.RenameVariable(node, ".MainStemFinalNodeNumber", ".FinalLeafNumber");
        }

        /// <summary> Rename Plant15 to Plant.</summary>
        private static void UpgradeToVersion13(XmlNode node, string fileName)
        {
            ConverterUtilities.RenameNode(node, "Plant15", "Plant");
            ConverterUtilities.RenameVariable(node, "using Models.PMF.OldPlant;", "using Models.PMF;");
            ConverterUtilities.RenameVariable(node, "Plant15", "Plant");

            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, @"(\w+).plant.status *== *\042out\042", @"!$1.IsAlive", null);  // /042 is a "

            foreach (XmlNode simulationNode in XmlUtilities.FindAllRecursivelyByType(node, "Simulation"))
            {
                List<XmlNode> plantModels = XmlUtilities.FindAllRecursivelyByType(simulationNode, "Plant");
                plantModels.RemoveAll(p => p.Name == "plant"); // remove lowercase plant nodes - these are in sugarcane

                if (plantModels.Count > 0)
                {
                    XmlUtilities.EnsureNodeExists(simulationNode, "SoilArbitrator");
                    XmlUtilities.EnsureNodeExists(plantModels[0].ParentNode, "MicroClimate");
                }
            }
        }

        /// <summary> Rename the "Simulations", "Messages", "InitialConditions" .db tables to be prefixed with an underscore. </summary>
        private static void UpgradeToVersion14(XmlNode node, string fileName)
        {
            string dbFileName = Path.ChangeExtension(fileName, ".db");
            if (File.Exists(dbFileName))
            {
                SQLite connection = new SQLite();
                connection.OpenDatabase(dbFileName, false);
                try
                {
                    DataTable tableData = connection.ExecuteQuery("SELECT * FROM sqlite_master");
                    foreach (string tableName in DataTableUtilities.GetColumnAsStrings(tableData, "Name"))
                    {
                        if (tableName == "Simulations" || tableName == "Messages" || tableName == "InitialConditions")
                            connection.ExecuteNonQuery("ALTER TABLE " + tableName + " RENAME TO " + "_" + tableName);
                    }
                }
                finally
                {
                    connection.CloseDatabase();
                }
            }
        }

        /// <summary> Ensure report variables have a square bracket around the first word.</summary>
        private static void UpgradeToVersion15(XmlNode node, string fileName)
        {
            List<string> modelNames = ConverterUtilities.GetAllModelNames(node);
            foreach (XmlNode report in XmlUtilities.FindAllRecursivelyByType(node, "report"))
            {
                List<string> variables = XmlUtilities.Values(report, "VariableNames/string");

                for (int i = 0; i < variables.Count; i++)
                {
                    // If the first word (delimited by '.') is a model name then make sure it has
                    // square brackets around it.
                    int indexPeriod = variables[i].IndexOf('.');
                    if (indexPeriod != -1)
                    {
                        string firstWord = variables[i].Substring(0, indexPeriod);

                        if (modelNames.Contains(firstWord) && !firstWord.StartsWith("["))
                            variables[i] = "[" + firstWord + "]" + variables[i].Substring(indexPeriod);
                    }
                }
                XmlUtilities.SetValues(report, "VariableNames/string", variables);
            }
        }

        /// <summary>Add nodes for new leaf tiller model </summary>
        private static void UpgradeToVersion16(XmlNode node, string fileName)
        {
            foreach (XmlNode n in XmlUtilities.FindAllRecursivelyByType(node, "LeafCohortParameters"))
            {
                XmlNode LagDurationAgeMultiplier = XmlUtilities.CreateNode(node.OwnerDocument, "ArrayFunction", "");
                XmlNode SenescenceDurationAgeMultiplier = XmlUtilities.CreateNode(node.OwnerDocument, "ArrayFunction", "");
                XmlNode LeafSizeAgeMultiplier = XmlUtilities.CreateNode(node.OwnerDocument, "ArrayFunction", "");
                XmlElement name = node.OwnerDocument.CreateElement("Name");
                XmlElement element = node.OwnerDocument.CreateElement("Values");

                name.InnerText = "LagDurationAgeMultiplier";
                element.InnerText = "1 1 1";
                LagDurationAgeMultiplier.AppendChild(name);
                LagDurationAgeMultiplier.AppendChild(element);

                name = node.OwnerDocument.CreateElement("Name");
                name.InnerText = "SenescenceDurationAgeMultiplier";
                element = node.OwnerDocument.CreateElement("Values");
                element.InnerText = "1 1 1";
                SenescenceDurationAgeMultiplier.AppendChild(name);
                SenescenceDurationAgeMultiplier.AppendChild(element);

                name = node.OwnerDocument.CreateElement("Name");
                element = node.OwnerDocument.CreateElement("Values");
                name.InnerText = "LeafSizeAgeMultiplier";
                element.InnerText = "1 1 1 1 1 1 1 1 1 1 1 1";
                LeafSizeAgeMultiplier.AppendChild(name);
                LeafSizeAgeMultiplier.AppendChild(element);

                if (ConverterUtilities.FindModelNode(n, "LagDurationAgeMultiplier") == null)
                    n.AppendChild(LagDurationAgeMultiplier);
                if (ConverterUtilities.FindModelNode(n, "SenescenceDurationAgeMultiplier") == null)
                    n.AppendChild(SenescenceDurationAgeMultiplier);
                if (ConverterUtilities.FindModelNode(n, "LeafSizeAgeMultiplier") == null)
                    n.AppendChild(LeafSizeAgeMultiplier);
            }
        }

        /// <summary>Rename CohortLive. to Live.</summary>
        private static void UpgradeToVersion17(XmlNode node, string fileName)
        {
            // Rename .CohortLive to .Live in all compositebiomass nodes and report variables.
            foreach (XmlNode biomass in XmlUtilities.FindAllRecursivelyByType(node, "CompositeBiomass"))
            {
                List<string> variables = XmlUtilities.Values(biomass, "Propertys/string");
                for (int i = 0; i < variables.Count; i++)
                {
                    variables[i] = variables[i].Replace(".CohortLive", ".Live");
                    variables[i] = variables[i].Replace(".CohortDead", ".Dead");
                }
                XmlUtilities.SetValues(biomass, "Propertys/string", variables);
            }
            foreach (XmlNode report in XmlUtilities.FindAllRecursivelyByType(node, "report"))
            {
                List<string> variables = XmlUtilities.Values(report, "VariableNames/string");
                for (int i = 0; i < variables.Count; i++)
                {
                    variables[i] = variables[i].Replace(".CohortLive", ".Live");
                    variables[i] = variables[i].Replace(".CohortDead", ".Dead");
                }
                XmlUtilities.SetValues(report, "VariableNames/string", variables);
            }

            // remove all live and dead nodes.
            foreach (XmlNode childToDelete in ConverterUtilities.FindModelNodes(node, "CompositeBiomass", "Live"))
                childToDelete.ParentNode.RemoveChild(childToDelete);
            foreach (XmlNode childToDelete in ConverterUtilities.FindModelNodes(node, "CompositeBiomass", "Dead"))
                childToDelete.ParentNode.RemoveChild(childToDelete);

            foreach (XmlNode childToDelete in ConverterUtilities.FindModelNodes(node, "Biomass", "Live"))
                childToDelete.ParentNode.RemoveChild(childToDelete);
            foreach (XmlNode childToDelete in ConverterUtilities.FindModelNodes(node, "Biomass", "Dead"))
                childToDelete.ParentNode.RemoveChild(childToDelete);

        }

        /// <summary> Rename CohortLive. to Live.</summary>
        private static void UpgradeToVersion18(XmlNode node, string fileName)
        {
            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
            {
                ConverterUtilities.SearchReplaceManagerCode(manager, ".SoilWater.dlayer", ".Thickness");
                ConverterUtilities.SearchReplaceManagerCode(manager, ".SoilWater.Thickness", ".Thickness");
                ConverterUtilities.SearchReplaceManagerCode(manager, ".SoilWater.LL15", ".LL15");
                ConverterUtilities.SearchReplaceManagerCode(manager, ".SoilWater.LL15mm", ".LL15mm");
                ConverterUtilities.SearchReplaceManagerCode(manager, ".SoilWater.DUL", ".DUL");
                ConverterUtilities.SearchReplaceManagerCode(manager, ".SoilWater.DULmm", ".DULmm");
                ConverterUtilities.SearchReplaceManagerCode(manager, ".SoilWater.SAT", ".SAT");
                ConverterUtilities.SearchReplaceManagerCode(manager, ".SoilWater.SATmm", ".SATmm");
            }

            foreach (XmlNode report in XmlUtilities.FindAllRecursivelyByType(node, "report"))
            {
                ConverterUtilities.SearchReplaceReportCode(report, ".SoilWater.dlayer", ".Thickness");
                ConverterUtilities.SearchReplaceReportCode(report, ".SoilWater.Thickness", ".Thickness");
                ConverterUtilities.SearchReplaceReportCode(report, ".SoilWater.LL15", ".LL15");
                ConverterUtilities.SearchReplaceReportCode(report, ".SoilWater.LL15mm", ".LL15mm");
                ConverterUtilities.SearchReplaceReportCode(report, ".SoilWater.DUL", ".DUL");
                ConverterUtilities.SearchReplaceReportCode(report, ".SoilWater.DULmm", ".DULmm");
                ConverterUtilities.SearchReplaceReportCode(report, ".SoilWater.SAT", ".SAT");
                ConverterUtilities.SearchReplaceReportCode(report, ".SoilWater.SATmm", ".SATmm");
            }
        }

        /// <summary> Add DMConversionEfficiency node</summary>
        private static void UpgradeToVersion19(XmlNode node, string fileName)
        {
            //Rename existing DMConversionEfficiencyFunction nodes
            foreach (XmlNode n in XmlUtilities.FindAllRecursivelyByType(node, "Leaf"))
            {
                XmlNode dmFunction = ConverterUtilities.FindModelNode(n, "DMConversionEfficiencyFunction");
                if (dmFunction != null)
                {
                    XmlUtilities.SetValue(dmFunction, "Name", "DMConversionEfficiency");
                }
            }

            List<XmlNode> nodeList = new List<XmlNode>();

            XmlUtilities.FindAllRecursively(node, "DMConversionEfficiencyFunction", ref nodeList);
            foreach (XmlNode n in nodeList)
                ConverterUtilities.RenameNode(n, "DMConversionEfficiencyFunction", "DMConversionEfficiency");

            nodeList.Clear();
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "Root"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "Leaf"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "GenericOrgan"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "ReproductiveOrgan"));

            foreach (XmlNode n in nodeList)
            {
                XmlNode DMnode;
                DMnode = XmlUtilities.CreateNode(node.OwnerDocument, "Constant", "");
                XmlElement name = node.OwnerDocument.CreateElement("Name");
                XmlElement element = node.OwnerDocument.CreateElement("FixedValue");
                name.InnerText = "DMConversionEfficiency";
                element.InnerText = "1.0";
                DMnode.AppendChild(name);
                DMnode.AppendChild(element);

                if (ConverterUtilities.FindModelNode(n, "DMConversionEfficiency") == null)
                    n.AppendChild(DMnode);
            }
        }

        private static void UpgradeToVersion20(XmlNode node, string filename)
        {
            List<XmlNode> nodeList = new List<XmlNode>(XmlUtilities.FindAllRecursivelyByType(node, "Root"));

            foreach (XmlNode n in nodeList)
            {
                XmlNode MRFnode;
                MRFnode = XmlUtilities.CreateNode(node.OwnerDocument, "Constant", "");
                XmlElement name = node.OwnerDocument.CreateElement("Name");
                XmlElement element = node.OwnerDocument.CreateElement("FixedValue");
                name.InnerText = "MaintenanceRespirationFunction";
                element.InnerText = "1.0";
                MRFnode.AppendChild(name);
                MRFnode.AppendChild(element);

                if (ConverterUtilities.FindModelNode(n, "MaintenanceRespirationFunction") == null)
                    n.AppendChild(MRFnode);
            }
        }

        /// <summary>Add RemobilisationCost to all organs </summary>
        private static void UpgradeToVersion21(XmlNode node, string fileName)
        {
            List<XmlNode> nodeList = new List<XmlNode>();

            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "Root"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "Leaf"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "GenericOrgan"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "ReproductiveOrgan"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "LeafCohortParameters"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "SimpleLeaf"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "Nodule"));

            foreach (XmlNode n in nodeList)
            {
                XmlNode DMnode;
                DMnode = XmlUtilities.CreateNode(node.OwnerDocument, "Constant", "");
                XmlElement name = node.OwnerDocument.CreateElement("Name");
                XmlElement element = node.OwnerDocument.CreateElement("FixedValue");
                name.InnerText = "RemobilisationCost";
                element.InnerText = "0";
                DMnode.AppendChild(name);
                DMnode.AppendChild(element);

                if (ConverterUtilities.FindModelNode(n, "RemobilisationCost") == null)
                    n.AppendChild(DMnode);
            }

        }
        /// <summary> Upgrades to version 22. Alter MovingAverage Function XProperty values.</summary>
        private static void UpgradeToVersion22(XmlNode node, string fileName)
        {
            string StartStage = "";
            foreach (XmlNode EmergePhase in XmlUtilities.FindAllRecursivelyByType(node, "EmergingPhase"))
            {
                StartStage = XmlUtilities.Value(EmergePhase, "End");
            }
            ConverterUtilities.RenameVariable(node, "InitialValue", "StageToStartMovingAverage");
            foreach (XmlNode MovingAverageFunction in XmlUtilities.FindAllRecursivelyByType(node, "MovingAverageFunction"))
            {
                XmlUtilities.SetValue(MovingAverageFunction, "StageToStartMovingAverage", StartStage);
            }

        }

        /// <summary> Upgrades to version 23. Add CarbonConcentration property to all organs. </summary>
        private static void UpgradeToVersion23(XmlNode node, string fileName)
        {
            List<XmlNode> nodeList = new List<XmlNode>();

            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "Root"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "Leaf"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "GenericOrgan"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "ReproductiveOrgan"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "LeafCohortParameters"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "SimpleLeaf"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "Nodule"));
            nodeList.AddRange(XmlUtilities.FindAllRecursivelyByType(node, "PerennialLeaf"));

            foreach (XmlNode n in nodeList)
            {
                XmlNode DMnode;
                DMnode = XmlUtilities.CreateNode(node.OwnerDocument, "Constant", "");
                XmlElement name = node.OwnerDocument.CreateElement("Name");
                XmlElement element = node.OwnerDocument.CreateElement("FixedValue");
                name.InnerText = "CarbonConcentration";
                element.InnerText = "0.4";
                DMnode.AppendChild(name);
                DMnode.AppendChild(element);

                if (ConverterUtilities.FindModelNode(n, "CarbonConcentration") == null)
                    n.AppendChild(DMnode);
            }

        }

        /// <summary> Upgrades to version 24. Add second argument to SoluteManager.Add method</summary>
        private static void UpgradeToVersion24(XmlNode node, string fileName)
        {
            foreach (XmlNode managerNode in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
            {
                ManagerConverter manager = new ManagerConverter();
                manager.Read(managerNode);
                List<MethodCall> methods = manager.FindMethodCalls("SoluteManager", "Add");
                foreach (MethodCall method in methods)
                {
                    if (method.Arguments.Count == 2)
                    {
                        method.Arguments.Insert(1, "SoluteManager.SoluteSetterType.Fertiliser");
                        manager.SetMethodCall(method);
                    }
                }
                manager.Write(managerNode);
            }

        }

        /// <summary>Upgrades to version 25. Add checkpoint fields and table to .db</summary>
        private static void UpgradeToVersion25(XmlNode node, string fileName)
        {
            string dbFileName = Path.ChangeExtension(fileName, ".db");
            if (File.Exists(dbFileName))
            {
                SQLite connection = new SQLite();
                connection.OpenDatabase(dbFileName, false);
                try
                {
                    DataTable tableData = connection.ExecuteQuery("SELECT * FROM sqlite_master");
                    List<string> tableNames = DataTableUtilities.GetColumnAsStrings(tableData, "Name").ToList();
                    if (!tableNames.Contains("_Checkpoints"))
                    {
                        connection.ExecuteNonQuery("BEGIN");

                        foreach (string tableName in tableNames)
                        {
                            List<string> columnNames = connection.GetColumnNames(tableName);
                            if (columnNames.Contains("SimulationID"))
                            {
                                connection.ExecuteNonQuery("ALTER TABLE " + tableName + " ADD COLUMN CheckpointID INTEGER DEFAULT 1");
                            }
                        }

                        // Now add a _checkpointfiles table.
                        connection.ExecuteNonQuery("CREATE TABLE _Checkpoints (ID INTEGER PRIMARY KEY ASC, Name TEXT, Version TEXT, Date TEXT)");
                        connection.ExecuteNonQuery("CREATE TABLE _CheckpointFiles (CheckpointID INTEGER, FileName TEXT, Contents BLOB)");
                        connection.ExecuteNonQuery("INSERT INTO [_Checkpoints] (Name) VALUES (\"Current\")");

                        connection.ExecuteNonQuery("END");
                    }
                }
                finally
                {
                    connection.CloseDatabase();
                }
            }
        }

        /// <summary> Upgrades to version 26. Add leaf development rate constant to perrenial leaf </summary>
        private static void UpgradeToVersion26(XmlNode node, string fileName)
        {
            foreach (XmlNode perennialLeaf in XmlUtilities.FindAllRecursivelyByType(node, "PerennialLeaf"))
                ConverterUtilities.AddConstantFuntionIfNotExists(perennialLeaf, "LeafDevelopmentRate", "1.0");
        }


        /// <summary> Upgrades to version 27. Some variables in Leaf became ints rather than doubles. Need to add convert.ToDouble(); </summary>
        private static void UpgradeToVersion27(XmlNode node, string fileName)
        {
            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
            {
                string replacePattern = @"Convert.ToDouble(zone.Get(${variable}))";
                string[] variableNames = new string[] { "ExpandedCohortNo", "AppearedCohortNo", "GreenCohortNo", "SenescingCohortNo" };
                foreach (string variableName in variableNames)
                {
                    string pattern = @"\(double\)zone.Get\((?<variable>\"".+\.Leaf\." + variableName + @"\"")\)";
                    ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, pattern, replacePattern, null);
                }
            }
        }

        /// <summary> Upgrades to version 28. Change ICrop to IPlant</summary>
        private static void UpgradeToVersion28(XmlNode node, string fileName)
        {
            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
            {
                ConverterUtilities.SearchReplaceManagerCode(manager, " ICrop", " IPlant");
                ConverterUtilities.SearchReplaceManagerCode(manager, "(ICrop)", "(IPlant)");
            }
        }


        /// <summary>Upgrades to version 29. Change AgPasture to have leaves, stems, stolons included as child model nodes </summary>
        private static void UpgradeToVersion29(XmlNode node, string fileName)
        {
            foreach (XmlNode pasture in XmlUtilities.FindAllRecursivelyByType(node, "PastureSpecies"))
            {
                XmlNode leaves = pasture.AppendChild(node.OwnerDocument.CreateElement("PastureAboveGroundOrgan"));
                XmlUtilities.SetValue(leaves, "Name", "Leaves");
                XmlNode genericTissue1 = leaves.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue1, "Name", "LeafCohort1");
                XmlNode genericTissue2 = leaves.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue2, "Name", "LeafCohort2");
                XmlNode genericTissue3 = leaves.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue3, "Name", "LeafCohort3");
                XmlNode genericTissue4 = leaves.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue4, "Name", "Dead");

                XmlNode stems = pasture.AppendChild(node.OwnerDocument.CreateElement("PastureAboveGroundOrgan"));
                XmlUtilities.SetValue(stems, "Name", "Stems");
                genericTissue1 = stems.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue1, "Name", "LeafCohort1");
                genericTissue2 = stems.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue2, "Name", "LeafCohort2");
                genericTissue3 = stems.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue3, "Name", "LeafCohort3");
                genericTissue4 = stems.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue4, "Name", "Dead");

                XmlNode stolons = pasture.AppendChild(node.OwnerDocument.CreateElement("PastureAboveGroundOrgan"));
                XmlUtilities.SetValue(stolons, "Name", "Stolons");
                genericTissue1 = stolons.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue1, "Name", "LeafCohort1");
                genericTissue2 = stolons.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue2, "Name", "LeafCohort2");
                genericTissue3 = stolons.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue3, "Name", "LeafCohort3");
                genericTissue4 = stolons.AppendChild(node.OwnerDocument.CreateElement("GenericTissue"));
                XmlUtilities.SetValue(genericTissue4, "Name", "Dead");
            }
        }

        /// <summary> Upgrades to version 30. Change DisplayAttribute </summary>
        private static void UpgradeToVersion30(XmlNode node, string fileName)
        {
            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
            {
                string pattern = @"DisplayType *= *DisplayAttribute\.DisplayTypeEnum\.";
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, pattern, "Type=DisplayType.", null);
            }
        }


        /// <summary> Upgrades to version 31. Change DisplayAttribute </summary>
        private static void UpgradeToVersion31(XmlNode node, string fileName)
        {
            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
            {
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, @"\.SoilWater\.SetWater_frac\((?<variable>.+)\)", ".SoilWater.SW = ${variable}", null);
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, @"\.SoilWater\.outflow_lat", ".SoilWater.LateralOutflow", null);
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, @"\.SoilWater\.flow_no3", ".SoilWater.FlowNO3", null);
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, @"\.SoilWater\.flow_nh4", ".SoilWater.FlowNH4", null);
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, @"\.SoilWater\.flow", ".SoilWater.Flow", null);
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, @"\.SoilWater\.flux", ".SoilWater.Flux", null);
                ConverterUtilities.SearchReplaceManagerCodeUsingRegEx(manager, @"\.SoilWater\.residueinterception", ".SoilWater.ResidueInterception", null);
            }
            foreach (XmlNode report in XmlUtilities.FindAllRecursivelyByType(node, "report"))
            {
                ConverterUtilities.SearchReplaceReportCodeUsingRegEx(report, @"\.SoilWater\.SetWater_frac\((?<variable>.+)\)", ".SoilWater.SW = ${variable}");
                ConverterUtilities.SearchReplaceReportCodeUsingRegEx(report, @"\.SoilWater\.outflow_lat", ".SoilWater.LateralOutflow");
                ConverterUtilities.SearchReplaceReportCodeUsingRegEx(report, @"\.SoilWater\.flow_no3", ".SoilWater.FlowNO3");
                ConverterUtilities.SearchReplaceReportCodeUsingRegEx(report, @"\.SoilWater\.flow_nh4", ".SoilWater.FlowNH4");
                ConverterUtilities.SearchReplaceReportCodeUsingRegEx(report, @"\.SoilWater\.flow", ".SoilWater.Flow");
                ConverterUtilities.SearchReplaceReportCodeUsingRegEx(report, @"\.SoilWater\.flux", ".SoilWater.Flux");
                ConverterUtilities.SearchReplaceReportCodeUsingRegEx(report, @"\.SoilWater\.residueinterception", ".SoilWater.ResidueInterception");
            }
        }

        /// <summary> Change the VaryByIndex in series from an integer index to a name of a factor.</summary>
        private static void UpgradeToVersion32(XmlNode node, string fileName)
        {
            foreach (XmlNode series in XmlUtilities.FindAllRecursivelyByType(node, "series"))
            {
                string[] parentTypesToMatch = new string[] { "Simulation", "Zone", "Experiment", "Folder", "Simulations" };
                XmlNode parent = XmlUtilities.ParentOfType(series, parentTypesToMatch);
                List<KeyValuePair<string, string>> factorNames;
                do
                {
                    factorNames = GetFactorNames(parent);
                    parent = parent.ParentNode;
                }
                while (factorNames.Count == 0 && parent != null);

                var uniqueFactorNames = CalculateDistinctFactorNames(factorNames);
                string value = XmlUtilities.Value(series, "FactorIndexToVaryColours");
                if (value != string.Empty)
                {
                    int index = Convert.ToInt32(value);
                    if (index > -1 && index < uniqueFactorNames.Count())
                        XmlUtilities.SetValue(series, "FactorToVaryColours", uniqueFactorNames[index]);
                    XmlUtilities.DeleteValue(series, "FactorIndexToVaryColours");
                }

                value = XmlUtilities.Value(series, "FactorIndexToVaryMarkers");
                if (value != string.Empty)
                {
                    int index = Convert.ToInt32(value);
                    if (index > -1 && index < uniqueFactorNames.Count())
                        XmlUtilities.SetValue(series, "FactorToVaryMarkers", uniqueFactorNames[index]);
                    XmlUtilities.DeleteValue(series, "FactorIndexToVaryMarkers");
                }

                value = XmlUtilities.Value(series, "FactorIndexToVaryLines");
                if (value != string.Empty)
                {
                    int index = Convert.ToInt32(value);
                    if (index > -1 && index < uniqueFactorNames.Count())
                        XmlUtilities.SetValue(series, "FactorToVaryLines", uniqueFactorNames[index]);
                    XmlUtilities.DeleteValue(series, "FactorIndexToVaryLines");
                }
            }
        }

        /// <summary> Create graph definitions for the specified model</summary>
        private static List<KeyValuePair<string, string>> GetFactorNames(XmlNode node)
        {
            string[] zoneTypes = new string[] { "Zone", "AgroforestrySystem", "CircularZone", "ZoneCLEM", "RectangularZone", "StripCropZone" };
            var factors = new List<KeyValuePair<string, string>>();
            if (node.Name == "Simulation" || Array.IndexOf(zoneTypes, node.Name) != -1)
                factors.AddRange(BuildListFromSimulation(node));
            else if (node.Name == "Experiment")
                factors.AddRange(BuildListFromExperiment(node));
            else
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name == "Simulation" || child.Name == "Experiment" || child.Name == "Folder")
                        factors.AddRange(GetFactorNames(child));
                }
            }
            return factors;
        }


        /// <summary>Build a list of simulation / zone pairs from the specified experiment</summary>
        private static List<KeyValuePair<string, string>> BuildListFromExperiment(XmlNode node)
        {
            string[] zoneTypes = new string[] { "Zone", "AgroforestrySystem", "CircularZone", "ZoneCLEM", "RectangularZone", "StripCropZone" };

            var factors = new List<KeyValuePair<string, string>>();

            Experiment exp = XmlUtilities.Deserialise(node, typeof(Experiment)) as Experiment;
            Apsim.ParentAllChildren(exp);
            if (exp != null)
            {
                XmlNode baseSimulation = XmlUtilities.FindByType(node, "Simulation");
                foreach (XmlNode zone in XmlUtilities.FindAllRecursivelyByTypes(baseSimulation, zoneTypes))
                {
                    foreach (List<FactorValue> combination in (exp).AllCombinations())
                    {
                        string zoneName = XmlUtilities.Value(zone, "Name");
                        string simulationName = exp.Name;
                        factors.Add(new KeyValuePair<string, string>("Simulation", null));
                        factors.Add(new KeyValuePair<string, string>("Zone", zoneName));
                        foreach (FactorValue value in combination)
                        {
                            simulationName += value.Name;
                            string factorName = value.Factor.Name;
                            if (value.Factor.Parent is Factor)
                            {
                                factorName = value.Factor.Parent.Name;
                            }
                            string factorValue = value.Name.Replace(factorName, "");
                            factors.Add(new KeyValuePair<string, string>(factorName, factorValue));
                        }
                        factors.Add(new KeyValuePair<string, string>("Experiment", exp.Name));
                    }
                }
            }
            return factors;
        }

        /// <summary>Build a list of simulation / zone pairs from the specified simulation</summary>
        private static List<KeyValuePair<string, string>> BuildListFromSimulation(XmlNode node)
        {
            var simulationZonePairs = new List<KeyValuePair<string, string>>();
            simulationZonePairs.Add(new KeyValuePair<string, string>("Simulation", XmlUtilities.Value(node, "Name")));
            foreach (XmlNode zone in XmlUtilities.ChildNodes(node, "Zone"))
                simulationZonePairs.Add(new KeyValuePair<string, string>("Zone", XmlUtilities.Value(zone, "Name")));
            return simulationZonePairs;
        }


        /// <summary> Go through all factors and determine which are distict.</summary>
        private static List<string> CalculateDistinctFactorNames(List<KeyValuePair<string, string>> factors)
        {
            var factorNamesToReturn = new List<string>();
            var factorNames = factors.Select(f => f.Key).Distinct();
            foreach (var factorName in factorNames)
            {
                List<string> factorValues = new List<string>();
                var matchingFactors = factors.FindAll(f => f.Key == factorName);
                var matchingFactorValues = matchingFactors.Select(f => f.Value);

                if (matchingFactorValues.Distinct().Count() > 1)
                {
                    // All factor values are the same so remove the factor.
                    factorNamesToReturn.Add(factorName);
                }
            }
            return factorNamesToReturn;
        }

        /// <summary>Change the stores object array in Supplement components to Stores</summary>
        private static void UpgradeToVersion33(XmlNode node, string fileName)
        {
            // Find all the Supplement components
            List<XmlNode> nodeList = new List<XmlNode>(XmlUtilities.FindAllRecursivelyByType(node, "Supplement"));

            foreach (XmlNode supplementNode in nodeList)
            {
                ConverterUtilities.RenameNode(supplementNode, "stores", "Stores");
            }
        }

        /// <summary> Upgrades to version 34. Change DisplayAttribute</summary>
        private static void UpgradeToVersion34(XmlNode node, string fileName)
        {
            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
            {
                ConverterUtilities.SearchReplaceManagerCode(manager, @"Models.SurfaceOM", "Models.Surface");
            }
            foreach (XmlNode surfaceOrganicMatter in XmlUtilities.FindAllRecursivelyByType(node, "SurfaceOrganicMatter"))
            {
                XmlUtilities.DeleteValue(surfaceOrganicMatter, "ResidueTypes");
                XmlUtilities.SetValue(surfaceOrganicMatter, "ResourceName", "SurfaceOrganicMatter");
                XmlUtilities.Rename(surfaceOrganicMatter, "PoolName", "InitialResidueName");
                XmlUtilities.Rename(surfaceOrganicMatter, "type", "InitialResidueType");
                XmlUtilities.Rename(surfaceOrganicMatter, "mass", "InitialResidueMass");
                XmlUtilities.Rename(surfaceOrganicMatter, "standing_fraction", "InitialStandingFraction");
                XmlUtilities.Rename(surfaceOrganicMatter, "cnr", "InitialCNR");
                XmlUtilities.Rename(surfaceOrganicMatter, "cpr", "InitialCPR");
                if (XmlUtilities.Value(surfaceOrganicMatter, "InitialCPR") == string.Empty)
                    XmlUtilities.DeleteValue(surfaceOrganicMatter, "InitialCPR");
            }
        }
        /// <summary> Change the stores object array in Supplement components to Stores</summary>
        private static void UpgradeToVersion35(XmlNode node, string fileName)
        {
            ConverterUtilities.RenameNode(node, "soil_heat_flux_fraction", "SoilHeatFluxFraction");
            ConverterUtilities.RenameNode(node, "night_interception_fraction", "NightInterceptionFraction");
            ConverterUtilities.RenameNode(node, "refheight", "ReferenceHeight");
        }

        /// <summary> Change the stores object array in Supplement components to Stores</summary>
        private static void UpgradeToVersion36(XmlNode node, string fileName)
        {
            foreach (XmlNode report in XmlUtilities.FindAllRecursivelyByType(node, "report"))
                ConverterUtilities.SearchReplaceReportCode(report, ".WaterSupplyDemandRatio", ".Leaf.Fw");
            foreach (XmlNode n in XmlUtilities.FindAllRecursivelyByType(node, "XProperty"))
                if (n.InnerText.Contains(".WaterSupplyDemandRatio"))
                    n.InnerText = n.InnerText.Replace(".WaterSupplyDemandRatio", ".Leaf.Fw");
        }

        /// <summary> Remove apex nodes from leaf objects </summary>
        private static void UpgradeToVersion37(XmlNode node, string fileName)
        {
            // Find all the Supplement components
            List<XmlNode> nodeList = XmlUtilities.FindAllRecursivelyByType(node, "ApexStandard");

            foreach (XmlNode apexNode in nodeList)
                apexNode.ParentNode.RemoveChild(apexNode);
        }


        /// <summary>
        /// Upgrades to version 38. Change SurfaceOrganicMatter.AddFaecesType to AddFaecesType.
        /// </summary>
        /// <param name="node">The node to upgrade.</param>
        /// <param name="fileName">The name of the .apsimx file</param>
        private static void UpgradeToVersion38(XmlNode node, string fileName)
        {
            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "manager"))
            {
                ConverterUtilities.SearchReplaceManagerCode(manager, @"SurfaceOrganicMatter.AddFaecesType", "AddFaecesType");
            }
        }

        /// <summary>
        /// Upgrades to version 39. Replaces TreeProxy.dates and TreeProxy.heights
        /// with TreeProxy.Dates and TreeProxy.Heights.
        /// </summary>
        /// <param name="node">The node to upgrade.</param>
        /// <param name="fileName">The name of the .apsimx file</param>
        private static void UpgradeToVersion39(XmlNode node, string fileName)
        {
            foreach (XmlNode tree in XmlUtilities.FindAllRecursivelyByType(node, "TreeProxy"))
            {
                tree.ChildNodes.Cast<XmlNode>().Where(n => n.Name == "dates").ToList().ForEach(n => ConverterUtilities.RenameNode(n, "dates", "Dates"));
                tree.ChildNodes.Cast<XmlNode>().Where(n => n.Name == "heights").ToList().ForEach(n => ConverterUtilities.RenameNode(n, "heights", "Heights"));
            }
        }

        /// <summary> Rename ThermalTime functions on phases to Progression </summary>
        private static void UpgradeToVersion40(XmlNode node, string fileName)
        {
            ConverterUtilities.RenamePMFFunction(node, "GenericPhase", "ThermalTime", "Progression");
            ConverterUtilities.RenamePMFFunction(node, "BuddingPhase", "ThermalTime", "Progression");

            List<XmlNode> CultivarList = new List<XmlNode>(XmlUtilities.FindAllRecursivelyByType(node, "Cultivar"));
            foreach (XmlNode cult in CultivarList)
                ConverterUtilities.SearchReplaceCultivarOverrides(cult, ".Vegetative.ThermalTime", ".Vegetative.Progression");

        }

        private static void MakeDMDemandsNode(XmlNode node, XmlNode organNode)
        {   
            //Make DMDemand node
            XmlNode DMDemands = XmlUtilities.CreateNode(node.OwnerDocument, "BiomassDemand", "DMDemands");
            organNode.AppendChild(DMDemands);

            //Add Structural demand function
            XmlNode structuralFraction = ConverterUtilities.FindModelNode(organNode, "StructuralFraction");
            if (structuralFraction == null)
            {
                structuralFraction = XmlUtilities.CreateNode(node.OwnerDocument, "Constant", "StructuralFraction");
                XmlUtilities.SetValue(structuralFraction, "FixedValue", "1.0");
            }
            XmlNode structural = XmlUtilities.CreateNode(node.OwnerDocument, "MultiplyFunction", "Structural");
            structural.AppendChild(ConverterUtilities.FindModelNode(organNode, "DMDemandFunction"));
            structural.AppendChild(structuralFraction);
            DMDemands.AppendChild(structural);
            //Add Metabolic Demand function
            ConverterUtilities.AddConstantFuntionIfNotExists(DMDemands, "Metabolic", "0.0");
            //Add Storage Demand function
            XmlNode Storage = XmlUtilities.CreateNode(node.OwnerDocument, "StorageDemandFunction", "Storage");
            XmlNode storageFraction = XmlUtilities.CreateNode(node.OwnerDocument, "SubtractFunction", "StorageFraction");
            ConverterUtilities.AddConstantFuntionIfNotExists(storageFraction, "One", "1.0");
            ConverterUtilities.AddVariableReferenceFuntionIfNotExists(storageFraction, "StructuralFraction", "[" + organNode.FirstChild.InnerText + "].DMDemands.Structural.StructuralFraction.Value()");
            Storage.AppendChild(storageFraction);
            DMDemands.AppendChild(Storage);
        }

        /// <summary>Rename CohortArrayLive functions which dont do anything and cause problems for checkpointing</summary>
        private static void UpgradeToVersion41(XmlNode node, string fileName)
        {
            // remove all live and dead cohortArrayLive nodes.
            foreach (XmlNode childToDelete in ConverterUtilities.FindModelNodes(node, "ArrayBiomass", "CohortArrayLive"))
                childToDelete.ParentNode.RemoveChild(childToDelete);
            foreach (XmlNode childToDelete in ConverterUtilities.FindModelNodes(node, "ArrayBiomass", "CohortArrayDead"))
                childToDelete.ParentNode.RemoveChild(childToDelete);
        }

        /// <summary>
        /// Upgrades to version 41. Upgrades parameterisation of DM demands.
        /// </summary>
        private static void UpgradeToVersion42(XmlNode node, string fileName)
        {
            List<string> organList = new List<string>(new string[] { "GenericOrgan", "SimpleLeaf", "Nodule", "PerennialLeaf", "Root" });
            foreach (string org in organList)
                foreach (XmlNode organNode in XmlUtilities.FindAllRecursivelyByType(node, org))
                {
                    MakeDMDemandsNode(node, organNode);
                }
            ConverterUtilities.RenameVariable(node, "DMDemandFunction", "DMDemands.Structural.DMDemandFunction");
        }
    }
}

