﻿// EnemySensor.cs
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

using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Component which fires when an enemy creature enters a box. Attached to other components.
    ///     REQUIRES that the EnemySensor be attached to a creature
    /// </summary>
    [JsonObject(IsReference = true)]
    public class EnemySensor : Body
    {
        public delegate void EnemySensed(List<CreatureAI> enemies);

        public EnemySensor()
        {
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            SenseTimer = new Timer(0.5f, false);
            SenseRadius = 15*15;
        }

        public EnemySensor(ComponentManager manager, string name, GameComponent parent, Matrix localTransform,
            Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
                base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            Tags.Add("Sensor");
            SenseTimer = new Timer(0.5f, false);
            SenseRadius = 15*15;
        }

        public Faction Allies { get; set; }
        public CreatureAI Creature { get; set; }
        public List<CreatureAI> Enemies { get; set; }
        public Timer SenseTimer { get; set; }
        public float SenseRadius { get; set; }
        public event EnemySensed OnEnemySensed;


        public void Sense()
        {
            if (Allies == null && Creature != null)
            {
                Allies = Creature.Faction;
            }

            var sensed = new List<CreatureAI>();
            var collide = new List<CreatureAI>();
            foreach (var faction in PlayState.ComponentManager.Factions.Factions)
            {
                if (PlayState.ComponentManager.Diplomacy.GetPolitics(Allies, faction.Value).GetCurrentRelationship() !=
                    Relationship.Hateful) continue;

                foreach (CreatureAI minion in faction.Value.Minions)
                {
                    if (!minion.IsActive) continue;

                    if (Creature != null && minion.Sensor.Enemies.Contains(Creature))
                    {
                        sensed.Add(minion);
                        continue;
                    }

                    float dist = (minion.Position - GlobalTransform.Translation).LengthSquared();

                    if (dist < SenseRadius && !PlayState.ChunkManager.ChunkData.CheckRaySolid(Position, minion.Position))
                    {
                        sensed.Add(minion);
                    }

                    if (dist < 1.0f)
                    {
                        collide.Add(minion);
                    }
                }
            }


            if (sensed.Count > 0)
            {
                OnEnemySensed.Invoke(sensed);
            }
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            SenseTimer.Update(gameTime);

            if (SenseTimer.HasTriggered)
            {
                Sense();
            }
            Enemies.RemoveAll(ai => ai.IsDead);

            base.Update(gameTime, chunks, camera);
        }

        private void EnemySensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            Enemies = enemies;
        }
    }
}