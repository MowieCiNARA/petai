using Vintagestory.API.Config;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace PetAI
{
    public class EntityPet : EntityAgent
    {

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            if (api.Side == EnumAppSide.Server)
            {
                GetBehavior<EntityBehaviorHealth>().onDamaged += (dmg, dmgSource) => applyPetArmor(dmg, dmgSource);
            }
        }

        public override string GetInfoText()
        {
            var tameable = GetBehavior<EntityBehaviorTameable>();
            var owner = tameable?.cachedOwner;
            if (owner == null) return base.GetInfoText();

            return string.Concat(base.GetInfoText(),
                    "\n",
                    Lang.Get("petai:gui-pet-owner", owner?.PlayerName),
                    "\n",
                    tameable.domesticationLevel == DomesticationLevel.DOMESTICATED ? Lang.Get("petai:gui-pet-obedience", Math.Round(tameable.obedience * 100, 2)) : Lang.Get("petai:gui-pet-domesticationProgress", Math.Round(tameable.domesticationProgress * 100, 2)),
                    "\n",
                    Lang.Get("petai:gui-pet-nestsize", Lang.Get("petai:gui-pet-nestsize-" + tameable.size.ToString().ToLower())));
        }

        public void DropInventoryOnGround()
        {
            for (int i = this.GetBehavior<EntityBehaviorAttachable>().Inventory.Count - 1; i >= 0; i--)
            {
                if (this.GetBehavior<EntityBehaviorAttachable>().Inventory[i].Empty) { continue; }

                Api.World.SpawnItemEntity(this.GetBehavior<EntityBehaviorAttachable>().Inventory[i].TakeOutWhole(), Pos.XYZ);
                this.GetBehavior<EntityBehaviorAttachable>().Inventory.MarkSlotDirty(i);
            }
        }

        private float applyPetArmor(float dmg, DamageSource dmgSource)
        {
            if (dmgSource.SourceEntity != null && dmgSource.Type != EnumDamageType.Heal)
            {
                foreach (var item in this.GetBehavior<EntityBehaviorAttachable>().Inventory)
                {
                    if (item != null && item.Itemstack != null && item.Itemstack.Item != null)
                    {
                        if (item.Itemstack.Item is ItemPetAccessory)
                        {
                            dmg *= (1.0f - (item.Itemstack.Item as ItemPetAccessory).damageReduction);
                        }
                    }
                }
            }
            return dmg;
        }

        public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
        {
            string ownerOfPet = GetBehavior<EntityBehaviorTameable>()?.ownerId;
            bool isOwnerOfPet = false;
            if (damageSource.Source == EnumDamageSource.Player)
            {
                if (damageSource.SourceEntity is EntityPlayer)
                {
                    isOwnerOfPet = ((EntityPlayer)damageSource.SourceEntity).PlayerUID == ownerOfPet;
                }
            }
            if (damageSource.CauseEntity is EntityPlayer) {
                isOwnerOfPet = ((EntityPlayer)damageSource.SourceEntity).PlayerUID == ownerOfPet;
            }
            if ((PetConfig.Current.PvpOff
                && GetBehavior<EntityBehaviorTameable>()?.domesticationLevel != DomesticationLevel.WILD
                && !isOwnerOfPet)
                || (damageSource.Source == EnumDamageSource.Fall
                && PetConfig.Current.FalldamageOff)
                || (isOwnerOfPet 
                && PetConfig.Current.SelfPetsDamageOff))
            {
                return false;
            }
            return base.ShouldReceiveDamage(damageSource, damage);
        }
    }
}