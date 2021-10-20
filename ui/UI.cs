using Godot;
using System;

public class UI : CanvasLayer {
  private bool gamePaused = false;
  private Control pauseMenu;

  public override void _Ready() {
    //   get_tree().paused = true
    pauseMenu = GetNode<Control>("pauseMenu");
  }

  public override void _Process(float delta) {
    if (Input.IsActionJustPressed("ui_cancel")) {
      if (gamePaused) {
        pauseMenu.Hide();
        GetTree().Paused = false;
        gamePaused = false;
      } else {
        pauseMenu.Show();
        GetTree().Paused = true;
        gamePaused = true;
      }
    }
  }
}
