using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace PetAI
{
    public class ItemPetAccessory : Item, IWearableShapeSupplier
    {
        public PetAccessoryType type
        {
            get
            {
                PetAccessoryType type;
                if (Enum.TryParse<PetAccessoryType>(Attributes["cothingType"].AsString(), out type))
                {
                    return type;
                }
                else
                {
                    return PetAccessoryType.NECK;
                }
            }
        }
        public int backpackSlots => Attributes["backpackslots"].AsInt(0);
        public bool canBeWornBy(string pet) => Attributes["validPets"].AsArray<string>(new string[0]).Contains(pet);

        public Shape GetShape(ItemStack stack, Vintagestory.API.Common.Entities.Entity forEntity, string texturePrefixCode)
        {
            if (Attributes["shapes"][forEntity.Code.Path].Exists)
            {
                var compositeShape = Attributes["shapes"][forEntity.Code.Path].AsObject<CompositeShape>();
                var shapePath = compositeShape.Base.CopyWithPath("shapes/" + compositeShape.Base.Path + ".json");
                var shape = Vintagestory.API.Common.Shape.TryGet(forEntity.Api, shapePath);
                shape?.SubclassForStepParenting(texturePrefixCode);
                return shape;
            }
            return null;
        }

        public float damageReduction => Attributes["damageReduction"].AsFloat(0);
    }
    public enum PetAccessoryType
    {
        NECK, HEAD, BACK, BODY, TAIL, PAWS
    }
}