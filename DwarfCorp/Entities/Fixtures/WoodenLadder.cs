using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class WoodenLadder : CraftedFixture
    {
        [EntityFactory("Wooden Ladder")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new WoodenLadder(
                Manager,
                Position,
                Data.GetData<Resource>("Resource", null));
        }

        public WoodenLadder()
        {

        }

        public WoodenLadder(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2,0), 
                new CraftDetails(manager, Resource))
        {
            this.LocalBoundingBoxOffset = new Vector3(0, 0, 0.45f);
            this.BoundingBoxSize = new Vector3(0.7f, 1, 0.1f);
            this.SetFlag(Flag.RotateBoundingBox, true);

            Name = "Wooden Ladder";
            Tags.Add("Climbable");
            OrientToWalls();
            CollisionType = CollisionType.Static;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            if (GetComponent<SimpleSprite>().HasValue(out var sprite))
            {
                sprite.OrientationType = SimpleSprite.OrientMode.Fixed;
                sprite.LocalTransform = Matrix.CreateTranslation(new Vector3(0, 0, 0.45f)) * Matrix.CreateRotationY(0.0f);
            }

            if (GetComponent<GenericVoxelListener>().HasValue(out var sensor))
            {
                sensor.LocalBoundingBoxOffset = new Vector3(0.0f, 0.0f, 1.0f);
                sensor.SetFlag(Flag.RotateBoundingBox, true);
                sensor.PropogateTransforms();
            }

            AddChild(new Flammable(manager, "Flammable"));
        }
    }
}