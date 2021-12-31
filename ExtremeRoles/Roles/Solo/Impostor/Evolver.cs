﻿using UnityEngine;

using Hazel;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;

namespace ExtremeRoles.Roles.Solo.Impostor
{
    public class Evolver : SingleRoleBase, IRoleAbility
    {
        public enum EvolverOption
        {
            IsEvolvedAnimation,
            IsEatingEndCleanBody,
            EatingRange,
            KillCoolReduceRate,
        }


        public GameData.PlayerInfo targetBody;
        public byte eatingBodyId;

        private float eatingRange = 1.0f;
        private float reduceRate = 1.0f;
        private bool isEvolvdAnimation;
        private bool isEatingEndCleanBody;

        private string defaultButtonText;
        private string eatingText;

        private float defaultKillCoolTime;
        public Evolver() : base(
            ExtremeRoleId.Evolver,
            ExtremeRoleType.Impostor,
            ExtremeRoleId.Evolver.ToString(),
            Palette.ImpostorRed,
            true, false, true, true)
        {
            this.isEatingEndCleanBody = false;
        }

        public RoleAbilityButton Button
        {
            get => this.evolveButton;
            set
            {
                this.evolveButton = value;
            }
        }
        private RoleAbilityButton evolveButton;

        public void CreateAbility()
        {
            this.defaultButtonText = Translation.GetString("evolve");

            this.CreateAbilityButton(
                this.defaultButtonText,
                Helper.Resources.LoadSpriteFromResources(
                    Resources.ResourcesPaths.TestButton, 115f),
                checkAbility: CheckAbility,
                abilityCleanUp: CleanUp,
                abilityNum: OptionsHolder.VanillaMaxPlayerNum - 1);
        }

        public bool IsAbilityUse()
        {
            setTargetDeadBody();
            return this.IsCommonUse() && this.targetBody != null;
        }

        public void CleanUp()
        {
            
            if (this.isEvolvdAnimation)
            {
                PlayerControl.LocalPlayer.RpcShapeshift(
                    PlayerControl.LocalPlayer, true);
            }

            this.KillCoolTime = this.KillCoolTime * this.reduceRate;
            this.CanKill = true;
            this.KillCoolTime = Mathf.Clamp(
                this.KillCoolTime, 0f, this.defaultKillCoolTime);

            if (!this.isEatingEndCleanBody) { return; }

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)RPCOperator.Command.CleanDeadBody,
                Hazel.SendOption.Reliable, -1);
            writer.Write(this.eatingBodyId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCOperator.CleanDeadBody(this.eatingBodyId);

            this.Button.ButtonText = this.defaultButtonText;
        }

        public bool CheckAbility()
        {
            setTargetDeadBody();

            bool result;

            if (this.targetBody == null)
            {
                result = false;
            }
            else
            {
                result = this.eatingBodyId == this.targetBody.PlayerId;
            }
            
            this.Button.ButtonText = result ? this.eatingText : this.defaultButtonText;

            return result;
        }

        public bool UseAbility()
        {
            this.eatingBodyId = this.targetBody.PlayerId;
            return true;
        }

        protected override void CreateSpecificOption(
            CustomOptionBase parentOps)
        {
            CustomOption.Create(
                GetRoleOptionId((int)EvolverOption.IsEvolvedAnimation),
                Design.ConcatString(
                    this.RoleName,
                    EvolverOption.IsEvolvedAnimation.ToString()),
                true, parentOps);

            CustomOption.Create(
                GetRoleOptionId((int)EvolverOption.IsEatingEndCleanBody),
                Design.ConcatString(
                    this.RoleName,
                    EvolverOption.IsEatingEndCleanBody.ToString()),
                true, parentOps);

            CustomOption.Create(
                GetRoleOptionId((int)EvolverOption.EatingRange),
                Design.ConcatString(
                    this.RoleName,
                    EvolverOption.EatingRange.ToString()),
                2.5f, 0.5f, 5.0f, 0.5f,
                parentOps);

            CustomOption.Create(
                GetRoleOptionId((int)EvolverOption.KillCoolReduceRate),
                Design.ConcatString(
                    this.RoleName,
                    EvolverOption.KillCoolReduceRate.ToString()),
                5, 1, 50, 1,
                parentOps, format: "unitPercentage");

            this.CreateRoleAbilityOption(
                parentOps, true, 10);
        }

        protected override void RoleSpecificInit()
        {
            this.RoleAbilityInit();

            if(!this.HasOtherKillCool)
            {
                this.HasOtherKillCool = true;
                this.KillCoolTime = PlayerControl.GameOptions.KillCooldown;
            }

            this.defaultKillCoolTime = this.KillCoolTime;
            
            var allOption = OptionsHolder.AllOption;

            this.isEvolvdAnimation = allOption[
                GetRoleOptionId((int)EvolverOption.IsEvolvedAnimation)].GetValue();
            this.isEatingEndCleanBody = allOption[
                GetRoleOptionId((int)EvolverOption.IsEatingEndCleanBody)].GetValue();
            this.eatingRange = allOption[
                GetRoleOptionId((int)EvolverOption.EatingRange)].GetValue();
            this.reduceRate = ((100f - (float)allOption[
                GetRoleOptionId((int)EvolverOption.KillCoolReduceRate)].GetValue()) / 100f);

            this.eatingText = Translation.GetString("eating");

        }

        private void setTargetDeadBody()
        {
            this.targetBody = null;

            foreach (Collider2D collider2D in Physics2D.OverlapCircleAll(
                PlayerControl.LocalPlayer.GetTruePosition(),
                this.eatingRange,
                Constants.PlayersOnlyMask))
            {
                if (collider2D.tag == "DeadBody")
                {
                    DeadBody component = collider2D.GetComponent<DeadBody>();

                    if (component && !component.Reported)
                    {
                        Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();
                        Vector2 truePosition2 = component.TruePosition;
                        if ((Vector2.Distance(truePosition2, truePosition) <= PlayerControl.LocalPlayer.MaxReportDistance) &&
                            (PlayerControl.LocalPlayer.CanMove) &&
                            (!PhysicsHelpers.AnythingBetween(
                                truePosition, truePosition2, Constants.ShipAndObjectsMask, false)))
                        {
                            this.targetBody = GameData.Instance.GetPlayerById(component.ParentId);
                            break;
                        }
                    }
                }
            }
        }
    }
}
