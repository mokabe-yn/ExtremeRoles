﻿using System.Collections;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using BepInEx.IL2CPP.Utils.Collections;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Roles;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Extension.State;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Roles.Solo.Host;
using ExtremeRoles.Performance;
using AmongUs.GameOptions;
using PowerTools;

namespace ExtremeRoles.Patches
{
    public static class IntroCutscenceHelper
    {

        public static void SetupIntroTeam(
            IntroCutscene __instance,
            ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            var role = ExtremeRoleManager.GetLocalPlayerRole();

            if (role.IsNeutral())
            {
                __instance.BackgroundBar.material.color = ColorPalette.NeutralColor;
                __instance.TeamTitle.text = Translation.GetString("Neutral");
                __instance.TeamTitle.color = ColorPalette.NeutralColor;
                __instance.ImpostorText.text = Translation.GetString("neutralIntro");
            }
            else if (role.Id == ExtremeRoleId.Xion)
            {
                __instance.BackgroundBar.material.color = ColorPalette.XionBlue;
                __instance.TeamTitle.text = Translation.GetString("yourHost");
                __instance.TeamTitle.color = ColorPalette.XionBlue;
                __instance.ImpostorText.text = Translation.GetString("youAreNewRuleEditor");
            }
        }

        public static void SetupIntroTeamIcons(
            IntroCutscene __instance,
            ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {

            var role = ExtremeRoleManager.GetLocalPlayerRole();

            // Intro solo teams
            if (role.IsNeutral() || role.Id == ExtremeRoleId.Xion)
            {
                var soloTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                soloTeam.Add(CachedPlayerControl.LocalPlayer);
                yourTeam = soloTeam;
            }
        }

        public static void SetupPlayerPrefab(IntroCutscene __instance)
        {
            Prefab.PlayerPrefab = UnityEngine.Object.Instantiate(
                __instance.PlayerPrefab);
            UnityEngine.Object.DontDestroyOnLoad(Prefab.PlayerPrefab);
            Prefab.PlayerPrefab.name = "poolablePlayerPrefab";
            Prefab.PlayerPrefab.gameObject.SetActive(false);
        }

        public static void SetupRole()
        {
            var localRole = ExtremeRoleManager.GetLocalPlayerRole();

            var setUpRole = localRole as IRoleSpecialSetUp;
            if (setUpRole != null)
            {
                setUpRole.IntroBeginSetUp();
            }

            var multiAssignRole = localRole as Roles.API.MultiAssignRoleBase;
            if (multiAssignRole != null)
            {
                setUpRole = multiAssignRole.AnotherRole as IRoleSpecialSetUp;
                if (setUpRole != null)
                {
                    setUpRole.IntroBeginSetUp();
                }
            }
        }

    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    public static class IntroCutsceneBeginImpostorPatch
    {
        public static void Prefix(
            IntroCutscene __instance,
            ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            IntroCutscenceHelper.SetupIntroTeamIcons(__instance, ref yourTeam);
            IntroCutscenceHelper.SetupPlayerPrefab(__instance);
        }

        public static void Postfix(
            IntroCutscene __instance,
            ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            IntroCutscenceHelper.SetupIntroTeam(__instance, ref yourTeam);
            IntroCutscenceHelper.SetupRole();
        }

    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    public static class BeginCrewmatePatch
    {
        public static void Prefix(
            IntroCutscene __instance,
            ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            IntroCutscenceHelper.SetupIntroTeamIcons(__instance, ref teamToDisplay);
            IntroCutscenceHelper.SetupPlayerPrefab(__instance);
        }

        public static void Postfix(
            IntroCutscene __instance,
            ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            IntroCutscenceHelper.SetupIntroTeam(__instance, ref teamToDisplay);
            IntroCutscenceHelper.SetupRole();
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    public static class IntroCutsceneCoBeginPatch
    {
        private static bool isAllPlyerDummy()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) { continue; }

                if (!player.GetComponent<DummyBehaviour>().enabled)
                {
                    return false;
                }
            }
            return true;
        }

        private static IEnumerator coBeginPatch(
            IntroCutscene instance)
        {

            GameObject roleAssignText = new GameObject("roleAssignText");
            var text = roleAssignText.AddComponent<Module.CustomMonoBehaviour.LoadingText>();
            text.SetFontSize(3.0f);
            text.SetMessage(Translation.GetString("roleAssignNow"));

            roleAssignText.gameObject.SetActive(true);

            if (AmongUsClient.Instance.AmHost)
            {
                if (AmongUsClient.Instance.NetworkMode != NetworkModes.LocalGame ||
                    !isAllPlyerDummy())
                {
                    // ホストは全員の処理が終わるまで待つ
                    while (!Manager.RoleManagerSelectRolesPatch.IsReady)
                    {
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
                Manager.RoleManagerSelectRolesPatch.AllPlayerAssignToExRole();
            }
            else
            {
                // ホスト以外はここまで処理済みである事を送信
                Manager.RoleManagerSelectRolesPatch.SetLocalPlayerReady();
            }

            // バニラの役職アサイン後すぐこの処理が走るので全員の役職が入るまで待機
            while (!ExtremeRolesPlugin.ShipState.IsRoleSetUpEnd)
            {
                yield return null;
            }

            SoundManager.Instance.PlaySound(instance.IntroStinger, false, 1f);
            if (GameManager.Instance.IsNormal())
            {
                instance.HideAndSeekPanels.SetActive(false);
                instance.CrewmateRules.SetActive(false);
                instance.ImpostorRules.SetActive(false);
                instance.ImpostorName.gameObject.SetActive(false);
                instance.ImpostorTitle.gameObject.SetActive(false);

                Il2CppSystem.Collections.Generic.List<PlayerControl> teamToShow = IntroCutscene.SelectTeamToShow(
                    (Il2CppSystem.Func<GameData.PlayerInfo, bool>)(
                        (GameData.PlayerInfo pcd) =>
                            !CachedPlayerControl.LocalPlayer.Data.Role.IsImpostor ||
                            pcd.Role.TeamType == CachedPlayerControl.LocalPlayer.Data.Role.TeamType
                    ));
                
                if (CachedPlayerControl.LocalPlayer.Data.Role.IsImpostor)
                {
                    instance.ImpostorText.gameObject.SetActive(false);
                }
                else
                {
                    int adjustedNumImpostors = GameOptionsManager.Instance.CurrentGameOptions.GetAdjustedNumImpostors(
                        GameData.Instance.PlayerCount);
                    if (adjustedNumImpostors == 1)
                    {
                        instance.ImpostorText.text = FastDestroyableSingleton<TranslationController>.Instance.GetString(
                            StringNames.NumImpostorsS, System.Array.Empty<Il2CppSystem.Object>());
                    }
                    else
                    {
                        instance.ImpostorText.text = FastDestroyableSingleton<TranslationController>.Instance.GetString(
                            StringNames.NumImpostorsP, new Il2CppSystem.Object[]
                            {
                                adjustedNumImpostors.ToString()
                            });
                    }
                    instance.ImpostorText.text = instance.ImpostorText.text.Replace("[FF1919FF]", "<color=#FF1919FF>");
                    instance.ImpostorText.text = instance.ImpostorText.text.Replace("[]", "</color>");
                }
                
                roleAssignText.gameObject.SetActive(false);
                Object.Destroy(roleAssignText);
                roleAssignText = null;

                yield return instance.ShowTeam(teamToShow, 3.0f);
                yield return instance.ShowRole();
            }
            else
            {
                instance.HideAndSeekPanels.SetActive(true);
                if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                {
                    instance.CrewmateRules.SetActive(false);
                    instance.ImpostorRules.SetActive(true);
                }
                else
                {
                    instance.CrewmateRules.SetActive(true);
                    instance.ImpostorRules.SetActive(false);
                }
                IntroCutscene.SelectTeamToShow(
                    (Il2CppSystem.Func<GameData.PlayerInfo, bool>)(
                        (GameData.PlayerInfo pcd) =>
                            CachedPlayerControl.LocalPlayer.Data.Role.IsImpostor != pcd.Role.IsImpostor
                    ));

                PlayerControl impostor = 
                    CachedPlayerControl.AllPlayerControls.Find(
                        (CachedPlayerControl pc) => pc.Data.Role.IsImpostor);

                instance.ImpostorName.gameObject.SetActive(true);
                instance.ImpostorTitle.gameObject.SetActive(true);
                instance.BackgroundBar.enabled = false;
                instance.TeamTitle.gameObject.SetActive(false);
                instance.ImpostorName.text = impostor.Data.PlayerName;
                PoolablePlayer playerSlot = instance.CreatePlayer(0, 1, impostor.Data, false);
                playerSlot.transform.localPosition = instance.impostorPos;
                playerSlot.transform.localScale = Vector3.one * instance.impostorScale;

                yield return ShipStatus.Instance.CosmeticsCache.PopulateFromPlayers();
                yield return new WaitForSecondsRealtime(6f);

                playerSlot.gameObject.SetActive(false);
                instance.HideAndSeekPanels.SetActive(false);
                instance.CrewmateRules.SetActive(false);
                instance.ImpostorRules.SetActive(false);

                LogicOptionsHnS logicOptionsHnS = 
                    GameManager.Instance.LogicOptions as LogicOptionsHnS;
                LogicHnSMusic logicHnSMusic = 
                    GameManager.Instance.GetLogicComponent<LogicHnSMusic>() as LogicHnSMusic;
                if (logicHnSMusic != null)
                {
                    logicHnSMusic.StartMusicWithIntro();
                }
                if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                {
                    float crewmateLeadTime = (float)logicOptionsHnS.GetCrewmateLeadTime();
                    instance.HideAndSeekTimerText.gameObject.SetActive(true);
                    instance.HideAndSeekPlayerVisual.gameObject.SetActive(true);
                    instance.HideAndSeekPlayerVisual.SetBodyType(PlayerBodyTypes.Seeker);
                    SpriteAnim component = instance.HideAndSeekPlayerVisual.GetComponent<SpriteAnim>();
                    instance.HideAndSeekPlayerVisual.UpdateFromPlayerData(PlayerControl.LocalPlayer.Data, PlayerControl.LocalPlayer.CurrentOutfitType, PlayerMaterial.MaskType.None, false);
                    component.Play(instance.HnSSeekerSpawnAnim, 1f);
                    instance.HideAndSeekPlayerVisual.SetBodyCosmeticsVisible(false);
                    instance.HideAndSeekPlayerVisual.ToggleName(false);

                    while (crewmateLeadTime > 0f)
                    {
                        instance.HideAndSeekTimerText.text = Mathf.RoundToInt(crewmateLeadTime).ToString();
                        crewmateLeadTime -= Time.deltaTime;
                        yield return null;
                    }
                }
                else
                {
                    ShipStatus.Instance.HideCountdown = (float)logicOptionsHnS.GetCrewmateLeadTime();
                    impostor.AnimateCustom(instance.HnSSeekerSpawnAnim);
                    impostor.cosmetics.SetBodyCosmeticsVisible(false);
                }
                impostor = null;
                playerSlot = null;
            }
            Object.Destroy(instance.gameObject);
            yield break;
        }
        public static bool Prefix(
            IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            __result = coBeginPatch(__instance).WrapToIl2Cpp();
            return false;
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
    public static class IntroCutsceneSetUpRoleTextPatch
    {
        private static IEnumerator showRoleText(
            SingleRoleBase role,
            IntroCutscene __instance)
        {
            __instance.YouAreText.color = role.GetNameColor();
            __instance.RoleText.text = role.GetColoredRoleName();
            __instance.RoleText.color = role.GetNameColor();
            __instance.RoleBlurbText.text = role.GetIntroDescription();
            __instance.RoleBlurbText.color = role.GetNameColor();

            if (role.Id != ExtremeRoleId.Lover ||
                role.Id != ExtremeRoleId.Sharer ||
                role.Id != ExtremeRoleId.Buddy)
            {
                if (role is MultiAssignRoleBase)
                {
                    if (((MultiAssignRoleBase)role).AnotherRole != null)
                    {
                        __instance.RoleBlurbText.fontSize *= 0.45f;
                    }
                }


                if (role.IsImpostor())
                {
                    __instance.RoleBlurbText.text +=
                        $"\n{Translation.GetString("impostorIntroText")}";
                }
                else if (role.IsCrewmate() && role.HasTask())
                {
                    __instance.RoleBlurbText.text +=
                        $"\n{Translation.GetString("crewIntroText")}";
                }
            }

            SoundManager.Instance.PlaySound(
                CachedPlayerControl.LocalPlayer.Data.Role.IntroSound, false, 1f);

            __instance.YouAreText.gameObject.SetActive(true);
            __instance.RoleText.gameObject.SetActive(true);
            __instance.RoleBlurbText.gameObject.SetActive(true);

            if (__instance.ourCrewmate == null)
            {
                __instance.ourCrewmate = __instance.CreatePlayer(
                    0, 1, CachedPlayerControl.LocalPlayer.Data, false);
                __instance.ourCrewmate.gameObject.SetActive(false);
            }
            __instance.ourCrewmate.gameObject.SetActive(true);
            __instance.ourCrewmate.transform.localPosition = new Vector3(0f, -1.05f, -18f);
            __instance.ourCrewmate.transform.localScale = new Vector3(1f, 1f, 1f);

            yield return new WaitForSeconds(2.5f);

            __instance.YouAreText.gameObject.SetActive(false);
            __instance.RoleText.gameObject.SetActive(false);
            __instance.RoleBlurbText.gameObject.SetActive(false);
            __instance.ourCrewmate.gameObject.SetActive(false);

            yield break;
        }

        public static bool Prefix(
            IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            var role = ExtremeRoleManager.GetLocalPlayerRole();
            if (role.IsVanillaRole()) { return true; }
            var awakeVanillaRole = role as IRoleAwake<RoleTypes>;
            if (awakeVanillaRole != null && !awakeVanillaRole.IsAwake)
            {
                return true;
            }

            __result = showRoleText(role, __instance).WrapToIl2Cpp();
            return false;
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
    public static class IntroCutsceneOnDestroyPatch
    {
        public static void Prefix()
        {
            if (OptionHolder.AllOption[(int)OptionHolder.CommonOptionKey.UseXion].GetValue())
            {
                Xion.XionPlayerToGhostLayer();
                Xion.RemoveXionPlayerToAllPlayerControl();

                if (AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame)
                {
                    foreach (PlayerControl player in CachedPlayerControl.AllPlayerControls)
                    {
                        if (player == null) { continue; }

                        if (!player.GetComponent<DummyBehaviour>().enabled) { continue; }

                        var role = ExtremeRoleManager.GameRole[player.PlayerId];
                        if (!role.HasTask())
                        {
                            continue;
                        }

                        GameData.PlayerInfo playerInfo = player.Data;

                        var (_, totalTask) = GameSystem.GetTaskInfo(playerInfo);
                        if (totalTask == 0)
                        {
                            GameSystem.SetTask(playerInfo, 
                                GameSystem.GetRandomCommonTaskId());
                        }
                    }
                }
            }

            Module.InfoOverlay.Button.SetInfoButtonToInGamePositon();

            var localRole = ExtremeRoleManager.GetLocalPlayerRole();

            var setUpRole = localRole as IRoleSpecialSetUp;
            if (setUpRole != null)
            {
                setUpRole.IntroEndSetUp();
            }

            var multiAssignRole = localRole as MultiAssignRoleBase;
            if (multiAssignRole != null)
            {
                setUpRole = multiAssignRole.AnotherRole as IRoleSpecialSetUp;
                if (setUpRole != null)
                {
                    setUpRole.IntroEndSetUp();
                }
            }
            disableMapObject();
        }

        private static void disableMapObject()
        {
            HashSet<string> disableObjectName = new HashSet<string>();

            bool isRemoveAdmin = OptionHolder.Ship.IsRemoveAdmin;
            bool isRemoveSecurity = OptionHolder.Ship.IsRemoveSecurity;
            bool isRemoveVital = OptionHolder.Ship.IsRemoveVital;

            if (ExtremeRolesPlugin.Compat.IsModMap)
            {
                var modMap = ExtremeRolesPlugin.Compat.ModMap;

                if (isRemoveAdmin)
                {
                    disableObjectName.UnionWith(
                        modMap.GetSystemObjectName(
                            Compat.Interface.SystemConsoleType.Admin));
                }
                if (isRemoveSecurity)
                {
                    disableObjectName.UnionWith(
                        modMap.GetSystemObjectName(
                            Compat.Interface.SystemConsoleType.SecurityCamera));
                }
                if (isRemoveVital)
                {
                    disableObjectName.UnionWith(
                        modMap.GetSystemObjectName(
                            Compat.Interface.SystemConsoleType.Vital));
                }
            }
            else
            {
                switch (GameOptionsManager.Instance.CurrentGameOptions.GetByte(
                    ByteOptionNames.MapId))
                {
                    case 0:
                        if (isRemoveAdmin)
                        {
                            disableObjectName.Add(
                                GameSystem.SkeldAdmin);
                        }
                        if (isRemoveSecurity)
                        {
                            disableObjectName.Add(
                                GameSystem.SkeldSecurity);
                        }
                        break;
                    case 1:
                        if (isRemoveAdmin)
                        {
                            disableObjectName.Add(
                                GameSystem.MiraHqAdmin);
                        }
                        if (isRemoveSecurity)
                        {
                            disableObjectName.Add(
                                GameSystem.MiraHqSecurity);
                        }
                        break;
                    case 2:
                        if (isRemoveAdmin)
                        {
                            disableObjectName.Add(
                                GameSystem.PolusAdmin1);
                            disableObjectName.Add(
                                GameSystem.PolusAdmin2);
                        }
                        if (isRemoveSecurity)
                        {
                            disableObjectName.Add(
                                GameSystem.PolusSecurity);
                        }
                        if (isRemoveVital)
                        {
                            disableObjectName.Add(
                                GameSystem.PolusVital);
                        }
                        break;
                    case 4:
                        if (isRemoveAdmin)
                        {
                            disableObjectName.Add(
                                GameSystem.AirShipArchiveAdmin);
                            disableObjectName.Add(
                                GameSystem.AirShipCockpitAdmin);
                        }
                        else
                        {
                            switch (OptionHolder.Ship.AirShipEnable)
                            {
                                case OptionHolder.AirShipAdminMode.ModeCockpitOnly:
                                    disableObjectName.Add(
                                        GameSystem.AirShipArchiveAdmin);
                                    break;
                                case OptionHolder.AirShipAdminMode.ModeArchiveOnly:
                                    disableObjectName.Add(
                                        GameSystem.AirShipCockpitAdmin);
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (isRemoveSecurity)
                        {
                            disableObjectName.Add(
                                GameSystem.AirShipSecurity);
                        }
                        if (isRemoveVital)
                        {
                            disableObjectName.Add(
                                GameSystem.AirShipVital);
                        }
                        break;
                    default:
                        break;
                }
            }

            foreach (string objectName in disableObjectName)
            {
                GameSystem.DisableMapModule(objectName);
            }
        }
    }
}
