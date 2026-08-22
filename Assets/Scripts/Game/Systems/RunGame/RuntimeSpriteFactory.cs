using UnityEngine;

namespace Game.Systems
{
    internal static class RuntimeSpriteFactory
    {
        private static Sprite whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null)
                {
                    return whiteSprite;
                }

                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.name = "RuntimeWhitePixel";
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, true);

                whiteSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                whiteSprite.name = "RuntimeWhiteSprite";
                return whiteSprite;
            }
        }
    }
}
