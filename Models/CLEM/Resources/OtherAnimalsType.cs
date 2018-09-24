﻿using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for bank account
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OtherAnimals))]
    [Description("This resource represents an other animal group (e.g. Chickens).")]
    public class OtherAnimalsType : CLEMModel, IResourceWithTransactionType
    {
        /// <summary>
        /// Current cohorts of this Other Animal Type.
        /// </summary>
        [XmlIgnore]
        public List<OtherAnimalsTypeCohort> Cohorts;

        /// <summary>
        /// The last group of individuals to be added or removed (for reporting)
        /// </summary>
        [XmlIgnore]
        public OtherAnimalsTypeCohort LastCohortChanged { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            Initialise();
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (Cohorts != null)
            {
                Cohorts.Clear();
            }
            Cohorts = null;
        }

        /// <summary>
        /// Age when individuals become adults for feeding and breeding rates
        /// </summary>
        [Description("Age when adult (months)")]
        [Required]
        public double AgeWhenAdult { get; set; }

        /// <summary>
        /// Age when individuals die
        /// </summary>
        [Description("Maximum age before death (months)")]
        [Required]
        public double MaxAge { get; set; }

        /// <summary>
        /// Initialise resource type
        /// </summary>
        public void Initialise()
        {
            Cohorts = new List<OtherAnimalsTypeCohort>();
            foreach (var child in this.Children)
            {
                if (child is OtherAnimalsTypeCohort)
                {
                    ((OtherAnimalsTypeCohort)child).SaleFlag = HerdChangeReason.InitialHerd;
                    Add(child, "Setup", this.Name);
                }
            }
        }

        #region Transactions

        /// <summary>
        /// Last transaction received
        /// </summary>
        [XmlIgnore]
        public ResourceTransaction LastTransaction { get; set; }

        /// <summary>
        /// Override base event
        /// </summary>
        protected void OnTransactionOccurred(EventArgs e)
        {
            EventHandler invoker = TransactionOccurred;
            if (invoker != null) invoker(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public event EventHandler TransactionOccurred;

        /// <summary>
        /// Add individuals to type based on cohort
        /// </summary>
        /// <param name="AddIndividuals"></param>
        /// <param name="ActivityName"></param>
        /// <param name="Reason"></param>
        public void Add(object AddIndividuals, string ActivityName, string Reason)
        {
            OtherAnimalsTypeCohort cohortToAdd = AddIndividuals as OtherAnimalsTypeCohort;

            OtherAnimalsTypeCohort cohortexists = Cohorts.Where(a => a.Age == cohortToAdd.Age & a.Gender == cohortToAdd.Gender).FirstOrDefault();

            if (cohortexists == null)
            {
                // add new
                Cohorts.Add(cohortToAdd);
            }
            else
            {
                cohortexists.Number += cohortToAdd.Number;
            }

            LastCohortChanged = cohortToAdd;
            ResourceTransaction details = new ResourceTransaction();
            details.Credit = cohortToAdd.Number;
            details.Activity = ActivityName;
            details.Reason = Reason;
            details.ResourceType = this.Name;
            details.ExtraInformation = cohortToAdd;
            LastTransaction = details;
            TransactionEventArgs eargs = new TransactionEventArgs();
            eargs.Transaction = LastTransaction;
            OnTransactionOccurred(eargs);
        }

        /// <summary>
        /// Remove individuals from type based on cohort
        /// </summary>
        /// <param name="RemoveRequest"></param>
        public void Remove(object RemoveRequest)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove individuals from type based on cohort
        /// </summary>
        /// <param name="RemoveIndividuals"></param>
        /// <param name="ActivityName"></param>
        /// <param name="Reason"></param>
        public void Remove(object RemoveIndividuals, string ActivityName, string Reason)
        {
            OtherAnimalsTypeCohort cohortToRemove = RemoveIndividuals as OtherAnimalsTypeCohort;
            OtherAnimalsTypeCohort cohortexists = Cohorts.Where(a => a.Age == cohortToRemove.Age & a.Gender == cohortToRemove.Gender).First();

            if (cohortexists == null)
            {
                // tried to remove individuals that do not exist
                throw new Exception("Tried to remove individuals from "+this.Name+" that do not exist");
            }
            else
            {
                cohortexists.Number -= cohortToRemove.Number;
                cohortexists.Number = Math.Max(0, cohortexists.Number);
            }

            LastCohortChanged = cohortToRemove;
            ResourceTransaction details = new ResourceTransaction();
            details.Debit = cohortToRemove.Number * -1;
            details.Activity = ActivityName;
            details.Reason = Reason;
            details.ResourceType = this.Name;
            details.ExtraInformation = cohortToRemove;
            LastTransaction = details;
            TransactionEventArgs eargs = new TransactionEventArgs();
            eargs.Transaction = LastTransaction;
            OnTransactionOccurred(eargs);
        }

        /// <summary>
        /// Set the amount in an account.
        /// </summary>
        /// <param name="NewAmount"></param>
        public void Set(double NewAmount)
        {
        }

        #endregion

    }
}
