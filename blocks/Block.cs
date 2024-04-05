using Godot;
using System;

public class Block
{
    public string key { get; set; }
    public Properties properties { get; set; }

    public BlockState defaultBlockState { get; set; }

    public Block(string key, Properties properties)
    {
        this.key = key.ToLower();
        this.properties = properties;

        if (defaultBlockState == null)
        {
            defaultBlockState = new BlockState(this);
        }
    }

    public class Properties
    {

        public bool isSolid { get; private set; } = true;
        public Properties NonSolid()
        {
            isSolid = false;
            return this;
        }
    }
}
