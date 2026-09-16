namespace StairsCrowd.Core
{
    // Stable IDs preserve existing saves and share codes.
    public static class ColorCatalog
    {
        public const int Count = 6;
        public static readonly string[] Names = { "红", "蓝", "绿", "黄", "紫", "黑" };
        public static readonly uint[] Rgb = { 0xD95C5C, 0x347FA3, 0x5C8F70, 0xD9AA45, 0x806FA3, 0x304A50 };
        public static bool Contains(int id) { return id >= 0 && id < Count; }
    }
}
