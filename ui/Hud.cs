using Godot;
using System;

public class Hud : Control {
  float timeElapsed = 0f;
  Label timeLabel;

  public override void _Ready() {
    timeLabel = GetNode<Label>("time");
  }

  public override void _Process(float delta) {
    timeElapsed += delta;
    timeLabel.Text = timeString();
  }

  string timeString() {
    var seconds = Math.Floor(timeElapsed % 60);
    var milliseconds = (timeElapsed % 60 - Math.Truncate(timeElapsed % 60)) * 1000;
    var minutes = Math.Floor(timeElapsed / 60);
    return string.Format("{0}:{1:00}.{2:000}", minutes, seconds, milliseconds);
  }
}
