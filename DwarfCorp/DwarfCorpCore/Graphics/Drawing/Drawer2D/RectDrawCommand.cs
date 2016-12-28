// RectDrawCommand.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     Draws a rectangle to the screen.
    /// </summary>
    public class RectDrawCommand : DrawCommand2D
    {
        public RectDrawCommand(Color fill, Color stroke, float strokeWeight, Rectangle bounds)
        {
            Bounds = bounds;
            FillColor = fill;
            StrokeColor = stroke;
            StrokeWeight = strokeWeight;
        }

        public Color FillColor { get; set; }
        public Color StrokeColor { get; set; }
        public float StrokeWeight { get; set; }
        public Rectangle Bounds { get; set; }


        public override void Render(SpriteBatch batch, Camera camera, Viewport viewport)
        {
            Drawer2D.FillRect(batch, Bounds, FillColor);
            if (StrokeColor != Color.Transparent)
                Drawer2D.DrawRect(batch, Bounds, StrokeColor, StrokeWeight);
        }
    }
}