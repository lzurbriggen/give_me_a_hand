using Godot;
using System;

public class StartMenu : Control {

  private Button startButton;
  private Button settingsButton;

  private PauseMenu pauseMenu;

  public override void _Ready() {
    startButton = GetNode<Button>("start");
    settingsButton = GetNode<Button>("settings");
    pauseMenu = GetNode<PauseMenu>("pauseMenu");

    startButton.Connect("pressed", this, nameof(startGame), new Godot.Collections.Array());
    settingsButton.Connect("pressed", this, nameof(showSettings), new Godot.Collections.Array());
  }

  void startGame() {
    GetTree().ChangeScene("res://scenes/dev_01.tscn");
  }

  void showSettings() {
    pauseMenu.Show();
  }

  public override void _Process(float delta) {
    if (Input.IsActionJustPressed("ui_cancel")) {
      if (pauseMenu.Visible) {
        pauseMenu.Hide();
      } else {
        pauseMenu.Show();
      }
    }
  }
}
