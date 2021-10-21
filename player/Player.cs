using Godot;
using System;

public class Player : Node2D {
  private RigidBody2D body;
  private StaticBody2D hand;
  private Line2D armLine;
  // private DampedSpringJoint2D armSpring;
  private PinJoint2D armSpring;
  private PlayerBody bodySprite;
  private AnimatedSprite handSprite;
  private Camera2D camera;
  private Timer jumpTimer;


  private bool closed = false;
  private Vector2 closePosition = Vector2.Zero;

  float remap(float value, float prevLow, float prevHigh, float newLow, float newHigh) {
    return newLow + (value - prevLow) * (newHigh - newLow) / (prevHigh - prevLow);
  }

  public override void _Ready() {
    body = GetNode<RigidBody2D>("body");
    hand = GetNode<StaticBody2D>("hand");
    armLine = GetNode<Line2D>("armLine");
    // armSpring = GetNode<DampedSpringJoint2D>("armSpring");
    armSpring = GetNode<PinJoint2D>("armJoint");
    // TODO: why does the GetNode<Sprite> not work here?
    bodySprite = body.GetNode<PlayerBody>("sprite");
    // GD.Print(GetNode<Node2D>("body/sprite") as Sprite);
    handSprite = hand.GetNode<AnimatedSprite>("sprite");
    camera = body.GetNode<Camera2D>("camera");

    jumpTimer = new Timer();
    AddChild(jumpTimer);
    jumpTimer.WaitTime = 0.75f;
    jumpTimer.Autostart = false;
    jumpTimer.OneShot = true;
    jumpTimer.Stop();

    // TODO: move to appropriate script
    Input.SetMouseMode(Input.MouseMode.Confined);
    // Input.SetMouseMode(Input.MouseMode.Hidden);

    var map = GetTree().Root.GetNode<Node2D>("root/map");
    var tilemap = (TileMap)map.GetChild(0).GetChild(1);
    var tilebounds = tilemap.GetUsedRect().Size * tilemap.CellSize;
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
    if (!jumpTimer.IsStopped()) {
      var mult = 1f - jumpStrengthMult();
      bodySprite.Modulate = new Color(1f, 1f * mult, 1f * mult);
    }
  }

  public override void _PhysicsProcess(float delta) {
    if (closed) {
      hand.GlobalPosition = closePosition;
      hand.Show();
      armLine.SetPointPosition(0, body.Position);
      armLine.SetPointPosition(1, hand.Position);
      // armLine.Width = (1f - Mathf.Clamp(body.Position.DistanceTo(hand.Position) / 48f, 0f, 1f)) * 4f + 2f;
      armLine.Width = 5;
      var dir = body.Position.DirectionTo(hand.Position);
      float angle = Mathf.Atan2(dir.y, dir.x);
      int octant = Mathf.RoundToInt(8 * angle / (2 * Mathf.Pi) + 8) % 8;
      handSprite.Animation = (8 - (octant + 2) % 8).ToString();

    } else {
      hand.GlobalPosition = body.GlobalPosition + body.GlobalPosition.DirectionTo(GetGlobalMousePosition()) * Mathf.Min(body.GlobalPosition.DistanceTo(GetGlobalMousePosition()), 48);
      armSpring.GlobalPosition = hand.GlobalPosition;
    }

    // TODO: remove?
    if (closed) {
      var moveForce = 10f;
      var moveDir = Vector2.Zero;
      if (Input.IsActionPressed("ui_left")) {
        moveDir += Vector2.Left;
      }
      if (Input.IsActionPressed("ui_right")) {
        moveDir += Vector2.Right;
      }
      // if (Input.IsActionPressed("ui_up")) {
      //   moveDir += Vector2.Up;
      // }
      // if (Input.IsActionPressed("ui_down")) {
      //   moveDir += Vector2.Down;
      // }
      // TODO: doesn't seem right...
      moveDir *= body.Position.DirectionTo(hand.Position).Rotated(-(float)Math.PI / 2f);
      body.ApplyImpulse(Vector2.Zero, moveDir.Normalized() * -moveForce);
    }
  }

  Vector2? getHandCollision() {
    var spaceState = GetWorld2d().DirectSpaceState;
    var result = spaceState.IntersectRay(body.GlobalPosition, GetGlobalMousePosition());
    if (result.Count == 0) {
      return null;
    }
    return (Vector2)result["position"];
  }

  float jumpStrengthMult() {
    if (!closed) {
      return 0f;
    }
    return remap(jumpTimer.TimeLeft, 0f, jumpTimer.WaitTime, 1f, 0.2f);
  }

  bool slowEnough() {
    return body.LinearVelocity.Length() <= 5f;
  }

  public override void _Input(InputEvent ev) {
    if (Input.IsActionJustReleased("grab")) {
      if (!closed) {
        if (slowEnough()) {
          var handPosition = getHandCollision();
          if (handPosition != null) {
            closed = true;
            armSpring.Position = hand.Position;
            // TODO: necessary?
            armSpring.SetPhysicsProcess(false);
            armSpring.NodeA = hand.GetPath();
            armSpring.NodeB = body.GetPath();
            armSpring.SetPhysicsProcess(true);
            closePosition = (Vector2)handPosition;
            handSprite.Animation = "0";
            armLine.Show();
          }
        }
      } else {
        // TODO: refactor duplicate code
        armSpring.NodeA = "";
        armSpring.NodeB = "";
        closed = false;
        handSprite.Animation = "default";
        armLine.Hide();
        bodySprite.Modulate = new Color(1f, 1f, 1f);
        jumpTimer.Stop();
      }
    }

    if (closed && Input.IsActionJustPressed("ui_accept")) {
      jumpTimer.Start();
    }

    if (closed && Input.IsActionJustReleased("ui_accept")) {
      armSpring.NodeA = "";
      armSpring.NodeB = "";
      bodySprite.Modulate = new Color(1f, 1f, 1f);
      var impulseAmount = jumpStrengthMult() * 500f;
      body.ApplyImpulse(Vector2.Zero, body.Position.DirectionTo(hand.Position) * impulseAmount);
      handSprite.Animation = "default";
      armLine.Hide();
      closed = false;
    }
  }
}
