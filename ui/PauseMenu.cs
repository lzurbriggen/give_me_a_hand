using Godot;
using System;

public class PauseMenu : Control {
  private int MasterBusIndex = AudioServer.GetBusIndex("Master");
  private int MusicBusIndex = AudioServer.GetBusIndex("Music");
  private int SfxBusIndex = AudioServer.GetBusIndex("Sfx");

  private Control rect;
  private HSlider musicSlider;
  private HSlider sfxSlider;

  public override void _Ready() {
    rect = GetNode<Control>("rect");
    musicSlider = rect.GetNode<HSlider>("musicSlider");
    sfxSlider = rect.GetNode<HSlider>("sfxSlider");

    musicSlider.Value = AudioServer.GetBusVolumeDb(MusicBusIndex);
    sfxSlider.Value = AudioServer.GetBusVolumeDb(SfxBusIndex);
  }

  public override void _Process(float delta) {
    AudioServer.SetBusVolumeDb(MusicBusIndex, Convert.ToSingle(musicSlider.Value));
    AudioServer.SetBusVolumeDb(SfxBusIndex, Convert.ToSingle(sfxSlider.Value));
    GD.Print(AudioServer.GetBusVolumeDb(MusicBusIndex));
    GD.Print(AudioServer.GetBusVolumeDb(SfxBusIndex));
  }
}
