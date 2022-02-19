using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using GhostedState = CollectionDeckTileActor.GhostedState;
using SlotStatus = CollectionDeck.SlotStatus;
using DelOnSlotEmptied = CollectionDeckSlot.DelOnSlotEmptied;
using Tray = DeckTrayDeckTileVisual;

namespace hearthstone_ex.Utils
{
	internal class CollectionDeckSlotWrapped : CollectionDeckSlot
	{
		private sealed class OnSlotEmptiedWrapper
		{
			private readonly DelOnSlotEmptied _original;

			//added to fix deck copy-paste and autogenerator
			//without this, while game generating deck, it always create new slot, so we have 2+ slots for same (golden) card, instead of one

			public OnSlotEmptiedWrapper([NotNull] DelOnSlotEmptied fn) => _original = fn;
			public void Invoke(CollectionDeckSlot slot) => _original(slot);
		}

		private void CopyFromEx(CollectionDeckSlot deckSlot)
		{
			CopyFrom(deckSlot);
			m_entityDefOverride = deckSlot.m_entityDefOverride;
		}

		private void WrapSlot(DelOnSlotEmptied fn)
		{
			OnSlotEmptied = fn.Target is OnSlotEmptiedWrapper ? fn : new OnSlotEmptiedWrapper(fn).Invoke;
		}

		public CollectionDeckSlotWrapped([NotNull] CollectionDeckSlot deckSlot)
		{
			CopyFromEx(deckSlot);
			WrapSlot(deckSlot.OnSlotEmptied);
		}

		public CollectionDeckSlotWrapped([NotNull] CollectionDeckSlot deckSlot, TAG_PREMIUM add, IEnumerable<TAG_PREMIUM> remove)
		{
			CopyFromEx(deckSlot);
			//it called from OnSlotEmptied
			OnSlotEmptied = null;

			var removedCount = 0;
			foreach (var tag in remove)
			{
				var count = deckSlot.GetCount(tag);
				RemoveCard(count, tag);
				removedCount += count;
			}

			//override removed cards with new one
			AddCard(removedCount, add);
			WrapSlot(deckSlot.OnSlotEmptied);
		}

		public CollectionDeckSlotWrapped([NotNull] CollectionDeckSlot deckSlot, TAG_PREMIUM add, params TAG_PREMIUM[ ] remove)
			: this(deckSlot, add, remove.AsEnumerable( ))
		{
		}
	}
}

namespace hearthstone_ex.Targets
{
	public partial class DeckTrayDeckTileVisual : LoggerGui.Static<DeckTrayDeckTileVisual>
	{
		[Conditional("DEBUG")]
		private static void LogMessage([CanBeNull] Actor actor, string msg, bool skip = false, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = -1)
		{
			if (skip)
				return;

			string GetName( )
			{
				if (actor != null)
				{
					var ent = actor.GetEntityDef( );
					if (ent != null)
						return ent.ToString( );
				}

				return "UNKNOWN ENTITY";
			}

			Logger.Message($"{GetName( )}: {msg}", memberName, "", sourceLineNumber);
		}
	}

	public partial class DeckTrayDeckTileVisual
	{
		private static bool ForceNonPremiumMaterial(TAG_PREMIUM normalTag, TAG_PREMIUM idealTag, bool inArena, Actor deckTileActor)
		{
			if (inArena && Options.Get( ).GetBool(Option.HAS_DISABLED_PREMIUMS_THIS_DRAFT))
			{
				LogMessage(deckTileActor, $"Material forced to {normalTag}!");
				return true;
			}

			if (idealTag <= normalTag)
			{
				string GetReason( )
				{
					return idealTag == TAG_PREMIUM.NORMAL ? "no others available" : "in already premium";
				}

				LogMessage(deckTileActor, $"Material forced to {normalTag}, because {GetReason( )}!");
				return true;
			}

			return false;
		}

