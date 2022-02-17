using System;
using System.Collections.Generic;
using Blizzard.T5.Jobs;
using Blizzard.T5.Services;
using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using Services = HearthstoneServices;
using Debugger = SceneDebugger;
using AchievementManagerHs = Hearthstone.Progression.AchievementManager;

namespace hearthstone_ex.Targets
{
	public partial class HearthstoneServices : LoggerFile.Static<HearthstoneServices>
	{
		private sealed class TempInternalAppMode : IService
		{
			private readonly HearthstoneApplication.AppModeOwerriden _modeOwerriden;

			public TempInternalAppMode( ) => _modeOwerriden = HearthstoneApplication.OwerrideAppMode(ApplicationMode.INTERNAL);

			public IEnumerator<IAsyncJobResult> Initialize(ServiceLocator serviceLocator)
			{
				_modeOwerriden.Dispose( );
				Logger.Message("Initialized!");
				yield break;
			}

			[CanBeNull]
			public Type[ ] GetDependencies( ) => new[ ] {typeof(Cheats)};

			public void Shutdown( )
			{
			}
		}

		private sealed class ServiceRegistrar
		{
			private readonly ServiceLocator _locator;

			private readonly Func<Type, bool, IService> _getFn;

			public ServiceRegistrar([NotNull] ServiceLocator locator)
			{
				_locator = locator;

				var fnInfo = AccessTools.Method(locator.GetType( ), "Get", new[ ] {typeof(Type), typeof(bool)});
				_getFn = AccessTools.MethodDelegate<Func<Type, bool, IService>>(fnInfo, locator, fnInfo.IsVirtual);
			}

			[CanBeNull]
			private T Get<T>(bool extraInfo = false) where T : IService
			{
				var type = typeof(T);
				var result = _getFn(type, true);
				if (result == null)
					Logger.Message($"{type.Name} service not found!");
				else if (extraInfo)
					Logger.Message($"{type.Name} service already exists!");
				return (T) result;
			}

			[NotNull]
			public T Create<T>( ) where T : class, IService, new( )
			{
				var result = new T( );
				_locator.RegisterService<T>(result);
				Logger.Message($"{typeof(T).Name} service added!");
				return result;
			}

			[NotNull]
			public T GetOrCreate<T>( ) where T : class, IService, new( )
			{
				return Get<T>(true) ?? Create<T>( );
			}
		}
	}

	[HarmonyPatch(typeof(Services))]
	public partial class HearthstoneServices
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(InstantiateServiceLocator))]
		public static void InstantiateServiceLocator([NotNull] ref ServiceLocator ___s_runtimeServices)
		{
			var reg = new ServiceRegistrar(___s_runtimeServices);

			LoggerGui.SetDefaultWindow(reg.GetOrCreate<Debugger>( ));
			Logger.Message($"{nameof(LoggerGui)}.{nameof(LoggerGui.DefaultWindow)} initialized!");
			reg.Create<TempInternalAppMode>( );
		}
	}
}
