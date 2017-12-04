using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultipleBombsAssembly
{
    public class MultipleBombsProperties : PropertiesBehaviour
    {
        private MultipleBombs multipleBombs;

        public MultipleBombsProperties()
        {
            AddProperty("CurrentMaximumBombCount", new Property(CurrentMaximumBombCount_Get, null));
        }

        private object CurrentMaximumBombCount_Get()
        {
            return multipleBombs.GetCurrentMaximumBombCount();
        }

        internal MultipleBombs MultipleBombs
        {
            get
            {
                return multipleBombs;
            }
            set
            {
                multipleBombs = value;
            }
        }
    }
}
