using Godot;
using System;

public class UI : CanvasLayer {
  private Control pauseMenu;

  public override void _Ready() {
    pauseMenu = GetNode<Control>("pauseMenu");
  }

  public override void _Process(float delta) {
    if (Input.IsActionJustPressed("ui_cancel")) {
      if (pauseMenu.Visible) {
        pauseMenu.Hide();
        Input.SetMouseMode(Input.MouseMode.Confined);
        GetTree().Paused = false;
      } else {
        pauseMenu.Show();
        Input.SetMouseMode(Input.MouseMode.Visible);
        GetTree().Paused = true;
      }
    }
  }
}
