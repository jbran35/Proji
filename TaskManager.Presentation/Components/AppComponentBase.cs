using BlazorBootstrap;
using Microsoft.AspNetCore.Components;

namespace TaskManager.Presentation.Components
{
    public abstract class AppComponentBase : ComponentBase
    {
        [Inject] protected ToastService ToastService { get; set; } = default!;

        [Inject] protected ILoggerFactory LoggerFactory { get; set; } = default!;

        private ILogger? _logger; 
        protected ILogger Logger => _logger ??= LoggerFactory.CreateLogger(GetType());

    }
}
