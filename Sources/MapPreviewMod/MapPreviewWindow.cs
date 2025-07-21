using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewWindow : Window
{
    public const int Timeout = 30 * 1000;

    public static MapPreviewWindow Instance => Find.WindowStack?.WindowOfType<MapPreviewWindow>();

    #if RW_1_6_OR_GREATER
    public static PlanetTile CurrentTile => WorldInterfaceManager.TileId;
    #else
    public static int CurrentTile => WorldInterfaceManager.TileId;
    #endif

    public Vector2 DefaultPos => new(UI.screenWidth - InitialSize.x - 50f, 105f);
    public override Vector2 InitialSize => new(MapPreviewMod.Settings.PreviewWindowSize, MapPreviewMod.Settings.PreviewWindowSize);
    protected override float Margin => 0f;

    private readonly MapPreviewWidgetWithPreloader _previewWidget = new(MapSizeUtility.MaxMapSize);

    public Map CurrentPreviewMap => _previewWidget.PreviewMap;

    public MapPreviewWindow()
    {
        layer = WindowLayer.SubSuper;
        closeOnCancel = false;
        doCloseButton = false;
        doCloseX = false;
        absorbInputAroundWindow = false;
        closeOnClickedOutside = false;
        preventCameraMotion = false;
        forcePause = false;
        resizeable = false;
        draggable = !MapPreviewMod.Settings.LockWindowPositions;
    }

    #if RW_1_6_OR_GREATER
    public void OnWorldTileSelected(World world, PlanetTile tileId, MapParent mapParent)
    #else
    public void OnWorldTileSelected(World world, int tileId, MapParent mapParent)
    #endif
    {
        MapPreviewGenerator.Instance.ClearQueue();

        if (!MapPreviewAPI.IsReadyForPreviewGen)
        {
            Close();
            return;
        }

        if (MapPreviewGenerator.CurrentRequest is { Pending: true } && MapPreviewGenerator.CurrentRequest.Timer.ElapsedMilliseconds > Timeout)
        {
            MapPreviewMod.Logger.Error("Map preview generation timed out! Stopping preview thread...");
            if (MapPreviewGenerator.Instance.Abort())
            {
                MapPreviewGenerator.Init();
            }
            else
            {
                Close();
                return;
            }
        }

        int seed = SeedRerollData.GetMapSeed(world, tileId);
        var mapSize = MapSizeUtility.DetermineMapSize(world, mapParent);

        float desiredSize = MapPreviewMod.Settings.PreviewWindowSize;
        float largeSide = Math.Max(mapSize.x, mapSize.z);
        float scale = desiredSize / largeSide;

        windowRect = new Rect(windowRect.x, windowRect.y, mapSize.x * scale, mapSize.z * scale);
        windowRect = windowRect.Rounded();

        var request = new MapPreviewRequest(seed, tileId, mapSize)
        {
            TextureSize = new IntVec2(_previewWidget.Texture.width, _previewWidget.Texture.height),
            GeneratorDef = mapParent?.MapGeneratorDef ?? MapGeneratorDefOf.Base_Player,
            UseTrueTerrainColors = MapPreviewMod.Settings.EnableTrueTerrainColors,
            #if !RW_1_6_OR_GREATER
            SkipRiverFlowCalc = MapPreviewMod.Settings.SkipRiverFlowCalc,
            #endif
            UseMinimalMapComponents = !MapPreviewMod.Settings.CompatibilityMode,
            ExistingBuffer = _previewWidget.Buffer
        };

        MapPreviewGenerator.Instance.QueuePreviewRequest(request);
        _previewWidget.Await(request);

        var pos = new Vector2((int) windowRect.x, (int) windowRect.y);
        if (pos != MapPreviewMod.Settings.PreviewWindowPos)
        {
            MapPreviewMod.Settings.PreviewWindowPos.Value = pos;
            MapPreviewMod.Settings.Write();
        }
    }

    public void ResetPositionAndSize()
    {
        windowRect.size = InitialSize;
        windowRect.position = DefaultPos;
    }

    public override void PreOpen()
    {
        base.PreOpen();

        if (Instance != this) Instance?.Close();

        MapPreviewGenerator.Init();

        Vector2 pos = MapPreviewMod.Settings.PreviewWindowPos;

        windowRect.x = pos.x >= 0 ? pos.x : DefaultPos.x;
        windowRect.y = pos.y >= 0 ? pos.y : DefaultPos.y;

        if (windowRect.x + windowRect.width > UI.screenWidth) windowRect.x = DefaultPos.x;
        if (windowRect.y + windowRect.height > UI.screenHeight) windowRect.y = DefaultPos.y;
    }

    public override void PreClose()
    {
        base.PreClose();

        _previewWidget.Dispose();

        MapPreviewGenerator.Instance.ClearQueue();

        var pos = new Vector2((int) windowRect.x, (int) windowRect.y);
        if (pos != MapPreviewMod.Settings.PreviewWindowPos)
        {
            MapPreviewMod.Settings.PreviewWindowPos.Value = pos;
            MapPreviewMod.Settings.Write();
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        _previewWidget.Draw(inRect.ContractedBy(5f));
    }
}
