using System.Linq;
using GlazeWM.Domain.Containers.Commands;
using GlazeWM.Domain.Containers.Events;
using GlazeWM.Domain.Monitors;
using GlazeWM.Domain.Workspaces;
using GlazeWM.Infrastructure.Bussing;
using GlazeWM.Infrastructure.WindowsApi.Events;

namespace GlazeWM.Domain.Windows.EventHandlers
{
  class WindowMovedOrResizedHandler : IEventHandler<WindowMovedOrResizedEvent>
  {
    private Bus _bus;
    private WindowService _windowService;
    private MonitorService _monitorService;
    private WorkspaceService _workspaceService;

    public WindowMovedOrResizedHandler(Bus bus, WindowService windowService, MonitorService monitorService, WorkspaceService workspaceService)
    {
      _bus = bus;
      _windowService = windowService;
      _monitorService = monitorService;
      _workspaceService = workspaceService;
    }

    public void Handle(WindowMovedOrResizedEvent @event)
    {
      var window = _windowService.GetWindows()
        .FirstOrDefault(window => window.Hwnd == @event.WindowHandle);

      if (window == null || !(window is FloatingWindow))
        return;

      // Update state with new location of the floating window.
      UpdateWindowPlacement(window);

      // Change floating window's parent workspace if out of its bounds.
      UpdateParentWorkspace(window);
    }

    private void UpdateWindowPlacement(Window window)
    {
      var updatedPlacement = _windowService.GetPlacementOfHandle(window.Hwnd).NormalPosition;
      window.FloatingPlacement = updatedPlacement;
    }

    private void UpdateParentWorkspace(Window window)
    {
      var currentWorkspace = _workspaceService.GetWorkspaceFromChildContainer(window);

      // Get workspace that encompasses most of the window.
      var targetMonitor = _monitorService.GetMonitorFromHandleLocation(window.Hwnd);
      var targetWorkspace = targetMonitor.DisplayedWorkspace;

      // Ignore if window is still within the bounds of its current workspace.
      if (currentWorkspace == targetWorkspace)
        return;

      // Change the window's parent workspace.
      _bus.Invoke(new MoveContainerWithinTreeCommand(window, targetWorkspace, false));
      _bus.RaiseEvent(new FocusChangedEvent(window));
    }
  }
}
