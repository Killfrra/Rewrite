using System;
using System.Numerics;
using System.Collections.Generic;

namespace Scripts {
    using Engine;

    class ExampleSpell: Spell {
        protected internal override void OnCast(){
            var area = new CircleAreaShape(Owner.Position, 200);
            var units = APIDraft.GetUnitsInTargetArea(Owner, area, maximumUnitsToPick: 1);
            var victim = units.First();

            victim.Buffs.Add(new Slow(3, -0.2f, -0.1f));
        }
    }

    class Slow: OldBuff {
        //TODO: StatModifier
        public Slow(float duration, float moveSpeedMod, float attackSpeedMod){
            AddType = BuffAddType.STACKS_AND_OVERLAPS;
            //StacksExclusive = true
            Type = BuffType.Slow;
            MaxStacks = 100;
            Stacks = 1;
            Duration = duration;
            IsHiddenOnClient = false;
        }
    }

    class OldBuff: Buff {
        public BuffAddType AddType { get; init; }
        public override void Stack(Buff buff){
            var type = (buff as OldBuff)?.AddType ?? AddType;
            if(type == BuffAddType.REPLACE_EXISTING){
                ReplaceWith(buff);
            } else if(type == BuffAddType.STACKS_AND_CONTINUE){
                AddStacks(buff.Stacks);
            } else if(type == BuffAddType.STACKS_AND_RENEWS){
                AddStacksAndRenew(buff.Stacks, buff.Duration);
            } else if(type == BuffAddType.STACKS_AND_OVERLAPS){
                OverlapWith(buff);
            }
        }
        protected internal override void OnUpdate(float diff)
        {
            if(RemainingDuration <= 0){
                if(
                    AddType == BuffAddType.REPLACE_EXISTING ||
                    AddType == BuffAddType.STACKS_AND_RENEWS ||
                    AddType == BuffAddType.STACKS_AND_OVERLAPS
                ){
                    Remove();
                } else if(
                    AddType == BuffAddType.STACKS_AND_CONTINUE
                ){
                    AddStacksAndRenew(-1, Duration);
                }
            }
        }
    }
}

namespace Engine {
    enum BuffType: int {
        Internal,
        Haste,
        Aura,
        CombatEnchancer,
        Damage,
        Shred,
        Slow,
        CombatDehancer,
        Invisibility,
        Suppression,
        Net,
        Heal,
        Stun,
        AmmoStack,
        Invulnerability,
        Silence,
        Poison,
        Snare,
        Blind,
        SpellImmunity,
    }
    enum BuffAddType: int {
        REPLACE_EXISTING,
        STACKS_AND_RENEWS,
        STACKS_AND_OVERLAPS,
        STACKS_AND_CONTINUE
    }
    enum SpellFlags : int {
        AutoCast = 0x2,
        InstantCast = 0x4,
        PersistThroughDeath = 0x8,
        NonDispellable = 0x10,
        NoClick = 0x20,
        AffectImportantBotTargets = 0x40,
        AllowWhileTaunted = 0x80,
        NotAffectZombie = 0x100,
        AffectUntargetable = 0x200,
        AffectEnemies = 0x400,
        AffectFriends = 0x800,
        AffectNeutral = 0x4000,
        AffectAllSides = 0x4C00,
        AffectBuildings = 0x1000,
        AffectMinions = 0x8000,
        AffectHeroes = 0x10000,
        AffectTurrets = 0x20000,
        AffectAllUnitTypes = 0x38000,
        NotAffectSelf = 0x2000,
        AlwaysSelf = 0x40000,
        AffectDead = 0x80000,
        AffectNotPet = 0x100000,
        AffectBarracksOnly = 0x200000,
        IgnoreVisibilityCheck = 0x400000,
        NonTargetableAlly = 0x800000,
        NonTargetableEnemy = 0x1000000,
        TargetableToAll = 0x2000000,
        NonTargetableAll = 0x1800000,
        AffectWards = 0x4000000,
        AffectUseable = 0x8000000,
        IgnoreAllyMinion = 0x10000000,
        IgnoreEnemyMinion = 0x20000000,
        IgnoreLaneMinion = 0x40000000,
        IgnoreClones = 0x80000000,
    }

    class Game {
        public static float Time;
    }

    class Buff {
        public int Slot = -1;
        public AttackableUnit Owner { get; init; }
        public ObjAIBase Caster { get; init; }
        public Spell Spell { get; init; }
        public BuffType Type { get; init; }
        public int MinStacks { get; init; } = 1;
        public int MaxStacks { get; init; } = 1;
        public int Stacks { get; protected set; } = 1;
        public float Duration { get; protected set; }
        public float StartTime;
        public float RemainingDuration => Duration - (Game.Time - StartTime);
        public bool IsHiddenOnClient { get; init; }

