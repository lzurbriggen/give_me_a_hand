using Godot;
using System;

public class Rocket : Node2D {
  private Area2D area;
  private AnimatedSprite sprite;
  private CPUParticles2D idleSmoke;
  private CPUParticles2D launchSmoke;
  private CPUParticles2D flightSmoke;
  private Camera2D camera;

  private Timer launchTimer;

  private bool flying = false;

  public override void _Ready() {
    area = GetNode<Area2D>("area");
    sprite = GetNode<AnimatedSprite>("sprite");

    idleSmoke = GetNode<CPUParticles2D>("idleSmoke");
    launchSmoke = GetNode<CPUParticles2D>("launchSmoke");
    flightSmoke = GetNode<CPUParticles2D>("flightSmoke");

    sprite.Play("idle");
    idleSmoke.Show();
    launchSmoke.Hide();
    flightSmoke.Hide();

    camera = GetNode<Camera2D>("camera");

    area.Connect("body_entered", this, nameof(bodyEntered));
    sprite.Connect("animation_finished", this, nameof(animFinished));

    launchTimer = new Timer();
    launchTimer.OneShot = true;
    launchTimer.WaitTime = 1.5f;
    launchTimer.Connect("timeout", this, nameof(launch));
    AddChild(launchTimer);
  }

  void bodyEntered(PhysicsBody2D body) {
    if (body is KinematicBody2D) {
      var hud = GetTree().Root.GetNode<CanvasLayer>("root/ui").GetNode<Hud>("hud");
      hud.Finished = true;
      launchTimer.Start();
      var player = body.GetParent<Player>();
      player.Hide();
      player.SetPhysicsProcess(false);
      player.SetProcess(false);
      camera.Current = true;
    }
  }

  void launch() {
    sprite.Play("launch");
    idleSmoke.Hide();
    launchSmoke.Show();
  }

  void animFinished() {
    if (sprite.Animation == "launch") {
      sprite.Play("flight");
      launchSmoke.Hide();
      flightSmoke.Show();
      flying = true;
    }
  }

  public override void _PhysicsProcess(float delta) {
    if (flying) {
      Position += new Vector2(0, -30f * delta);

      if (Position.y <= -3300) {
        GetTree().ChangeScene("res://scenes/start.tscn");
      }
    }
  }
}
