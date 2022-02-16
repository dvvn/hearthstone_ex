using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using hearthstone_ex.Utils;
using Mgr = ReconnectMgr;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Mgr))]
	public class ReconnectMgr : LoggerGui.Static<ReconnectMgr>
	{
		//NEVER restart game to reconnect

		[HarmonyPrefix]
		[HarmonyPatch(nameof(Mgr.FullResetRequired), MethodType.Getter)]
		public static bool FullResetRequired_get(ref bool __result)
		{
			__result = false;
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(Mgr.FullResetRequired), MethodType.Setter)]
		public static void FullResetRequired_set(bool value)
		{
			string GetCallerName(MethodBase method)
			{
				return method.DeclaringType?.FullName ?? method.Name;
			}

			Logger.Message($"{nameof(Mgr)}.{nameof(Mgr.FullResetRequired)}.Get sets to {value} by {GetCallerName(new StackTrace( ).GetFrame(1).GetMethod( ))}", new CallerInfo( ));
		}
	}
}
