﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyRegistrySystem.Models
{
    internal class FamilyMember
    {
        public string S_I { get; set; }
        public int MemberID { get; set; }
        public string HouseholdNumber { get; set; }
        public bool IsHead { get; set; }
        public string RowIndicator { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Relationship { get; set; }
        public DateTime Birthday { get; set; }
        public int Age { get; set; }
        public string Sex { get; set; }
        public string CivilStatus { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(HouseholdNumber) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(FirstName);
        }
    }
}
