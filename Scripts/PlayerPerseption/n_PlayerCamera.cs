using Godot;
using System;

namespace Meow;
public partial class n_PlayerCamera : Node3D
{
    [Export]
    public Camera3D Camera;
    [Export(PropertyHint.Range, "0.1,1.0")] 
    float camSensitivity = 0.3f;
    [Export(PropertyHint.Range, "-90,0,1")] 
    float minCamPitch = -85f;
    [Export(PropertyHint.Range, "0,90,1")] 
    float maxCamPitch = 85f;
    [Export]
    public bool DragInHorizontal = true;
    [Export]
    public float DragSpeed = 5f;

    public Marker3D CameraCenter;

    public override void _Ready()
    {
        base._Ready();
        if (Camera == null)
        {
            Camera = FindChild("Camera3D", true) as Camera3D;
        }

        CameraCenter = GetNode<Marker3D>("CameraCenter");
        SetProcessInput(true);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

    }

    public override void _Input(InputEvent @event)
    {
        if (Camera == null)
            return;
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Visible ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }

        Vector3 camRot = CameraCenter.RotationDegrees;

        if (@event is InputEventMouseMotion mouseMotion)
        {
            if (Input.IsMouseButtonPressed(MouseButton.Middle))
            {
                var move = Vector3.Zero;
                if (DragInHorizontal)
                {
                    move = - ((Vector3.Forward * mouseMotion.Relative.Y * -1f + Vector3.Right * DragSpeed * mouseMotion.Relative.X) * DragSpeed * (float)GetProcessDeltaTime()).Rotated(Vector3.Up, CameraCenter.GlobalRotation.Y);
                }
                else
                {
                    move = (CameraCenter.GlobalTransform.Basis.Y * mouseMotion.Relative.Y * -1f + CameraCenter.GlobalTransform.Basis.X * DragSpeed * mouseMotion.Relative.X) * DragSpeed * (float)GetProcessDeltaTime();
                }
                
                Position += move;
            }
            else if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                camRot.Y -= mouseMotion.Relative.X * camSensitivity;
                camRot.X -= mouseMotion.Relative.Y * camSensitivity;
            }
            
        }
        camRot.X = Mathf.Clamp(camRot.X, minCamPitch, maxCamPitch); //prevents camera from going endlessly around the player
        CameraCenter.RotationDegrees = camRot;
    }
}
