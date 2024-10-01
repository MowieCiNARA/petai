using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PetAI
{
    // This is rather stupid design as it just copies the existing Grow-Behavior
    // Maybe there can be done something more clever with harmony patching
    public class EntityBehaviorRaisable : EntityBehavior
    {
        ITreeAttribute growTree;
        JsonObject typeAttributes;
        long callbackId;

        float _HoursToGrow;

        bool keepTexture;

        internal float HoursToGrow
        {
            get { return _HoursToGrow * PetConfig.Current.Difficulty.growingMultiplier; }
            set { _HoursToGrow = value; }
        }

        internal AssetLocation[] AdultEntityCodes
        {
            get { return AssetLocation.toLocations(typeAttributes["adultEntityCodes"].AsArray<string>(new string[0])); }
        }

        internal double TimeSpawned
        {
            get { return growTree.GetDouble("timeSpawned"); }
            set { growTree.SetDouble("timeSpawned", value); }
        }



        public EntityBehaviorRaisable(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            base.Initialize(properties, typeAttributes);

            this.typeAttributes = typeAttributes;
            HoursToGrow = typeAttributes["hoursToGrow"].AsFloat(96);

            growTree = entity.WatchedAttributes.GetTreeAttribute("grow");

            if (growTree == null)
            {
                entity.WatchedAttributes.SetAttribute("grow", growTree = new TreeAttribute());
                TimeSpawned = entity.World.Calendar.TotalHours;
            }

            keepTexture = typeAttributes["keepTexture"].AsBool(false);

            callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
        }


        private void CheckGrowth(float dt)
        {
            if (!entity.Alive) return;

            if (entity.World.Calendar.TotalHours >= TimeSpawned + HoursToGrow)
            {
                AssetLocation[] entityCodes = AdultEntityCodes;
                if (entityCodes.Length == 0) return;
                AssetLocation code = entityCodes[entity.World.Rand.Next(entityCodes.Length)];

                EntityProperties adultType = entity.World.GetEntityType(code);

                if (adultType == null)
                {
                    entity.World.Logger.Error("Misconfigured entity. Entity with code '{0}' is configured (via Grow behavior) to grow into '{1}', but no such entity type was registered.", entity.Code, code);
                    return;
                }

                Cuboidf collisionBox = adultType.SpawnCollisionBox;

                // Delay adult spawning if we're colliding
                if (entity.World.CollisionTester.IsColliding(entity.World.BlockAccessor, collisionBox, entity.ServerPos.XYZ, false))
                {
                    callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
                    return;
                }

                Entity adult = entity.World.ClassRegistry.CreateEntity(adultType);

                adult.ServerPos.SetFrom(entity.ServerPos);
                adult.Pos.SetFrom(adult.ServerPos);

                entity.Die(EnumDespawnReason.Expire, null);
                entity.World.SpawnEntity(adult);

                if (adult.HasBehavior<EntityBehaviorTameable>())
                {
                    adult.GetBehavior<EntityBehaviorTameable>().domesticationStatus = entity.GetBehavior<EntityBehaviorTameable>().domesticationStatus;
                }
                adult.GetBehavior<EntityBehaviorNameTag>()?.SetName(entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName);
                if (keepTexture)
                {
                    //Attempt to not change the texture during growing up
                    adult.WatchedAttributes.SetInt("textureIndex", entity.WatchedAttributes.GetInt("textureIndex", 0));
                }
                if(entity is EntityPet childPet && adult is EntityPet adultPet){

                }
            }
            else
            {
                callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
                double age = entity.World.Calendar.TotalHours - TimeSpawned;
                if (age >= 0.1 * HoursToGrow)
                {
                    float newAge = (float)(age / HoursToGrow - 0.1);
                    if (newAge >= 1.01f * growTree.GetFloat("age"))
                    {
                        growTree.SetFloat("age", newAge);
                        entity.WatchedAttributes.MarkPathDirty("grow");
                    }
                }
            }

            entity.World.FrameProfiler.Mark("entity-checkgrowth");
        }

        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            entity.World.UnregisterCallback(callbackId);
        }

        public override string PropertyName()
        {
            return "raisable";
        }
    }
}