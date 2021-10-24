using Godot;
using System;

public class Player : Node2D {
  private RigidBody2D body;
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

  float remap(float value, float prevLow, float prevHigh, float newLow, float newHigh) {
	return newLow + (value - prevLow) * (newHigh - newLow) / (prevHigh - prevLow);
  }

  public override void _Ready() {
	body = GetNode<RigidBody2D>("body");
	hand = GetNode<StaticBody2D>("hand");
	armLine = GetNode<Line2D>("armLine");
	aimLine = GetNode<Line2D>("aimLine");
	// armSpring = GetNode<DampedSpringJoint2D>("armSpring");
	armSpring = GetNode<PinJoint2D>("armJoint");
	bodySprite = body.GetNode<PlayerBody>("sprite");
	handSprite = hand.GetNode<AnimatedSprite>("sprite");
	camera = body.GetNode<ShakeCamera>("camera");
	dustScn = ResourceLoader.Load<PackedScene>("res://player/dust.tscn");

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

	body.Connect("body_entered", this, nameof(bodyEntered));
  }

  Vector2? getRayPointFromBody(Vector2 vec) {
	var spaceState = GetWorld2d().DirectSpaceState;
	var result = spaceState.IntersectRay(body.GlobalPosition, body.GlobalPosition + vec * 8.1f);
	if (result.Count == 0) {
	  return null;
	}
	return (Vector2)result["position"];
  }

  // TODO: this is a mess. couldn't figure out a simple way to get contact positions 
  void bodyEntered(PhysicsBody2D other) {
	if (other != null) {
	  if (body.LinearVelocity.Length() > 30f) {
		camera.addTrauma(Mathf.Min((Mathf.Max(body.LinearVelocity.Length() - 60f, 0f)) / 60f + 0.09f, 0.14f));
		var dir = body.LinearVelocity.Normalized();
		float angle = Mathf.Atan2(dir.y, dir.x);
		int cardinalDir = Mathf.RoundToInt(4 * angle / (2 * Mathf.Pi) + 4) % 4;
		Vector2? result;
		if (cardinalDir == 0) {
		  result = getRayPointFromBody(Vector2.Left);
		} else if (cardinalDir == 1) {
		  result = getRayPointFromBody(Vector2.Up);
		} else if (cardinalDir == 2) {
		  result = getRayPointFromBody(Vector2.Right);
		} else {
		  result = getRayPointFromBody(Vector2.Down);
		}
		if (result != null) {
		  var dust = dustScn.Instance<Dust>();
		  dust.Position = (Vector2)result;
		  GetTree().Root.AddChild(dust);
		}
	  }
	}
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
		body.LinearVelocity = Vector2.Zero;
		body.Position += moveDir * moveForce * delta;
	  }
	}
  }

  Vector2? getHandCollision() {
	var spaceState = GetWorld2d().DirectSpaceState;
	var dir = body.GlobalPosition.DirectionTo(GetGlobalMousePosition());
	var result = spaceState.IntersectRay(body.GlobalPosition, body.GlobalPosition + dir * 320f);
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
	return body.LinearVelocity.Length() <= 7f;
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
		  }
		}
	  }
	}

	if (closed && Input.IsActionJustReleased("ui_accept")) {
	  var impulseAmount = jumpStrengthMult() * 500f;
	  body.ApplyImpulse(Vector2.Zero, body.Position.DirectionTo(hand.Position) * impulseAmount);
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
