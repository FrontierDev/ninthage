using Game.Server;
using Game.Shared.Persistence;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Runtime.Tests.Persistence
{
    public sealed class CharacterPersistenceRoundTripTests
    {
        private const string FIXTURE_RELATIVE_PATH = "Tests/Fixtures/player_data_phase0_baseline.json";

        [Test]
        public void CurrentPlayerDataFixtureRoundTripsWithoutLosingCharacterState()
        {
            var fixturePath = Path.Combine(Application.dataPath, FIXTURE_RELATIVE_PATH);
            Assert.That(File.Exists(fixturePath), Is.True, $"Persistence fixture was not found at {fixturePath}.");

            var fixtureJson = File.ReadAllText(fixturePath);
            var loadedData = JsonUtility.FromJson<PlayerDataWrapper>(fixtureJson);

            Assert.That(loadedData, Is.Not.Null);
            Assert.That(loadedData.Accounts, Is.Not.Null);
            Assert.That(loadedData.Accounts, Has.Count.EqualTo(1));

            var account = loadedData.Accounts[0];
            Assert.That(account, Is.Not.Null);
            Assert.That(account.Username, Is.EqualTo("phase0_baseline_user"));
            Assert.That(account.PasswordHash, Is.EqualTo("phase0-baseline-password-hash"));
            Assert.That(account.PasswordSalt, Is.EqualTo("phase0-baseline-password-salt"));
            Assert.That(account.Characters, Is.Not.Null);
            Assert.That(account.Characters, Has.Count.EqualTo(1));

            var character = account.Characters[0];
            Assert.That(character, Is.Not.Null);
            Assert.That(character.Name, Is.EqualTo("Aveline Stone"));
            Assert.That(character.Title, Is.EqualTo("The Wayfinder"));
            Assert.That(character.GUID, Is.EqualTo("character-phase0-baseline-0001"));
            Assert.That(character.Level, Is.EqualTo(27));
            Assert.That(character.Experience, Is.EqualTo(123456));
            Assert.That(character.ClassID, Is.EqualTo("class_ranger"));
            Assert.That(character.RaceID, Is.EqualTo("race_human"));
            Assert.That(character.PVPFactionID, Is.EqualTo("faction_silver_covenant"));
            AssertStringListEquivalent(new[] { "spell_arcane_bolt", "spell_healing_wind" }, character.KnownSpellIDs);

            Assert.That(character.SavedInventory, Has.Count.EqualTo(1));
            AssertInventoryEntry(
                character.SavedInventory[0],
                3,
                "item-inventory-phase0-0001",
                "item_health_potion",
                4,
                87,
                new[] { "mod_healing_potion", "mod_phase0_baseline" });

            Assert.That(character.SavedEquipment, Has.Count.EqualTo(1));
            AssertEquipmentEntry(
                character.SavedEquipment[0],
                1,
                "item-equipment-phase0-0001",
                "item_wayfinder_bow",
                1,
                64,
                new[] { "mod_precision", "mod_wind_attunement" });

            Assert.That(character.SavedWeaponSkills, Has.Count.EqualTo(1));
            Assert.That(character.SavedWeaponSkills[0].WeaponType, Is.EqualTo(2));
            Assert.That(character.SavedWeaponSkills[0].Level, Is.EqualTo(11));
            Assert.That(character.SavedWeaponSkills[0].Experience, Is.EqualTo(7654));

            Assert.That(character.SavedTalents, Has.Count.EqualTo(1));
            Assert.That(character.SavedTalents[0].TalentID, Is.EqualTo("talent_eagle_eye"));
            Assert.That(character.SavedTalents[0].Rank, Is.EqualTo(3));

            Assert.That(character.SavedReputations, Has.Count.EqualTo(1));
            Assert.That(character.SavedReputations[0].FactionID, Is.EqualTo("faction_silver_covenant"));
            Assert.That(character.SavedReputations[0].Reputation, Is.EqualTo(845));

            Assert.That(character.SavedQuests, Has.Count.EqualTo(1));
            Assert.That(character.SavedQuests[0].QuestID, Is.EqualTo("quest_find_waystone"));
            Assert.That(character.SavedQuests[0].Progress, Is.EqualTo(new[] { 2, 5 }));
            Assert.That(character.SavedQuests[0].IsCompleted, Is.False);

            Assert.That(character.SavedResources, Has.Count.EqualTo(1));
            Assert.That(character.SavedResources[0].StatID, Is.EqualTo("health"));
            Assert.That(character.SavedResources[0].Current, Is.EqualTo(87.5f));

            var serializedJson = JsonUtility.ToJson(loadedData);
            var roundTrippedData = JsonUtility.FromJson<PlayerDataWrapper>(serializedJson);

            Assert.That(roundTrippedData, Is.Not.Null);
            Assert.That(roundTrippedData.Accounts, Is.Not.Null);
            Assert.That(roundTrippedData.Accounts, Has.Count.EqualTo(1));
            AssertAccountEquivalent(account, roundTrippedData.Accounts[0]);
        }

        private static void AssertAccountEquivalent(PlayerAccountData expected, PlayerAccountData actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.Username, Is.EqualTo(expected.Username));
            Assert.That(actual.PasswordHash, Is.EqualTo(expected.PasswordHash));
            Assert.That(actual.PasswordSalt, Is.EqualTo(expected.PasswordSalt));
            Assert.That(actual.Characters, Has.Count.EqualTo(expected.Characters.Count));

            for (var index = 0; index < expected.Characters.Count; index++)
                AssertCharacterEquivalent(expected.Characters[index], actual.Characters[index]);
        }

        private static void AssertCharacterEquivalent(CharacterData expected, CharacterData actual)
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Title, Is.EqualTo(expected.Title));
            Assert.That(actual.GUID, Is.EqualTo(expected.GUID));
            Assert.That(actual.Level, Is.EqualTo(expected.Level));
            Assert.That(actual.Experience, Is.EqualTo(expected.Experience));
            Assert.That(actual.ClassID, Is.EqualTo(expected.ClassID));
            Assert.That(actual.RaceID, Is.EqualTo(expected.RaceID));
            Assert.That(actual.PVPFactionID, Is.EqualTo(expected.PVPFactionID));
            AssertStringListEquivalent(expected.KnownSpellIDs, actual.KnownSpellIDs);

            Assert.That(actual.SavedInventory, Has.Count.EqualTo(expected.SavedInventory.Count));
            for (var index = 0; index < expected.SavedInventory.Count; index++)
                AssertInventoryEntryEquivalent(expected.SavedInventory[index], actual.SavedInventory[index]);

            Assert.That(actual.SavedEquipment, Has.Count.EqualTo(expected.SavedEquipment.Count));
            for (var index = 0; index < expected.SavedEquipment.Count; index++)
                AssertEquipmentEntryEquivalent(expected.SavedEquipment[index], actual.SavedEquipment[index]);

            Assert.That(actual.SavedWeaponSkills, Has.Count.EqualTo(expected.SavedWeaponSkills.Count));
            for (var index = 0; index < expected.SavedWeaponSkills.Count; index++)
            {
                Assert.That(actual.SavedWeaponSkills[index].WeaponType, Is.EqualTo(expected.SavedWeaponSkills[index].WeaponType));
                Assert.That(actual.SavedWeaponSkills[index].Level, Is.EqualTo(expected.SavedWeaponSkills[index].Level));
                Assert.That(actual.SavedWeaponSkills[index].Experience, Is.EqualTo(expected.SavedWeaponSkills[index].Experience));
            }

            Assert.That(actual.SavedTalents, Has.Count.EqualTo(expected.SavedTalents.Count));
            for (var index = 0; index < expected.SavedTalents.Count; index++)
            {
                Assert.That(actual.SavedTalents[index].TalentID, Is.EqualTo(expected.SavedTalents[index].TalentID));
                Assert.That(actual.SavedTalents[index].Rank, Is.EqualTo(expected.SavedTalents[index].Rank));
            }

            Assert.That(actual.SavedReputations, Has.Count.EqualTo(expected.SavedReputations.Count));
            for (var index = 0; index < expected.SavedReputations.Count; index++)
            {
                Assert.That(actual.SavedReputations[index].FactionID, Is.EqualTo(expected.SavedReputations[index].FactionID));
                Assert.That(actual.SavedReputations[index].Reputation, Is.EqualTo(expected.SavedReputations[index].Reputation));
            }

            Assert.That(actual.SavedQuests, Has.Count.EqualTo(expected.SavedQuests.Count));
            for (var index = 0; index < expected.SavedQuests.Count; index++)
            {
                Assert.That(actual.SavedQuests[index].QuestID, Is.EqualTo(expected.SavedQuests[index].QuestID));
                Assert.That(actual.SavedQuests[index].Progress, Is.EqualTo(expected.SavedQuests[index].Progress));
                Assert.That(actual.SavedQuests[index].IsCompleted, Is.EqualTo(expected.SavedQuests[index].IsCompleted));
            }

            Assert.That(actual.SavedResources, Has.Count.EqualTo(expected.SavedResources.Count));
            for (var index = 0; index < expected.SavedResources.Count; index++)
            {
                Assert.That(actual.SavedResources[index].StatID, Is.EqualTo(expected.SavedResources[index].StatID));
                Assert.That(actual.SavedResources[index].Current, Is.EqualTo(expected.SavedResources[index].Current));
            }
        }

        private static void AssertStringListEquivalent(IList<string> expected, IList<string> actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual, Has.Count.EqualTo(expected.Count));

            for (var index = 0; index < expected.Count; index++)
                Assert.That(actual[index], Is.EqualTo(expected[index]));
        }

        private static void AssertInventoryEntry(
            SavedInventoryEntry entry,
            int slotIndex,
            string itemGuid,
            string itemId,
            int stackSize,
            int durability,
            IList<string> modifications)
        {
            Assert.That(entry.SlotIndex, Is.EqualTo(slotIndex));
            Assert.That(entry.ItemGUID, Is.EqualTo(itemGuid));
            Assert.That(entry.ItemID, Is.EqualTo(itemId));
            Assert.That(entry.StackSize, Is.EqualTo(stackSize));
            Assert.That(entry.Durability, Is.EqualTo(durability));
            AssertStringListEquivalent(modifications, entry.Modifications);
        }

        private static void AssertEquipmentEntry(
            SavedEquipmentEntry entry,
            int slotIndex,
            string itemGuid,
            string itemId,
            int stackSize,
            int durability,
            IList<string> modifications)
        {
            Assert.That(entry.SlotIndex, Is.EqualTo(slotIndex));
            Assert.That(entry.ItemGUID, Is.EqualTo(itemGuid));
            Assert.That(entry.ItemID, Is.EqualTo(itemId));
            Assert.That(entry.StackSize, Is.EqualTo(stackSize));
            Assert.That(entry.Durability, Is.EqualTo(durability));
            AssertStringListEquivalent(modifications, entry.Modifications);
        }

        private static void AssertInventoryEntryEquivalent(SavedInventoryEntry expected, SavedInventoryEntry actual)
        {
            AssertInventoryEntry(
                actual,
                expected.SlotIndex,
                expected.ItemGUID,
                expected.ItemID,
                expected.StackSize,
                expected.Durability,
                expected.Modifications);
        }

        private static void AssertEquipmentEntryEquivalent(SavedEquipmentEntry expected, SavedEquipmentEntry actual)
        {
            AssertEquipmentEntry(
                actual,
                expected.SlotIndex,
                expected.ItemGUID,
                expected.ItemID,
                expected.StackSize,
                expected.Durability,
                expected.Modifications);
        }

        [Serializable]
        private sealed class PlayerDataWrapper
        {
            public List<PlayerAccountData> Accounts = new();
        }
    }
}
