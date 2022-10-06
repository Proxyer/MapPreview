using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldInterface))]
internal class Patch_RimWorld_WorldInterface
{
    private static int _tileId = -1;
    private static bool _openedPreviewSinceEnteringMap;

    static Patch_RimWorld_WorldInterface() => MapPreviewAPI.OnWorldChanged += Refresh;
    
    [HarmonyPostfix]
    [HarmonyPatch("WorldInterfaceUpdate")]
    private static void WorldInterfaceUpdate(WorldInterface __instance)
    {
        if (!WorldRendererUtility.WorldRenderedNow)
        {
            if (_openedPreviewSinceEnteringMap && !MapPreviewAPI.IsGeneratingPreview)
            {
                MapPreviewWindow.Instance?.Close();
                _openedPreviewSinceEnteringMap = false;
                _tileId = -1;
            }
            return;
        }
        
        if (_tileId != __instance.SelectedTile)
        {
            _tileId = __instance.SelectedTile;

            if (_tileId != -1)
            {
                var tile = Find.World.grid[_tileId];
                if (!tile.biome.impassable && (tile.hilliness != Hilliness.Impassable || TileFinder.IsValidTileForNewSettlement(_tileId)))
                {
                    if (!MapPreviewMod.Settings.EnableMapPreview || !MapPreviewAPI.IsReady) return;
                    var window = MapPreviewWindow.Instance;
                    if (window == null) Find.WindowStack.Add(window = new MapPreviewWindow());
                    window.OnWorldTileSelected(Find.World, _tileId);
                    _openedPreviewSinceEnteringMap = true;
                    return;
                }
            }
            
            MapPreviewWindow.Instance?.Close();
        }
    }

    public static void Refresh()
    {
        _tileId = -1;
        if (!MapPreviewMod.Settings.EnableMapPreview) MapPreviewWindow.Instance?.Close();
    }
}