using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
    public class FlexibleSprinklers: Mod
    {
        public static FlexibleSprinklers Instance { get; private set; } = null!;

        public bool SkipVanillaBehavior { get; private set; } = false;
        public ISprinklerBehavior SprinklerBehavior { get; private set; } = new FlexibleSprinklerBehavior(FlexibleSprinklerBehavior.TileWaterBalanceMode.Restrictive);

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            var harmony = new Harmony(ModManifest.UniqueID);
            ObjectPatches.Apply(harmony);
        }

        public SprinklerInfo GetSprinklerInfo(Object sprinkler)
        {
            // TODO: handle modded sprinklers with custom shapes

            if (!sprinkler.IsSprinkler())
                return new SprinklerInfo { Layout = new HashSet<IntPoint>() };
            return new SprinklerInfo { Layout = SprinklerLayouts.Vanilla(sprinkler.GetModifiedRadiusForSprinkler() + 1) };
        }
    }
}