		private static TAG_PREMIUM SetActorSlot(bool inArena, bool isGhosted,
												[NotNull] CollectionDeckSlot deckSlot, [NotNull] CollectionDeckTileActor deckTileActor)
		{
			//copy every slot for future modify
			//because we must have the same slot on card with 2+ copies

			var DEFAULT_TAG = deckSlot.PreferredPremium;
			if (isGhosted)
			{
				//dont touch ghosted cards
				deckTileActor.SetSlot(deckSlot);
				LogMessage(deckTileActor, $"Using default {DEFAULT_TAG} material while card is ghosted");
				return DEFAULT_TAG;
			}

			var IDEAL_TAG = deckTileActor.GetEntityDef( ).GetBestPossiblePremiumType(msg => Logger.Message(msg));
			var NORMAL_TAG = deckSlot.UnPreferredPremium;
			if (ForceNonPremiumMaterial(NORMAL_TAG, IDEAL_TAG, inArena, deckTileActor))
			{
				var otherTags = EnumsChecker<TAG_PREMIUM>.Get( ).OtherEnums(NORMAL_TAG);
				deckTileActor.SetSlot(new CollectionDeckSlotWrapped(deckSlot, NORMAL_TAG, otherTags));
				return NORMAL_TAG;
			}

			if (DEFAULT_TAG != NORMAL_TAG && DEFAULT_TAG != IDEAL_TAG)
			{
				//add only non-CUSTOM_TAG's here
				deckTileActor.SetSlot(new CollectionDeckSlotWrapped(deckSlot));
				LogMessage(deckTileActor, $"Using default {DEFAULT_TAG} material");
				return DEFAULT_TAG;
			}

			deckTileActor.SetSlot(new CollectionDeckSlotWrapped(deckSlot, IDEAL_TAG, NORMAL_TAG));
			LogMessage(deckTileActor, $"Using custom slot with {IDEAL_TAG} tag. Default tag is {DEFAULT_TAG}");
			return IDEAL_TAG;
		}
	}

	[HarmonyPatch(typeof(Tray))]
	public partial class DeckTrayDeckTileVisual
	{
		// ReSharper disable InconsistentNaming
		private static readonly MethodInfo GetGhostedState = AccessTools.Method(typeof(Tray), nameof(GetGhostedState));

		[HarmonyPrefix]
		[HarmonyPatch(nameof(SetUpActor))]
		public static bool SetUpActor(Tray __instance, bool ___m_inArena, bool ___m_useSliderAnimations,
									  [CanBeNull] CollectionDeckSlot ___m_slot, [CanBeNull] CollectionDeckTileActor ___m_actor, [NotNull] CollectionDeck ___m_deck)
		{
			//full rebuild of game's function

			if (___m_actor == null || ___m_slot == null)
				return false;
			if (string.IsNullOrEmpty(___m_slot.CardID))
				return false;

			//m_actor.SetSlot
			//m_actor.SetPremium
			//m_actor.SetEntityDef
			//m_actor.SetGhosted
			//m_actor.UpdateDeckCardProperties

			var entityDef = ___m_slot.GetEntityDef( );
			___m_actor.SetEntityDef(entityDef);

			var ghosted = (GhostedState) GetGhostedState.Invoke(__instance, null);
			LogMessage(___m_actor, $"Ghosted state set to {ghosted}", ghosted == GhostedState.NONE);
			___m_actor.SetGhosted(ghosted);

			var premiumTag = SetActorSlot(___m_inArena, ghosted != GhostedState.NONE, ___m_slot, ___m_actor);
			___m_actor.SetPremium(premiumTag);

			var isUnique = /*entityDef != null &&*/ entityDef.IsElite( );
			if (isUnique && ___m_inArena && ___m_slot.Count > 1)
				isUnique = false;

			___m_actor.UpdateDeckCardProperties(isUnique, false, ___m_slot.Count, ___m_useSliderAnimations);

			DefLoader.Get( ).LoadCardDef(entityDef.GetCardId( ), (cardID, cardDef, data) =>
			{
				using (cardDef)
				{
					if (!cardID.Equals(___m_actor.GetEntityDef( ).GetCardId( )))
						return;
					___m_actor.SetCardDef(cardDef);
					___m_actor.UpdateAllComponents( );
					___m_actor.UpdateMaterial(cardDef.CardDef.GetDeckCardBarPortrait( ));
					___m_actor.UpdateGhostTileEffect( );
				}
			}, quality: new CardPortraitQuality(1, premiumTag));

			return false;
		}
	}
}
