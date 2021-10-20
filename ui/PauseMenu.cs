using Godot;
using System;

public class PauseMenu : Control {
  private int MasterBusIndex = AudioServer.GetBusIndex("Master");
  private int MusicBusIndex = AudioServer.GetBusIndex("Music");
  private int SfxBusIndex = AudioServer.GetBusIndex("Sfx");

  private Control rect;
  private HSlider musicSlider;
  private HSlider sfxSlider;
  private Button exitButton;

  public override void _Ready() {
    this.Hide();
    rect = GetNode<Control>("rect");
    musicSlider = rect.GetNode<HSlider>("musicSlider");
    sfxSlider = rect.GetNode<HSlider>("sfxSlider");
    exitButton = rect.GetNode<Button>("exit");
    exitButton.Connect("pressed", this, nameof(exitGame), new Godot.Collections.Array());

    musicSlider.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(MusicBusIndex));
    sfxSlider.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(SfxBusIndex));
  }

  public override void _Process(float delta) {
    AudioServer.SetBusVolumeDb(MusicBusIndex, GD.Linear2Db(Convert.ToSingle(musicSlider.Value)));
    AudioServer.SetBusVolumeDb(SfxBusIndex, GD.Linear2Db(Convert.ToSingle(sfxSlider.Value)));
  }

  void exitGame() {
    GetTree().Quit();
  }
}
