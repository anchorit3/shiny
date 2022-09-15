﻿using Shiny.Sensors;

namespace Sample.Sensors;


public class SensorViewModel<TReading> : ReactiveObject, ISensorViewModel
{
    IDisposable? sensorSub;


    public SensorViewModel(ISensor<TReading> sensor, string valueName, string? title = null)
    {
        this.Title = title ?? sensor.GetType().Name.Replace("Impl", String.Empty);
        this.ValueName = valueName;
        this.ToggleText = sensor.IsAvailable ? "Start" : "Sensor Not Available";

        this.Toggle = ReactiveCommand.Create(() =>
        {
            if (!sensor.IsAvailable)
                return;

            if (this.sensorSub == null)
            {
                this.ToggleText = "Stop";
                this.sensorSub = sensor
                    .WhenReadingTaken()
                    .Sample(TimeSpan.FromMilliseconds(500))
                    .Subscribe(x => this.Value = x!.ToString());
            }
            else
            {
                this.ToggleText = "Start";
                this.sensorSub.Dispose();
                this.sensorSub = null;
            }
        });
    }


    public string? Title { get; }
    public ICommand Toggle { get; }
    public string? ValueName { get; }


    [Reactive] public string? Value { get; private set; }
    [Reactive] public string? ToggleText { get; private set; }
}
