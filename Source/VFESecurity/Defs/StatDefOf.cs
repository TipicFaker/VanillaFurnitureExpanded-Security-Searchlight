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
using Harmony;

namespace VFESecurity
{

    [DefOf]
    public static class StatDefOf
    {

        public static StatDef VFES_EnergyShieldEnergyMax;
        public static StatDef VFES_EnergyShieldRechargeRate;
        public static StatDef VFES_EnergyShieldRadius;

    }

}
