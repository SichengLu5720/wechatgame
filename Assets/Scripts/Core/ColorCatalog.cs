namespace StairsCrowd.Core
{
    // Stable IDs preserve existing saves and share codes.
    public static class ColorCatalog
    {
        public const int Count = 6;
        public static readonly string[] Names = { "红", "蓝", "绿", "黄", "紫", "黑" };
        public static readonly uint[] Rgb = { 0xEE5147, 0x187FE8, 0x43BF58, 0xE9AC16, 0xB44CDB, 0x202024 };
        public static bool Contains(int id) { return id >= 0 && id < Count; }
    }
}
