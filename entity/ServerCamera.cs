using Godot;
using System;

public partial class ServerCamera : Entity
{
    private float minSpeed = 300f;
    private float speed;
    private float mouseSensitivity = 20f;

    private Vector2 rotDir;

    [Export] Node3D yRot, xRot;
    [Export] CollisionShape3D collision;

    [Export] Label posLabel;
    [Export] Label chunkPosLabel;
    [Export] Label sectionPosLabel;

    [Export] Label blockPosLabel;

    public override void _Process(double delta)
    {
        base._Process(delta);

        posLabel.Text = $"{this.GlobalPosition.X}/{this.GlobalPosition.Y}/{this.GlobalPosition.Z}";
        chunkPosLabel.Text = $"{this.lastChunk.X}/{this.lastChunk.Y}";
        sectionPosLabel.Text = $"{this.lastSection}";

        blockPosLabel.Text = $"{this.lookAtBlock}";

        float fdelta = (float)delta;
        float rotSpeed = mouseSensitivity * fdelta;

        if (rotDir.Length() > 0)
        {
            yRot.RotateY(Mathf.DegToRad(-rotDir.X * rotSpeed));
            float changeX = -rotDir.Y * rotSpeed;
            if (changeX + xRot.RotationDegrees.X < 90 && changeX + xRot.RotationDegrees.X > -90)
            {
                xRot.RotateX(Mathf.DegToRad(changeX));
            }

            rotDir = Vector2.Zero;
        }

        if (Input.IsActionPressed("sprint"))
        {
            speed = minSpeed * 5;
        } 
        else
        {
            speed = minSpeed;
        }

        if (Input.IsActionJustPressed("collision"))
        {
            collision.Disabled = !collision.Disabled;
        }

        if (Input.IsActionJustPressed("hit_left"))
        {
            Vector2I blockChunkPos = new Vector2I(Mathf.FloorToInt((float)lookAtBlock.X / GWS.CHUNK_WIDTH), Mathf.FloorToInt((float)lookAtBlock.Z / GWS.CHUNK_WIDTH));
            int blockSectionPos = Mathf.FloorToInt((float)lookAtBlock.Y / GWS.SECTION_HEIGHT);

            int blockIndex = World.GetBlockIndex(new Vector3I(lookAtBlock.X - blockChunkPos.X * GWS.CHUNK_WIDTH, lookAtBlock.Y - blockSectionPos * GWS.SECTION_HEIGHT, lookAtBlock.Z - blockChunkPos.Y * GWS.CHUNK_WIDTH));
            
            if (TryGetDimSection(out DimSection dimSection) && dimSection.TryGetChunk(blockChunkPos, out Chunk chunk) && chunk.TryGetSection(blockSectionPos, out ChunkSection section))
            {
                section.SetBlockStateData(Blocks.Air.defaultBlockState, blockIndex);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        float fdelta = (float)delta;

        Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
        Vector3 direction = (yRot.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        Vector3 velocity = Velocity;
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * speed * fdelta;
            velocity.Z = direction.Z * speed * fdelta;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, speed * fdelta);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, speed * fdelta);
        }

        if (Input.IsActionPressed("up"))
        {
            velocity.Y = speed * fdelta;
        }
        else if (Input.IsActionPressed("down"))
        {
            velocity.Y = -speed * fdelta;
        } 
        else
        {
            velocity.Y = Mathf.MoveToward(velocity.Y, 0, speed * fdelta);
        }


        Velocity = velocity;
        MoveAndSlide();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            rotDir = motion.Relative;
        }
    }
}
