using Godot;
using System;

public class Player : Node2D {
  private RigidBody2D body;
  private StaticBody2D hand;
  private Line2D armLine;
  private DampedSpringJoint2D armSpring;
  // private PinJoint2D armSpring;
  private AnimatedSprite handSprite;
  private Camera2D camera;


  private bool closed = false;
  private Vector2 closePosition = Vector2.Zero;

  public override void _Ready() {
    body = GetNode<RigidBody2D>("body");
    hand = GetNode<StaticBody2D>("hand");
    armLine = GetNode<Line2D>("armLine");
    armSpring = GetNode<DampedSpringJoint2D>("armSpring");
    // armSpring = GetNode<PinJoint2D>("armJoint");
    handSprite = hand.GetNode<AnimatedSprite>("sprite");
    camera = body.GetNode<Camera2D>("camera");

    // TODO: move to appropriate script
    Input.SetMouseMode(Input.MouseMode.Confined);
    // Input.SetMouseMode(Input.MouseMode.Hidden);

    var map = GetTree().Root.GetNode<Node2D>("root/map");
    var tilemap = (TileMap)map.GetChild(0).GetChild(1);
    var tilebounds = tilemap.GetUsedRect().Size * tilemap.CellSize;
    GD.Print(tilemap.GetUsedRect().Size * tilemap.CellSize);
    camera.LimitLeft = 0;
    camera.LimitBottom = 0;
    camera.LimitRight = Mathf.RoundToInt(tilebounds.x);
    camera.LimitTop = -Mathf.RoundToInt(tilebounds.y);
  }

  public override void _Process(float delta) {
    var handPosition = getHandCollision();
    if (!closed) {
      if (handPosition != null) {
        hand.GlobalPosition = (Vector2)handPosition;
        hand.Show();
      } else {
        hand.Hide();
      }
    }
  }

  public override void _PhysicsProcess(float delta) {
    if (closed) {
      hand.GlobalPosition = closePosition;
      hand.Show();
    } else {
      hand.GlobalPosition = body.GlobalPosition + body.GlobalPosition.DirectionTo(GetGlobalMousePosition()) * Mathf.Min(body.GlobalPosition.DistanceTo(GetGlobalMousePosition()), 48);
      armSpring.GlobalPosition = hand.GlobalPosition;
    }
    armLine.SetPointPosition(0, body.Position);
    armLine.SetPointPosition(1, hand.Position);
    armLine.Width = (1f - Mathf.Clamp(body.Position.DistanceTo(hand.Position) / 48f, 0f, 1f)) * 4f + 2f;
  }

  Vector2? getHandCollision() {
    var spaceState = GetWorld2d().DirectSpaceState;
    var result = spaceState.IntersectRay(body.GlobalPosition, GetGlobalMousePosition());
    if (result.Count == 0) {
      return null;
    }
    return (Vector2)result["position"];
  }

  public override void _Input(InputEvent ev) {

    if (Input.IsActionJustPressed("grab")) {

      var handPosition = getHandCollision();
      if (handPosition != null) {
        closed = true;
        armSpring.NodeA = hand.GetPath();
        armSpring.NodeB = body.GetPath();
        closePosition = (Vector2)handPosition;
        handSprite.Animation = "hold";
        armLine.Show();
      }
    }

    if (closed && Input.IsActionJustReleased("ui_accept")) {
      armSpring.NodeA = "";
      armSpring.NodeB = "";
      closed = false;
      var impulseAmount = (Mathf.Clamp(body.Position.DistanceTo(hand.Position) / 48f, 0f, 1f)) * 250f + 50f;
      body.ApplyImpulse(Vector2.Zero, body.Position.DirectionTo(hand.Position) * impulseAmount);
      handSprite.Animation = "default";
      armLine.Hide();
    }

    var moveForce = 10f;
    if (Input.IsActionPressed("ui_left")) {
      body.ApplyImpulse(Vector2.Zero, Vector2.Left * moveForce);
    }
    if (Input.IsActionPressed("ui_right")) {
      body.ApplyImpulse(Vector2.Zero, Vector2.Right * moveForce);
    }
    if (Input.IsActionPressed("ui_up")) {
      body.ApplyImpulse(Vector2.Zero, Vector2.Up * moveForce);
    }
    if (Input.IsActionPressed("ui_down")) {
      body.ApplyImpulse(Vector2.Zero, Vector2.Down * moveForce);
    }

    // TODO: move to appropriate script
    if (Input.IsActionPressed("ui_cancel")) {
      // GetTree().Quit();
    }
  }
}