        public void AddStacks(int delta){
            int prevValue = Stacks;
            int unclamped = prevValue + delta;
            Stacks = Math.Clamp(unclamped, MinStacks, MaxStacks);
            if(Stacks != prevValue){
                OnUpdateAmmo(prevValue, delta);
            }
        }
        public void Renew(float duration){
            Duration = duration; // + RemainingDuration
            StartTime = Game.Time;
        }
        public void AddStacksAndRenew(int stacks, float duration){
            AddStacks(stacks);
            Renew(duration);
        }

        public void ReplaceWith(Buff buff){
            Owner.Buffs.Replace(this, buff);
        }
        public void OverlapWith(Buff buff){
            Owner.Buffs.Overlap(this, buff);
        }
        public void Remove(){
            Owner.Buffs.Remove(this);
        }

        public virtual void Stack(Buff buff){}

        protected internal virtual void OnActivate(){}
        protected internal virtual void OnDeactivate(){}
        protected internal virtual void OnUpdate(float diff){}
        // OnUpdateStacks
        protected virtual void OnUpdateAmmo(int prevValue, int delta){
            int unclamped = prevValue + delta;
            if(unclamped < MinStacks)
            {
                Remove();
            }
        }

        //TODO: static?
        protected internal virtual void PreLoad(){}

        protected internal virtual void OnAllowAdd(Buff buff){}
        protected internal virtual void OnBeingDodged(){}

        //TODO:
        protected internal virtual void OnAssist(){}
        protected internal virtual void OnBeingHit(){}
        protected internal virtual void OnBeingSpellHit(){}
        protected internal virtual void OnCollision(){}
        protected internal virtual void OnCollisionTerrain(){}
        protected internal virtual void OnDealDamage(){}
        protected internal virtual void OnDeath(){}
        protected internal virtual void OnDisconnect(){}
        protected internal virtual void OnHeal(){}
        protected internal virtual void OnHitUnit(){}
        protected internal virtual void OnKill(){}
        protected internal virtual void OnLaunchAttack(){}
        protected internal virtual void OnLaunchMissile(){}
        protected internal virtual void OnLevelUp(){}
        protected internal virtual void OnLevelUpSpell(){}
        protected internal virtual void OnMiss(){}
        protected internal virtual void OnMissileEnd(){}
        protected internal virtual void OnMoveEnd(){}
        protected internal virtual void OnMoveFailure(){}
        protected internal virtual void OnMoveSuccess(){}
        protected internal virtual void OnPreAttack(){}
        protected internal virtual void OnPreDamage(){}
        protected internal virtual void OnPreDealDamage(){}
        protected internal virtual void OnPreMitigationDamage(){}
        protected internal virtual void OnReconnect(){}
        protected internal virtual void OnResurrect(){}
        protected internal virtual void OnSpellCast(){}
        protected internal virtual void OnSpellHit(){}
        protected internal virtual void OnTakeDamage(){}
        protected internal virtual void OnZombie(){}

        protected internal virtual void OnUpdateActions(){}
        protected internal virtual void OnUpdateStats(){}

        protected void SetCurrentBuffToolTipVar(int index, int value){
            
        }
    }

    class BuffManager {
        AttackableUnit _owner;
        public BuffManager(AttackableUnit owner){
            _owner = owner;
        }
        List<Buff> _buffs = new();

        public bool Has(Type type){
            return Get(type) != null;
        }
        public Buff? Get(Type type){
            return GetAll(type).First();
        }
        public IEnumerable<Buff> GetAll(Type type){
            for(int i = _buffs.Count; i >= 0; i--){
                var buff = _buffs[i];
            //foreach(var buff in _buffs){
                if(buff.GetType() == type){
                    yield return buff;
                }
            }
        }

        public bool Has(BuffType type){
            return Get(type) != null;
        }
        public Buff? Get(BuffType type){
            return GetAll(type).First();
        }
        public IEnumerable<Buff> GetAll(BuffType type){
            for(int i = _buffs.Count; i >= 0; i--){
                var buff = _buffs[i];
            //foreach(var buff in _buffs){
                if(buff?.Type == type){
                    yield return buff;
                }
            }
        }

        public void Add(Buff buff){
            Buff? existing = Get(buff.GetType());
            if(existing != null){
                existing.Stack(buff);
            } else {
                AddToNewSlot(buff);
            }
        }
        public void AddToNewSlot(Buff buff){
            int freeSlot = 0; //TODO:
            AddToSlot(freeSlot, buff);
        }
        // Made private to avoid checks.
        private void AddToSlot(int slot, Buff buff){
            buff.Slot = slot;
            buff.OnActivate();
            _buffs.Add(buff);
        }
        public void Remove(Buff buff){
            buff.Slot = -1;
            buff.OnDeactivate();
            _buffs.Remove(buff);
        }
        public void Replace(Buff one, Buff another){
            AddToSlot(one.Slot, another);
            Remove(one);
        }
        public void Overlap(Buff one, Buff another){
            AddToSlot(one.Slot, another);
        }

