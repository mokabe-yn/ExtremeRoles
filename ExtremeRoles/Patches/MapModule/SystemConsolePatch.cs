﻿using HarmonyLib;

namespace ExtremeRoles.Patches.MapModule
{
    [HarmonyPatch(typeof(SystemConsole), nameof(SystemConsole.CanUse))]
    public static class SystemConsoleCanUsePatch
    {
        public static bool Prefix(
            ref float __result, SystemConsole __instance,
            [HarmonyArgument(0)] GameData.PlayerInfo pc,
            [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
        {
            canUse = couldUse = false;
            __result = float.MaxValue;

            var role = Roles.ExtremeRoleManager.GameRole[pc.PlayerId];
            var icon = __instance.useIcon;

            if ((icon == ImageNames.CamsButton) && role.CanUseSecurity) { return true; }
            if ((icon == ImageNames.VitalsButton) && role.CanUseVital) { return true; }

            return false;
        }
    }
}
