using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyRegistrySystem.Models
{
    internal class Household
    {
        public int HouseholdNumber { get; set; }
        public int BarangayID { get; set; }
        public DateTime DateRegistered { get; set; }
    }
}
