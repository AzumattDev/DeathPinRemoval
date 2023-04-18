using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace DeathPinRemoval;

[HarmonyPatch(typeof(TombStone), nameof(TombStone.GiveBoost))]
static class TombstoneGiveBoostPatch
{
    static void Prefix(TombStone __instance)
    {
        DeathPinRemovalPlugin.DeathPinRemovalLogger.LogDebug("Starting TombstoneGiveBoostPatch");

        if (DeathPinRemovalPlugin.removeOnEmpty.Value != DeathPinRemovalPlugin.Toggle.On)
        {
            DeathPinRemovalPlugin.DeathPinRemovalLogger.LogDebug(
                "Not removing tombstone pin because removeOnEmpty is not enabled.");
            return;
        }

        DeathPinRemovalPlugin.DeathPinRemovalLogger.LogDebug("Looking for the closest death pin.");

        Minimap.PinData closestPin = GetClosestPin(__instance.transform.position, 10);

        if (closestPin == null || closestPin.m_type != Minimap.PinType.Death)
        {
            DeathPinRemovalPlugin.DeathPinRemovalLogger.LogDebug(
                "No death pin found within 10 units, looking for the closest pin within 5 units.");

            closestPin = GetClosestPin(__instance.transform.position, 5);
        }

        if (closestPin != null)
        {
            DeathPinRemovalPlugin.DeathPinRemovalLogger.LogDebug($"Removing death pin with name: {closestPin.m_name}");

            try
            {
                bool takeInput = (Chat.instance == null || !Chat.instance.HasFocus()) && !Console.IsVisible() &&
                                 !TextInput.IsVisible() && !Menu.IsVisible() && !InventoryGui.IsVisible();
                float deltaTime = Time.deltaTime;

                // All of this is to get the pin to remove itself from the minimap. Vanilla methods are called like they are in the Minimap.Update method. Must call all of them to get the pin to remove.
                Minimap.instance.SetMapMode(Minimap.MapMode.Large);
                Minimap.instance.RemovePin(closestPin);
                Minimap.instance.UpdateMap(Player.m_localPlayer, deltaTime, takeInput);
                Minimap.instance.UpdateDynamicPins(deltaTime);
                Minimap.instance.UpdatePins();
                Minimap.instance.UpdateBiome(Player.m_localPlayer);
                Minimap.instance.UpdateNameInput();
                Minimap.instance.UpdatePlayerPins(deltaTime);
                Minimap.instance.SetMapMode(Minimap.MapMode.None);
            }
            catch (Exception e)
            {
                DeathPinRemovalPlugin.DeathPinRemovalLogger.LogError(
                    $"Caught exception when removing tombstone pin: {e}");
            }
        }
        else
        {
            DeathPinRemovalPlugin.DeathPinRemovalLogger.LogDebug("No death pin found to remove.");
        }

        DeathPinRemovalPlugin.DeathPinRemovalLogger.LogDebug("Finished TombstoneGiveBoostPatch");
    }

    public static Minimap.PinData GetClosestPin(Vector3 pos, float radius)
    {
        // Basically the same as Minimap.GetClosestPin but with no check for active in hierarchy on the pin. Just if it exists and has the m_save.
        // This is because the tombstone pin is not active in hierarchy when the game has nomap mode. Effectively, making it "not exist" in the vanilla method.
        Minimap.PinData closestPin = null;
        float num1 = 999999f;
        foreach (Minimap.PinData pin in Minimap.instance.m_pins)
        {
            if (pin.m_save && pin.m_uiElement)
            {
                float num2 = Utils.DistanceXZ(pos, pin.m_pos);
                if (num2 < (double)radius && (num2 < (double)num1 || closestPin == null))
                {
                    closestPin = pin;
                    num1 = num2;
                }
            }
        }

        return closestPin;
    }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.AddPin))]
static class MinimapAddPinPatch
{
    static bool Prefix(Minimap __instance, Vector3 pos,
        Minimap.PinType type,
        string name,
        bool save,
        bool isChecked,
        long ownerID = 0)
    {
        if (DeathPinRemovalPlugin.totalPinRemoval.Value != DeathPinRemovalPlugin.Toggle.On) return true;
        return type != Minimap.PinType.Death;
    }
}