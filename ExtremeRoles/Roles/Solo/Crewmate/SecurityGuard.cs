﻿using System;

using UnityEngine;

using ExtremeRoles.Module;
using ExtremeRoles.Helper;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Module.RoleAbilityButton;

namespace ExtremeRoles.Roles.Solo.Crewmate
{
    public class SecurityGuard : SingleRoleBase, IRoleAbility, IRoleAwake<RoleTypes>
    {
        public class SecurityGuardAbilityButton : RoleAbilityButtonBase
        {
            public int CurAbilityNum
            {
                get => this.abilityNum;
            }

            private int abilityNum = 0;
            private bool isVentSeal;
            private int ventSealScrewNum;
            private int cameraSetNum;
            private string cameraSetString;
            private string ventSealString;
            private Sprite cameraSetSprite;
            private Sprite ventSealSprite;
            private Func<bool> ventModeCheck;

            private TMPro.TextMeshPro abilityCountText = null;

            public SecurityGuardAbilityButton(
                Func<bool> ability,
                Func<bool> canUse,
                Sprite cameraSetSprite,
                Sprite ventSealSprite,
                Vector3 positionOffset,
                Action abilityCleanUp,
                Func<bool> abilityCheck,
                int ventSealScrewNum,
                int cameraSetNum,
                Func<bool> isVentMode,
                KeyCode hotkey = KeyCode.F,
                bool mirror = false) : base(
                    "",
                    ability,
                    canUse,
                    cameraSetSprite,
                    positionOffset,
                    abilityCleanUp,
                    abilityCheck,
                    hotkey, mirror)
            {
                this.abilityCountText = GameObject.Instantiate(
                    this.Button.cooldownTimerText,
                    this.Button.cooldownTimerText.transform.parent);
                updateAbilityCountText();
                this.abilityCountText.enableWordWrapping = false;
                this.abilityCountText.transform.localScale = Vector3.one * 0.5f;
                this.abilityCountText.transform.localPosition += new Vector3(-0.05f, 0.65f, 0);

                this.ventModeCheck = isVentMode;

                this.ventSealString = Translation.GetString("ventSeal");
                this.cameraSetString = Translation.GetString("cameraSet");
                this.ButtonText = this.cameraSetString;

                this.ventSealScrewNum = ventSealScrewNum;
                this.cameraSetSprite = cameraSetSprite;
                this.cameraSetNum = cameraSetNum;
                this.ventSealSprite = ventSealSprite;
            }

            public void UpdateAbilityCount(int newCount)
            {
                this.abilityNum = newCount;
                this.updateAbilityCountText();
            }

            protected override void AbilityButtonUpdate()
            {
                if (this.CanUse() && this.abilityNum > 0)
                {
                    this.Button.graphic.color = this.Button.buttonLabelText.color = Palette.EnabledColor;
                    this.Button.graphic.material.SetFloat("_Desat", 0f);
                }
                else
                {
                    this.Button.graphic.color = this.Button.buttonLabelText.color = Palette.DisabledClear;
                    this.Button.graphic.material.SetFloat("_Desat", 1f);
                }
                if (this.abilityNum == 0)
                {
                    Button.SetCoolDown(0, this.CoolTime);
                    return;
                }

                this.isVentSeal = this.ventModeCheck();
                if (this.isVentSeal)
                {
                    this.ButtonSprite = this.ventSealSprite;
                    this.ButtonText = this.ventSealString;
                }
                else
                {
                    this.ButtonSprite = this.cameraSetSprite;
                    this.ButtonText = this.cameraSetString;
                }

                if (this.Timer >= 0)
                {
                    bool abilityOn = this.IsHasCleanUp() && IsAbilityOn;

                    if (abilityOn || (!PlayerControl.LocalPlayer.inVent && PlayerControl.LocalPlayer.moveable))
                    {
                        this.Timer -= Time.deltaTime;
                    }
                    if (abilityOn)
                    {
                        if (!this.AbilityCheck())
                        {
                            this.Timer = 0;
                            this.IsAbilityOn = false;
                        }
                    }
                }

                if (this.Timer <= 0 && this.IsHasCleanUp() && IsAbilityOn)
                {
                    this.IsAbilityOn = false;
                    this.Button.cooldownTimerText.color = Palette.EnabledColor;
                    this.CleanUp();
                    this.reduceAbilityCount();
                    this.ResetCoolTimer();
                }

                if (this.abilityNum > 0)
                {
                    Button.SetCoolDown(
                        this.Timer,
                        (this.IsHasCleanUp() && this.IsAbilityOn) ? this.AbilityActiveTime : this.CoolTime);
                }
            }

            protected override void OnClickEvent()
            {
                if (this.CanUse() &&
                    this.Timer < 0f &&
                    this.abilityNum > 0 &&
                    !this.IsAbilityOn &&
                    (
                        (this.abilityNum - this.ventSealScrewNum > 0 && this.isVentSeal) || 
                        (this.abilityNum - this.cameraSetNum > 0)
                    ))
                {
                    Button.graphic.color = this.DisableColor;

                    if (this.UseAbility())
                    {
                        if (this.IsHasCleanUp())
                        {
                            this.Timer = this.AbilityActiveTime;
                            Button.cooldownTimerText.color = this.TimerOnColor;
                            this.IsAbilityOn = true;
                        }
                        else
                        {
                            this.reduceAbilityCount();
                            this.ResetCoolTimer();
                        }
                    }
                }
            }

