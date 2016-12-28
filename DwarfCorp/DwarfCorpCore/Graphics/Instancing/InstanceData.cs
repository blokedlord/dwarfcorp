﻿using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    ///     An instance data represents a single instantiation of an object model
    ///     at a given location, with a given color.
    /// </summary>
    public class InstanceData
    {
        private static uint maxID;

        public InstanceData(Matrix world, Color colour, bool shouldDraw)
        {
            Transform = world;
            Color = colour;
            ID = maxID;
            maxID++;
            ShouldDraw = shouldDraw;
            Depth = 0.0f;
        }

        public Matrix Transform { get; set; }
        public Color Color { get; set; }
        public uint ID { get; set; }
        public bool ShouldDraw { get; set; }
        public float Depth { get; set; }
    }
}