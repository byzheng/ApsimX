﻿//--------------------------------------------------------------------------------------------------------------------------
// <copyright file="AgPasture.PastureSpecies.Organs.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
//     Copyright (c) APSIM initiative. All rights reserved.
// </copyright>
//--------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Models.Soils;
using Models.PMF;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using Models.Soils.Arbitrator;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.AgPasture
{
    /// <summary>Describes a generic above ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureAboveGroundOrgan : Model, IRemovableBiomass
    {
        /// <summary>The collection of tissues for this organ.</summary>
        [ChildLink]
        public GenericTissue[] Tissue;

        /// <summary>Return live biomass. Used by STOCK. g/m^2</summary>
        public Biomass Live
        {
            get
            {
                double availPropn = 1.0;
                if (this.Name == "Stolons")
                    availPropn = ((PastureSpecies)(this.Parent)).FractionStolonStanding;
                Biomass live = new Biomass();
                live.StructuralWt = DMLive * 0.10 * availPropn;
                live.StructuralN = NLive * 0.10 * availPropn;
                live.DMDOfStructural = DigestibilityLive;
                return live;
            }
        }

        /// <summary>Return dead biomass. Used by STOCK. g/m^2</summary>
        public Biomass Dead
        {
            get
            {
                double availPropn = 1.0;
                if (this.Name == "Stolons")
                    availPropn = ((PastureSpecies)(this.Parent)).FractionStolonStanding;
                Biomass dead = new Biomass();
                dead.StructuralWt = DMDead * 0.10 * availPropn;
                dead.StructuralN = NDead * 0.10 * availPropn;
                dead.DMDOfStructural = DigestibilityDead;
                return dead;
            }
        }

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>
        /// Biomass removal logic for this organ.
        /// </summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">Biomass to remove</param>
        public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType biomassToRemove)
        {
            // TODO: Work out what to do with biomassToRemove.FractionLiveToResidue
            // Live removal
            for (int t = 0; t < Tissue.Length - 1; t++)
            {
                Tissue[t].DM *= (1.0 - biomassToRemove.FractionLiveToRemove);
                Tissue[t].Namount *= (1.0 - biomassToRemove.FractionLiveToRemove);
                Tissue[t].NRemobilisable *= (1.0 - biomassToRemove.FractionLiveToRemove);
            }

            // Dead removal
            Tissue[Tissue.Length - 1].DM *= (1.0 - biomassToRemove.FractionDeadToRemove);
            Tissue[Tissue.Length - 1].Namount *= (1.0 - biomassToRemove.FractionDeadToRemove);
            Tissue[Tissue.Length - 1].NRemobilisable *= (1.0 - biomassToRemove.FractionDeadToRemove);
        }

        #region Organ specific characteristics  ----------------------------------------------------------------------------

        /// <summary>Gets or sets the N concentration for optimum growth (kg/kg).</summary>
        internal double NConcOptimum = 0.04;

        /// <summary>Gets or sets the maximum N concentration, for luxury uptake (kg/kg).</summary>
        internal double NConcMaximum = 0.05;

        /// <summary>Gets or sets the minimum N concentration, structural N (kg/kg).</summary>
        internal double NConcMinimum = 0.012;

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        internal double MinimumLiveDM = 0.0;

        /// <summary>Proportion of organ DM that is standing, available to harvest (0-1).</summary>
        internal double FractionStanding = 1.0;

        /// <summary>List of BiomassRemovalTypes with default biomass removal fractions for given removal types.</summary>
        private Dictionary<string, OrganBiomassRemovalType> defaultRemovalFractions = new Dictionary<string, OrganBiomassRemovalType>();

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Organ properties (summary of tissues)  ---------------------------------------------------------------------

        /// <summary>Gets the total dry matter in this organ (kg/ha).</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the live (green) tissues (kg/ha).</summary>
        internal double DMLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double DMDead
        {
            get { return Tissue[Tissue.Length - 1].DM; }
        }

        /// <summary>The dry matter in the live (green) tissues available to harvest (kg/ha).</summary>
        internal double DMLiveHarvestable
        {
            get { return Math.Max(0.0, Math.Min(DMLive - MinimumLiveDM, DMLive * FractionStanding)); }
        }

        /// <summary>The dry matter in the dead tissues available to harvest (kg/ha).</summary>
        internal virtual double DMDeadHarvestable
        {
            get { return DMDead * FractionStanding; }
        }

        /// <summary>The total N amount in this tissue (kg/ha).</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the live (green) tissues (kg/ha).</summary>
        internal double NLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double NDead
        {
            get { return Tissue[Tissue.Length - 1].Namount; }
        }

        /// <summary>Gets the average N concentration in this organ (kg/kg).</summary>
        internal double NconcTotal
        {
            get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); }
        }

        /// <summary>Gets the average N concentration in the live tissues (kg/kg).</summary>
        internal double NconcLive
        {
            get { return MathUtilities.Divide(NLive, DMLive, 0.0); }
        }

        /// <summary>Gets the average N concentration in dead tissues (kg/kg).</summary>
        internal double NconcDead
        {
            get { return MathUtilities.Divide(NDead, DMDead, 0.0); }
        }

        /// <summary>Gets the amount of senesced N available for remobilisation (kg/ha).</summary>
        internal double NSenescedRemobilisable
        {
            get { return Tissue[Tissue.Length - 1].NRemobilisable; }
        }

        /// <summary>Gets the amount of senesced N remobilised into new growth (kg/ha).</summary>
        internal double NSenescedRemobilised
        {
            get { return Tissue[Tissue.Length - 1].NRemobilised; }
        }

        /// <summary>Gets the amount of luxury N available for remobilisation (kg/ha).</summary>
        internal double NLuxuryRemobilisable
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].NRemobilisable;

                return result;
            }
        }

        /// <summary>Gets the amount of luxury N remobilised into new growth (kg/ha).</summary>
        internal double NLuxuryRemobilised
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].NRemobilised;

                return result;
            }
        }

        /// <summary>Gets the DM amount added to this organ via growth (kg/ha).</summary>
        internal double DMGrowth
        {
            get { return Tissue[0].DMTransferedIn; }
        }

        /// <summary>Gets the amount of N added to this organ via growth (kg/ha).</summary>
        internal double NGrowth
        {
            get { return Tissue[0].NTransferedIn; }
        }

        /// <summary>Gets the DM amount senescing from this organ (kg/ha).</summary>
        internal double DMSenesced
        {
            get { return Tissue[Tissue.Length - 2].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N senescing from this organ (kg/ha).</summary>
        internal double NSenesced
        {
            get { return Tissue[Tissue.Length - 2].NTransferedOut; }
        }

        /// <summary>Gets the DM amount detached from this organ (kg/ha).</summary>
        internal double DMDetached
        {
            get { return Tissue[Tissue.Length - 1].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N detached from this organ (kg/ha).</summary>
        internal double NDetached
        {
            get { return Tissue[Tissue.Length - 1].NTransferedOut; }
        }

        /// <summary>Gets the average digestibility of all biomass for this organ (kg/kg).</summary>
        internal double DigestibilityTotal
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;

                return MathUtilities.Divide(digestableDM, DMTotal, 0.0);
            }
        }

        /// <summary>Gets the average digestibility of live biomass for this organ (kg/kg).</summary>
        internal double DigestibilityLive
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;

                return MathUtilities.Divide(digestableDM, DMLive, 0.0);
            }
        }

        /// <summary>Gets the average digestibility of dead biomass for this organ (kg/kg).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double DigestibilityDead
        {
            get { return Tissue[Tissue.Length - 1].Digestibility; }
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Organ methods  ---------------------------------------------------------------------------------------------

        /// <summary>Reset all amounts to zero in all tissues of this organ.</summary>
        internal void DoResetOrgan()
        {
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].DM = 0.0;
                Tissue[t].Namount = 0.0;
                Tissue[t].Pamount = 0.0;
                DoCleanTransferAmounts();
            }
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        internal void DoCleanTransferAmounts()
        {
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].DMTransferedIn = 0.0;
                Tissue[t].DMTransferedOut = 0.0;
                Tissue[t].NTransferedIn = 0.0;
                Tissue[t].NTransferedOut = 0.0;
                Tissue[t].NRemobilisable = 0.0;
                Tissue[t].NRemobilised = 0.0;
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue).</summary>
        /// <param name="fraction">The fraction to kill in each tissue</param>
        internal void DoKillOrgan(double fraction = 1.0)
        {
            if (1.0 - fraction > Epsilon)
            {
                double fractionRemaining = 1.0 - fraction;
                for (int t = 0; t < Tissue.Length - 1; t++)
                {
                    Tissue[Tissue.Length - 1].DM += Tissue[t].DM * fraction;
                    Tissue[Tissue.Length - 1].Namount += Tissue[t].Namount * fraction;
                    Tissue[t].DM *= fractionRemaining;
                    Tissue[t].Namount *= fractionRemaining;
                }
            }
            else
            {
                for (int t = 0; t < Tissue.Length - 1; t++)
                {
                    Tissue[Tissue.Length - 1].DM += Tissue[t].DM;
                    Tissue[Tissue.Length - 1].Namount += Tissue[t].Namount;
                    Tissue[t].DM = 0.0;
                    Tissue[t].Namount = 0.0;
                }
            }
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        internal void DoTissueTurnover(double[] turnoverRate)
        {
            double turnedoverDM;
            double turnedoverN;

            // get amounts turned over
            for (int t = 0; t < Tissue.Length; t++)
            {
                if (turnoverRate[t] > 0.0)
                {
                    turnedoverDM = Tissue[t].DM * turnoverRate[t];
                    turnedoverN = Tissue[t].Namount * turnoverRate[t];
                    Tissue[t].DMTransferedOut += turnedoverDM;
                    Tissue[t].NTransferedOut += turnedoverN;

                    if (t < Tissue.Length - 1)
                    {
                        // pass amounts turned over from this tissue to the next (except last one)
                        Tissue[t + 1].DMTransferedIn += turnedoverDM;
                        Tissue[t + 1].NTransferedIn += turnedoverN;

                        // get the amounts remobilisable (luxury N)
                        double totalLuxuryN = (Tissue[t].DM + Tissue[t].DMTransferedIn - Tissue[t].DMTransferedOut) * (NconcLive - NConcOptimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, totalLuxuryN * Tissue[t].FractionNLuxuryRemobilisable);
                    }
                    else
                    {
                        // N transferred into dead tissue in excess of minimum N concentration is remobilisable
                        double remobilisableN = Tissue[t].DMTransferedIn * (NconcLive - NConcMinimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, remobilisableN);
                    }
                }
            }
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        /// <returns>A flag whether mass balance was maintained or not</returns>
        internal bool DoOrganUpdate()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            for (int t = 0; t < Tissue.Length; t++)
                Tissue[t].DoUpdateTissue();

            // check mass balance
            bool dmIsOk = Math.Abs(previousDM + DMGrowth - DMDetached - DMTotal) <= Epsilon;
            bool nIsOk = Math.Abs(previousN + NGrowth - NSenescedRemobilised - NDetached - NTotal) <= Epsilon;
            return (dmIsOk || nIsOk);
        }

        /// <summary>Adds a removal type to the defaultRemovalFractions.</summary>
        /// <param name="typeName">The name of the removal type</param>
        /// <param name="removalFractions">The default removal fractions</param>
        internal void SetRemovalFractions(string typeName, OrganBiomassRemovalType removalFractions)
        {
            defaultRemovalFractions.Add(typeName, removalFractions);
        }

        /// <summary>Gets the default removal fractions for a given removal type.</summary>
        /// <param name="typeName">The type of removal</param>
        /// <returns>The default removal fractions</returns>
        internal OrganBiomassRemovalType GetRemovalFractions(string typeName)
        {
            if (defaultRemovalFractions.ContainsKey(typeName))
                return defaultRemovalFractions[typeName];
            else
                return null;
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        /// <summary>Minimum significant difference between two values.</summary>
        const double Epsilon = 0.000000001;
    }

    /// <summary>Describes a generic below ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureBelowGroundOrgan
    {
        /// <summary>Flag which method for computing soil available water will be used.</summary>
        private PastureSpecies.PlantAvailableWaterMethod myWaterAvailableMethod;

        /// <summary>Flag which method for computing available soil nitrogen will be used.</summary>
        private PastureSpecies.PlantAvailableNitrogenMethod myNitrogenAvailableMethod = PastureSpecies.PlantAvailableNitrogenMethod.BasicAgPasture;

        /// <summary>Soil nitrogen model.</summary>
        private SoilNitrogen SoilNitrogen;

        /// <summary>The solute manager in this zone</summary>
        public SoluteManager solutes = null;

        /// <summary>Ammonium uptake coefficient.</summary>
        private double KNH4 = 1.0;

        /// <summary>Nitrate uptake coefficient.</summary>
        private double KNO3 = 1.0;

        /// <summary>Availability factor for NH4.</summary>
        private double kuNH4 = 0.50;

        /// <summary>Availability factor for NO3.</summary>
        private double kuNO3 = 0.95;

        /// <summary>Maximum daily amount of N that can be taken up by the plant (kg/ha).</summary>
        private double MaximumNUptake = 10.0;

        /// <summary>Number of layers in the soil.</summary>
        private int nLayers;

        /// <summary>Soil object where these roots are growing.</summary>
        public Soil mySoil = null;

        /// <summary>Name of pasture species</summary>
        private string pastureName;

        /// <summary>Specific root length (m/gDM).</summary>
        private double mySpecificRootLength = 100.0;

        /// <summary>Reference value for root length density for the Water and N availability.</summary>
        private double ReferenceRLD = 5.0;

        /// <summary>Exponent controlling the effect of soil moisture variations on water extractability.</summary>
        private double ExponentSoilMoisture = 1.50;

        /// <summary>Reference value of Ksat for water availability function.</summary>
        private double ReferenceKSuptake = 15.0;

        /// <summary>Constructor, initialise tissues.</summary>
        /// <param name="numTissues">The number of tissues in the organ</param>
        /// <param name="numLayers">The number of layers in the soil</param>
        /// <param name="myWaterAvailableMethod">Water uptake method</param>
        /// <param name="myNitrogenAvailableMethod">NItrogen uptake method</param>
        /// <param name="KNH4">Ammonium uptake coefficient</param>
        /// <param name="KNO3">Nitrate uptake coefficient</param>
        /// <param name="kuNH4">Availability factor for NH4</param>
        /// <param name="kuNO3">Availability factor for NO3</param>
        /// <param name="MaximumNUptake">Maximum daily amount of N that can be taken up by the plant (kg/ha)</param>
        /// <param name="soil">Soil where roots are growing</param>
        /// <param name="nameOfPasture">Name of pasture</param>
        /// <param name="initialRootDepth">Initial root depth</param>
        /// <param name="initialDMWeight">Initial dry matter</param>
        /// <param name="specificRootLength">Specific root length (m/gDM)</param>
        /// <param name="referenceRLD">Reference value for root length density for the Water and N availability.</param>
        /// <param name="exponentSoilMoisture">Exponent controlling the effect of soil moisture variations on water extractability</param>
        /// <param name="referenceKSuptake">Reference value of Ksat for water availability function</param>
        /// <param name="myRootDepthMaximum"></param>
        /// <param name="myRootDistributionDepthParam"></param>
        /// <param name="myRootDistributionExponent"></param>
        /// <param name="rootBottomDistributionFactor"></param>
        public PastureBelowGroundOrgan(int numTissues, int numLayers, 
                                       PastureSpecies.PlantAvailableWaterMethod myWaterAvailableMethod,
                                       PastureSpecies.PlantAvailableNitrogenMethod myNitrogenAvailableMethod,
                                       double KNH4, double KNO3, double MaximumNUptake,
                                       double kuNH4, double kuNO3,
                                       Soil soil,
                                       string nameOfPasture, double initialRootDepth, double initialDMWeight, double specificRootLength, 
                                       double referenceRLD, double exponentSoilMoisture,
                                       double referenceKSuptake, 
                                       double myRootDepthMaximum, double myRootDistributionDepthParam, double rootBottomDistributionFactor,
                                       double myRootDistributionExponent)
        {
            // Typically two tissues below ground, one live and one dead
            TissueCount = numTissues;
            Tissue = new RootTissue[TissueCount];
            for (int t = 0; t < TissueCount; t++)
                Tissue[t] = new RootTissue(numLayers);
            this.myWaterAvailableMethod = myWaterAvailableMethod;
            this.myNitrogenAvailableMethod = myNitrogenAvailableMethod;
            this.KNO3 = KNO3;
            this.KNH4 = KNH4;
            this.kuNH4 = kuNH4;
            this.kuNO3 = kuNO3;
            this.MaximumNUptake = MaximumNUptake;         

            nLayers = numLayers;
            mySoil = soil;
            pastureName = nameOfPasture;
            mySpecificRootLength = specificRootLength;
            ReferenceRLD = referenceRLD;
            ExponentSoilMoisture = exponentSoilMoisture;
            ReferenceKSuptake = referenceKSuptake;
            Name = soil.Parent.Name;
            mySoilNH4Available = new double[nLayers];
            mySoilNO3Available = new double[nLayers];
            solutes = Apsim.Find(soil, typeof(SoluteManager)) as SoluteManager;
            if (solutes == null)
                throw new Exception("Cannot find solute manager in zone");

            SoilNitrogen = Apsim.Find(soil, typeof(SoilNitrogen)) as SoilNitrogen;
            if (SoilNitrogen == null)
                throw new Exception("Cannot find SoilNitrogen in zone");

            // Initialise root DM, N, depth, and distribution
            this.Depth = initialRootDepth;
            RootDistributionTarget(myRootDepthMaximum, myRootDistributionDepthParam, rootBottomDistributionFactor, myRootDistributionExponent);
            double[] iniRootFraction = CurrentRootDistributionTarget(myRootDepthMaximum);
            for (int layer = 0; layer < nLayers; layer++)
                Tissue[0].DMLayer[layer] = initialDMWeight * iniRootFraction[layer];
        }

        /// <summary>The collection of tissues for this organ.</summary>
        internal RootTissue[] Tissue { get; set; }

        /// <summary>Amount of plant available water in the soil (mm).</summary>
        internal double[] mySoilWaterAvailable { get; private set; }

        /// <summary>Amount of NH4-N in the soil available to the plant (kg/ha).</summary>
        internal double[] mySoilNH4Available { get; private set; }

        /// <summary>Amount of NO3-N in the soil available to the plant (kg/ha).</summary>
        internal double[] mySoilNO3Available { get; private set; }

        /// <summary>Name of root zone.</summary>
        internal string Name { get; private set; }

        #region Root specific characteristics  -----------------------------------------------------------------------------

        /// <summary>Gets or sets the N concentration for optimum growth (kg/kg).</summary>
        internal double NConcOptimum = 2.0;

        /// <summary>Gets or sets the maximum N concentration, for luxury uptake (kg/kg).</summary>
        internal double NConcMaximum  = 2.5;

        /// <summary>Gets or sets the minimum N concentration, structural N (kg/kg).</summary>
        internal double NConcMinimum = 0.6;

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        internal double MinimumLiveDM = 0.0;

        /// <summary>Gets or sets the rooting depth (mm).</summary>
        internal double Depth { get; set; }

        /// <summary>Gets or sets the layer at the bottom of the root zone.</summary>
        internal int BottomLayer
        {
            get
            {
                return RootZoneBottomLayer();
            }
        }

        /// <summary>Gets or sets the target (ideal) DM fractions for each layer (0-1).</summary>
        internal double[] TargetDistribution { get; set; }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Organ Properties (summary of tissues)  ---------------------------------------------------------------------

        /// <summary>The number of tissue pools in this organ.</summary>
        internal int TissueCount;

        /// <summary>Gets the total dry matter in this organ (kg/ha).</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the live (green) tissues (kg/ha).</summary>
        internal double DMLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double DMDead
        {
            get { return Tissue[TissueCount - 1].DM; }
        }

        /// <summary>The total N amount in this tissue (kg/ha).</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the live (green) tissues (kg/ha).</summary>
        internal double NLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double NDead
        {
            get { return Tissue[TissueCount - 1].Namount; }
        }

        /// <summary>Gets the average N concentration in this organ (kg/kg).</summary>
        internal double NconcTotal
        {
            get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); }
        }

        /// <summary>Gets the average N concentration in the live tissues (kg/kg).</summary>
        internal double NconcLive
        {
            get { return MathUtilities.Divide(NLive, DMLive, 0.0); }
        }

        /// <summary>Gets the average N concentration in dead tissues (kg/kg).</summary>
        internal double NconcDead
        {
            get { return MathUtilities.Divide(NDead, DMDead, 0.0); }
        }

        /// <summary>Gets the amount of senesced N available for remobilisation (kg/ha).</summary>
        internal double NSenescedRemobilisable
        {
            get { return Tissue[TissueCount - 1].NRemobilisable; }
        }

        /// <summary>Gets the amount of senesced N remobilised into new growth (kg/ha).</summary>
        internal double NSenescedRemobilised
        {
            get { return Tissue[TissueCount - 1].NRemobilised; }
        }

        /// <summary>Gets the amount of luxury N available for remobilisation (kg/ha).</summary>
        internal double NLuxuryRemobilisable
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].NRemobilisable;

                return result;
            }
        }

        /// <summary>Gets the amount of senesced N remobilised into new growth (kg/ha).</summary>
        internal double NLuxuryRemobilised
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                    result += Tissue[t].NRemobilised;

                return result;
            }
        }

        /// <summary>Gets the DM amount added to this organ via growth (kg/ha).</summary>
        internal double DMGrowth
        {
            get { return Tissue[0].DMTransferedIn; }
        }

        /// <summary>Gets the amount of N added to this organ via growth (kg/ha).</summary>
        internal double NGrowth
        {
            get { return Tissue[0].NTransferedIn; }
        }

        /// <summary>Gets the DM amount senescing from this organ (kg/ha).</summary>
        internal double DMSenesced
        {
            get { return Tissue[TissueCount - 2].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N senescing from this organ (kg/ha).</summary>
        internal double NSenesced
        {
            get { return Tissue[TissueCount - 2].NTransferedOut; }
        }

        /// <summary>Gets the DM amount detached from this organ (kg/ha).</summary>
        internal double DMDetached
        {
            get { return Tissue[TissueCount - 1].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N detached from this organ (kg/ha).</summary>
        internal double NDetached
        {
            get { return Tissue[TissueCount - 1].NTransferedOut; }
        }

        /// <summary>Finds out the amount of plant available water in the soil.</summary>
        /// <param name="myZone">The soil information</param>
        internal double[] EvaluateSoilWaterAvailable(ZoneWaterAndN myZone)
        {
            if (myWaterAvailableMethod == PastureSpecies.PlantAvailableWaterMethod.DefaultAPSIM)
                return PlantAvailableSoilWaterDefault(myZone);
            else if (myWaterAvailableMethod == PastureSpecies.PlantAvailableWaterMethod.AlternativeKL)
                return PlantAvailableSoilWaterAlternativeKL(myZone);
            else if (myWaterAvailableMethod == PastureSpecies.PlantAvailableWaterMethod.AlternativeKS)
                return PlantAvailableSoilWaterAlternativeKS(myZone);
            else
                throw new Exception("Invalid water uptake method found");
        }

        /// <summary>Estimates the amount of plant available water in each soil layer of the root zone.</summary>
        /// <remarks>This is the default APSIM method, with kl representing the daily rate for water extraction</remarks>
        /// <param name="myZone">The soil information</param>
        /// <returns>The amount of available water in each layer (mm)</returns>
        internal double[] PlantAvailableSoilWaterDefault(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(pastureName);
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                result[layer] = Math.Max(0.0, myZone.Water[layer] - (soilCropData.LL[layer] * mySoil.Thickness[layer]));
                result[layer] *= FractionLayerWithRoots(layer) * soilCropData.KL[layer];
            }

            return result;
        }

        /// <summary>Estimates the amount of plant available  water in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This is an alternative method, kl representing a soil limiting factor for water extraction (clayey soils have lower values)
        ///  this is further modified by soil water content (a reduction for dry soil). A plant related factor is defined based on root
        ///  length density (limiting conditions when RLD is below ReferenceRLD)
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        /// <returns>The amount of available water in each layer (mm)</returns>
        internal double[] PlantAvailableSoilWaterAlternativeKL(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(pastureName);
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                double rldFac = Math.Min(1.0, RootLengthDensity[layer] / ReferenceRLD);
                double swFac;
                if (mySoil.SoilWater.SWmm[layer] >= mySoil.DULmm[layer])
                    swFac = 1.0;
                else if (mySoil.SoilWater.SWmm[layer] <= mySoil.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.LL15mm[layer]) /
                                        (mySoil.DULmm[layer] - mySoil.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, ExponentSoilMoisture);
                }

                // Total available water
                result[layer] = Math.Max(0.0, myZone.Water[layer] - (soilCropData.LL[layer] * mySoil.Thickness[layer]));

                // Actual plant available water
                result[layer] *= FractionLayerWithRoots(layer) * Math.Min(1.0, soilCropData.KL[layer] * swFac * rldFac);
            }

            return result;
        }

        /// <summary>Estimates the amount of plant available water in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This is an alternative method, which does not use kl. A factor based on Ksat is used instead. This is further modified
        ///  by soil water content and a plant related factor, defined based on root length density. All three factors are normalised 
        ///  (using ReferenceKSat and ReferenceRLD for KSat and root and DUL for soil water content). The effect of all factors are
        ///  assumed to vary between zero and one following exponential functions, such that the effect is 90% at the reference value.
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        /// <returns>The amount of available water in each layer (mm)</returns>
        internal double[] PlantAvailableSoilWaterAlternativeKS(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(pastureName);
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                double condFac = 1.0 - Math.Pow(10.0, -mySoil.KS[layer] / ReferenceKSuptake);
                double rldFac = 1.0 - Math.Pow(10.0, -RootLengthDensity[layer] / ReferenceRLD);
                double swFac;
                if (mySoil.SoilWater.SWmm[layer] >= mySoil.DULmm[layer])
                    swFac = 1.0;
                else if (mySoil.SoilWater.SWmm[layer] <= mySoil.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.LL15mm[layer]) /
                                        (mySoil.DULmm[layer] - mySoil.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, ExponentSoilMoisture);
                }

                // Total available water
                result[layer] = Math.Max(0.0, myZone.Water[layer] - soilCropData.LL[layer]) * mySoil.Thickness[layer];

                // Actual plant available water
                result[layer] *= FractionLayerWithRoots(layer) * Math.Min(1.0, rldFac * condFac * swFac);
            }

            return result;
        }

        /// <summary>Computes how much of the layer is actually explored by roots (considering depth only).</summary>
        /// <param name="layer">The index for the layer being considered</param>
        /// <returns>The fraction of the layer that is explored by roots (0-1)</returns>
        internal double FractionLayerWithRoots(int layer)
        {
            double fractionInLayer = 0.0;
            if (layer < BottomLayer)
            {
                fractionInLayer = 1.0;
            }
            else if (layer == BottomLayer)
            {
                double depthTillTopThisLayer = 0.0;
                for (int z = 0; z < layer; z++)
                    depthTillTopThisLayer += mySoil.Thickness[z];
                fractionInLayer = (Depth - depthTillTopThisLayer) / mySoil.Thickness[layer];
                fractionInLayer = Math.Min(1.0, Math.Max(0.0, fractionInLayer));
            }

            return fractionInLayer;
        }

        /// <summary>Gets the root length density by volume (mm/mm^3).</summary>
        public double[] RootLengthDensity
        {
            get
            {
                double[] result = new double[nLayers];
                double totalRootLength = Tissue[0].DM * mySpecificRootLength; // m root/m2 
                totalRootLength *= 0.0000001; // convert into mm root/mm2 soil)
                for (int layer = 0; layer < result.Length; layer++)
                {
                    result[layer] = Tissue[0].FractionWt[layer] * totalRootLength / mySoil.Thickness[layer];
                }
                return result;
            }
        }

        /// <summary>Finds out the amount of plant available nitrogen (NH4 and NO3) in the soil.</summary>
        /// <param name="myZone">The soil information</param>
        /// <param name="mySoilWaterUptake">Soil water uptake</param>
        internal void EvaluateSoilNitrogenAvailable(ZoneWaterAndN myZone, double[] mySoilWaterUptake)
        {
            if (myNitrogenAvailableMethod == PastureSpecies.PlantAvailableNitrogenMethod.BasicAgPasture)
                PlantAvailableSoilNBasicAgPasture(myZone);
            else if (myNitrogenAvailableMethod == PastureSpecies.PlantAvailableNitrogenMethod.DefaultAPSIM)
                PlantAvailableSoilNDefaultAPSIM(myZone);
            else if (myNitrogenAvailableMethod == PastureSpecies.PlantAvailableNitrogenMethod.AlternativeRLD)
                PlantAvailableSoilNAlternativeRLD(myZone);
            else if (myNitrogenAvailableMethod == PastureSpecies.PlantAvailableNitrogenMethod.AlternativeWup)
                PlantAvailableSoilNAlternativeWup(myZone, mySoilWaterUptake);
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>This is a basic method, used as default in old AgPasture, all N in the root zone is available</remarks>
        /// <param name="myZone">The soil information</param>
        private void PlantAvailableSoilNBasicAgPasture(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                mySoilNH4Available[layer] = myZone.NH4N[layer] * layerFrac;
                mySoilNO3Available[layer] = myZone.NO3N[layer] * layerFrac;
            }
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This method approximates the default approach in APSIM plants (method 3 in Plant1 models)
        /// Soil water status and uptake coefficient control the availability, which is a square function of N content.
        /// Uptake is capped for a maximum value plants can take in one day.
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        private void PlantAvailableSoilNDefaultAPSIM(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            double swFac;  // the soil water factor
            double bdFac;  // the soil density factor
            double potAvailableN; // potential available N
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                bdFac = 100.0 / (mySoil.Thickness[layer] * mySoil.BD[layer]);
                if (myZone.Water[layer] >= mySoil.DULmm[layer])
                    swFac = 1.0;
                else if (myZone.Water[layer] <= mySoil.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.LL15mm[layer]) /
                                        (mySoil.DULmm[layer] - mySoil.LL15mm[layer]);
                    waterRatio = MathUtilities.Bound(waterRatio, 0.0, 1.0);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, ExponentSoilMoisture);
                }

                // get NH4 available
                potAvailableN = Math.Pow(myZone.NH4N[layer] * layerFrac, 2.0) * swFac * bdFac * KNH4;
                mySoilNH4Available[layer] = Math.Min(myZone.NH4N[layer] * layerFrac, potAvailableN);

                // get NO3 available
                potAvailableN = Math.Pow(myZone.NO3N[layer] * layerFrac, 2.0) * swFac * bdFac * KNO3;
                mySoilNO3Available[layer] = Math.Min(myZone.NO3N[layer] * layerFrac, potAvailableN);
            }

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > MaximumNUptake)
            {
                double upFraction = MaximumNUptake / potAvailableN;
                for (int layer = 0; layer <= BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This method considers soil water status and root length density to define factors controlling N availability.
        /// Soil water status is used to define a factor that varies from zero at LL, below which no uptake can happen, 
        ///  to one at DUL, above which no restrictions to uptake exist.
        /// Root length density is used to define a factor varying from zero if there are no roots to one when root length
        ///  density is equal to a ReferenceRLD, above which there are no restrictions for uptake.
        /// Factors for each N form can also alter the amount available.
        /// Uptake is caped for a maximum value plants can take in one day.
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        private void PlantAvailableSoilNAlternativeRLD(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            double swFac;  // the soil water factor
            double rldFac;  // the root density factor
            double potAvailableN; // potential available N
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                rldFac = Math.Min(1.0, MathUtilities.Divide(RootLengthDensity[layer], ReferenceRLD, 1.0));
                if (myZone.Water[layer] >= mySoil.DULmm[layer])
                    swFac = 1.0;
                else if (myZone.Water[layer] <= mySoil.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.LL15mm[layer]) /
                                        (mySoil.DULmm[layer] - mySoil.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, ExponentSoilMoisture);
                }

                // get NH4 available
                potAvailableN = myZone.NH4N[layer] * layerFrac;
                mySoilNH4Available[layer] = potAvailableN * Math.Min(1.0, swFac * rldFac * kuNH4);

                // get NO3 available
                potAvailableN = myZone.NO3N[layer] * layerFrac;
                mySoilNO3Available[layer] = potAvailableN * Math.Min(1.0, swFac * rldFac * kuNO3);
            }

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > MaximumNUptake)
            {
                double upFraction = MaximumNUptake / potAvailableN;
                for (int layer = 0; layer <= BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This method considers soil water as the main factor controlling N availability/uptake.
        /// Availability is given by the proportion of water taken up in each layer, further modified by uptake factors
        /// Uptake is caped for a maximum value plants can take in one day.
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        /// <param name="mySoilWaterUptake">Soil water uptake</param>
        private void PlantAvailableSoilNAlternativeWup(ZoneWaterAndN myZone, double[] mySoilWaterUptake)
        {
            double layerFrac; // the fraction of layer within the root zone
            double potAvailableN; // potential available N
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                double swuFac = MathUtilities.Divide(mySoilWaterUptake[layer], myZone.Water[layer], 0.0);

                // get NH4 available
                potAvailableN = myZone.NH4N[layer] * layerFrac;
                mySoilNH4Available[layer] = potAvailableN * Math.Min(1.0, swuFac * kuNH4);

                // get NO3 available
                potAvailableN = myZone.NO3N[layer] * layerFrac;
                mySoilNO3Available[layer] = potAvailableN * Math.Min(1.0, swuFac * kuNO3);
            }

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > MaximumNUptake)
            {
                double upFraction = MaximumNUptake / potAvailableN;
                for (int layer = 0; layer <= BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Organ methods  ---------------------------------------------------------------------------------------------

        /// <summary>Reset all amounts to zero in all tissues of this organ.</summary>
        internal void DoResetOrgan()
        {
            for (int t = 0; t < TissueCount; t++)
            {
                Tissue[t].DM = 0.0;
                Tissue[t].Namount = 0.0;
                Tissue[t].Pamount = 0.0;
                DoCleanTransferAmounts();
            }
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        internal void DoCleanTransferAmounts()
        {
            for (int t = 0; t < TissueCount; t++)
            {
                Tissue[t].DMTransferedIn = 0.0;
                Tissue[t].DMTransferedOut = 0.0;
                Tissue[t].NTransferedIn = 0.0;
                Tissue[t].NTransferedOut = 0.0;
                Tissue[t].NRemobilisable = 0.0;
                Tissue[t].NRemobilised = 0.0;
                Array.Clear(Tissue[t].DMLayersTransferedIn, 0, Tissue[t].DMLayersTransferedIn.Length);
                Array.Clear(Tissue[t].NLayersTransferedIn, 0, Tissue[t].NLayersTransferedIn.Length);
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue).</summary>
        /// <param name="fraction">The fraction to kill in each tissue</param>
        internal void DoKillOrgan(double fraction = 1.0)
        {
            if (1.0 - fraction > Epsilon)
            {
                double fractionRemaining = 1.0 - fraction;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        Tissue[TissueCount - 1].DMLayer[layer] += Tissue[t].DMLayer[layer] * fraction;
                        Tissue[TissueCount - 1].NamountLayer[layer] += Tissue[t].NamountLayer[layer] * fraction;
                        Tissue[t].DMLayer[layer] *= fractionRemaining;
                        Tissue[t].NamountLayer[layer] *= fractionRemaining;
                    }
                }
            }
            else
            {
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        Tissue[TissueCount - 1].DMLayer[layer] += Tissue[t].DMLayer[layer];
                        Tissue[TissueCount - 1].NamountLayer[layer] += Tissue[t].NamountLayer[layer];
                    }
                    Tissue[t].DM = 0.0;
                    Tissue[t].Namount = 0.0;
                }
            }
        }

        /// <summary>Adds root material (DM and N) to the soil's FOM pool.</summary>
        public void DoEndOrgan(double CarbonFractionInDM)
        {
            FOMLayerLayerType[] FOMdataLayer = new FOMLayerLayerType[nLayers];

            double amountDM = DMTotal;
            double amountN = NTotal;
            for (int layer = 0; layer < nLayers; layer++)
            {
                FOMType fomData = new FOMType();
                fomData.amount = amountDM * Tissue[0].FractionWt[layer];
                fomData.N = amountN * Tissue[0].FractionWt[layer];
                fomData.C = amountDM * CarbonFractionInDM * Tissue[0].FractionWt[layer];
                fomData.P = 0.0; // P not considered here
                fomData.AshAlk = 0.0; // Ash not considered here

                FOMLayerLayerType layerData = new FOMLayerLayerType();
                layerData.FOM = fomData;
                layerData.CNR = 0.0; // not used here
                layerData.LabileP = 0; // not used here

                FOMdataLayer[layer] = layerData;
            }

            FOMLayerType FOMData = new FOMLayerType();
            FOMData.Type = pastureName;
            FOMData.Layer = FOMdataLayer;
            SoilNitrogen.DoIncorpFOM(FOMData);
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        internal void DoTissueTurnover(double[] turnoverRate)
        {
            double turnoverDM;
            double turnoverN;

            // get amounts turned over
            for (int t = 0; t < TissueCount; t++)
            {
                if (turnoverRate[t] > 0.0)
                {
                    turnoverDM = Tissue[t].DM * turnoverRate[t];
                    turnoverN = Tissue[t].Namount * turnoverRate[t];
                    Tissue[t].DMTransferedOut += turnoverDM;
                    Tissue[t].NTransferedOut += turnoverN;

                    if (t < TissueCount - 1)
                    {
                        // pass amounts turned over from this tissue to the next
                        Tissue[t + 1].DMTransferedIn += turnoverDM;
                        Tissue[t + 1].NTransferedIn += turnoverN;

                        // incoming stuff need to be given for each layer
                        for (int layer = 0; layer <= BottomLayer; layer++)
                        {
                            Tissue[t + 1].DMLayersTransferedIn[layer] = turnoverDM * Tissue[t].FractionWt[layer];
                            Tissue[t + 1].NLayersTransferedIn[layer] = turnoverN * Tissue[t].FractionWt[layer];
                        }

                        // get the amounts remobilisable (luxury N)
                        double totalLuxuryN = (Tissue[t].DM + Tissue[t].DMTransferedIn - Tissue[t].DMTransferedOut) * (NconcLive - NConcOptimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, totalLuxuryN * Tissue[t + 1].FractionNLuxuryRemobilisable);
                    }
                    else
                    {
                        // N transferred into dead tissue in excess of minimum N concentration is remobilisable
                        double remobilisableN = Tissue[t].DMTransferedIn * (NconcLive - NConcMinimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, remobilisableN);
                    }
                }
            }
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        internal bool DoOrganUpdate()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            for (int t = 0; t < TissueCount; t++)
                Tissue[t].DoUpdateTissue();

            // check mass balance
            bool dmIsOk = Math.Abs(previousDM + DMGrowth - DMDetached - DMTotal) <= Epsilon;
            bool nIsOk = Math.Abs(previousN + NGrowth - NSenescedRemobilised - NDetached - NTotal) <= Epsilon;
            return (dmIsOk || nIsOk);
        }


        /// <summary>Gets the index of the layer at the bottom of the root zone.</summary>
        /// <returns>The index of a layer</returns>
        private int RootZoneBottomLayer()
        {
            int result = 0;
            double currentDepth = 0.0;
            for (int layer = 0; layer < nLayers; layer++)
            {
                if (Depth > currentDepth)
                {
                    result = layer;
                    currentDepth += mySoil.Thickness[layer];
                }
                else
                    layer = nLayers;
            }

            return result;
        }

        /// <summary>Computes the target (or ideal) distribution of roots in the soil profile.</summary>
        /// <remarks>
        /// This distribution is solely based on root parameters (maximum depth and distribution parameters)
        /// These values will be used to allocate initial rootDM as well as any growth over the profile
        /// </remarks>
        /// <param name="myRootDepthMaximum"></param>
        /// <param name="myRootDistributionDepthParam"></param>
        /// <param name="rootBottomDistributionFactor"></param>
        /// <param name="myRootDistributionExponent"></param>
        /// <returns>A weighting factor for each soil layer (mm equivalent)</returns>
        public void RootDistributionTarget(double myRootDepthMaximum, double myRootDistributionDepthParam, 
                                           double rootBottomDistributionFactor, double myRootDistributionExponent)
        {
            // 1. Base distribution calculated using a combination of linear and power functions:
            //  It considers homogeneous distribution from surface down to a fraction of root depth (DepthForConstantRootProportion),
            //   below this depth the proportion of root decrease following a power function (with exponent ExponentRootDistribution),
            //   it reaches zero slightly below the MaximumRootDepth (defined by rootBottomDistributionFactor), but the function is
            //   truncated at MaximumRootDepth. The values are not normalised.
            //  The values are further adjusted using the values of XF (so there will be less roots in those layers)

            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(pastureName);
            double depthTop = 0.0;
            double depthBottom = 0.0;
            double depthFirstStage = Math.Min(myRootDepthMaximum, myRootDistributionDepthParam);

            for (int layer = 0; layer < nLayers; layer++)
            {
                depthBottom += mySoil.Thickness[layer];
                if (depthTop >= myRootDepthMaximum)
                {
                    // totally out of root zone
                    result[layer] = 0.0;
                }
                else if (depthBottom <= depthFirstStage)
                {
                    // totally in the first stage
                    result[layer] = mySoil.Thickness[layer] * soilCropData.XF[layer];
                }
                else
                {
                    // at least partially on second stage
                    double maxRootDepth = myRootDepthMaximum * rootBottomDistributionFactor;
                    result[layer] = Math.Pow(maxRootDepth - Math.Max(depthTop, depthFirstStage), myRootDistributionExponent + 1)
                                  - Math.Pow(maxRootDepth - Math.Min(depthBottom, myRootDepthMaximum), myRootDistributionExponent + 1);
                    result[layer] /= (myRootDistributionExponent + 1) * Math.Pow(maxRootDepth - depthFirstStage, myRootDistributionExponent);
                    if (depthTop < depthFirstStage)
                    {
                        // partially in first stage
                        result[layer] += depthFirstStage - depthTop;
                    }

                    result[layer] *= soilCropData.XF[layer];
                }

                depthTop += mySoil.Thickness[layer];
            }

            TargetDistribution = result;
        }


        /// <summary>Computes the current target distribution of roots in the soil profile.</summary>
        /// <remarks>
        /// This distribution is a correction of the target distribution, taking into account the depth of soil
        /// as well as the current rooting depth
        /// </remarks>
        /// <param name="myRootDepthMaximum"></param>
        /// <returns>The proportion of root mass expected in each soil layer (0-1)</returns>
        public double[] CurrentRootDistributionTarget(double myRootDepthMaximum)
        {
            double cumProportion = 0.0;
            double topLayersDepth = 0.0;
            double[] result = new double[nLayers];

            // Get the total weight over the root zone, first layers totally within the root zone
            for (int layer = 0; layer < BottomLayer; layer++)
            {
                cumProportion += TargetDistribution[layer];
                topLayersDepth += mySoil.Thickness[layer];
            }
            // Then consider layer at the bottom of the root zone
            double layerFrac = Math.Min(1.0, (myRootDepthMaximum - topLayersDepth) / (Depth - topLayersDepth));
            cumProportion += TargetDistribution[BottomLayer] * layerFrac;

            // Normalise the weights to be a fraction, adds up to one
            if (cumProportion > Epsilon)
            {
                for (int layer = 0; layer < BottomLayer; layer++)
                    result[layer] = TargetDistribution[layer] / cumProportion;
                result[BottomLayer] = TargetDistribution[BottomLayer] * layerFrac / cumProportion;
            }

            return result;
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        /// <summary>Minimum significant difference between two values.</summary>
        const double Epsilon = 0.000000001;
    }

    /// <summary>Describes a generic tissue of a pasture species.</summary>
    [Serializable]
    public class GenericTissue : Model
    {
        #region Basic properties  ------------------------------------------------------------------------------------------

        ////- Characteristics (parameters) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the fraction of luxury N remobilisable per day (0-1).</summary>
        internal double FractionNLuxuryRemobilisable = 0.0;

        /// <summary>Gets or sets the sugar fraction on new growth, i.e. soluble carbohydrate (0-1).</summary>
        internal double FractionSugarNewGrowth = 0.0;

        /// <summary>Gets or sets the digestibility of cell walls (0-1).</summary>
        internal double DigestibilityCellWall = 0.5;

        /// <summary>Gets or sets the digestibility of proteins (0-1).</summary>
        internal double DigestibilityProtein = 1.0;

        ////- State properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the dry matter weight (kg/ha).</summary>
        internal virtual double DM { get; set; }

        /// <summary>Gets or sets the nitrogen content (kg/ha).</summary>
        internal virtual double Namount { get; set; }

        /// <summary>Gets or sets the phosphorus content (kg/ha).</summary>
        internal virtual double Pamount { get; set; }

        ////- Amounts in and out >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the DM amount transferred into this tissue (kg/ha).</summary>
        internal double DMTransferedIn { get; set; }

        /// <summary>Gets or sets the DM amount transferred out of this tissue (kg/ha).</summary>
        internal double DMTransferedOut { get; set; }

        /// <summary>Gets or sets the amount of N transferred into this tissue (kg/ha).</summary>
        internal double NTransferedIn { get; set; }

        /// <summary>Gets or sets the amount of N transferred out of this tissue (kg/ha).</summary>
        internal double NTransferedOut { get; set; }

        /// <summary>Gets or sets the amount of N available for remobilisation (kg/ha).</summary>
        internal double NRemobilisable { get; set; }

        /// <summary>Gets or sets the amount of N remobilised into new growth (kg/ha).</summary>
        internal double NRemobilised { get; set; }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Derived properties (outputs)  ------------------------------------------------------------------------------

        /// <summary>Gets the nitrogen concentration (kg/kg).</summary>
        internal double Nconc
        {
            get { return MathUtilities.Divide(Namount, DM, 0.0); }
            set { Namount = value * DM; }
        }

        /// <summary>Gets the phosphorus concentration (kg/kg).</summary>
        internal double Pconc
        {
            get { return MathUtilities.Divide(Pamount, DM, 0.0); }
            set { Pamount = value * DM; }
        }

        /// <summary>Gets the digestibility of this tissue (kg/kg).</summary>
        /// <remarks>Digestibility of sugars is assumed to be 100%.</remarks>
        internal double Digestibility
        {
            get
            {
                double tissueDigestibility = 0.0;
                if (DM > 0.0)
                {
                    double cnTissue = DM * CarbonFractionInDM / Namount;
                    double ratio1 = CNratioCellWall / cnTissue;
                    double ratio2 = CNratioCellWall / CNratioProtein;
                    double fractionSugar = DMTransferedIn * FractionSugarNewGrowth / DM;
                    double fractionProtein = (ratio1 - (1.0 - fractionSugar)) / (ratio2 - 1.0);
                    double fractionCellWall = 1.0 - fractionSugar - fractionProtein;
                    tissueDigestibility = fractionSugar + (fractionProtein * DigestibilityProtein) + (fractionCellWall * DigestibilityCellWall);
                }

                return tissueDigestibility;
            }
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Tissue methods  --------------------------------------------------------------------------------------------

        /// <summary>Removes a fraction of remobilisable N for use into new growth.</summary>
        /// <param name="fraction">The fraction to remove (0-1)</param>
        internal void DoRemobiliseN(double fraction)
        {
            NRemobilised = NRemobilisable * fraction;
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        internal virtual void DoUpdateTissue()
        {
            DM += DMTransferedIn - DMTransferedOut;
            Namount += NTransferedIn - (NTransferedOut + NRemobilised);
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Constants  -------------------------------------------------------------------------------------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        const double CarbonFractionInDM = 0.4;

        /// <summary>Carbon to nitrogen ratio of proteins (kg/kg).</summary>
        const double CNratioProtein = 3.5;

        /// <summary>Carbon to nitrogen ratio of cell walls (kg/kg).</summary>
        const double CNratioCellWall = 100.0;

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double MyPrecision = 0.000000001;

        #endregion ---------------------------------------------------------------------------------------------------------
    }

    /// <summary>Describes a root tissue of a pasture species.</summary>
    [Serializable]
    internal class RootTissue : GenericTissue
    {
        /// <summary>Constructor, initialise array.</summary>
        /// <param name="numLayers">The number of layers in the soil</param>
        public RootTissue(int numLayers)
        {
            nLayers = numLayers;
            DMLayer = new double[nLayers];
            NamountLayer = new double[nLayers];
            PamountLayer = new double[nLayers];
            DMLayersTransferedIn = new double[nLayers];
            NLayersTransferedIn = new double[nLayers];
        }

        /// <summary>Number of layers in the soil.</summary>
        private int nLayers;

        #region Basic properties  ------------------------------------------------------------------------------------------

        ////- State properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the dry matter weight (kg/ha).</summary>
        internal override double DM
        {
            get { return DMLayer.Sum(); }
            set
            {
                double[] prevRootFraction = FractionWt;
                for (int layer = 0; layer < nLayers; layer++)
                    DMLayer[layer] = value * prevRootFraction[layer];
            }
        }

        /// <summary>Gets or sets the DM amount for each layer (kg/ha).</summary>
        internal double[] DMLayer;

        /// <summary>Gets or sets the nitrogen content (kg/ha).</summary>
        internal override double Namount
        {
            get { return NamountLayer.Sum(); }
            set
            {
                for (int layer = 0; layer < nLayers; layer++)
                    NamountLayer[layer] = value * FractionWt[layer];
            }
        }

        /// <summary>Gets or sets the N content for each layer (kg/ha).</summary>
        internal double[] NamountLayer;

        /// <summary>Gets or sets the phosphorus content (kg/ha).</summary>
        internal override double Pamount
        {
            get { return PamountLayer.Sum(); }
            set
            {
                for (int layer = 0; layer < nLayers; layer++)
                    PamountLayer[layer] = value * FractionWt[layer];
            }
        }

        /// <summary>Gets or sets the P content for each layer (kg/ha).</summary>
        internal double[] PamountLayer { get; set; }

        ////- Amounts in and out >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the DM amount transferred into this tissue, for each layer (kg/ha).</summary>
        internal double[] DMLayersTransferedIn { get; set; }

        /// <summary>Gets or sets the amount of N transferred into this tissue, for each layer (kg/ha).</summary>
        internal double[] NLayersTransferedIn { get; set; }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Derived properties (outputs)  ------------------------------------------------------------------------------

        /// <summary>Gets the dry matter fraction for each layer (0-1).</summary>
        internal double[] FractionWt
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = MathUtilities.Divide(DMLayer[layer], DM, 0.0);
                return result;
            }
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Tissue methods  --------------------------------------------------------------------------------------------

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        internal override void DoUpdateTissue()
        {
            // removals first as they do not change distribution over the profile
            DM -= DMTransferedOut;
            Namount -= NTransferedOut;

            // additions need to consider distribution over the profile
            DMTransferedIn = DMLayersTransferedIn.Sum();
            NTransferedIn = NLayersTransferedIn.Sum();
            if ((DMTransferedIn > MyPrecision) && (NTransferedIn > MyPrecision))
            {
                for (int layer = 0; layer < nLayers; layer++)
                {
                    DMLayer[layer] += DMLayersTransferedIn[layer];
                    NamountLayer[layer] += NLayersTransferedIn[layer] - (NRemobilised * (NLayersTransferedIn[layer] / NTransferedIn));
                }
            }
        }

        #endregion ---------------------------------------------------------------------------------------------------------
    }
}