            private void reduceAbilityCount()
            {
                this.abilityNum = this.isVentSeal ? 
                    this.abilityNum - this.ventSealScrewNum : this.abilityNum - this.cameraSetNum;
                if (this.abilityCountText != null)
                {
                    updateAbilityCountText();
                }
            }

            private void updateAbilityCountText()
            {
                this.abilityCountText.text = string.Concat(
                    Translation.GetString("buttonCountText"),
                    string.Format(Translation.GetString("securityGuardScrewNum"),
                        this.abilityNum, this.isVentSeal ? this.ventSealScrewNum : this.cameraSetNum));
            }
        }

        public bool IsAwake
        {
            get
            {
                return GameSystem.IsLobby || this.awakeRole;
            }
        }

        public RoleTypes NoneAwakeRole => RoleTypes.Crewmate;

        public RoleAbilityButtonBase Button
        { 
            get => this.abilityButton;
            set
            {
                this.abilityButton = value;
            }
        }

        public enum SecurityGuardOption
        {
            AwakeTaskGage
        }

        private bool awakeRole = false;
        private float awakeTaskGage;
        private Vent targetVent;
        private Vector2 prevPos;
        private RoleAbilityButtonBase abilityButton;
        public SecurityGuard() : base(
            ExtremeRoleId.SecurityGuard,
            ExtremeRoleType.Crewmate,
            ExtremeRoleId.SecurityGuard.ToString(),
            Palette.CrewmateBlue,
            false, true, false, false)
        { }

        public string GetFakeOptionString() => "";

        public void Update(PlayerControl rolePlayer)
        {
            if (!this.awakeRole)
            {
                if (Player.GetPlayerTaskGage(rolePlayer) >= this.awakeTaskGage)
                {
                    this.awakeRole = true;
                }
            }
        }
        public void CreateAbility()
        {
            this.Button = new SecurityGuardAbilityButton(
                UseAbility,
                IsAbilityUse,
                Resources.Loader.CreateSpriteFromResources(
                    Resources.Path.TestButton),
                Resources.Loader.CreateSpriteFromResources(
                    Resources.Path.TestButton),
                new Vector3(-1.8f, -0.06f, 0),
                CleanUp,
                IsAbilityCheck,
                5, 10,
                IsVentMode);
        }

        public bool UseAbility()
        {
            this.prevPos = PlayerControl.LocalPlayer.GetTruePosition();
            return true;
        }

        public bool IsAbilityUse() => this.IsCommonUse();

        public bool IsAbilityCheck() => this.prevPos == PlayerControl.LocalPlayer.GetTruePosition();

        public bool IsVentMode()
        {
            Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();
            foreach (Vent vent in ShipStatus.Instance.AllVents)
            {
                if (ExtremeRolesPlugin.GameDataStore.CustomVent.IsCustomVent(vent.Id))
                {
                    continue;
                }
                float distance = Vector2.Distance(vent.transform.position, truePosition);
                if (distance <= vent.UsableDistance)
                {
                    this.targetVent = vent;
                    return true;
                }
            }
            return false;
        }

        public void CleanUp()
        {
            if (this.targetVent != null)
            {

            }
            else
            {

            }
        }


        public void RoleAbilityResetOnMeetingStart()
        {
            return;     
        }

        public void RoleAbilityResetOnMeetingEnd()
        {
            this.targetVent = null;
        }

        public override string GetColoredRoleName(bool isTruthColor = false)
        {
            if (isTruthColor || IsAwake)
            {
                return base.GetColoredRoleName();
            }
            else
            {
                return Design.ColoedString(
                    Palette.White, Translation.GetString(RoleTypes.Crewmate.ToString()));
            }
        }
        public override string GetFullDescription()
        {
            if (IsAwake)
            {
                return Translation.GetString(
                    $"{this.Id}FullDescription");
            }
            else
            {
                return Translation.GetString(
                    $"{RoleTypes.Crewmate}FullDescription");
            }
        }

        public override string GetImportantText(bool isContainFakeTask = true)
        {
            if (IsAwake)
            {
                return base.GetImportantText(isContainFakeTask);

            }
            else
            {
                return Design.ColoedString(
                    Palette.White,
                    $"{this.GetColoredRoleName()}: {Translation.GetString("crewImportantText")}");
            }
        }

        public override string GetIntroDescription()
        {
            if (IsAwake)
            {
                return base.GetIntroDescription();
            }
            else
            {
                return PlayerControl.LocalPlayer.Data.Role.Blurb;
            }
        }

        public override Color GetNameColor(bool isTruthColor = false)
        {
            if (isTruthColor || IsAwake)
            {
                return base.GetNameColor(isTruthColor);
            }
            else
            {
                return Palette.White;
            }
        }

        protected override void CreateSpecificOption(
            CustomOptionBase parentOps)
        {
            CreateIntOption(
                SecurityGuardOption.AwakeTaskGage,
                100, 0, 100, 10,
                parentOps,
                format: OptionUnit.Percentage);
        }

        protected override void RoleSpecificInit()
        {
            this.awakeTaskGage = (float)OptionHolder.AllOption[
                GetRoleOptionId(SecurityGuardOption.AwakeTaskGage)].GetValue() / 100.0f;
            if (this.awakeTaskGage <= 0.0f)
            {
                this.awakeRole = true;
            }
            else
            {
                this.awakeRole = false;
            }
        }
    }
}
