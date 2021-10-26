using Godot;
using System;

public class Player : Node2D {
  // private RigidBody2D body;
  private KinematicBody2D body;
  private Vector2 velocity;
  private StaticBody2D hand;
  private Line2D armLine;
  private Line2D aimLine;
  // private DampedSpringJoint2D armSpring;
  private PinJoint2D armSpring;
  private PlayerBody bodySprite;
  private AnimatedSprite handSprite;
  private ShakeCamera camera;
  private Timer jumpTimer;

  private PackedScene dustScn;


  private bool closed = false;
  private Vector2 closePosition = Vector2.Zero;

  private AudioStreamPlayer hitSound;
  private AudioStreamPlayer jumpSound;
  private AudioStreamPlayer jumpChargeSound;

  private Timer blinkTimer;
  private Random random;

  float remap(float value, float prevLow, float prevHigh, float newLow, float newHigh) {
    return newLow + (value - prevLow) * (newHigh - newLow) / (prevHigh - prevLow);
  }

  public override void _Ready() {
    // body = GetNode<RigidBody2D>("body");
    body = GetNode<KinematicBody2D>("body2");
    hand = GetNode<StaticBody2D>("hand");
    armLine = GetNode<Line2D>("armLine");
    aimLine = GetNode<Line2D>("aimLine");
    // armSpring = GetNode<DampedSpringJoint2D>("armSpring");
    armSpring = GetNode<PinJoint2D>("armJoint");
    bodySprite = body.GetNode<PlayerBody>("sprite");
    handSprite = hand.GetNode<AnimatedSprite>("sprite");
    camera = body.GetNode<ShakeCamera>("camera");
    dustScn = ResourceLoader.Load<PackedScene>("res://player/dust.tscn");

    hitSound = GetNode<AudioStreamPlayer>("hitSound");
    jumpSound = GetNode<AudioStreamPlayer>("jumpSound");
    jumpChargeSound = GetNode<AudioStreamPlayer>("jumpChargeSound");

    random = new Random();
    blinkTimer = new Timer();
    AddChild(blinkTimer);
    blinkTimer.OneShot = true;
    blinkTimer.Connect("timeout", this, nameof(blink));
    startBlinkTimer();

    jumpTimer = new Timer();
    AddChild(jumpTimer);
    jumpTimer.WaitTime = 0.75f;
    jumpTimer.Autostart = false;
    jumpTimer.OneShot = true;
    jumpTimer.Stop();

    // TODO: move to appropriate script
    Input.SetMouseMode(Input.MouseMode.Confined);

    var map = GetTree().Root.GetNode<Node2D>("root/map");
    var tilemap = (TileMap)map.GetChild(0).GetChild(1);
    var tilebounds = tilemap.GetUsedRect().Size * tilemap.CellSize;
    camera.LimitLeft = 0;
    camera.LimitBottom = 0;
    camera.LimitRight = Mathf.RoundToInt(tilebounds.x);
    camera.LimitTop = -Mathf.RoundToInt(tilebounds.y);
  }

  void startBlinkTimer() {
    blinkTimer.WaitTime = (float)random.NextDouble() * 5f + 0.25f;
    blinkTimer.Start();
  }
  void blink() {
    if (bodySprite.Animation == "default" || bodySprite.Animation == "blink") {
      bodySprite.Play("blink");
      bodySprite.Frame = 0;
    }
    startBlinkTimer();
  }

  Vector2? getRayPointFromBody(Vector2 vec) {
    var spaceState = GetWorld2d().DirectSpaceState;
    var result = spaceState.IntersectRay(body.GlobalPosition, body.GlobalPosition + vec * 8.1f, new Godot.Collections.Array(body));
    if (result.Count == 0) {
      return null;
    }
    return (Vector2)result["position"];
  }

  public override void _Process(float delta) {
    var handPosition = getHandCollision();

    if (body.GlobalPosition.x < camera.LimitLeft) {
      body.GlobalPosition = new Vector2(camera.LimitRight - 0.1f, body.GlobalPosition.y);
    } else if (body.GlobalPosition.x > camera.LimitRight) {
      body.GlobalPosition = new Vector2(camera.LimitLeft + 0.1f, body.GlobalPosition.y);
    }

    if (!closed) {
      if (handPosition != null) {
        hand.GlobalPosition = (Vector2)handPosition;
        hand.Show();
        aimLine.Show();
      } else {
        hand.Hide();
        aimLine.Hide();
      }
    }
    if (!jumpTimer.IsStopped()) {
      if ((jumpTimer.TimeLeft / jumpTimer.WaitTime) < 0.05f) {
        bodySprite.Modulate = new Color(1f, 0, 0);
        handSprite.Modulate = new Color(1f, 0, 0);
        armLine.DefaultColor = new Color(1f, 0, 0);
      }
      bodySprite.Play("compress");
    }
  }

