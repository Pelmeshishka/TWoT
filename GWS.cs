using Godot;
using System;

public class GWS
{
    public const int WORLD_WIDTH = WORLD_CHUNKS * CHUNK_WIDTH;
    public const int WORLD_HEIGHT = WORLD_SECTIONS * SECTION_HEIGHT;

    public const int WORLD_CHUNKS = 128*2;
    public const int WORLD_SECTIONS = 20;

    public const int CHUNK_WIDTH = 16;
    public const int SECTION_HEIGHT = 16;

    public const int SECTION_BLOCKS_COUNT = SECTION_HEIGHT * CHUNK_WIDTH * CHUNK_WIDTH;

    public const string DBIP = "localhost";
    public const int DBPORT = 5432;
    public const string DBNAME = "db_twot";

    public const ushort searchPort = 4343;

    public const int SIMULATING_DISTANCE = 3;

    public const int MAX_BLOCKS_IN_SECTION = GWS.CHUNK_WIDTH * GWS.CHUNK_WIDTH * GWS.SECTION_HEIGHT;

}
