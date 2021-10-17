using Godot;
using System;

public class Player : Node2D {
  private RigidBody2D body;
  private StaticBody2D hand;
  private Line2D armLine;
  private DampedSpringJoint2D armSpring;

  private bool closed = false;
  private Vector2 closePosition = Vector2.Zero;

  public override void _Ready() {
    body = GetNode<RigidBody2D>("body");
    hand = GetNode<StaticBody2D>("hand");
    armLine = GetNode<Line2D>("armLine");
    armSpring = GetNode<DampedSpringJoint2D>("armSpring");

    // TODO: move to appropriate script
    Input.SetMouseMode(Input.MouseMode.Confined);

  }

  public override void _PhysicsProcess(float delta) {
    if (closed) {
      hand.GlobalPosition = closePosition;
    } else {
      hand.GlobalPosition = body.GlobalPosition + body.GlobalPosition.DirectionTo(GetGlobalMousePosition()) * Mathf.Min(body.GlobalPosition.DistanceTo(GetGlobalMousePosition()), 48);
    }
    armLine.SetPointPosition(0, body.Position);
    armLine.SetPointPosition(1, hand.Position);
    armLine.Width = (1f - Mathf.Clamp(body.Position.DistanceTo(hand.Position) / 48f, 0f, 1f)) * 4f + 2f;
  }

  public override void _Input(InputEvent ev) {

    if (Input.IsActionJustPressed("grab")) {
      closed = true;
      armSpring.NodeA = body.GetPath();
      armSpring.NodeB = hand.GetPath();
      closePosition = GetGlobalMousePosition();
    }

    if (closed && Input.IsActionJustReleased("ui_accept")) {
      armSpring.NodeA = "";
      armSpring.NodeB = "";
      closed = false;
      // TODO: calculate fractional impulse
      body.ApplyImpulse(Vector2.Zero, body.Position.DirectionTo(hand.Position) * 500f);
    }

    // TODO: move to appropriate script
    if (Input.IsActionPressed("ui_cancel")) {
      GetTree().Quit();
    }
  }
}
