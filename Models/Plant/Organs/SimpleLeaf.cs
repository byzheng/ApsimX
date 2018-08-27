using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    /// <summary>
    /// This plant organ is parameterised using a simple leaf organ type which provides the core functions of intercepting radiation, providing a photosynthesis supply and a transpiration demand.  It is parameterised as follows.
    /// 
    /// **Dry Matter Supply**
    /// 
    /// DryMatter Fixation Supply (Photosynthesis) provided to the Organ Arbitrator (for partitioning between organs) is calculated each day as the product of a unstressed potential and a series of stress factors.
    /// DM is not retranslocated out of this organ.
    /// 
    /// **Dry Matter Demands**
    /// 
    /// A given fraction of daily DM demand is determined to be structural and the remainder is non-structural.
    /// 
    /// **Nitrogen Demands**
    /// 
    /// The daily Storage N demand is the product of Total DM demand and a Maximum N concentration less the structural N demand.
    /// The daily structural N demand is the product of Total DM demand and a Minimum N concentration. 
    /// The Nitrogen demand switch is a multiplier applied to nitrogen demand so it can be turned off at certain phases.
    /// 
    /// **Nitrogen Supplies**
    /// 
    /// As the organ senesces a fraction of senesced N is made available to the arbitrator as NReallocationSupply.
    /// A fraction of Storage N is made available to the arbitrator as NRetranslocationSupply
    /// 
    /// **Biomass Senescence and Detachment**
    /// 
    /// Senescence is calculated as a proportion of the live dry matter.
    /// Detachment of biomass into the surface organic matter pool is calculated daily as a proportion of the dead DM.
    /// 
    /// **Canopy**
    /// 
    /// The user can model the canopy by specifying either the LAI and an extinction coefficient, or by specifying the canopy cover directly.  If the cover is specified, LAI is calculated using an inverted Beer-Lambert equation with the specified cover value.
    /// 
    /// The canopies values of Cover and LAI are passed to the MicroClimate module which uses the Penman Monteith equation to calculate potential evapotranspiration for each canopy and passes the value back to the crop.
    /// The effect of growth rate on transpiration is captured using the Fractional Growth Rate (FRGR) function which is parameterised as a function of temperature for the simple leaf. 
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SimpleLeaf : GenericOrgan, ICanopy, ILeaf, IHasWaterDemand
    {
        /// <summary>The plant</summary>
        [Link]
        private Plant Plant = null;

        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;

        #region Leaf Interface
        /// <summary>
        /// Number of initiated cohorts that have not appeared yet
        /// </summary>
        public int ApicalCohortNo { get; set; }
        /// <summary>
        /// reset leaf numbers
        /// </summary>
        public void Reset() { }
        /// <summary></summary>
        public int InitialisedCohortNo { get; set; }
        /// <summary></summary>
        public void RemoveHighestLeaf() { }
        /// <summary>
        /// 
        /// </summary>
        public bool CohortsInitialised { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int TipsAtEmergence { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int CohortsAtInitialisation { get; set; }
        /// <summary>
        /// 
        ///</summary>
        public int AppearedCohortNo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double PlantAppearedLeafNo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="proprtionRemoved"></param>
        public void DoThin(double proprtionRemoved) { }

        /// <summary>Apex number by age</summary>
        /// <param name="age">Threshold age</param>
        public double ApexNumByAge(double age) { return 0; }
        #endregion

        #region Canopy interface

        /// <summary>Gets the canopy. Should return null if no canopy present.</summary>
        public string CanopyType { get { return Plant.CropType; } }

        /// <summary>Albedo.</summary>
        [Description("Albedo")]
        public double Albedo { get; set; }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("GSMAX")]
        public double Gsmax { get; set; }

        /// <summary>Gets or sets the R50.</summary>
        [Description("R50")]
        public double R50 { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAI { get; set; }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI + LAIDead; } }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen
        {
            get
            {
                if (Plant.IsAlive)
                {
                    double greenCover = 0.0;
                    if (CoverFunction == null)
                        greenCover = 1.0 - Math.Exp(-ExtinctionCoefficientFunction.Value() * LAI);
                    else
                        greenCover = CoverFunction.Value();
                    return Math.Min(Math.Max(greenCover, 0.0), 0.999999999); // limiting to within 10^-9, so MicroClimate doesn't complain
                }
                else
                    return 0.0;

            }
        }

        /// <summary>Gets the cover total.</summary>
        [Units("0-1")]
        public double CoverTotal
        {
            get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); }
        }

        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double Height { get; set; }
        /// <summary>Gets the depth.</summary>
        [Units("mm")]
        public double Depth { get { return Height; } }//  Fixme.  This needs to be replaced with something that give sensible numbers for tree crops

        /// <summary>Gets or sets the FRGR.</summary>
        [Units("mm")]
        public double FRGR { get; set; }

        private double _PotentialEP = 0;
        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [Units("mm")]
        public double PotentialEP
        {
            get { return _PotentialEP; }
            set
            {
                _PotentialEP = value;
                MicroClimatePresent = true;
            }
        }
        /// <summary>
        /// Flag to test if Microclimate is present
        /// </summary>
        public bool MicroClimatePresent { get; set; }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }
        #endregion

        #region Parameters
        /// <summary>The FRGR function</summary>
        [Link]
        IFunction FRGRFunction = null;   // VPD effect on Growth Interpolation Set

        /// <summary>The cover function</summary>
        [Link(IsOptional = true)]
        IFunction CoverFunction = null;

        /// <summary>The lai function</summary>
        [Link(IsOptional = true)]
        IFunction LAIFunction = null;
        /// <summary>The extinction coefficient function</summary>
        [Link(IsOptional = true)]
        IFunction ExtinctionCoefficientFunction = null;
        /// <summary>The photosynthesis</summary>
        [Link]
        IFunction Photosynthesis = null;
        /// <summary>The height function</summary>
        [Link]
        IFunction HeightFunction = null;
        /// <summary>The lai dead function</summary>
        [Link]
        IFunction LaiDeadFunction = null;

        /// <summary>The structure</summary>
        [Link(IsOptional = true)]
        public Structure Structure = null;
        
        /// <summary>Water Demand Function</summary>
        [Link(IsOptional = true)]
        IFunction WaterDemandFunction = null;

        /// <summary>The Stage that leaves are initialised on</summary>
        [Description("The Stage that leaves are initialised on")]
        public string LeafInitialisationStage { get; set; } = "Emergence";
        


        #endregion

        #region States and variables

        /// <summary>Gets or sets the k dead.</summary>
        public double KDead { get; set; }                  // Extinction Coefficient (Dead)
        /// <summary>Calculates the water demand.</summary>
        public double CalculateWaterDemand()
        {
            if (WaterDemandFunction != null)
                return WaterDemandFunction.Value();
            else
            {
                return PotentialEP;
            }
        }
        /// <summary>Gets the transpiration.</summary>
        public double Transpiration { get { return WaterAllocation; } }

        /// <summary>Gets the fw.</summary>
        public double Fw { get { return MathUtilities.Divide(WaterAllocation, CalculateWaterDemand(), 1); } }

        /// <summary>Gets the function.</summary>
        public double Fn
        {
            get
            {
                if (Live != null)
                    return MathUtilities.Divide(Live.N, Live.Wt * MaxNconc, 1);
                return 0;
            }
        }

        /// <summary>Gets the metabolic N concentration factor.</summary>
        public double FNmetabolic
        {
            get
            {
                double factor = 0.0;
                if (Live != null)
                    factor = MathUtilities.Divide(Live.N - Live.StructuralN, Live.Wt * (CritNconc - MinNconc), 1.0);
                return Math.Min(1.0, factor);
            }
        }

        /// <summary>Gets or sets the lai dead.</summary>
        public double LAIDead { get; set; }


        /// <summary>Gets the cover dead.</summary>
        public double CoverDead { get { return 1.0 - Math.Exp(-KDead * LAIDead); } }

        /// <summary>Gets the RAD int tot.</summary>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot
        {
            get
            {
                if (MicroClimatePresent)
                {
                    double TotalRadn = 0;
                    for (int i = 0; i < LightProfile.Length; i++)
                        TotalRadn += LightProfile[i].amount;
                    return TotalRadn;
                }
                else
                    return CoverGreen * MetData.Radn;
            }
        }

        private bool LeafInitialised = false;
        #endregion

        #region Arbitrator Methods
        /// <summary>Gets or sets the water allocation.</summary>
        [XmlIgnore]
        public double WaterAllocation { get; set; }

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        public override BiomassSupplyType GetDryMatterSupply()
        {
            base.GetDryMatterSupply();   // get our base GenericOrgan to fill a supply structure first.
            DMSupply.Fixation = Photosynthesis.Value();
            return DMSupply;
        }

        #endregion

        #region Events
        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnSowing(object sender, SowPlant2Type data)
        {
            MicroClimatePresent = false;
            Clear();
        }

        /// <summary>Called when [phase changed].</summary>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == LeafInitialisationStage)
            {
                LeafInitialised = true;
            }
        }

        #endregion

        #region Component Process Functions

        /// <summary>Clears this instance.</summary>
        protected override void Clear()
        {
            base.Clear();
            Height = 0;
            LAI = 0;
            LeafInitialised = false;
        }
        #endregion

        #region Top Level time step functions
        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private new void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            base.OnDoPotentialPlantGrowth(sender, e);
            if (LeafInitialised)
            {
                if (MicroClimatePresent == false)
                    throw new Exception(this.Name + " is trying to calculate water demand but no MicroClimate module is present.  Include a microclimate node in your zone");

                FRGR = FRGRFunction.Value();
                if (CoverFunction == null && ExtinctionCoefficientFunction == null)
                    throw new Exception("\"CoverFunction\" or \"ExtinctionCoefficientFunction\" should be defined in " + this.Name);
                if (CoverFunction != null)
                    LAI = (Math.Log(1 - CoverGreen) / (ExtinctionCoefficientFunction.Value() * -1));
                if (LAIFunction != null)
                    LAI = LAIFunction.Value();

                Height = HeightFunction.Value();

                LAIDead = LaiDeadFunction.Value();

            }
        }

        #endregion

    }
}