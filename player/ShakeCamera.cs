using Godot;
using System;

public class ShakeCamera : Camera2D {
  float decay = 0.8f;
  Vector2 maxOffset = new Vector2(100, 75);
  float maxRoll = 0.1f;

  float trauma = 0.0f;
  float traumaPower = 2f;
  RandomNumberGenerator rnd = new RandomNumberGenerator();

  public override void _Ready() {
	SetProcess(true);
	SetPhysicsProcess(true);
  }

  public void addTrauma(float amount) {
	trauma = Mathf.Min(trauma + amount, 1.0f);
  }

  public override void _Process(float delta) {
	if (trauma > 0.0f) {
	  trauma = Mathf.Max(trauma - decay * delta, 0);
	  shake();
	}
  }

  protected void shake() {
	var amount = Mathf.Pow(trauma, traumaPower);
	Rotation = maxRoll * amount * rnd.RandfRange(-1, 1);
	var offset = Offset;
	offset.x = maxOffset.x * amount * rnd.RandfRange(-1, 1);
	offset.y = maxOffset.y * amount * rnd.RandfRange(-1, 1);
	Offset = offset;
  }
}
