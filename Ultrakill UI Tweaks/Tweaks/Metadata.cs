using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker.Tweaks
{
    public class Metadata
    {
        public string Name = "Placeholder Tweak";
        public string Description = "This is a placeholder for descriptions.";
        public Sprite Image;
        public bool DescriptionEnabled = false;
        public bool RequiresRestart;

        public Metadata(string Name, string Description = "", bool RequiresRestart = false, Sprite Image = null)
        {
            this.Name = Name;
            this.Description = Description;
            this.RequiresRestart = RequiresRestart;
            this.Image = Image;

            if (Description.Length > 0)
            {
                DescriptionEnabled = true;
            }
        }
    }
}
