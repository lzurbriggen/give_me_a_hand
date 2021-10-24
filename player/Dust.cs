using Godot;
using System;

public class Dust : CPUParticles2D {
  public override void _Ready() {
	this.Emitting = true;
  }

  public override void _Process(float delta) {
	if (!this.Emitting) {
	  QueueFree();
	}
  }
}
