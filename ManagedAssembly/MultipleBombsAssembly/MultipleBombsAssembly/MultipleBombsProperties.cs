using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultipleBombsAssembly
{
    public class MultipleBombsProperties : PropertiesBehaviour
    {
        internal MultipleBombs MultipleBombs { get; set; }

        public MultipleBombsProperties()
        {
            AddProperty("CurrentMaximumBombCount", () => MultipleBombs.GetCurrentMaximumBombCount(), null);
            AddProperty("CurrentFreePlayBombCount", () => { return MultipleBombs.CurrentFreePlayBombCount; }, (object value) => { MultipleBombs.CurrentFreePlayBombCount = (int)value; });
        }
    }
}
