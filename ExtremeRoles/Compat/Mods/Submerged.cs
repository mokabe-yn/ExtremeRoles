﻿using System;
using System.Linq;
using System.Reflection;

using ExtremeRoles.Compat.Interface;

using BepInEx;

using HarmonyLib;
using UnityEngine;

namespace ExtremeRoles.Compat.Mods
{
    public class Submerged : CompatModBase, IMapMod 
    {
        public const string Guid = "Submerged";

        public ShipStatus.MapType MapType => (ShipStatus.MapType)5;

        public Submerged(PluginInfo plugin) : base(Guid, plugin)
        {

        }

        public Console GetConsole(TaskTypes task)
        {
            throw new System.NotImplementedException();
        }

        public SystemConsole GetSystemConsole(SystemConsoleType sysConsole)
        {
            throw new System.NotImplementedException();
        }

        public bool IsCustomSabotageNow()
        {
            throw new System.NotImplementedException();
        }

        public bool IsCustomSabotageTask(TaskTypes saboTask)
        {
            throw new System.NotImplementedException();
        }

        public void RepairCustomSabotage()
        {
            throw new System.NotImplementedException();
        }

        public void RepairCustomSabotage(TaskTypes saboTask)
        {
            throw new System.NotImplementedException();
        }

        public Sprite SystemConsoleUseSprite(SystemConsoleType sysConsole)
        {
            throw new System.NotImplementedException();
        }

        protected override void PatchAll(Harmony harmony)
        {
            Type exileCont = ClassType.First(
                t => t.Name == "SubmergedExileController");
            MethodInfo wrapUpAndSpawn = AccessTools.Method(
                exileCont, "WrapUpAndSpawn");

            ExileController cont = null;
            MethodInfo wrapUpAndSpawnPrefix = SymbolExtensions.GetMethodInfo(
                () => Patches.SubmergedExileControllerWrapUpAndSpawnPatch.Prefix(cont));
            MethodInfo wrapUpAndSpawnPostfix = SymbolExtensions.GetMethodInfo(
                () => Patches.SubmergedExileControllerWrapUpAndSpawnPatch.Postfix(cont));
            
            harmony.Patch(wrapUpAndSpawn,
                new HarmonyMethod(wrapUpAndSpawnPrefix),
                new HarmonyMethod(wrapUpAndSpawnPostfix));
        }
    }
}
