using Godot;
using System;

public partial class TesterCamera : Camera3D
{
    [Export] public float MoveSpeed = 5.0f;      // 移动速度
    [Export] public float RotateSpeed = 0.002f;  // 旋转灵敏度
    [Export] public float ZoomSpeed = 0.1f;      // 缩放速度
    [Export] public float RotationDamp = 0.9f;   // 旋转阻尼
    [Export] public float MoveDamp = 0.1f;       // 移动阻尼

    private Vector3 _moveDirection;
    private Vector3 _targetPosition;
    private Basis _targetBasis;
    private bool _isRotating;

    public override void _Ready()
    {
        _targetBasis = Basis;
        GD.Print("Free Camera Enabled");
    }

    public override void _Process(double delta)
    {
        HandleInput(delta);
        SmoothMovement(delta);
        ApplyTransform();
    }

    private void HandleInput(double delta)
    {
        // 鼠标右键拖拽旋转
        if (Input.IsMouseButtonPressed(MouseButton.Right))
        {
            RotateCamera(Input.GetLastMouseScreenVelocity());
            _isRotating = true;
        }
        else
        {
            _isRotating = false;
        }

        // WASD 移动
        HandleMovement(delta);

        // 滚轮缩放
        //HandleZoom();
    }

    private void RotateCamera(Vector2 delta)
    {
        // 垂直旋转（上下看）
        float vertical = delta.Y * RotateSpeed;
        _targetBasis = _targetBasis.Rotated(Vector3.Up, vertical);

        // 水平旋转（左右看）
        float horizontal = delta.X * RotateSpeed;
        _targetBasis = _targetBasis.Rotated(Vector3.Right, horizontal);
    }

    private void HandleMovement(double delta)
    {
        Vector3 forward = _targetBasis.Z * -1;
        Vector3 right = _targetBasis.X;

        // 计算移动方向
        _moveDirection = Vector3.Zero;
        if (Input.IsActionPressed("ui_up")) _moveDirection += forward;
        if (Input.IsActionPressed("ui_down")) _moveDirection -= forward;
        if (Input.IsActionPressed("ui_left")) _moveDirection -= right;
        if (Input.IsActionPressed("ui_right")) _moveDirection += right;

        // 标准化并应用速度
        if (_moveDirection != Vector3.Zero)
        {
            _moveDirection = _moveDirection.Normalized();
            _targetPosition += _moveDirection * MoveSpeed * (float)delta;
        }
    }

    //private void HandleZoom()
    //{
    //    float scroll = Input.Get;
    //    _targetPosition += _targetBasis.Z * scroll * ZoomSpeed;
    //}

    private void SmoothMovement(double delta)
    {
        // 平滑插值位置
        Position = Position.Lerp(_targetPosition, (float)delta * MoveDamp);

        // 平滑插值旋转（使用球面插值）
        Basis currentBasis = Basis.Slerp(_targetBasis, (float)delta * RotationDamp);
        Basis = currentBasis;
    }

    private void ApplyTransform()
    {
        Transform = new Transform3D(_targetBasis, Position);
    }

}
