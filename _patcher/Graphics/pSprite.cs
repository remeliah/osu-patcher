using _patcher.Constants;
using _patcher.Graphics.Skinning;
using _patcher.Graphics.Sprites;
using _patcher.Helpers;
using _patcher.Wrappers;
using System;
using System.Reflection;

namespace _patcher.Graphics
{
    internal class pSprite : pDrawable
    {
        private static readonly ConstructorInfo BaseSprite = ILPatch.FindConstructorBySignature(Patterns.Sprite_Constructor);

        internal pSprite(object texture, Fields fieldType, Origins origin, Clocks clock, float posX, float posY, float drawDepth, bool alwaysDraw, Color colour, object tag = null)
        {
            Instance = CreateSpriteInstance(texture, fieldType, origin, clock, posX, posY, drawDepth, alwaysDraw, colour, tag);
        }

        internal pSprite(string spriteName, float posX, float posY, SkinSource source = SkinSource.All, Origins origin = Origins.Centre, Fields field = Fields.TopLeft)
            : this(TextureManager.Load(spriteName, source), field, origin, Clocks.Game, posX, posY, 1, true, Color.White)
        {
        }

        internal pSprite(object texture, Fields fieldType, Origins origin, Clocks clock, float posX, float posY)
            : this(texture, fieldType, origin, clock, posX, posY, 1, false, Color.White)
        {
        }

        internal pSprite(object texture, Origins origin, float posX, float posY, float drawDepth, bool alwaysDraw, Color colour)
            : this(texture, Fields.TopLeft, origin, Clocks.Game, posX, posY, drawDepth, alwaysDraw, colour, null)
        {
        }

        internal pSprite(object texture, float posX, float posY, float drawDepth, bool alwaysDraw, Color colour)
            : this(texture, Fields.TopLeft, Origins.TopLeft, Clocks.Game, posX, posY, drawDepth, alwaysDraw, colour, null)
        {
        }

        private object CreateSpriteInstance(object texture, Fields fieldType, Origins origin, Clocks clock, float posX, float posY, float drawDepth, bool alwaysDraw, Color colour, object tag)
        {
            object startPosition = Xna.CreateVector2(posX, posY);
            object xnaColor = colour.ToXnaColor();
            var parameters = BaseSprite.GetParameters();

            return BaseSprite.Invoke(new object[]
            {
                texture,
                Enum.ToObject(parameters[1].ParameterType, fieldType),
                Enum.ToObject(parameters[2].ParameterType, origin),
                Enum.ToObject(parameters[3].ParameterType, clock),
                startPosition,
                drawDepth,
                alwaysDraw,
                xnaColor,
                tag
            });
        }
    }
}