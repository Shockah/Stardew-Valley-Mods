using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
    public struct SprinklerInfo
    {
        public ISet<IntPoint> Layout { get; set; }

        public int Power
        {
            get
            {
                return Layout.Count;
            }
        }
    }
}