using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DwarfCorp
{
    public partial class PersistentWorldData
    {
        public Dictionary<String, int> CachedResourceTagCounts = new Dictionary<String, int>();
        public Dictionary<string, bool> CachedCanBuildVoxel = new Dictionary<string, bool>();
    }

    public partial class WorldManager
    {
        public bool RemoveResourcesWithToss(ResourceAmount resources, Vector3 position, Zone Zone) // Todo: Kill this one.
        {
            if (!Zone.Resources.HasResource(resources))
                return false;
            if (!(Zone is Stockpile))
                return false;

            var stock = Zone as Stockpile;
            var num = stock.Resources.RemoveMaxResources(resources, resources.Count);

            if (Library.GetResourceType(resources.Type).HasValue(out var resourceType))
                foreach (var tag in resourceType.Tags)
                    if (PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                        PersistentData.CachedResourceTagCounts[tag] -= num;

            for (int i = 0; i < num; i++)
            {
                var startPosition = stock.GetBoundingBox().Center() + new Vector3(0.0f, 1.0f, 0.0f);
                var newEntity = EntityFactory.CreateEntity<GameComponent>(resources.Type + " Resource", startPosition);

                if (newEntity.GetRoot().GetComponent<Physics>().HasValue(out var newPhysics))
                    newPhysics.CollideMode = Physics.CollisionMode.None;

                var toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f), 2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, position);
                newEntity.AnimationQueue.Add(toss);
                toss.OnComplete += () => newEntity.Die();
            }

            RecomputeCachedVoxelstate();
            return true;
        }

        public bool RemoveResources(List<ResourceAmount> resources)
        {
            foreach (var resource in resources)
            {
                int count = 0;
                var resourceType = Library.GetResourceType(resource.Type);
                foreach (var stock in EnumerateZones().Where(s => resources.All(r => s is Stockpile && (s as Stockpile).IsAllowed(r.Type))))
                {
                    int num = stock.Resources.RemoveMaxResources(resource, resource.Count - count);

                    if (resourceType.HasValue(out var type))
                        foreach (var tag in type.Tags)
                        {
                            if (PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                            {
                                PersistentData.CachedResourceTagCounts[tag] -= num;
                                Trace.Assert(PersistentData.CachedResourceTagCounts[tag] >= 0);
                            }
                        }

                    count += num;

                    if (count >= resource.Count)
                        break;
                }
            }

            RecomputeCachedVoxelstate();
            return true;
        }

        public List<ResourceAmount> GetResourcesWithTags(List<Quantitiy<String>> tags) // Todo: This is only ever called with a list of 1.
        {
            var tagsRequired = new Dictionary<String, int>();
            var tagsGot = new Dictionary<String, int>();
            var amounts = new Dictionary<String, ResourceAmount>();

            foreach (var quantity in tags)
            {
                tagsRequired[quantity.Type] = quantity.Count;
                tagsGot[quantity.Type] = 0;
            }

            var r = new Random();

            foreach (var stockpile in EnumerateZones())
                foreach (var resource in stockpile.Resources.Enumerate().OrderBy(x => r.Next()))
                    foreach (var requirement in tagsRequired)
                    {
                        var got = tagsGot[requirement.Key];

                        if (requirement.Value <= got) continue;

                        if (!Library.GetResourceType(resource.Type).HasValue(out var type) || !type.Tags.Contains(requirement.Key)) continue;

                        int amountToRemove = Math.Min(resource.Count, requirement.Value - got);

                        if (amountToRemove <= 0) continue;

                        tagsGot[requirement.Key] += amountToRemove;

                        if (amounts.ContainsKey(resource.Type))
                            amounts[resource.Type].Count += amountToRemove;
                        else
                            amounts[resource.Type] = new ResourceAmount(resource.Type, amountToRemove);
                    }

            var toReturn = new List<ResourceAmount>();

            foreach (var requirement in tagsRequired)
            {
                ResourceAmount maxAmount = null;

                foreach (var pair in amounts)
                {
                    if (!Library.GetResourceType(pair.Key).HasValue(out var type) || !type.Tags.Contains(requirement.Key)) continue;
                    if (maxAmount == null || pair.Value.Count > maxAmount.Count)
                        maxAmount = pair.Value;
                }

                if (maxAmount != null)
                    toReturn.Add(maxAmount);
            }
            return toReturn;
        }

        public bool HasResourcesWithTags(IEnumerable<Quantitiy<String>> resources)
        {
            foreach (var resource in resources)
            {
                int count = EnumerateZones().Sum(stock => stock.Resources.GetResourceCount(resource.Type));

                if (count < resource.Count)
                    return false;
            }

            return true;
        }

        public bool HasResources(IEnumerable<ResourceAmount> resources)
        {
            foreach (ResourceAmount resource in resources)
            {
                int count = EnumerateZones().Sum(stock => stock.Resources.GetResourceCount(resource.Type));

                if (count < resources.Where(r => r.Type == resource.Type).Sum(r => r.Count))
                    return false;
            }

            return true;
        }

        public bool HasResource(String resource)
        {
            return HasResources(new List<ResourceAmount>() { new ResourceAmount(resource) });
        }

        public int CountResourcesWithTag(String tag)
        {
            List<ResourceAmount> resources = ListResourcesWithTag(tag);
            int amounts = 0;

            foreach (ResourceAmount amount in resources)
            {
                amounts += amount.Count;
            }

            return amounts;
        }

        public List<ResourceAmount> ListResourcesWithTag(String tag, bool allowHeterogenous = true)
        {
            if (allowHeterogenous)
                return ListResources()
                    .Where(r => Library.GetResourceType(r.Key).HasValue(out var res) && res.Tags.Contains(tag))
                    .Select(r => r.Value)
                    .ToList();

            ResourceAmount maxAmount = null;
            foreach (var pair in ListResources())
            {
                if (!Library.GetResourceType(pair.Value.Type).HasValue(out var type) || !type.Tags.Contains(tag)) continue;

                if (maxAmount == null || pair.Value.Count > maxAmount.Count)
                    maxAmount = pair.Value;
            }

            return maxAmount != null ? new List<ResourceAmount>() { maxAmount } : new List<ResourceAmount>();
        }

        public Dictionary<string, ResourceAmount> ListResources()
        {
            var toReturn = new Dictionary<string, ResourceAmount>();

            foreach (var stockpile in EnumerateZones().Where(pile => !(pile is Graveyard)))
            {
                foreach (var resource in stockpile.Resources.Enumerate())
                {
                    if (resource.Count == 0)
                        continue;

                    if (toReturn.ContainsKey(resource.Type))
                        toReturn[resource.Type].Count += resource.Count;
                    else
                        toReturn[resource.Type] = new ResourceAmount(resource);
                }
            }

            return toReturn;
        }

        public bool AddResources(ResourceAmount resources)
        {
            var amount = new ResourceAmount(resources.Type, resources.Count);
            var resource = Library.GetResourceType(amount.Type);

            foreach (Stockpile stockpile in EnumerateZones().Where(s => s is Stockpile && (s as Stockpile).IsAllowed(resources.Type)))
            {
                int space = stockpile.ResourceCapacity - stockpile.Resources.CurrentResourceCount;

                if (space >= amount.Count)
                {
                    stockpile.AddResource(amount);

                    if (resource.HasValue(out var res))
                        foreach (var tag in res.Tags)
                        {
                            if (!PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                                PersistentData.CachedResourceTagCounts[tag] = 0;
                            PersistentData.CachedResourceTagCounts[tag] += amount.Count;
                        }

                    RecomputeCachedVoxelstate();
                    return true;
                }
                else
                {
                    var amountToMove = space;
                    stockpile.AddResource(new ResourceAmount(resources.Type, amountToMove));
                    amount.Count -= amountToMove;

                    if (resource.HasValue(out var res))
                        foreach (var tag in res.Tags)
                        {
                            if (!PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                                PersistentData.CachedResourceTagCounts[tag] = 0;
                            PersistentData.CachedResourceTagCounts[tag] += amountToMove;
                        }

                    RecomputeCachedVoxelstate();

                    if (amount.Count == 0)
                        return true;
                }
            }

            return false;
        }

        public Dictionary<string, Pair<ResourceAmount>> ListResourcesInStockpilesPlusMinions()
        {
            var stocks = ListResources();
            var toReturn = new Dictionary<string, Pair<ResourceAmount>>();

            foreach (var pair in stocks)
                toReturn[pair.Key] = new Pair<ResourceAmount>(pair.Value, new ResourceAmount(pair.Value.Type, 0));

            foreach (var creature in PlayerFaction.Minions)
            {
                var inventory = creature.Creature.Inventory;
                foreach (var i in inventory.Resources)
                {
                    var resource = i.Resource;
                    if (toReturn.ContainsKey(resource))
                        toReturn[resource].Second.Count += 1;
                    else
                        toReturn[resource] = new Pair<ResourceAmount>(new ResourceAmount(resource, 0), new ResourceAmount(resource));
                }
            }

            return toReturn;
        }

        public void RecomputeCachedVoxelstate()
        {
            foreach (var type in Library.EnumerateVoxelTypes())
            {
                bool nospecialRequried = type.BuildRequirements.Count == 0;
                PersistentData.CachedCanBuildVoxel[type.Name] = type.IsBuildable && ((nospecialRequried && HasResource(type.ResourceToRelease)) || (!nospecialRequried && HasResourcesCached(type.BuildRequirements)));
            }
        }

        public bool HasResourcesCached(IEnumerable<String> resources)
        {
            foreach (var resource in resources)
            {
                if (!PersistentData.CachedResourceTagCounts.ContainsKey(resource))
                    return false;

                if (PersistentData.CachedResourceTagCounts[resource] == 0)
                    return false;
            }

            return true;
        }

        public void RecomputeCachedResourceState()
        {
            PersistentData.CachedResourceTagCounts.Clear();

            foreach (var resource in ListResources())
                if (Library.GetResourceType(resource.Key).HasValue(out var type))
                    foreach (var tag in type.Tags)
                    {
                        Trace.Assert(type.Tags.Count(t => t == tag) == 1);
                        if (!PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                            PersistentData.CachedResourceTagCounts[tag] = resource.Value.Count;
                        else
                            PersistentData.CachedResourceTagCounts[tag] += resource.Value.Count;
                    }

            RecomputeCachedVoxelstate();
        }

        public bool CanBuildVoxel(VoxelType type)
        {
            if (PersistentData.CachedCanBuildVoxel.ContainsKey(type.Name))
                return PersistentData.CachedCanBuildVoxel[type.Name];
            return false;
        }
    }
}
