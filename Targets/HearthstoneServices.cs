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
    public partial class HearthstoneServices : LoggerConsole.Static<HearthstoneServices>
    {
        private sealed class TempInternalAppMode : IService
        {
            private readonly HearthstoneApplication.AppModeOwerriden m_modeOwerriden;

            public TempInternalAppMode() => this.m_modeOwerriden = HearthstoneApplication.OwerrideAppMode(ApplicationMode.INTERNAL);

            public IEnumerator<IAsyncJobResult> Initialize(ServiceLocator serviceLocator)
            {
                this.m_modeOwerriden.Dispose();
                Logger.Message("Initialized!");
                yield break;
            }

            [CanBeNull]
            public Type[] GetDependencies() => new[] {typeof(Cheats)};

            public void Shutdown() { }
        }

        private sealed class ServiceRegistrar
        {
            private readonly ServiceLocator m_locator;
            private readonly CallerInfo m_infoFull;

            private readonly Func<Type, bool, IService> m_getFn;

            public ServiceRegistrar([NotNull] ServiceLocator locator, CallerInfo caller_info_full)
            {
                this.m_locator = locator;
                this.m_infoFull = caller_info_full;

                var fn_info = AccessTools.Method(locator.GetType(), "Get", new[] {typeof(Type), typeof(bool)});
                this.m_getFn = AccessTools.MethodDelegate<Func<Type, bool, IService>>(fn_info, locator, fn_info.IsVirtual);
            }

            [CanBeNull]
            public T Get<T>(bool extra_info = false) where T : IService
            {
                var type = typeof(T);
                var result = this.m_getFn(type, true);
                if (result == null)
                    Logger.Message($"{type.Name} service not found!", this.m_infoFull);
                else if (extra_info)
                    Logger.Message($"{type.Name} service already exists!", this.m_infoFull);
                return (T) result;
            }

            [NotNull]
            public T Create<T>() where T : class, IService, new()
            {
#if false
                    var state = this.m_locator.LocatorState;
                    if (state == ServiceState.Ready || state == ServiceState.Error)
                    {
                        Logger.Message($"Unable to register {type.Name} service! Locator state is {state}.", this.m_info);
                        return (T) (object) null;
                    }
#endif
                var result = new T();
                this.m_locator.RegisterService<T>(result);
                Logger.Message($"{typeof(T).Name} service added!", this.m_infoFull);
                return result;
            }

            [NotNull]
            public T GetOrCreate<T>() where T : class, IService, new()
            {
                return this.Get<T>(true) ?? this.Create<T>();
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
            var reg = new ServiceRegistrar(___s_runtimeServices, new CallerInfo());

            LoggerGui.SetDefaultWindow(reg.GetOrCreate<Debugger>());
            Logger.Message($"{nameof(LoggerGui)}.{nameof(LoggerGui.DefaultWindow)} initialized!");
            reg.Create<TempInternalAppMode>();
        }
    }
}
