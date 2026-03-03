using _patcher.Graphics.Sprites;
using _patcher.Wrappers;
using System;

namespace _patcher.Graphics
{
    internal class pSpriteUnloadable : pSprite
    {
        internal delegate void VoidDelegate();
        internal event VoidDelegate OnUnload;

        internal pSpriteUnloadable(object texture, Fields fieldType, Origins origin, Clocks clock, float posX, float posY, float drawDepth, bool alwaysDraw, Color colour, object tag = null)
            : base(texture, fieldType, origin, clock, posX, posY, drawDepth, alwaysDraw, colour, tag)
        {
        }

        internal pSpriteUnloadable(object texture, Fields fieldType, Origins origin, Clocks clock, float posX, float posY)
            : base(texture, fieldType, origin, clock, posX, posY)
        {
        }

        internal pSpriteUnloadable(object texture, Origins origin, float posX, float posY, float drawDepth, bool alwaysDraw, Color colour)
            : base(texture, origin, posX, posY, drawDepth, alwaysDraw, colour)
        {
        }

        internal pSpriteUnloadable(object texture, float posX, float posY, float drawDepth, bool alwaysDraw, Color colour)
            : base(texture, posX, posY, drawDepth, alwaysDraw, colour)
        {
        }
    }
}