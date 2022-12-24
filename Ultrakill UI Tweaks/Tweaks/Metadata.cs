using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ULTRAKILLtweaker.Tweaks
{
    public class Metadata
    {
        public string Name = "Placeholder Tweak";
        public string Description = "This is a placeholder for descriptions.";
        public bool DescriptionEnabled = false;
        public bool RequiresRestart;

        public Metadata(string Name, string Description = "", bool RequiresRestart = false)
        {
            this.Name = Name;
            this.Description = Description;
            this.RequiresRestart = RequiresRestart;

            if (Description.Length > 0)
            {
                DescriptionEnabled = true;
            }
        }
    }
}