  public override void _PhysicsProcess(float delta) {
    if (closed) {
      hand.GlobalPosition = closePosition;
      hand.Show();
      armLine.SetPointPosition(0, body.Position);
      armLine.SetPointPosition(1, hand.Position);
      armLine.Width = 4;
      var dir = body.Position.DirectionTo(hand.Position);
      float angle = Mathf.Atan2(dir.y, dir.x);
      // TODO: something's wrong i can feel it
      int octant = Mathf.RoundToInt(8 * angle / (2 * Mathf.Pi) + 8) % 8;
      handSprite.Animation = (8 - (octant + 2) % 8).ToString();

    } else {
      aimLine.SetPointPosition(0, body.Position);
      aimLine.SetPointPosition(1, hand.Position);
      armSpring.GlobalPosition = hand.GlobalPosition;

      if (slowEnough()) {
        aimLine.DefaultColor = new Color(1f, 1f, 1f, 0.5f);
      } else {
        aimLine.DefaultColor = new Color(1f, 0, 0, 0.5f);
      }
    }

    if (OS.IsDebugBuild()) {
      var moveForce = 500f;
      var moveDir = Vector2.Zero;
      if (Input.IsActionPressed("ui_left")) {
        moveDir += Vector2.Left;
      }
      if (Input.IsActionPressed("ui_right")) {
        moveDir += Vector2.Right;
      }
      if (Input.IsActionPressed("ui_up")) {
        moveDir += Vector2.Up;
      }
      if (Input.IsActionPressed("ui_down")) {
        moveDir += Vector2.Down;
      }
      if (moveDir.Length() > 0f) {
        velocity = Vector2.Zero;
        body.Position += moveDir * moveForce * delta;
      }
    }

    // var velocity = speed * delta * direction # move slow? increases speed or multiply this x 100

    // velocity *= (1f - 0.2f * delta);
    velocity.y += 98f * 2f * delta;
    body.MoveAndSlide(velocity);
    if (body.IsOnFloor()) {
      velocity.y = 0;
    }
    if (body.GetSlideCount() > 0) {
      var collision = body.GetSlideCollision(0);
      if (collision != null) {

        if (velocity.Length() > 30f) {
          var hitPower = (Mathf.Max(velocity.Length() - 150f, 0f)) / 60f;
          camera.addTrauma(Mathf.Min(hitPower, 0.14f));
          hitSound.VolumeDb = (-18f) * (1f - Mathf.Clamp(hitPower, 0f, 1f));
          hitSound.Play();
          var dust = dustScn.Instance<Dust>();
          dust.Position = collision.Position;
          GetTree().Root.AddChild(dust);
        }

        // damping seems weird
        if (collision.Normal.y == -1) {
          velocity.x *= (1f - 0.2f * delta);
        }

        var bounciness = 0.5f;

        velocity = velocity.Bounce(collision.Normal) * bounciness;
      }
    }

  }

  Vector2? getHandCollision() {
    var spaceState = GetWorld2d().DirectSpaceState;
    var dir = body.GlobalPosition.DirectionTo(GetGlobalMousePosition());
    var result = spaceState.IntersectRay(body.GlobalPosition, body.GlobalPosition + dir * 320f, new Godot.Collections.Array(body));
    if (result.Count == 0) {
      return null;
    }
    var normal = (Vector2)result["normal"];
    return (Vector2)result["position"] + normal * 2f;
  }

  float jumpStrengthMult() {
    if (!closed) {
      return 0f;
    }
    return remap(jumpTimer.TimeLeft, 0f, jumpTimer.WaitTime, 1f, 0.2f);
  }

  bool slowEnough() {
    return velocity.Length() <= 2f;
  }

  void stopGrab() {
    armSpring.NodeA = "";
    armSpring.NodeB = "";
    closed = false;
    handSprite.Animation = "default";
    armLine.Hide();
    aimLine.Show();
    bodySprite.Stop();
    bodySprite.Play("default");
    bodySprite.Modulate = new Color(1f, 1f, 1f);
    handSprite.Modulate = new Color(1f, 1f, 1f);
    armLine.DefaultColor = new Color(1f, 1f, 1f);
    jumpTimer.Stop();
  }

  public override void _Input(InputEvent ev) {
    if (Input.IsActionJustPressed("ui_accept")) {
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
            aimLine.Hide();
            jumpTimer.Start();
            jumpChargeSound.Play();
          }
        }
      }
    }

    if (closed && Input.IsActionJustReleased("ui_accept")) {
      jumpChargeSound.Stop();
      jumpSound.Play();
      var impulseAmount = jumpStrengthMult() * 500f;
      velocity = body.Position.DirectionTo(hand.Position) * impulseAmount;
      // body.ApplyImpulse(Vector2.Zero, body.Position.DirectionTo(hand.Position) * impulseAmount);
      stopGrab();
      var downPoint = getRayPointFromBody(Vector2.Down);
      if (downPoint != null) {
        var dust = dustScn.Instance<Dust>();
        dust.Position = (Vector2)downPoint;
        GetTree().Root.AddChild(dust);
      }
    }
  }
}
