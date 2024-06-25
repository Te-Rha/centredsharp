﻿using CentrED.Lights;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;

namespace CentrED.Map;

public class LightObject : MapObject
{
    public LightObject(StaticObject so)
    {
        var staticTile = so.StaticTile;
        int testX = staticTile.X + 1;
        int testY = staticTile.Y + 1;

        var tiles = Application.CEDGame.MapManager.StaticTiles[testX, testY];

        if (tiles != null && tiles.Count > 0) // This should work for all tiles to be initialized
        {
            var z5 = (sbyte)(staticTile.Z + 5);

            foreach (var o in tiles)
            {
                var td = TileDataLoader.Instance.StaticData[o.StaticTile.Id];
                if (!td.IsTransparent || !Application.CEDGame.MapManager.CanDrawStatic(o)) continue;

                if (o.Tile.Z < Application.CEDGame.MapManager.MaxZ && o.Tile.Z >= z5)
                {
                    return; // don't draw
                }
            }
        }
        
        var lightColor = 0;
        var isHued = false;
        byte lightId;
        
        var graphic = staticTile.Id;
        if (
            graphic >= 0x3E02 && graphic <= 0x3E0B
            || graphic >= 0x3914 && graphic <= 0x3929
            || graphic == 0x0B1D
        )
        {
            lightId = 2;
        }
        else
        {
            var tiledata = TileDataLoader.Instance.StaticData[staticTile.Id];
            lightId = tiledata.Layer;
        }
        if (lightId > 200)
        {
            lightColor = (ushort)(lightId - 200);
            lightId = 1;
        }
        if (LightColors.GetHue(staticTile.Id, out ushort color, out bool ishue))
        {
            lightColor = color;
            isHued = ishue;
        }
        if (lightId >= LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT)
        {
            return;
        }
        if (lightColor != 0)
        {
            lightColor++;
        }
        
        var spriteInfo = LightsManager.Instance.GetLight(lightId);
        if (spriteInfo.Texture == null)
        {
            return;
        }
        Texture = spriteInfo.Texture;
        TextureBounds = spriteInfo.UV;

        Vector4 hue = Vector4.Zero;
        hue.Z = 1f; //alpha
        hue.X = lightColor;
        hue.W = (int)(hue.X > 1.0f
            ? isHued
                ? HuesManager.HueMode.HUED
                : HuesManager.HueMode.LIGHT
            : HuesManager.HueMode.NONE);
        
        for (var i = 0; i < Vertices.Length; i++)
        {
            Vertices[i].Hue = hue;
        }
        
        var centerX = staticTile.X * TileObject.TILE_SIZE - so.TextureBounds.Height / 4f;
        var centerY = staticTile.Y * TileObject.TILE_SIZE - so.TextureBounds.Height / 4f;
        var posZ = staticTile.Z * TileObject.TILE_Z_SCALE; //Handle FlatView
        var sqrt2 = (float)Math.Sqrt(2);

        
        //It should be centered in the center of static tile graphic
        Vertices[0].Position = new Vector3(centerX - TextureBounds.Width / 2f * sqrt2, centerY, posZ);
        Vertices[1].Position = new Vector3(centerX, centerY - TextureBounds.Height / 2f * sqrt2, posZ);
        Vertices[2].Position = new Vector3(centerX, centerY + TextureBounds.Height / 2f * sqrt2, posZ);
        Vertices[3].Position = new Vector3(centerX + TextureBounds.Width / 2f * sqrt2, centerY , posZ);
        
        float onePixel = Math.Max(1.0f / Texture.Width, Epsilon.value);
        var texX = TextureBounds.X / (float)Texture.Width + onePixel / 2f;
        var texY = TextureBounds.Y / (float)Texture.Height + onePixel / 2f;
        var texWidth = TextureBounds.Width / (float)Texture.Width - onePixel;
        var texHeight = TextureBounds.Height / (float)Texture.Height - onePixel;

        Vertices[0].Texture = new Vector3(texX, texY, 0f);
        Vertices[1].Texture = new Vector3(texX + texWidth, texY, 0f);
        Vertices[2].Texture = new Vector3(texX, texY + texHeight, 0f);
        Vertices[3].Texture = new Vector3(texX + texWidth, texY + texHeight, 0f);

        Valid = true;
    }
}