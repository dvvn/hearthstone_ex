using System;
using System.Collections.Generic;
using Blizzard.T5.Jobs;
using Blizzard.T5.Services;
using HarmonyLib;
using hearthstone_ex.Utils;
using SceneDebuggerHs = SceneDebugger;
using ServiceManagerHs = Blizzard.T5.Services.ServiceManager;
using AchievementManagerHs = Hearthstone.Progression.AchievementManager;

namespace hearthstone_ex.Targets
{
	public partial class ServiceManager : LoggerFile.Static<ServiceManager>
	{
		private sealed class TempInternalAppMode : IService
		{
			private readonly HearthstoneApplication.AppModeOwerriden _modeOwerriden;

			public TempInternalAppMode()
			{
				_modeOwerriden = HearthstoneApplication.OwerrideAppMode(ApplicationMode.INTERNAL);
			}

			public IEnumerator<IAsyncJobResult> Initialize(ServiceLocator serviceLocator)
			{
				_modeOwerriden.Dispose();
				Logger.Message("Initialized!");
				yield break;
			}


			public Type[] GetDependencies() => new[] { typeof(Cheats) };

			public void Shutdown()
			{
			}
		}

		private sealed class ServiceRegistrar
		{
			private readonly ServiceLocator _locator;

			private readonly Func<Type, bool /*includeUninitialized*/, IService> _getFn;

			public ServiceRegistrar(ServiceLocator locator)
			{
				_locator = locator;

				var fnInfo = AccessTools.Method(locator.GetType(), "Get", new[] { typeof(Type), typeof(bool) });
				_getFn = AccessTools.MethodDelegate<Func<Type, bool, IService>>(fnInfo, locator, fnInfo.IsVirtual);
			}


			private T Get<T>(bool extraInfo = false) where T : IService
			{
				var type = typeof(T);
				var result = _getFn(type, true);
				if (result == null)
					Logger.Message($"{type.Name} service not found!");
				else if (extraInfo)
					Logger.Message($"{type.Name} service already exists!");
				return (T)result;
			}


			public T Create<T>() where T : class, IService, new()
			{
				var result = new T();
				_locator.RegisterService<T>(result);
				Logger.Message($"{typeof(T).Name} service added!");
				return result;
			}

			public void Register(IService service)
			{
				_locator.RegisterService(service.GetType(), service);
				Logger.Message($"{service.GetType().Name} service added!");
			}

			public T GetOrCreate<T>() where T : class, IService, new()
			{
				return Get<T>(true) ?? Create<T>();
			}
		}
	}

	[HarmonyPatch(typeof(ServiceManagerHs))]
	public partial class ServiceManager
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(StartRuntimeServices))]
		public static void StartRuntimeServices(ServiceLocator runtimeServiceLocator)
		{
			runtimeServiceLocator.RegisterService<TempInternalAppMode>();
			Logger.Message($"{nameof(TempInternalAppMode)} service added!");
			var dbg = AccessTools.CreateInstance<SceneDebuggerHs>();
			LoggerGui.SetDefaultWindow(dbg);
			dbg.Initialize(runtimeServiceLocator);
			Logger.Message($"{nameof(SceneDebuggerHs)} service added!");
		}
	}
}