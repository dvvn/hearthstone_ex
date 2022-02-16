using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using GhostedState = CollectionDeckTileActor.GhostedState;
using SlotStatus = CollectionDeck.SlotStatus;
using DelOnSlotEmptied = CollectionDeckSlot.DelOnSlotEmptied;
using Tray = DeckTrayDeckTileVisual;

namespace hearthstone_ex.Targets
{
	//logging
	public partial class DeckTrayDeckTileVisual : LoggerGui.Static<DeckTrayDeckTileVisual>
	{
		[Conditional("DEBUG")]
		private static void LogMessage([CanBeNull] Actor actor, string msg, [NotNull] CallerInfo info_full)
		{
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

			Logger.Message($"{GetName( )}: {msg}", info_full);
		}

		[Conditional("DEBUG")]
		private static void LogGhostedState([NotNull] Actor actor, GhostedState state, CallerInfo info_full)
		{
			if (state == GhostedState.NONE)
				return;
			LogMessage(actor, $"Ghost state set to {state}", info_full);
		}
	}

	//simple helpers
	public partial class DeckTrayDeckTileVisual
	{
		private sealed class OnSlotEmptiedWrapper
		{
			private readonly DelOnSlotEmptied m_original;

			//added to fix deck copy-paste and autogenerator
			//without this, while game generating deck, it always create new slot, so we have 2+ slots for same (golden) card, instead of one

			private OnSlotEmptiedWrapper(DelOnSlotEmptied original) => this.m_original = original;
			private void Invoke(CollectionDeckSlot slot) => this.m_original(slot);

			[NotNull]
			public static DelOnSlotEmptied Create([NotNull] CollectionDeckSlot slot)
			{
				var fn = slot.OnSlotEmptied;
				if (fn.Target is OnSlotEmptiedWrapper)
					throw new AccessViolationException("Slot already copied!");
				return new OnSlotEmptiedWrapper(fn).Invoke;
			}
		}

		[NotNull]
		private static CollectionDeckSlot CopySlot([NotNull] CollectionDeckSlot deck_slot)
		{
			if (deck_slot.OnSlotEmptied.Target is OnSlotEmptiedWrapper)
				return deck_slot;

			var custom_slot = new CollectionDeckSlot( );
			custom_slot.CopyFrom(deck_slot);

			custom_slot.m_entityDefOverride = deck_slot.m_entityDefOverride;
			custom_slot.OnSlotEmptied = OnSlotEmptiedWrapper.Create(deck_slot);

			return custom_slot;
		}

		[NotNull]
		private static CollectionDeckSlot CopySlot([NotNull] CollectionDeckSlot deck_slot, TAG_PREMIUM add, [NotNull] IEnumerable<TAG_PREMIUM> remove)
		{
			var slot = CopySlot(deck_slot);

			var on_slot_emptied = slot.OnSlotEmptied;
			slot.OnSlotEmptied = _ => { };

			var removed_count = 0;
			foreach (var tag in remove)
			{
				var count = deck_slot.GetCount(tag);
				slot.RemoveCard(count, tag);
				removed_count += count;
			}

			slot.AddCard(removed_count, add);
			slot.OnSlotEmptied = on_slot_emptied;
			return slot;
		}

		[NotNull]
		private static CollectionDeckSlot CopySlot([NotNull] CollectionDeckSlot deck_slot, TAG_PREMIUM add,
												   [NotNull] params TAG_PREMIUM[ ] remove)
		{
			return CopySlot(deck_slot, add, remove.AsEnumerable( ));
		}

		private static GhostedState GetGhostState(CollectionDeckSlot deck_slot, [CanBeNull] CollectionDeck deck)
		{
			var status = deck?.GetSlotStatus(deck_slot) ?? SlotStatus.UNKNOWN;
			switch (status)
			{
				case SlotStatus.NOT_VALID:
					return GhostedState.RED;
				case SlotStatus.MISSING:
					return GhostedState.BLUE;
				default:
					return GhostedState.NONE;
			}
		}
	}

