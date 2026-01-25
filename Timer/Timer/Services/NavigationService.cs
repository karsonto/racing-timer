using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Timer.ViewModels;
using Timer.Views;

namespace Timer.Services
{
    /// <summary>
    /// 导航服务实现，负责管理ViewModel到View的映射和视图实例的创建
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly Dictionary<Type, Type> _viewModelToViewMap = new();
        private readonly Dictionary<object, UserControl> _viewCache = new();

        /// <summary>
        /// 初始化NavigationService实例，自动注册所有ViewModel-View映射
        /// </summary>
        public NavigationService()
        {
            RegisterViewModels();
        }

        /// <summary>
        /// 注册所有ViewModel到View的映射关系
        /// </summary>
        private void RegisterViewModels()
        {
            Register<RaceTimerViewModel, RaceTimerView>();
            Register<MultiRaceTimerViewModel, RaceTimerView>();  // 新的多组并行计时
            Register<ScoreViewModel, ScoreView>();
            Register<ParticipantViewModel, ParticipantView>();
            Register<GroupViewModel, GroupView>();
            Register<ProjectViewModel, ProjectView>();
            Register<DeviceViewModel, DeviceView>();
            Register<ChipViewModel, ChipView>();
        }

        /// <summary>
        /// 注册ViewModel类型到View类型的映射
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel类型</typeparam>
        /// <typeparam name="TView">View类型</typeparam>
        public void Register<TViewModel, TView>() where TViewModel : class where TView : UserControl, new()
        {
            _viewModelToViewMap[typeof(TViewModel)] = typeof(TView);
        }

        /// <summary>
        /// 导航到指定ViewModel类型的页面（验证映射是否存在）
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel类型</typeparam>
        /// <exception cref="InvalidOperationException">当ViewModel类型未注册时抛出</exception>
        public void NavigateTo<TViewModel>() where TViewModel : class
        {
            var viewModelType = typeof(TViewModel);
            if (!_viewModelToViewMap.ContainsKey(viewModelType))
            {
                throw new InvalidOperationException($"No view registered for ViewModel type {viewModelType.Name}");
            }
        }

        /// <summary>
        /// 导航到指定ViewModel实例的页面（验证映射是否存在）
        /// </summary>
        /// <param name="viewModel">ViewModel实例</param>
        /// <exception cref="ArgumentNullException">当viewModel为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当ViewModel类型未注册时抛出</exception>
        public void NavigateTo(object viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            var viewModelType = viewModel.GetType();
            if (!_viewModelToViewMap.ContainsKey(viewModelType))
            {
                throw new InvalidOperationException($"No view registered for ViewModel type {viewModelType.Name}");
            }
        }

        /// <summary>
        /// 获取指定ViewModel对应的视图实例
        /// 如果视图已缓存，返回缓存的实例；否则创建新实例并缓存
        /// </summary>
        /// <param name="viewModel">ViewModel实例</param>
        /// <returns>视图实例，如果无法创建则返回null</returns>
        public object? GetView(object viewModel)
        {
            if (viewModel == null)
            {
                return null;
            }

            // Check cache first
            if (_viewCache.TryGetValue(viewModel, out var cachedView))
            {
                return cachedView;
            }

            // Create new view
            var viewModelType = viewModel.GetType();
            if (!_viewModelToViewMap.TryGetValue(viewModelType, out var viewType))
            {
                // View not registered - return null instead of throwing exception
                // This allows graceful degradation
                return null;
            }

            try
            {
                var view = (UserControl)Activator.CreateInstance(viewType)!;
                if (view == null)
                {
                    // Failed to create view instance
                    return null;
                }

                view.DataContext = viewModel;
                _viewCache[viewModel] = view;

                return view;
            }
            catch (Exception ex)
            {
                // Log error and return null to prevent application crash
                // In production, this should use ILoggingService
                System.Diagnostics.Debug.WriteLine($"Failed to create view for {viewModelType.Name}: {ex.Message}");
                return null;
            }
        }
    }
}

