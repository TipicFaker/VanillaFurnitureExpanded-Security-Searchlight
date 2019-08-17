﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using Harmony;

namespace VFESecurity
{

    public abstract class ArtilleryStrikeArrivalAction : IExposable
    {

        public abstract void Arrived(ActiveArtilleryStrike artilleryStrike, int tile);

        public virtual void ExposeData()
        {
        }

    }

}