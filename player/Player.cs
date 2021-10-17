using Godot;
using System;

public class Player : Node {
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  private PhysicsBody2D body;
  private PhysicsBody2D hand;
  private Line2D armLine;

  public override void _Ready() {
    body = GetNode<PhysicsBody2D>("body");
    hand = GetNode<PhysicsBody2D>("hand");
    armLine = GetNode<Line2D>("armLine");
  }

  public override void _PhysicsProcess(float delta) {
    armLine.SetPointPosition(0, body.Position);
    armLine.SetPointPosition(1, hand.Position);
    armLine.Width = (1f - Mathf.Clamp(body.Position.DistanceTo(hand.Position) / 48f, 0f, 1f)) * 4f + 2f;
  }

  public override void _Input(InputEvent ev) {
    if (ev is InputEventMouseMotion) {
      hand.Position = (ev as InputEventMouseMotion).Position;
    }
  }
}
