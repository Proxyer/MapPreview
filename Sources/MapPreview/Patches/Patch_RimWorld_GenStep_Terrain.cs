using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Terrain))]
internal static class Patch_RimWorld_GenStep_Terrain
{
    internal static bool SkipRiverFlowCalc = false;

    [HarmonyPrefix]
    [HarmonyPatch("GenerateRiverLookupTexture")]
    private static bool GenerateRiverLookupTexture()
    {
        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        return !SkipRiverFlowCalc;
    }
}