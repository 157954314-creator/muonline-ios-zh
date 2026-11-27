using Client.Main.Content;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Threading.Tasks;

namespace Client.Main.Objects
{
    public class MapTileObject : ModelObject
    {
        private new ILogger _logger = AppLoggerFactory?.CreateLogger<MapTileObject>();

        public MapTileObject()
        {
            // MU .OZT textures and model shaders output straight (non-premultiplied) RGBA.
            // Using BlendState.AlphaBlend (Src=One) causes A==0 and soft-edge pixels (e.g. World75 so_grass.OZT)
            // to add 100% of their RGB as bright glowing rectangles.
            BlendState = BlendState.NonPremultiplied;
            RenderShadow = false;
        }

        public override async Task Load()
        {
            var modelPath = Path.Join($"Object{World.WorldIndex}", $"Object{(Type + 1).ToString().PadLeft(2, '0')}.bmd");

            Model = await BMDLoader.Instance.Prepare(modelPath);

            if (Model == null)
                _logger?.LogDebug($"Can't load MapObject for model: {modelPath}");

            await base.Load();
        }
    }
}
