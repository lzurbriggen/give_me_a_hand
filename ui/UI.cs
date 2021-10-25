using Godot;
using System;

public class UI : CanvasLayer {
  private bool gamePaused = false;
  private Control pauseMenu;

  public override void _Ready() {
    pauseMenu = GetNode<Control>("pauseMenu");
  }

  public override void _Process(float delta) {
    if (Input.IsActionJustPressed("ui_cancel")) {
      if (gamePaused) {
        pauseMenu.Hide();
        Input.SetMouseMode(Input.MouseMode.Confined);
        GetTree().Paused = false;
        gamePaused = false;
      } else {
        pauseMenu.Show();
        Input.SetMouseMode(Input.MouseMode.Visible);
        GetTree().Paused = true;
        gamePaused = true;
      }
    }
  }
}