        public void Clear(Type type){
            foreach(var buff in GetAll(type)){
                Remove(buff);
            }
        }
        public void Clear(BuffType type){
            foreach(var buff in GetAll(type)){
                Remove(buff);
            }
        }
        public void ClearNegative(){
            Clear(
                BuffType.CombatDehancer |
                BuffType.Suppression |
                0 //TODO:
            );
        }
        
        int Count(Type type, ObjAIBase? caster = null){
            int count = 0;
            foreach(var buff in _buffs){
                if(
                    buff.GetType() == type &&
                    (caster == null || buff.Caster == caster)
                ){
                    count++;
                }
            }
            return count;
        }
    }

    class Item {
        public ObjAIBase Owner { get; init; }

        protected internal virtual void OnActivate(){}
        protected internal virtual void OnDeactivate(){}

        //TODO:
        protected internal virtual void OnAssist(){}
        protected internal virtual void OnBeingDodged(){}
        protected internal virtual void OnBeingHit(){}
        protected internal virtual void OnDealDamage(){}
        protected internal virtual void OnDeath(){}
        protected internal virtual void OnHitUnit(){}
        protected internal virtual void OnKill(){}
        protected internal virtual void OnMiss(){}
        protected internal virtual void OnPreDamage(){}
        protected internal virtual void OnPreDealDamage(){}
        protected internal virtual void OnSpellCast(){}
    }
    class ItemManager {
        ObjAIBase _owner;
        public ItemManager(ObjAIBase owner){
            _owner = owner;
        }
        List<Spell> _items = new();
    }
    class Spell {
        public ObjAIBase Owner { get; init; }

        protected internal virtual void OnActivate(){}
        protected internal virtual void OnDeactivate(){}

        //TODO:
        protected internal virtual void OnCast(){}
    }
    class SpellManager {
        ObjAIBase _owner;
        public SpellManager(ObjAIBase owner){
            _owner = owner;
        }
        List<Spell> _spells = new();
    }

    class CharacterScript {
        protected internal virtual void OnActivate(){}

        protected internal virtual void OnAssistUnit(){}
        protected internal virtual void OnBeingHit(){}
        protected internal virtual void OnDisconnect(){}
        protected internal virtual void OnDodge(){}
        protected internal virtual void OnHitUnit(){}
        protected internal virtual void OnKillUnit(){}
        protected internal virtual void OnLaunchAttack(){}
        protected internal virtual void OnLevelUp(){}
        protected internal virtual void OnLevelUpSpell(){}
        protected internal virtual void OnMiss(){}
        protected internal virtual void OnNearbyDeath(){}
        protected internal virtual void OnPreAttack(){}
        protected internal virtual void OnPreDamage(){}
        protected internal virtual void OnPreDealDamage(){}
        protected internal virtual void OnReconnect(){}
        protected internal virtual void OnResurrect(){}
        protected internal virtual void OnSpellCast(){}
    }

    class AIScript {
        protected internal virtual void OnActivate(){}

        //TODO:
        protected internal virtual void OnOrder(OrderType order){}
        protected internal virtual void OnTargetLost(Reason reason, AttackableUnit target){}
        protected internal virtual void OnTauntBegin(){}
        protected internal virtual void OnTauntEnd(){}
        protected internal virtual void OnFearBegin(){}
        protected internal virtual void OnFearEnd(){}
        protected internal virtual void OnCharmBegin(){}
        protected internal virtual void OnCharmEnd(){}
        protected internal virtual void OnStopMove(){}
        protected internal virtual void OnAICommand(){}
        protected internal virtual void OnReachedDestinationForGoingToLastLocation(){}
        protected internal virtual void HaltAI(){}

        
    }

    class GameObject {
        public Vector2 Position;
    }
    class AttackableUnit: GameObject {
        public BuffManager Buffs;
        public AttackableUnit(){
            Buffs = new(this);
        }
    }
    class ObjAIBase: AttackableUnit {
        public ItemManager Items;
        public SpellManager Spells;
        public CharacterScript CharScript;
        public AIScript AI;
        public ObjAIBase(): base(){
            Items = new(this);
            Spells = new(this);
        }
    }
    class AreaShape {}
    class CircleAreaShape: AreaShape {
        Vector2 Center;
        float Range;
        public CircleAreaShape(Vector2 center, float range){
            Center = center;
            Range = range;
        }
    }
    class RectangleAreaShape: AreaShape {
        Vector2 Center;
        float HalfWidth;
        float HalfLength;
    }

    class APIDraft{
        public static IEnumerable<AttackableUnit> GetUnitsInTargetArea
        (
            ObjAIBase attacker,
            AreaShape shape,
            SpellFlags flags = 0,
            string? buffNameFilter = null,
            bool inclusiveBuffFilter = false,
            int maximumUnitsToPick = int.MaxValue, // Randomly selects results.
            bool visible = false // Whether to select only visible units.
        ){
            yield return null;
        }
    }
}