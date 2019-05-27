using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using static Logger;
using static Core;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

public class WarStatus
{
    public List<SystemStatus> systems = new List<SystemStatus>();
    public RelationTracker relationTracker = new RelationTracker(UnityGameInstance.BattleTechGame.Simulation);
    internal List<WarFaction> factionTracker = new List<WarFaction>();
    internal Dictionary<Faction, List<StarSystem>> attackTargets = new Dictionary<Faction, List<StarSystem>>();
    internal Dictionary<Faction, List<StarSystem>> defenseTargets = new Dictionary<Faction, List<StarSystem>>();
    internal Dictionary<Faction, Dictionary<Faction, float>> attackResources = new Dictionary<Faction, Dictionary<Faction, float>>();

    // initialize a collection of all planets
    public WarStatus(bool init, bool distribute)
    {
        if (!init) return;
        var sim = UnityGameInstance.BattleTechGame.Simulation;
        foreach (var planet in sim.StarSystems)
        {
            systems.Add(new SystemStatus(planet.Name, distribute));
            ChangeSystemOwnership(sim, planet, planet.Owner, true);
        }
    }
}

public class SystemStatus
{
    public readonly string name;

    // Dictionary to hold each faction's numerical influence
    public Dictionary<Faction, float> influenceTracker = new Dictionary<Faction, float>();
    public Dictionary<Faction, int> neighborSystems;
    public readonly Faction owner;
    public string ownerName;

    public SystemStatus(string systemName, bool initialDistribution)
    {
        var sim = UnityGameInstance.BattleTechGame.Simulation;
        name = systemName;
        //owner = sim.StarSystems.First(s => s.Name == name).Owner;
        //ownerName = owner.ToString();
        CalculateNeighbours(sim, initialDistribution);
        if (initialDistribution)
            DistributeInfluence();
    }

    private void DistributeInfluence()
    {
        Log(">>> DistributeInfluence");
        influenceTracker.Add(owner, Core.Settings.DominantInfluence);
        int remainingInfluence = Core.Settings.MinorInfluencePool;
        //Log("\nremainingInfluence: " + remainingInfluence);
        //Log("=====================================================");
        while (remainingInfluence > 0)
        {
            foreach (var faction in neighborSystems.Keys)
            {
                var influenceDelta = neighborSystems[faction];
                remainingInfluence -= influenceDelta;
                //Log($"{faction.ToString(),-20} gains {influenceDelta,2}, leaving {remainingInfluence}");
                if (influenceTracker.ContainsKey(faction))
                    influenceTracker[faction] += influenceDelta;
                else
                    influenceTracker.Add(faction, influenceDelta);
            }
        }

        var totalInfluence = influenceTracker.Values.Sum();
        //Log($"\ntotalInfluence for {name}: {totalInfluence}");
        //Log("=====================================================");
        // need percentages from InfluenceTracker data 
        var tempDict = new Dictionary<Faction, float>();
        foreach (var kvp in influenceTracker)
        {
            //Log($"{kvp.Key}: {kvp.Value}");
            tempDict.Add(kvp.Key, kvp.Value / totalInfluence * 100);
        }

        influenceTracker = tempDict;
    }

    // Find how many friendly and opposing neighbors are present for the star system.
    // thanks to WarTech by Morphyum
    public void CalculateNeighbours(SimGameState sim, bool initialDistribution)
    {
        //Log($"count of sim.StarSystems: {sim.StarSystems.Count}");
        var originalSystem = sim.StarSystems.FirstOrDefault(s => s.Name == name);
        //Log($"{name}, originalSystem {originalSystem.Name}");
        neighborSystems = new Dictionary<Faction, int>();
        var neighbors = sim.Starmap.GetAvailableNeighborSystem(originalSystem);
        // build a list of all neighbors
        foreach (var neighborSystem in neighbors)
        {
            if (neighborSystems.ContainsKey(neighborSystem.Owner))
                neighborSystems[neighborSystem.Owner] += 1;
            else
                neighborSystems.Add(neighborSystem.Owner, 1);

            // the rest happens only once at initial distribution
            // build list of attack targets
            if (!initialDistribution &&
                !Core.WarStatus.attackTargets.ContainsKey(neighborSystem.Owner) &&
                neighborSystem.Owner != originalSystem.Owner)
            {
                var tempList = new List<StarSystem> {originalSystem};
                Core.WarStatus.attackTargets.Add(neighborSystem.Owner, tempList);
            }
            else if (!initialDistribution &&
                     Core.WarStatus.attackTargets.ContainsKey(neighborSystem.Owner) &&
                     !Core.WarStatus.attackTargets[neighborSystem.Owner].Contains(originalSystem) &&
                     (neighborSystem.Owner != originalSystem.Owner))
            {
                Core.WarStatus.attackTargets[neighborSystem.Owner].Add(originalSystem);
            }

            // build list of defense targets
            if (!initialDistribution &&
                !Core.WarStatus.defenseTargets.ContainsKey(originalSystem.Owner) &&
                neighborSystem.Owner != originalSystem.Owner)
            {
                var tempList = new List<StarSystem> {originalSystem};
                Core.WarStatus.defenseTargets.Add(originalSystem.Owner, tempList);
            }
            else if (!initialDistribution &&
                     Core.WarStatus.defenseTargets.ContainsKey(neighborSystem.Owner) &&
                     !Core.WarStatus.defenseTargets[originalSystem.Owner].Contains(originalSystem) &&
                     neighborSystem.Owner != originalSystem.Owner)
            {
                Core.WarStatus.defenseTargets[originalSystem.Owner].Add(originalSystem);
            }
        }
    }
}

public class WarProgress
{
    public static WarStatus WarStatus;

    public void PotentialTargets(Faction faction)
    {
        WarStatus = new WarStatus(false, false);
        //if (Core.WarStatus.attackTargets.Keys.Contains(faction))
        //{
        //    Log("------------------------------------------------------");
        //    Log(faction.ToString());
        //    Log("Attack Targets");
        //
        //    foreach (StarSystem attackedSystem in Core.WarStatus.attackTargets[faction])
        //        Log($"{attackedSystem.Name, -30} : {attackedSystem.Owner}");
        //}
        //else
        //{
        //    Log($"No Attack Targets for {faction.ToString()}!");
        //}
        //
        //if (Core.WarStatus.defenseTargets.Keys.Contains(faction))
        //{
        //    Log("------------------------------------------------------");
        //    Log(faction.ToString());
        //    Log("Defense Targets");
        //    foreach (StarSystem defensedsystem in Core.WarStatus.defenseTargets[faction])
        //        Log($"{defensedsystem.Name,-30} : {defensedsystem.Owner}");
        //}
        //else
        //{
        //    Log($"No Defense Targets for {faction.ToString()}!");
        //}
    }
}