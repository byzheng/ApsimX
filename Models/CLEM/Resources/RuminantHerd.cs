﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;

namespace Models.CLEM.Resources
{

    ///<summary>
    /// Parent model of Ruminant Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all rumiant types (herds or breeds) for the simulation.")]
    public class RuminantHerd: ResourceBaseWithTransactions
    {
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// Current state of this resource.
        /// </summary>
        [XmlIgnore]
        public List<Ruminant> Herd;

        /// <summary>
        /// List of requested purchases.
        /// </summary>
        [XmlIgnore]
        public List<Ruminant> PurchaseIndividuals;

        /// <summary>
        /// The last individual to be added or removed (for reporting)
        /// </summary>
        [XmlIgnore]
        public object LastIndividualChanged { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            id = 1;
            Herd = new List<Ruminant>();
            PurchaseIndividuals = new List<Ruminant>();
            LastIndividualChanged = new Ruminant();

            // for each Ruminant type 
            foreach (RuminantType rType in Apsim.Children(this, typeof(RuminantType)))
            {
                foreach (RuminantInitialCohorts ruminantCohorts in Apsim.Children(rType, typeof(RuminantInitialCohorts)))
                {
                    foreach (var ind in ruminantCohorts.CreateIndividuals())
                    {
                        ind.SaleFlag = HerdChangeReason.InitialHerd;
                        AddRuminant(ind);
                    }
                }
            }

            // Assign mothers to suckling calves
            foreach (string HerdName in Herd.Select(a => a.HerdName).Distinct())
            {
                List<Ruminant> herd = Herd.Where(a => a.HerdName == HerdName).ToList();

                // get list of females of breeding age and condition
                List<RuminantFemale> breedFemales = herd.Where(a => a.Gender == Sex.Female & a.Age >= a.BreedParams.MinimumAge1stMating + a.BreedParams.GestationLength & a.Weight >= (a.BreedParams.MinimumSize1stMating * a.StandardReferenceWeight) & a.Weight >= (a.BreedParams.CriticalCowWeight * a.StandardReferenceWeight)).OrderByDescending(a => a.Age).ToList().Cast<RuminantFemale>().ToList();

                // get list of all sucking individuals
                List<Ruminant> sucklingList = herd.Where(a => a.Weaned == false).ToList();

                if (breedFemales.Count() == 0)
                {
                    if (sucklingList.Count > 0)
                    {
                        Summary.WriteWarning(this, String.Format("Insufficient breeding females to assign ({0}) sucklings for herd ({1})", sucklingList.Count, HerdName));
                    }
                }
                else
                {
                    // gestation interval at smallest size generalised curve
                    double minAnimalWeight = breedFemales[0].StandardReferenceWeight - ((1 - breedFemales[0].BreedParams.SRWBirth) * breedFemales[0].StandardReferenceWeight) * Math.Exp(-(breedFemales[0].BreedParams.AgeGrowthRateCoefficient * (breedFemales[0].BreedParams.MinimumAge1stMating * 30.4)) / (Math.Pow(breedFemales[0].StandardReferenceWeight, breedFemales[0].BreedParams.SRWGrowthScalar)));
                    double IPIminsize = Math.Pow(breedFemales[0].BreedParams.InterParturitionIntervalIntercept * (minAnimalWeight / breedFemales[0].StandardReferenceWeight), breedFemales[0].BreedParams.InterParturitionIntervalCoefficient) * 30.64;
                    // restrict minimum period between births
                    IPIminsize = Math.Max(IPIminsize, breedFemales[0].BreedParams.GestationLength + 61);

                    // assign calves to cows
                    int sucklingCount = 0;
                    foreach (var suckling in sucklingList)
                    {
                        sucklingCount++;
                        if (breedFemales.Count > 0)
                        {
                            breedFemales[0].DryBreeder = false;

                            //Initialise female milk production in at birth so ready for sucklings to consume
                            double milkTime = 15; // equivalent to mid month production

                            // need to calculate normalised animal weight here for milk production
                            breedFemales[0].NormalisedAnimalWeight = breedFemales[0].StandardReferenceWeight - ((1 - breedFemales[0].BreedParams.SRWBirth) * breedFemales[0].StandardReferenceWeight) * Math.Exp(-(breedFemales[0].BreedParams.AgeGrowthRateCoefficient * (breedFemales[0].Age * 30.4)) / (Math.Pow(breedFemales[0].StandardReferenceWeight, breedFemales[0].BreedParams.SRWGrowthScalar)));
                            double milkProduction = breedFemales[0].BreedParams.MilkPeakYield * breedFemales[0].Weight / breedFemales[0].NormalisedAnimalWeight * (Math.Pow(((milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay), breedFemales[0].BreedParams.MilkCurveSuckling)) * Math.Exp(breedFemales[0].BreedParams.MilkCurveSuckling * (1 - (milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay));
                            breedFemales[0].MilkProduction = Math.Max(milkProduction, 0.0);
                            breedFemales[0].MilkAmount = milkProduction * 30.4;

                            // generalised curve
                            double IPIcurrent = Math.Pow(breedFemales[0].BreedParams.InterParturitionIntervalIntercept * (breedFemales[0].Weight / breedFemales[0].StandardReferenceWeight), breedFemales[0].BreedParams.InterParturitionIntervalCoefficient) * 30.64;
                            // restrict minimum period between births
                            IPIcurrent = Math.Max(IPIcurrent, breedFemales[0].BreedParams.GestationLength + 61);

                            breedFemales[0].NumberOfBirths = Convert.ToInt32((breedFemales[0].Age - suckling.Age - breedFemales[0].BreedParams.GestationLength - breedFemales[0].BreedParams.MinimumAge1stMating) / ((IPIcurrent + IPIminsize) / 2));

                            //breedFemales[0].Parity = breedFemales[0].Age - suckling.Age - 9;
                            // I removed the -9 as this would make it conception month not birth month
                            breedFemales[0].AgeAtLastBirth = breedFemales[0].Age - suckling.Age;
                            breedFemales[0].AgeAtLastConception = breedFemales[0].AgeAtLastBirth - breedFemales[0].BreedParams.GestationLength;
                            breedFemales[0].SuccessfulPregnancy = true;

                            // suckling mother set
                            suckling.Mother = breedFemales[0];
                            // add suckling to suckling offspring of mother.
                            suckling.Mother.SucklingOffspring.Add(suckling);

                            // check if a twin and if so apply next individual to same mother.
                            // otherwise remove this mother from the list
                            if (ZoneCLEM.RandomGenerator.NextDouble() >= breedFemales[0].BreedParams.TwinRate)
                            {
                                breedFemales.RemoveAt(0);
                            }
                        }
                        else
                        {
                            Summary.WriteWarning(this, String.Format("Insufficient breeding females to assign ({0}) sucklings for herd ({1})", sucklingList.Count - sucklingCount, HerdName));
                        }
                    }

                    // assigning values for the remaining females who haven't just bred.
                    foreach (var female in breedFemales)
                    {
                        female.DryBreeder = true;
                        // generalised curve
                        double IPIcurrent = Math.Pow(breedFemales[0].BreedParams.InterParturitionIntervalIntercept * (breedFemales[0].Weight / breedFemales[0].StandardReferenceWeight), breedFemales[0].BreedParams.InterParturitionIntervalCoefficient) * 30.64;
                        // restrict minimum period between births
                        IPIcurrent = Math.Max(IPIcurrent, breedFemales[0].BreedParams.GestationLength + 61);
                        breedFemales[0].NumberOfBirths = Convert.ToInt32((breedFemales[0].Age - breedFemales[0].BreedParams.MinimumAge1stMating) / ((IPIcurrent + IPIminsize) / 2)) - 1;
                        female.AgeAtLastBirth = breedFemales[0].Age - 12;
                    }
                }
            }

            //List<IModel> childNodes = Apsim.Children(this, typeof(IModel));

            //foreach (IModel childModel in childNodes)
            //{
            //    //cast the generic IModel to a specfic model.
            //    RuminantType ruminantType = childModel as RuminantType;
            //    foreach (var ind in ruminantType.CreateIndividuals())
            //    {
            //        ind.SaleFlag = HerdChangeReason.InitialHerd;
            //        AddRuminant(ind);
            //    }
            //}
        }

        /// <summary>
        /// Add individual/cohort to the the herd
        /// </summary>
        /// <param name="ind">Individual Ruminant to add</param>
        public void AddRuminant(Ruminant ind)
        {
            if (ind.ID == 0)
            {
                ind.ID = this.NextUniqueID;
            }
            Herd.Add(ind);
            LastIndividualChanged = ind;

            ResourceTransaction details = new ResourceTransaction();
            details.Credit = 1;
            details.Activity = "Unknown";
            details.Reason = "Unknown";
            details.ResourceType = this.Name;
            details.ExtraInformation = ind;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);

            // remove change flag
            ind.SaleFlag = HerdChangeReason.None;
        }

