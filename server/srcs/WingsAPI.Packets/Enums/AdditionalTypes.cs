namespace WingsEmu.Packets.Enums
{
    public class AdditionalTypes
    {
        public enum AbsorbedSpirit : byte
        {
            ApplyEffectIfPresent = 11,
            ApplyEffectIfNotPresent = 12,
            ResistForcedMovement = 21,
            ResistForcedMovementNegated = 22,
            MagicCooldownIncreased = 31,
            MagicCooldownDecreased = 32
        }

        public enum Absorption : byte
        {
            AllAttackIncreased = 11,
            AllAttackDecreased = 12,
            MeleeAttackIncreased = 21,
            MeleeAttackDecreased = 22,
            RangedAttackIncreased = 31,
            RangedAttackDecreased = 32,
            MagicalAttackIncreased = 41,
            MagicalAttacksDecreased = 42
        }

        public enum AbsorptionAndPowerSkill : byte
        {
            AddDamageToHP = 11,
            RemoveDamnageFromHP = 12,
            DamageIncreasedSkill = 41,
            DamageDecreasedSkill = 42,
            CriticalIncreasedSkill = 51,
            CriticalDecreasedSkill = 52
        }

        public enum AngerSkill : byte
        {
            AttackInRangeNotLocation = 11,
            AttackInRangeNotLocationNegated = 12,
            ReduceEnemyHPChance = 21,
            ReduceEnemyHPByDamageChance = 22,
            BlockGoodEffect = 31,
            BlockGoodEffectNegated = 32,
            OnlyNormalAttacks = 41,
            OnlyNormalAttacksNegated = 42
        }

        public enum ArenaCamera : byte
        {
            CallParticipant1 = 11,
            CallParticipant2 = 12,
            CallParticipant2Negated = 21,
            CallParticipant2NegatedNegated = 22,
            CallParticipant3 = 31,
            CallParticipant3Negated = 32,
            SwitchView = 41,
            SwitchViewNegated = 42,
            SeeHiddenAllies = 51,
            SeeHiddenAlliesNegated = 52
        }

        public enum AttackPower : byte
        {
            AllAttacksIncreased = 11,
            AllAttacksDecreased = 12,
            MeleeAttacksIncreased = 21,
            MeleeAttacksDecreased = 22,
            RangedAttacksIncreased = 31,
            RangedAttacksDecreased = 32,
            MagicalAttacksIncreased = 41,
            MagicalAttacksDecreased = 42,
            AttackLevelIncreased = 51,
            AttackLevelDecreased = 52
        }

        public enum BackToMiniland : byte
        {
            BackToMinilandHpRegen = 11,
            HiddenEnemyAdditionalDamage = 21,
            HiddenEnemyDamageReceivedIncreased = 31,
            HiddenEnemyDamageReceivedDecreased = 32,
            HiddenEnemyDetection = 41,
            UseSkillManyTimes = 51
        }

        public enum BearSpirit : byte
        {
            IncreaseMaximumHP = 11,
            DecreaseMaximumHP = 12,
            IncreaseMaximumMP = 31,
            DecreaseMaximumMP = 32
        }

        public enum Block : byte
        {
            ChanceAllIncreased = 11,
            ChanceAllDecreased = 12,
            ChanceMeleeIncreased = 21,
            ChanceMeleeDecreased = 22,
            ChanceRangedIncreased = 31,
            ChanceRangedDecreased = 32,
            ChanceMagicalIncreased = 41,
            ChanceMagicalDecreased = 42
        }

        public enum Buff : byte
        {
            ChanceCausing = 11,
            ChanceRemoving = 12,
            PreventingBadEffect = 21,
            PreventingBadEffectNegated = 22,
            NearbyObjectsAboveLevel = 31,
            NearbyObjectsBelowLevel = 32,
            EffectResistance = 41,
            EffectResistanceNegated = 42,
            CancelGroupOfEffects = 51,
            CounteractPoison = 52
        }

        public enum CalculatingLevel : byte
        {
            CalculatedAttackLevel = 11,
            CalculatedAttackLevelNegated = 12,
            CalculatedDefenceLevel = 21,
            CalculatedDefenceLevelNegated = 22
        }

        public enum Capture : byte
        {
            CaptureAnimal = 11,
            CaptureAnimalNegated = 12
        }

        public enum Casting : byte
        {
            EffectDurationIncreased = 11,
            EffectDurationDecreased = 12,
            ManaForSkillsIncreased = 21,
            ManaForSkillsDecreased = 22,
            AttackSpeedIncreased = 31,
            AttackSpeedDecreased = 32,
            CastingSkillFailed = 41,
            CastingSkillFailedNegated = 42,
            InterruptCasting = 51,
            InterruptCastingNegated = 52
        }

        public enum ChangingPlace : byte
        {
            ReplaceTargetPosition = 11,
            ReplaceTargetPositionNegated = 12,
            IncreaseReputationLostAfterDeath = 21,
            DecreaseReputationLostAfterDeath = 22,
            IncreaseXpLostAfterDeath = 31,
            DecreaseXpLostAfterDeath = 32,
            IncreaseDamageVersusAngels = 41,
            DecreaseDamageVersusAngels = 42,
            IncreaseDamageVersusDemons = 51,
            DecreaseDamageVersusDemons = 52
        }

        public enum Count : byte
        {
            Summon = 11,
            SummonChance = 12,
            BelialBound = 21,
            BelialBoundNegated = 22,
            IncreaseCritAttackOnEnd = 31,
            DecreaseCritAttackOnEnd = 32,
            IncreaseCritAttackOnDefenseEnd = 41,
            DecreaseCritAttackOnDefenseEnd = 42
        }

        public enum Critical : byte
        {
            InflictingIncreased = 11,
            InflictingReduced = 12,
            DamageIncreased = 21,
            DamageIncreasedInflictingReduced = 22,
            DamageIncreasingChance = 31,
            DamageReducingChance = 32,
            ReceivingIncreased = 41,
            ReceivingDecreased = 42,
            DamageFromCriticalIncreased = 51,
            DamageFromCriticalDecreased = 52
        }

        public enum Damage : byte
        {
            DamageIncreased = 11,
            DamageDecreased = 12,
            MeleeIncreased = 21,
            MeleeDecreased = 22,
            RangedIncreased = 31,
            RangedDecreased = 32,
            MagicalIncreased = 41,
            MagicalDecreased = 42
        }

        public enum DamageConvertingSkill : byte
        {
            TransferInflictedDamage = 11,
            TransferInflictedDamageNegated = 12,
            IncreaseDamageTransfered = 21,
            DecreaseDamageTransfered = 22,
            HPRecoveryIncreased = 31,
            HPRecoveryDecreased = 32,
            AdditionalDamageCombo = 41,
            AdditionalDamageComboNegated = 42,
            ReflectMaximumReceivedDamage = 51,
            ReflectMaximumReceivedDamageNegated = 52
        }

        public enum DarkCloneSummon : byte
        {
            SummonDarkCloneChance = 11,
            SummonDarkCloneChanceNegated = 12,
            ConvertRecoveryToDamage = 21,
            ConvertRecoveryToDamageNegated = 22,
            ConvertDamageToHPChance = 31,
            ConvertDamageToHPChanceNegated = 32,
            IncreaseEnemyCooldownChance = 41,
            IncreaseEnemyCooldownChanceNegated = 42,
            DarkElementDamageIncreaseChance = 51,
            DarkElementDamageDecreaseChance = 52
        }

        public enum DealDamageAround : byte
        {
            DamageDeflect = 11,
            DamageDeflectNegated = 12,
            DealAreaDamagePerSecond = 21,
            DealAreaDamagePerSecondNegated = 22,
            SummonOnDefend = 31,
            SummonOnDefendDouble = 32,
            NosmateAttackIncrease = 41,
            NosmateAttackIncreaseNegated = 42,
            EffectiveOnEnemyInAreaPerSecond = 51,
            EffectiveOnEnemyInAreaPerSecondNegated = 52
        }

        public enum DebuffResistance : byte
        {
            IncreaseBadEffectChance = 11,
            NeverBadEffectChance = 12,
            IncreaseBadGeneralEffectChance = 21,
            NeverBadGeneralEffectChance = 22,
            IncreaseBadMagicEffectChance = 31,
            NeverBadMagicEffectChance = 32,
            IncreaseBadToxicEffectChance = 41,
            NeverBadToxicEffectChance = 42,
            IncreaseBadDiseaseEffectChance = 51,
            NeverBadDiseaseEffectChance = 52
        }

        public enum Defence : byte
        {
            AllIncreased = 11,
            AllDecreased = 12,
            MeleeIncreased = 21,
            MeleeDecreased = 22,
            RangedIncreased = 31,
            RangedDecreased = 32,
            MagicalIncreased = 41,
            MagicalDecreased = 42,
            DefenceLevelIncreased = 51,
            DefenceLevelDecreased = 52
        }

        public enum DodgeAndDefencePercent : byte
        {
            DodgeIncreased = 11,
            DodgeDecreased = 12,
            DodgingMeleeIncreased = 21,
            DodgingMeleeDecreased = 22,
            DodgingRangedIncreased = 31,
            DodgingRangedDecreased = 32,
            DefenceIncreased = 41,
            DefenceReduced = 42
        }

        public enum DragonSkills : byte
        {
            ChangeIntoDragon = 11,
            ChangeIntoHaetae = 12,
            CooldownResetChance = 21,
            CannotUseBuffChance = 31,
            CannotUseBuffChanceNegated = 32,
            ReceivedExpAndJobIncrease = 41,
            ReceivedExpAndJobIncreaseNegated = 42,
            MagicArrowChance = 51,
            MagicArrowChanceNegated = 52
        }

        public enum Drain : byte
        {
            CastDrain = 11,
            CastDrainNegated = 12,
            TransferEnemyHP = 21,
            TransferEnemyHPNegated = 22
        }

        // 21-40
        public enum DrainAndSteal : byte
        {
            ReceiveHpFromMP = 11,
            ReceiveHpFromMPNegated = 12,
            ReceiveMpFromHP = 21,
            ReceiveMpFromHPNegated = 22,
            GiveEnemyHP = 31,
            LeechEnemyHP = 32,
            GiveEnemyMP = 41,
            LeechEnemyMP = 42,
            ConvertEnemyMPToHP = 51,
            ConvertEnemyHPToMP = 52
        }

        public enum DropItemTwice : byte
        {
            DoubleDropChance = 11,
            DoubleDropChanceNegated = 12,
            EffectOnEnemyWhileAttackingChance = 21,
            EffectOnEnemyWhileAttackingChanceNegated = 22,
            EffectOnSelfWhileAttackingChance = 31,
            EffectOnSelfWhileAttackingChanceNegated = 32,
            EffectOnEnemyWhileDefendingChance = 41,
            EffectOnEnemyWhileDefendingChanceNegated = 42,
            EffectOnSelfWhileDefendingChance = 51,
            EffectOnSelfWhileDefendingChanceNegated = 52
        }

        public enum EffectSummon : byte
        {
            CooldownResetChance = 11,
            CooldownResetChanceNegated = 12,
            TeamEffectAppliedChance = 21,
            TeamEffectDeletedChance = 22,
            IfMobHigherLevelDamageIncrease = 31,
            IfMobHigherLevelDamageDecrease = 32,
            BlockNegativeEffect = 41,
            BlockNegativeEffectNegated = 42,
            ChanceToGive = 51,
            ChanceToDelete = 52
        }

        public enum Element : byte
        {
            FireIncreased = 11,
            FireDecreased = 12,
            WaterIncreased = 21,
            WaterDecreased = 22,
            LightIncreased = 31,
            LightDecreased = 32,
            DarkIncreased = 41,
            DarkDecreased = 42,
            AllIncreased = 51,
            AllDecreased = 52
        }

        public enum ElementResistance : byte
        {
            AllIncreased = 11,
            AllDecreased = 12,
            FireIncreased = 21,
            FireDecreased = 22,
            WaterIncreased = 31,
            WaterDecreased = 32,
            LightIncreased = 41,
            LightDecreased = 42,
            DarkIncreased = 51,
            DarkDecreased = 52
        }

        public enum EnemyElementResistance : byte
        {
            AllIncreased = 11,
            AllDecreased = 12,
            FireIncreased = 21,
            FireDecreased = 22,
            WaterIncreased = 31,
            WaterDecreased = 32,
            LightIncreased = 41,
            LightDecreased = 42,
            DarkIncreased = 51,
            DarkDecreased = 52
        }

        public enum FairyXPIncrease : byte
        {
            TeleportToLocation = 11,
            TeleportToLocationNegated = 12,
            IncreaseFairyXPPoints = 21,
            IncreaseFairyXPPointsNegated = 22
        }

        public enum FalconSkill : byte
        {
            CausingChanceLocation = 11,
            RemovingChanceLocation = 12,
            Hide = 21,
            HideNegated = 22,
            Ambush = 31,
            AmbushNegated = 32,
            FalconFollowing = 41,
            FalconFollowingNegated = 42,
            FalconFocusLowestHP = 51,
            FalconFocusLowestHPNegated = 52
        }

        public enum FearSkill : byte
        {
            RestoreRemainingEnemyHP = 11,
            DecreaseRemainingEnemyHP = 12,
            TimesUsed = 21,
            TimesUsedNegated = 22,
            AttackRangedIncreased = 31,
            AttackRangedDecreased = 32,
            MoveAgainstWill = 41,
            MoveAgainstWillNegated = 42,
            ProduceWhenAmbushe = 51,
            ProduceWhenAmbushNegated = 52
        }

        public enum FireCannoneerRangeBuff : byte
        {
            AOEIncreased = 11,
            AOEDecreased = 12,
            Flinch = 21,
            FlinchNegated = 22
        }

        public enum FocusEnemyAttentionSkill : byte
        {
            FocusEnemyAttention = 11

            // Unknown = 12, Unknown2 = 21, Unknown3 = 22,
        }

        public enum FourthGlacernonFamilyRaid : byte
        {
            AllInFieldReceiveDamage = 11, // Look nearly the same as 12
            AllInFieldsReceiveDamage = 12 // Look nearly the same as 11
        }

        public enum FrozenDebuff : byte
        {
            MovementLocked = 11,
            MovementLockedNegated = 12

            // Unknown = 21, Unknown2 = 22
        }

        public enum GuarantedDodgeRangedAttack : byte
        {
            AttackHitChance = 11,
            AttackHitChanceNegated = 12,
            AlwaysDodgeProbability = 21,
            AlwaysDodgeProbabilityNegated = 22,
            NoPenalty = 31,
            NoPenaltyNegated = 32,
            DistanceDamageIncreasing = 41,
            DistanceDamageIncreasingNegated = 42
        }

        public enum HealingBurningAndCasting : byte
        {
            RestoreHP = 11,
            DecreaseHP = 12,
            RestoreMP = 21,
            DecreaseMP = 22,
            RestoreHPWhenCasting = 31,
            DecreaseHPWhenCasting = 32,
            RestoreHPWhenCastingInterrupted = 41,
            DecreaseHPWhenCastingInterrupted = 42,
            HPIncreasedByConsumingMP = 51,
            HPDecreasedByConsumingMP = 52
        }

        public enum HideBarrelSkill : byte
        {
            NoHPConsumption = 11,
            NoHPRecovery = 12
        }

        public enum HPMP : byte
        {
            RestoreDecreasedHP = 11,
            DecreaseRemainingHP = 12,
            RestoreDecreasedMP = 21,
            DecreaseRemainingMP = 22,
            HPRestored = 31,
            HPReduced = 32,
            MPRestored = 41,
            MPReduced = 42,
            ReceiveAdditionalHP = 51,
            ReceiveAdditionalMP = 52
        }

        public enum HugeSnowman : byte
        {
            SnowStorm = 11,
            SnowStormNegated = 12,
            EarthQuake = 21,
            EarthQuakeNegated = 22
        }

        public enum IncreaseAllDamage : byte
        {
            AllAttackIncrease = 11,
            AllAttackDecrease = 12,
            MeleeAttackIncrease = 21,
            MeleeAttackDecrease = 22,
            RangeAttackIncrease = 31,
            RangeAttackDecrease = 32,
            MagicAttackIncrease = 41,
            MagicAttackDecrease = 42,
            ConcentrationIncrease = 51,
            ConcentrationDecrease = 52
        }

        public enum IncreaseDamage : byte
        {
            IncreasingProbability = 11,
            DecreasingProbability = 12,
            FireIncreased = 21,
            FireDecreased = 22,
            WaterIncreased = 31,
            WaterDecreased = 32,
            LightIncreased = 41,
            LightDecreased = 42,
            DarkIncreased = 51,
            DarkDecreased = 52
        }

        public enum IncreaseDamageDebuffs : byte
        {
            RuneHpEffect = 11,
            RuneHpEffectNegated = 12,
            MASP4AttackSuccussedPoints = 21,
            MASP4DefenseSuccussedPoints = 22,
            MASP4CanUseAbility = 31,
            MASP4CanUseAbilityNegated = 32,
            EffectOnShadowAttack = 41,
            EffectOnShadowAttackNegated = 42,
            AttackAndDefIncreaseOnDebuff = 51,
            AttackAndDefIncreasOnBuff = 52
        }

        public enum IncreaseDamageInLoD : byte
        {
            LodMonstersAttackIncrease = 11,
            LodMonstersAttackDecrease = 12,
            VesselMonstersAttackIncrease = 21,
            VesselMonstersAttackDecrease = 22,
            ExpGainIncrease = 31,
            ExpGainDecrease = 32,
            JobGainIncrease = 41,
            JobGainDecrease = 42,
            DodgeIncrease = 51,
            DodgeDecrease = 52
        }

        public enum IncreaseDamageVersus : byte
        {
            FireDamageIncreaseChance = 11,
            FireDamageDecreaseChance = 12,
            VesselAndLodMobDamageIncrease = 21,
            VesselAndLodMobDamageDecrease = 22,
            PvpDamageAndSpeedRainbowBattleIncrease = 31,
            PvpDamageAndSpeedRainbowBattleDecrease = 32,
            VesselAndFrozenCrownMobDamageIncrease = 41,
            VesselAndFrozenCrownMobDamageDecrease = 42
        }

        public enum IncreaseDamageVersusMonsters : byte
        {
            LowLevelPlantAttackIncrease = 11,
            LowLevelPlantAttackDecrease = 12,
            LowLevelAnimalAttackIncrease = 21,
            LowLevelAnimalAttackDecrease = 22,
            LowLevelMonsterAttackIncrease = 31,
            LowLevelMonsterAttackDecrease = 32,
            KovoltAttackIncrease = 41,
            KovoltAttackDecrease = 42,
            CatsyAttackIncrease = 51,
            CatsyAttackDecrease = 52
        }

        public enum IncreaseDamageVersusMonsters_2 : byte
        {
            LowLevelSpiritAttackIncrease = 11,
            LowLevelSpiritAttackDecrease = 12,
            AngelAttackIncrease = 21,
            AngelAttackDecrease = 22,
            DemonAttackIncrease = 31,
            DemonAttackDecrease = 32,
            LowLevelUndeadAttackIncrease = 41,
            LowLevelUndeadAttackDecrease = 42,
            ProductionPointsUseDecrease = 51,
            ProductionPointsUseDecreaseNegated = 52
        }

        public enum IncreaseElementByResis : byte
        {
            AllElementResisIncrease = 11,
            AllElementResisDecrease = 12,
            FireElementResisIncrease = 21,
            FireElementResisDecrease = 22,
            WaterElementResisIncrease = 31,
            WaterElementResisDecrease = 32,
            LightElementResisIncrease = 41,
            LightElementResisDecrease = 42,
            ShadowElementResisIncrease = 51,
            ShadowElementResisDecrease = 52
        }

        public enum IncreaseElementDamage : byte
        {
            AllElementAttackIncrease = 11,
            AllElementAttackDecrease = 12,
            FireElementAttackIncrease = 21,
            FireElementAttackDecrease = 22,
            WaterElementAttackIncrease = 31,
            WaterElementAttackDecrease = 32,
            LightElementAttackIncrease = 41,
            LightElementAttackDecrease = 42,
            ShadowElementAttackIncrease = 51,
            ShadowElementAttackDecrease = 52
        }

        public enum IncreaseElementFairy : byte
        {
            FairyElementIncrease = 11,
            FairyElementDecrease = 12,
            FairyElementIncreaseWhileAttackingChance = 21,
            FairyElementDecreaseWhileAttackingChance = 22,
            DamageToMonstersIncrease = 31,
            DamageToMonstersDecrease = 32
        }

        public enum IncreaseElementProcent : byte
        {
            FireElementIncrease = 11,
            FireElementDecrease = 12,
            WaterElementIncrease = 21,
            WaterElementDecrease = 22,
            LightElementIncrease = 31,
            LightElementDecrease = 32,
            ShadowElementIncrease = 41,
            ShadowElementDecrease = 42,
            AllElementIncrease = 51,
            AllElementDecrease = 52
        }

        public enum IncreaseSpPoints : byte
        {
            SpCardAttackPointIncrease = 11,
            SpCardAttackPointDecrease = 12,
            SpCardDefensePointIncrease = 21,
            SpCardDefensePointDecrease = 22,
            SpCardElementPointIncrease = 31,
            SpCardElementPointDecrease = 32,
            SpCardHpMpPointIncrease = 41,
            SpCardHpMpPointDecrease = 42,
            AccuracyIncrease = 51,
            AccuracyDecrease = 52
        }

        public enum InflictSkill : byte
        {
            InflictDamageAtLocation = 11,
            InflictDamageAtLocationNegated = 12
        }

        public enum Item : byte
        {
            EXPIncreased = 11,
            EXPIncreasedNegated = 12,
            AttackIncreased = 21,
            DefenceIncreased = 22,
            DropItemsWhenAttacked = 31,
            DropItemsWhenAttackedNegated = 32,
            ScrollPower = 41,
            ScrollPowerNegated = 42,
            IncreaseEarnedGold = 51,
            IncreaseEarnedGoldNegated = 52
        }

        public enum JumpBackPush : byte
        {
            JumpBackChance = 11,
            PushBackChance = 21,
            PushBackChanceNegated = 22,
            MeleeDurationIncreased = 31,
            MeleeDurationDecreased = 32,
            RangedDurationIncreased = 41,
            RangedDurationDecreased = 42,
            MagicalDurationIncreased = 51,
            MagicalDurationDecreased = 52
        }

        public enum LeonaPassiveSkill : byte
        {
            IncreaseDamageAgainst = 11,
            DecreaseDamageAgainst = 12,
            IncreaseRecoveryItems = 21,
            DecreaseRecoveryItems = 22,
            OnSPWearCausing = 31,
            OnSPWearRemoving = 32,
            DefenceIncreasedInPVP = 41,
            DefenceDecreasedInPVP = 42,
            AttackIncreasedInPVP = 51,
            AttackDecreasedInPVP = 52
        }

        public enum LightAndShadow : byte
        {
            InflictDamageToMP = 11,
            IncreaseMPByAbsorbedDamage = 12,
            RemoveBadEffects = 21,
            RemoveGoodEffects = 22,
            InflictDamageOnUndead = 31,
            HealUndead = 32,
            AdditionalDamageWhenHidden = 41,
            AdditionalDamageOnHiddenEnemy = 42
        }

        public enum LordBerios : byte
        {
            CauseDamage = 11,
            CauseDamageNegated = 12
        }

        public enum LordCalvinas : byte
        {
            InflictDamageAtLocation = 11,
            InflictDamageAtLocationNegated = 12
        }

        public enum LordHatus : byte
        {
            InflictDamageAtLocation = 11,
            InflictDamageAtLocationNegated = 12
        }

        public enum LordMorcos : byte
        {
            InflictDamageAfter = 11,
            InflictDamageAfterNegated = 12
        }

        public enum MagicShield : byte
        {
            MagicShieldDefend = 11,
            MagicShieldDefendNegated = 12,
            IncreaseElementResisOnDamageTaken = 21,
            DecreaseElementResisOnDamageTaken = 22,
            MaxAdditionalHpIncrease = 31,
            MaxAdditionalHpDecrease = 32,
            IgnoreBlock = 41,
            IgnoreBlockNegated = 42,
            DodgeIncrease = 51,
            AccuracyIncrease = 52
        }

        public enum MaxHPMP : byte
        {
            MaximumHPIncreased = 11,
            MaximumHPDecreased = 12,
            MaximumMPIncreased = 21,
            MaximumMPDecreased = 22,
            IncreasesMaximumHP = 31,
            DecreasesMaximumHP = 32,
            IncreasesMaximumMP = 41,
            DecreasesMaximumMP = 42,
            MaximumHPMPIncreased = 51,
            MaximumHPMPDecreased = 52
        }

        public enum MeditationSkill : byte
        {
            CausingChance = 11,
            RemovingChance = 12,
            ShortMeditation = 21,
            ShortMeditationNegated = 22,
            RegularMeditation = 31,
            RegularMeditationNegated = 32,
            LongMeditation = 41,
            LongMeditationNegated = 42,
            Sacrifice = 51,
            SacrificeNegated = 52
        }

        public enum MeteoriteTeleport : byte
        {
            SummonInVisualRange = 11,
            SummonInVisualRangeNegated = 12,
            TransformTarget = 21,
            TransformTargetNegated = 22,
            TeleportForward = 31,
            TeleportForwardNegated = 32,
            CauseMeteoriteFall = 41,
            CauseMeteoriteFallNegated = 42,
            TeleportYouAndGroupToSavedLocation = 51,
            TeleportYouAndGroupToSavedLocationNegated = 52
        }

        // 41-60
        public enum Mode : byte
        {
            Range = 11,
            ReturnRange = 12,
            EffectNoDamage = 21,
            DirectDamage = 22,
            AttackTimeIncreased = 31,
            AttackTimeDecreased = 32,
            ModeChance = 41,
            ModeChanceNegated = 42,
            OccuringChance = 51,
            OccuringChanceNegated = 52
        }

        public enum Morale : byte
        {
            MoraleIncreased = 11,
            MoraleDecreased = 12,
            MoraleDoubled = 21,
            MoraleHalved = 22,
            LockMorale = 31,
            LockMoraleNegated = 32,
            SkillCooldownIncreased = 41,
            SkillCooldownDecreased = 42,
            IgnoreEnemyMorale = 51,
            IgnoreEnemyMoraleNegated = 52
        }

        public enum Move : byte
        {
            MovementImpossible = 11,
            MovementImpossibleNegated = 12,
            MoveSpeedIncreasedPercentage = 21,
            MoveSpeedDecreasedPercentage = 22,
            InvisibleMovement = 31,
            InvisibleMovementNegated = 32,
            MovementSpeedIncreased = 41,
            MovementSpeedDecreased = 42,
            TempMaximized = 51,
            TempMaximizedNegated = 52
        }

        public enum MultAttack : byte
        {
            AllAttackIncreased = 11,
            AllAttackDecreased = 12,
            MeleeAttackIncreased = 21,
            MeleeAttackDecreased = 22,
            RangedAttackIncreased = 31,
            RangedAttackDecreased = 32,
            MagicalAttackIncreased = 41,
            MagicalAttackDecreased = 42
        }

        public enum MultDefence : byte
        {
            AllDefenceIncreased = 11,
            AllDefenceDecreased = 12,
            MeleeDefenceIncreased = 21,
            MeleeDefenceDecreased = 22,
            RangedDefenceIncreased = 31,
            RangedDefenceDecreased = 32,
            MagicalDefenceIncreased = 41,
            MagicalDefenceDecreased = 42
        }

        public enum NoCharacteristicValue : byte
        {
            AllPowersNullified = 11,
            AllResistancesNullified = 12,
            FireElementNullified = 21,
            FireResistanceNullified = 22,
            WaterElementNullified = 31,
            WaterResistanceNullified = 32,
            LightElementNullified = 41,
            LightResistanceNullified = 42,
            DarkElementNullified = 51,
            DarkResistanceNullified = 52
        }

        public enum NoDefeatAndNoDamage : byte
        {
            DecreaseHPNoDeath = 11,
            DecreaseHPNoKill = 12,
            NeverReceiveDamage = 21,
            NeverCauseDamage = 22,
            TransferAttackPower = 31,
            TransferAttackPowerNegated = 32
        }

        public enum Quest : byte
        {
            SummonMonsterBased = 11,
            SummonMonsterBasedNegated = 12
        }

        public enum Recovery : byte
        {
            HPRecoveryIncreased = 11,
            HPRecoveryDecreased = 12,
            MPRecoveryIncreased = 21,
            MPRecoveryDecreased = 22
        }

        public enum RecoveryAndDamagePercent : byte
        {
            HPRecovered = 11,
            HPReduced = 12,
            MPRecovered = 21,
            MPReduced = 22,
            DecreaseEnemyHP = 31,
            DecreaseSelfHP = 32
        }

        public enum ReflectDamage : byte
        {
            DeflectDamageOnCrit = 11,
            DeflectDamageOnCritNegated = 12,
            DamageDodge = 21,
            DamageDodgeNegated = 22,
            CritAttackIncrease = 31,
            CritAttackIncreaseNegated = 32,
            TakeMpOnDamage = 41,
            TakeMpOnDamageNegated = 42,
            AllAttackIncreasePerMagicDefense = 51,
            AllAttackIncreasePerMagicDefenseNegated = 52
        }

        public enum Reflection : byte
        {
            HPIncreased = 11,
            HPDecreased = 12,
            MPIncreased = 21,
            MPDecreased = 22,
            EnemyHPIncreased = 31,
            EnemyHPDecreased = 32,
            EnemyMPIncreased = 41,
            EnemyMPDecreased = 42,
            ChanceMpLost = 52
        }

        public enum ReputHeroLevel : byte
        {
            ReputIncreased = 11,
            ReputDecreased = 12,
            ReceivedHeroExpIncrease = 21,
            ReceivedHeroExpDecrease = 22,
            IfInTeamGetItemPerHours = 31,
            IfInTeamGetItemPerHoursNegated = 32,
            EnemyEffectDeleteChance = 41,
            EnemyEffectDeleteChanceNegated = 42,
            CreateBuffDragonVitality = 51,
            CreateBuffStrongDragonVitality = 52
        }

        public enum Runes_1 : byte
        {
            ApocalypsePowerOnAttack = 11,
            ApocalypsePowerOnAttackNegated = 12,
            ReflectionPowerOnAttack = 21,
            ReflectionPowerOnAttackNegated = 22,
            WolfPowerOnAttack = 31,
            WolfPowerOnAttackNegated = 32,
            EnemyPushOnAttack = 41,
            EnemyPushOnDefend = 42,
            ExplosionPowerOnMeleeAttack = 51,
            ExplosionPowerOnMeleeAttackNegated = 52
        }

        public enum Runes_2 : byte
        {
            AgilityPowerOnAttack = 11,
            AgilityPowerOnAttackNegated = 12,
            LightningPowerOnAttack = 21,
            LightningPowerOnAttackNegated = 22,
            CursePowerOnAttack = 31,
            CursePowerOnAttackNegated = 32,
            BearPowerOnAttack = 41,
            BearPowerOnAttackNegated = 42,
            FrostPowerOnAttack = 51,
            FrostPowerOnAttackNegated = 52
        }

        public enum SecondSPCard : byte
        {
            PlantBomb = 11,
            SetBombWhenAttack = 12, // Same as 22!
            PlantSelfDestructionBomb = 21,
            PlantBombWhenAttack = 22, // Same as 12!
            ReduceEnemySkill = 31,
            ReduceEnemySkillNegated = 32,
            HitAttacker = 41,
            HitAttackerNegated = 42
        }

        public enum SESpecialist : byte
        {
            EnterNumberOfBuffsAndDamage = 11,
            EnterNumberOfBuffs = 12,
            MovingAura = 31,
            DontNeedToEnter = 32,
            LowerHPStrongerEffect = 41,
            DoNotNeedToEnter = 42
        }

        public enum SniperAttack : byte
        {
            ChanceCausing = 11,
            ChanceRemoving = 12,
            AmbushRangeIncreased = 21,
            AmbushRangeIncreasedNegated = 22,
            ProduceChance = 31,
            ProduceChanceNegated = 32,
            KillerHPReducing = 41,
            KillerHPIncreasing = 42,
            ReceiveCriticalFromSniper = 51,
            ReceiveCriticalFromSniperNegated = 52
        }

        public enum SP2MA_1 : byte
        {
            DodgeAndMakeChance = 11,
            DodgeAndMakeChanceNegated = 12,
            EnhancementWhenEnlightened = 21,
            FullMoonSkillUse = 31,
            LotusFlowerSkillUse = 41,
            SignUseNextAttackIncrease = 51,
            SignUseNextDamageTakenDecrease = 52
        }

        public enum SP2MA_2 : byte
        {
            AchieveWhenAttacked = 11,
            AchieveWhenAttackedNegated = 12,
            AdditionalAttackChance = 21,
            AdditionalAttackChanceNegated = 22,
            SignUseMarkedEnemyAttackIncrease = 31,
            SignUseMarkedEnemyAttackIncreaseNegated = 32,
            CreateFullMoonBoundWhenEnemyBoundByMoonlight = 41,
            CreateFullMoonBoundWhenEnemyBoundByMoonlightNegated = 42
        }

        public enum SPCardUpgrade : byte
        {
            LowerSPScroll = 11,
            LowerSPScrollNegated = 12,
            HigherSPScroll = 21,
            HigherSPScrollNegated = 22
        }

        public enum SpecialActions : byte
        {
            PushBack = 11,
            PushBackNegated = 12,
            FocusEnemies = 21,
            FocusEnemiesNegated = 22,
            Charge = 31,
            ChargeNegated = 32,
            RunAway = 41,
            RunAwayNegated = 42,
            Hide = 51,
            SeeHiddenThings = 52
        }

        // 1-20
        public enum SpecialAttack : byte
        {
            NoAttack = 11,
            NoAttackNegated = 12,
            MeleeDisabled = 21,
            MeleeDisabledNegated = 22,
            RangedDisabled = 31,
            RangedDisabledNegated = 32,
            MagicDisabled = 41,
            MagicDisabledNegated = 42,
            FailIfMiss = 51,
            FailIfMissNegated = 52
        }

        public enum SpecialBehaviour : byte
        {
            TeleportRandom = 11,
            TeleportRandomNegated = 12,
            JumpToEveryObject = 21,
            JumpToEveryObjectNegated = 22,
            InflictOnTeam = 31,
            InflictOnEnemies = 32,
            TransformInto = 41,
            TransformIntoNegated = 42
        }

        public enum SpecialCritical : byte
        {
            AlwaysInflict = 11,
            AlwaysInflictNegated = 12,
            NeverInflict = 21,
            NeverInflictNegated = 22,
            AlwaysReceives = 31,
            AlwaysReceivesNegated = 32,
            NeverReceives = 41,
            NeverReceivesNegated = 42,
            InflictingChancePercent = 51,
            ReceivingChancePercent = 52
        }

        public enum SpecialDamageAndExplosions : byte
        {
            ChanceExplosion = 11,
            ChanceExplosionNegated = 12,
            ExplosionCauses = 21,
            ExplosionCausesNegated = 22,
            SurroundingDamage = 31,
            SurroundingDamageNegated = 32
        }

        public enum SpecialDefence : byte
        {
            AllDefenceNullified = 11,
            AllDefenceNullifiedNegated = 12,
            MeleeDefenceNullified = 21,
            MeleeDefenceNullifiedNegated = 22,
            RangedDefenceNullified = 31,
            RangedDefenceNullifiedNegated = 32,
            MagicDefenceNullified = 41,
            MagicDefenceNullifiedNegated = 42,
            NoDefence = 51,
            NoDefenceNegated = 52
        }

        public enum SpecialEffects : byte
        {
            DecreaseKillerHP = 11,
            IncreaseKillerHP = 12,
            ToPrefferedAttack = 21,
            ToNonPrefferedAttack = 22,
            Gibberish = 31,
            GibberishNegated = 32,
            AbleToFightPVP = 41,
            AbleToFightPVPNegated = 42,
            ShadowAppears = 51,
            ShadowAppearsNegated = 52
        }

        public enum SpecialEffects2 : byte
        {
            FocusEnemy = 11,
            RemoveEnemyAttention = 12,
            TeleportInRadius = 21,
            TeleportInRadiusNegated = 22,
            MainWeaponCausingChance = 31,
            MainWeaponCausingChanceNegated = 32,
            SecondaryWeaponCausingChance = 41,
            SecondaryWeaponCausingChanceNegated = 42,
            BefriendMonsters = 51,
            BefriendMonstersNegated = 52
        }

        public enum SpecialisationBuffResistance : byte
        {
            IncreaseDamageAgainst = 11,
            ReduceDamageAgainst = 12,
            IncreaseCriticalAgainst = 21,
            ReduceCriticalAgainst = 22,
            ResistanceToEffect = 31,
            ResistanceToEffectNegated = 32,
            IncreaseDamageInPVP = 41,
            DecreaseDamageInPVP = 42,
            RemoveGoodEffects = 51,
            RemoveBadEffects = 52
        }

        public enum StealBuff : byte
        {
            IgnoreDefenceChance = 11,
            IgnoreDefenceChanceNegated = 12,
            ReduceCriticalReceivedChance = 21,
            ReduceCriticalReceivedChanceNegated = 22,
            ChanceSummonOnyxDragon = 31,
            ChanceSummonOnyxDragonNegated = 32,
            StealGoodEffect = 41,
            StealGoodEffectNegated = 42
        }

        public enum SummonAndRecoverHP : byte
        {
            ChanceSummon = 11,
            ChanceSummonNegated = 12,
            RestoreHP = 21,
            ReduceHP = 22
        }

        public enum Summons : byte
        {
            SummonUponDeath = 11,
            SummonUponDeathChance = 12,
            Summons = 21,
            SummonningChance = 22,
            SummonTrainingDummy = 31,
            SummonTrainingDummyChance = 32,
            SummonTimedMonsters = 41,
            SummonTimedMonstersChance = 42,
            SummonGhostMP = 51,
            SummonGhostMPChance = 52
        }

        public enum SummonSkill : byte
        {
            Summon = 31,
            SummonTimed = 32
        }

        public enum Target : byte
        {
            AllHitRateIncreased = 11,
            AllHitRateDecreased = 12,
            MeleeHitRateIncreased = 21,
            MeleeHitRateDecreased = 22,
            RangedHitRateIncreased = 31,
            RangedHitRateDecreased = 32,
            MagicalConcentrationIncreased = 41,
            MagicalConcentrationDecreased = 42
        }

        public enum TauntSkill : byte
        {
            ReflectsMaximumDamageFrom = 11,
            ReflectsMaximumDamageFromNegated = 12,
            DamageInflictedIncreased = 21,
            DamageInflictedDecreased = 22,
            EffectOnKill = 31,
            EffectOnKillNegated = 32,
            TauntWhenKnockdown = 41,
            TauntWhenNormal = 42,
            ReflectBadEffect = 51,
            ReflectBadEffectNegated = 52
        }

        public enum TeamArenaBuff : byte
        {
            DamageTakenIncreased = 11,
            DamageTakenDecreased = 12,
            AttackPowerIncreased = 21,
            AttackPowerDecreased = 22
        }

        public enum TimeCircleSkills : byte
        {
            GatherEnergy = 11,
            GatherEnergyNegated = 12,
            DisableHPConsumption = 21,
            DisableHPRecovery = 22,
            DisableMPConsumption = 31,
            DisableMPRecovery = 32,
            CancelAllBuff = 41,
            CancelAllBuffNegated = 42,
            ItemCannotBeUsed = 51,
            ItemCannotBeUsedNegated = 52
        }

        public enum VulcanoElementBuff : byte
        {
            SkillsIncreased = 11,
            SkillsDecreased = 12,
            ReducesEnemyAttack = 21,
            ReducesEnemyAttackNegated = 22,
            PullBackBuffIncreasing = 31,
            PullBackBuffIncreasingNegated = 32,
            CriticalDefence = 41,
            CriticalDefenceNegated = 42
        }
    }
}