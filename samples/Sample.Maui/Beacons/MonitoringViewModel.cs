﻿using Shiny;
using Shiny.Beacons;

namespace Sample.Beacons;


public class MonitoringViewModel : ViewModel
{
    public MonitoringViewModel(BaseServices services, IBeaconMonitoringManager? beaconManager = null) : base(services)
    {
        this.Add = this.Navigation.Command("CreatePage");
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            if (beaconManager == null)
            {
                await dialogs.Alert("Beacon monitoring is not supported on this platform");
                return;
            }
            var regions = await beaconManager.GetMonitoredRegions();

            this.Regions = regions
                .Select(x => new CommandItem
                {
                    Text = $"{x.Identifier}",
                    Detail = $"{x.Uuid}/{x.Major ?? 0}/{x.Minor ?? 0}",
                    PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await beaconManager.StopMonitoring(x.Identifier);
                        this.Load.Execute(null);
                    })
                })
                .ToList();
        });

        this.StopAllMonitoring = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var result = await this.Dialogs.Confirm("Are you sure you wish to stop all monitoring");
                if (result)
                {
                    await beaconManager.StopAllMonitoring();
                    this.Load.Execute(null);
                }
            },
            Observable.Return(beaconManager != null)
        );
    }


    public ICommand Load { get; }
    public ICommand Add { get; }
    public ICommand StopAllMonitoring { get; }
    [Reactive] public IList<CommandItem> Regions { get; private set; }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.Load.Execute(null);
        return base.InitializeAsync(parameters);
    }
}