        /// <summary>
        /// Remove individual/cohort from the herd
        /// </summary>
        /// <param name="ind">Individual Ruminant to remove</param>
        public void RemoveRuminant(Ruminant ind)
        {
            // Remove mother ID from any suckling offspring
            if (ind.Gender == Sex.Female)
            {
                foreach (var offspring in (ind as RuminantFemale).SucklingOffspring)
                {
                    offspring.Mother = null;
                }
            }
            Herd.Remove(ind);
            LastIndividualChanged = ind;

            ResourceTransaction details = new ResourceTransaction();
            details.Debit = -1;
            details.Activity = "Unknown";
            details.Reason = "Unknown";
            details.ResourceType = this.Name;
            details.ExtraInformation = ind;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);

            // remove change flag
            ind.SaleFlag = HerdChangeReason.None;
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (Herd != null)
            {
                Herd.Clear();
            }
            Herd = null;
            if (PurchaseIndividuals != null)
            {
                PurchaseIndividuals.Clear();
            }
            PurchaseIndividuals = null;
        }

        /// <summary>
        /// Remove list of Ruminants from the herd
        /// </summary>
        /// <param name="list">List of Ruminants to remove</param>
        public void RemoveRuminant(List<Ruminant> list)
        {
            foreach (var ind in list)
            {
                // report removal
                RemoveRuminant(ind);
            }
        }

        /// <summary>
        /// Gte the next unique individual id number
        /// </summary>
        public int NextUniqueID { get { return id++; } }
        private int id = 1;

        #region Transactions

        // Must be included away from base class so that APSIM Event.Subscriber can find them 

        /// <summary>
        /// Override base event
        /// </summary>
        protected new void OnTransactionOccurred(EventArgs e)
        {
            EventHandler invoker = TransactionOccurred;
            if (invoker != null) invoker(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public new event EventHandler TransactionOccurred;

        private void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            LastTransaction = (e as TransactionEventArgs).Transaction;
            OnTransactionOccurred(e);
        }

        #endregion

    }
}
