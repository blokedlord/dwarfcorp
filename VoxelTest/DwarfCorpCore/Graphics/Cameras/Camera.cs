﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    [JsonObject(IsReference = true)]
    public class Camera
    {
        public Vector3 Target { get; set; }
        private Vector3 p = Vector3.Zero;

        private void setP(Vector3 v)
        {
            p = v;
        }

        public Vector3 Position
        {
            get { return p; }
            set { setP(value); }
        }

        public float FOV { get; set; }
        public float AspectRatio { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }
        public Vector3 UpVector { get; set; }
        public Matrix ViewMatrix { get; set; }
        public Matrix ProjectionMatrix { get; set; }
        public Vector3 Velocity { get; set; }
        private Vector3 lastPosition = Vector3.Zero;


        public enum ProjectionMode
        {
            Orthographic,
            Perspective
        }

        public ProjectionMode Projection { get; set; }
        public int LastWheel { get; set; }


        public Camera(Vector3 target, Vector3 position, float fov, float aspectRatio, float nearPlane, float farPlane)
        {
            UpVector = Vector3.Up;
            Target = target;
            Position = position;
            NearPlane = nearPlane;
            FarPlane = farPlane;
            AspectRatio = aspectRatio;
            FOV = fov;
            Velocity = Vector3.Zero;
            Projection = ProjectionMode.Perspective;
            UpdateViewMatrix();
            UpdateProjectionMatrix();
        }

        public Camera()
        {
  
        }


        public Vector3 Project(Vector3 pos)
        {
            return GameState.Game.Graphics.GraphicsDevice.Viewport.Project(pos, ProjectionMatrix, ViewMatrix, Matrix.Identity);
        }

        public Vector3 UnProject(Vector3 pos)
        {
            return GameState.Game.Graphics.GraphicsDevice.Viewport.Unproject(pos, ProjectionMatrix, ViewMatrix, Matrix.Identity);
        }


        public virtual void Update(DwarfTime time, ChunkManager chunks)
        {
            lastPosition = Position;
            UpdateViewMatrix();
        }

        public virtual void UpdateViewMatrix()
        {
            ViewMatrix = Matrix.CreateLookAt(Position, Target, UpVector);
        }

        public void UpdateProjectionMatrix()
        {
            if(Projection == ProjectionMode.Perspective)
            {
                ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(FOV, AspectRatio, NearPlane, FarPlane);
            }
            else
            {
                float height = (float) (30.0f * Math.Tan(FOV / 2));
                float width = (float) (AspectRatio * 30.0f * Math.Tan(FOV / 2));
                ProjectionMatrix = Matrix.CreateOrthographic(width, height, NearPlane, FarPlane);
            }
        }

        public bool IsInView(BoundingBox boundingSphere)
        {
            return GetFrustrum().Intersects(boundingSphere);
        }

        public BoundingFrustum GetFrustrum()
        {
            return new BoundingFrustum(ViewMatrix * ProjectionMatrix);
        }
    }

}