using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace hearthstone_ex.Utils
{
	internal static class TagConvertor
	{
		private struct TagInfo
		{
			//GAME_TAG.XXXX
			public string Key;

			//TAG_XXXXX
			public Type Tag;

			//public override string ToString( )
			//{
			//	return $"{Key}: {Tag}";
			//}
		}

		private static IReadOnlyDictionary<int, TagInfo> FillAllTags( )
		{
			var knownTags = AccessTools.AllTypes( ).Where(t => t.IsEnum && t.Name.StartsWith("TAG_")).ToArray( );

			var dict = new Dictionary<int, TagInfo>( );
			foreach (var value in typeof(GAME_TAG).GetEnumValues( ).Cast<GAME_TAG>( ))
			{
				var name = value.ToString( );

				if (dict.ContainsKey((int) value))
				{
					var entry = dict[(int) value];
					entry.Key = null;
				}
				else
				{
					dict.Add((int) value, new TagInfo
					{
						Key = name,
						Tag = knownTags.FirstOrDefault(t => t.Name.Length == name.Length + 4 && t.Name.EndsWith(name))
					});
				}
			}

			return dict;
		}

		private static readonly IReadOnlyDictionary<int, TagInfo> _allTags = FillAllTags( );

		private static string JoinTagsImpl /*<TKey, TValue>*/(IEnumerable<KeyValuePair /*<TKey, TValue>*/<int, int>> tags, string separator)
		{
			var infoDef = new TagInfo( );
			return string.Join(separator ?? Environment.NewLine, tags.Select(item =>
			{
				if (!_allTags.TryGetValue(item.Key, out var info))
					info = infoDef;

				var keyStr = info.Key ?? item.Key.ToString( );
				var tagStr = info.Tag == null ? item.Value.ToString( ) : Enum.GetName(info.Tag, item.Value);

				return new {Key = keyStr, Tag = tagStr};
			}).OrderBy(p => p.Key).Select(p => $"{p.Key}: {p.Tag}"));
		}

		public static string JoinTags(this TagMap tags, string separator = null)
		{
			return JoinTagsImpl(tags.GetMap( ), separator);
		}

		public static string JoinTags(this EntityBase ent, string separator = null)
		{
			return JoinTags(ent.GetTags( ), separator);
		}

		public static string JoinTags(this Network.Entity ent, string separator = null)
		{
			return JoinTagsImpl(ent.Tags.Select(q => new KeyValuePair<int, int>(q.Name, q.Value)), separator);
		}
	}
}
