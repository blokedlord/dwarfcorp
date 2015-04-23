﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds a path from point A to point B and fills the blackboard with
    /// this information.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PlanAct : CreatureAct
    {
        public Timer PlannerTimer { get; set; }
        public int MaxExpansions { get; set; }

        public string PathOut { get; set; }

        public string TargetName { get; set; }

        public List<Creature.MoveAction> Path { get { return GetPath(); } set {  SetPath(value);} }
        public Voxel Target { get { return GetTarget(); } set {  SetTarget(value);} }

        public PlanSubscriber PlanSubscriber { get; set; }

        public int MaxTimeouts { get; set; }

        public int Timeouts { get; set; }

        private bool WaitingOnResponse { get; set; }

        public float Radius { get; set; }

        public enum PlanType
        {
            Adjacent,
            Into,
            Radius
        }


        public PlanType Type { get; set; }

        public PlanAct()
        {

        }

        public PlanAct(CreatureAI agent, string pathOut, string target, PlanType planType) :
            base(agent)
        {
            Type = planType;
            Name = "Plan to " + target;
            PlannerTimer = new Timer(1.0f, false);
            MaxExpansions = 1000;
            PathOut = pathOut;
            TargetName = target;
            PlanSubscriber = new PlanSubscriber(PlayState.PlanService);
            WaitingOnResponse = false;
            MaxTimeouts = 4;
            Timeouts = 0;
            Radius = 0;
        }

        public Voxel GetTarget()
        {
            return Agent.Blackboard.GetData<Voxel>(TargetName);
        }

        public void SetTarget(Voxel target)
        {
            Agent.Blackboard.SetData(TargetName, target);
        }

        public List<Creature.MoveAction> GetPath()
        {
            return Agent.Blackboard.GetData<List<Creature.MoveAction>>(PathOut);
        }

        public void SetPath(List<Creature.MoveAction> path)
        {
            Agent.Blackboard.SetData(PathOut, path);
        }

        public override IEnumerable<Status> Run()
        {
            Path = null;
            Timeouts = 0;
            PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);
            Voxel voxUnder = new Voxel();
            Voxel voxAbove = new Voxel();
            while(true)
            {
                if (Path != null)
                {
                    yield return Status.Success;
                    break;
                }

                if(Timeouts > MaxTimeouts)
                {
                    yield return Status.Fail;
                    break;
                }

                PlannerTimer.Update(DwarfTime.LastTime);

                ChunkManager chunks = PlayState.ChunkManager;
                if(PlannerTimer.HasTriggered || Timeouts == 0)
                {

                    if (!chunks.ChunkData.GetFirstVoxelUnder(Agent.Position, ref voxUnder, true))
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                        break;
                    }

                    chunks.ChunkData.GetVoxel(null, voxUnder.Position + new Vector3(0, 1, 0), ref voxAbove);


                    if(Target == null)
                    {
                        if (Creature.Faction == PlayState.Master.Faction)
                        {
                            PlayState.AnnouncementManager.Announce(Creature.Stats.FullName + " got lost.",
                                Creature.Stats.FullName + "'s target was lost.");
                        }
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                        break;
                    }

                    if(voxAbove != null)
                    {
                        Path = null;

                   

                        AstarPlanRequest aspr = new AstarPlanRequest
                        {
                            Subscriber = PlanSubscriber,
                            Start = voxAbove,
                            MaxExpansions = MaxExpansions,
                            Sender = Agent
                        };

                        if (Type == PlanType.Radius)
                        {
                            aspr.GoalRegion = new SphereGoalRegion(Target, Radius);   
                        }
                        else if ( Type == PlanType.Into)
                        {
                            aspr.GoalRegion = new VoxelGoalRegion(Target);
                        }
                        else if (Type == PlanType.Adjacent)
                        {
                            aspr.GoalRegion = new AdjacentVoxelGoalRegion2D(Target);
                        }
                        

                        PlanSubscriber.SendRequest(aspr);
                        PlannerTimer.Reset(PlannerTimer.TargetTimeSeconds);
                        WaitingOnResponse = true;
                        yield return Status.Running;

                    }
                    else
                    {
                        Path = null;
                        if (Creature.Faction == PlayState.Master.Faction)
                        {
                            PlayState.AnnouncementManager.Announce(Creature.Stats.FullName + " got lost.",
                                Creature.Stats.FullName + " couldn't find a path. The target was invalid.");
                        }
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                        break;
                    }

                    Timeouts++;
                }
                else
                {
                    Status statusResult = Status.Running;

                    while(PlanSubscriber.Responses.Count > 0)
                    {
                        AStarPlanResponse response;
                        PlanSubscriber.Responses.TryDequeue(out response);

                        if (response.Success)
                        {
                            Path = response.Path;

                            if (Type == PlanType.Adjacent && Path.Count > 0)
                            {
                                Path.RemoveAt(Path.Count - 1);
                            }
                            WaitingOnResponse = false;

                            statusResult = Status.Success;
                        }
                        else
                        {
                            if (Creature.Faction == PlayState.Master.Faction)
                            {
                                PlayState.AnnouncementManager.Announce(Creature.Stats.FullName + " got lost.",
                                    Creature.Stats.FullName + " couldn't find a path in time.");
                            }
                            Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                            statusResult = Status.Fail;

                        }
                    }
                    yield return statusResult;
                }
            }
        }
    }

}