	//core funtions
	public partial class DeckTrayDeckTileVisual
	{
		private static bool ForceNonPremiumMaterial(TAG_PREMIUM normal_tag, TAG_PREMIUM ideal_tag, bool in_arena, Actor deck_tile_actor, CallerInfo info_full)
		{
			if (in_arena && Options.Get( ).GetBool(Option.HAS_DISABLED_PREMIUMS_THIS_DRAFT))
			{
				LogMessage(deck_tile_actor, $"Material forced to {normal_tag}!", info_full);
				return true;
			}

			if (ideal_tag <= normal_tag)
			{
				string GetReason( )
				{
					return ideal_tag == TAG_PREMIUM.NORMAL ? "no others available" : "in already premium";
				}

				LogMessage(deck_tile_actor, $"Material forced to {normal_tag}, because {GetReason( )}!", info_full);
				return true;
			}

			return false;
		}

		private static TAG_PREMIUM GetPremiumMaterial(bool in_arena, bool is_ghosted,
													  [NotNull] CollectionDeckSlot deck_slot, [NotNull] CollectionDeckTileActor deck_tile_actor, CallerInfo info)
		{
			//copy every slot for future modify
			//because we must have the same slot on card with 2+ copies

			var DEFAULT_TAG = deck_slot.PreferredPremium;
			if (is_ghosted)
			{
				//dont touch ghosted cards
				deck_tile_actor.SetSlot(deck_slot);
				LogMessage(deck_tile_actor, $"Using default {DEFAULT_TAG} material while card is ghosted", info);
				return DEFAULT_TAG;
			}

			var IDEAL_TAG = deck_tile_actor.GetEntityDef( ).GetBestPossiblePremiumType(msg => Logger.Message(msg, info));
			var NORMAL_TAG = deck_slot.UnPreferredPremium;
			if (ForceNonPremiumMaterial(NORMAL_TAG, IDEAL_TAG, in_arena, deck_tile_actor, info))
			{
				var other_tags = EnumsChecker<TAG_PREMIUM>.Get( ).OtherEnums(NORMAL_TAG);
				deck_tile_actor.SetSlot(CopySlot(deck_slot, NORMAL_TAG, other_tags));
				return NORMAL_TAG;
			}

			if (DEFAULT_TAG != NORMAL_TAG && DEFAULT_TAG != IDEAL_TAG)
			{
				//add only non-CUSTOM_TAG's here
				deck_tile_actor.SetSlot(CopySlot(deck_slot));
				LogMessage(deck_tile_actor, $"Using default {DEFAULT_TAG} material", info);
				return DEFAULT_TAG;
			}

			deck_tile_actor.SetSlot(CopySlot(deck_slot, IDEAL_TAG, NORMAL_TAG));
			LogMessage(deck_tile_actor, $"Using custom slot with {IDEAL_TAG} tag. Default tag is {DEFAULT_TAG}", info);
			return IDEAL_TAG;
		}
	}

	[HarmonyPatch(typeof(Tray))]
	public partial class DeckTrayDeckTileVisual
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(SetUpActor))]
		public static bool SetUpActor(bool ___m_inArena, bool ___m_useSliderAnimations,
									  [CanBeNull] CollectionDeckSlot ___m_slot, [CanBeNull] CollectionDeckTileActor ___m_actor, [NotNull] CollectionDeck ___m_deck)
		{
			//full rebuild of game's function

			if (___m_actor == null || ___m_slot == null)
				return false;
			if (string.IsNullOrEmpty(___m_slot.CardID))
				return false;

			var entity_def = ___m_slot.GetEntityDef( );
			___m_actor.SetEntityDef(entity_def);

			var ghosted = GetGhostState(___m_slot, ___m_deck);
			LogGhostedState(___m_actor, ghosted, new CallerInfo( ));
			___m_actor.SetGhosted(ghosted);

			var premium_tag = GetPremiumMaterial(___m_inArena, ghosted != GhostedState.NONE, ___m_slot, ___m_actor, new CallerInfo( ));
			___m_actor.SetPremium(premium_tag);

			var is_unique = /*entity_def != null &&*/ entity_def.IsElite( );
			if (is_unique && ___m_inArena && ___m_slot.Count > 1)
				is_unique = false;

			___m_actor.UpdateDeckCardProperties(is_unique, false, ___m_slot.Count, ___m_useSliderAnimations);

			DefLoader.Get( ).LoadCardDef(entity_def.GetCardId( ), (cardID, cardDef, data) =>
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
			}, quality: new CardPortraitQuality(1, premium_tag));

			return false;
		}
	}
}
