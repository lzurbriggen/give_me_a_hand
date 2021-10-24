using Godot;
using System;

public class PlayerBody : AnimatedSprite {

  public override void _Ready() {

  }

  public override void _PhysicsProcess(float delta) {
	GlobalRotation = 0;
  }
}
