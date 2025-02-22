﻿using GlazeWM.Domain.Containers;
using GlazeWM.Domain.Containers.Commands;
using GlazeWM.Domain.Windows.Commands;
using GlazeWM.Infrastructure.Bussing;

namespace GlazeWM.Domain.Windows.CommandHandlers
{
  class RemoveWindowHandler : ICommandHandler<RemoveWindowCommand>
  {
    private Bus _bus;
    private ContainerService _containerService;

    public RemoveWindowHandler(Bus bus, ContainerService containerService)
    {
      _bus = bus;
      _containerService = containerService;
    }

    public CommandResponse Handle(RemoveWindowCommand command)
    {
      var window = command.Window;

      // Keep references to the window's parent and grandparent prior to detaching.
      var parent = window.Parent;
      var grandparent = parent.Parent;

      if (window is IResizable)
        _bus.Invoke(new DetachAndResizeContainerCommand(window));
      else
        _bus.Invoke(new DetachContainerCommand(window));

      // Get container to switch focus to after the window has been removed. The OS automatically
      // switches focus to a different window after closing, so by setting `PendingFocusContainer`
      // this behavior is overridden.
      var containerToFocus = parent.LastFocusedDescendant ?? grandparent.LastFocusedDescendant;
      _containerService.PendingFocusContainer = containerToFocus;

      return CommandResponse.Ok;
    }
  }
}
