namespace Netex.TermbitImage;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

public class TermbitImage
{
    private Dictionary<string, string> _metadata = new();
    
    public const byte FormatVersion = 2;

    public byte Version { get; private set; } = FormatVersion;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public TermbitImageFlags Flags { get; private set; }

    public TermbitImageCell[] Cells { get; private set; } = null!;
    
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    public bool IsCompressed
    {
        get => Flags.HasFlag(TermbitImageFlags.Compressed);
        
        set
        {
            if (value) Flags |= TermbitImageFlags.Compressed;
            else Flags &= ~TermbitImageFlags.Compressed;
        }
    }
    
    public TermbitImageCell this[int x, int y]
    {
        get
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                throw new IndexOutOfRangeException("Invalid coordinates.");

            return Cells[(y * Width) + x];
        }

        set
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                throw new IndexOutOfRangeException("Invalid coordinates.");

            Cells[(y * Width) + x] = value;
        }
    }

    public TermbitImage(int width, int height)
    {
        Width = width;
        Height = height;

        DestructiveResize(Width, Height);
    }

    public void DestructiveResize(int width, int height)
    {
        Width = width;
        Height = height;

        Cells = new TermbitImageCell[Width * Height];
    }

    public void NonDestructiveResize(int width, int height)
    {
        Width = width;
        Height = height;

        var newBitmap = new TermbitImageCell[Width * Height];
        Array.Fill(newBitmap, TermbitImageCell.Empty);

        for (var i = 0; i < Cells.Length && i < newBitmap.Length; i++)
            newBitmap[i] = Cells[i];

        Cells = newBitmap;
    }

    public void AddMetadata(string key, string value)
    {
        _metadata[key] = value;

        if (_metadata.Any()) Flags |= TermbitImageFlags.ContainsMetadata;
        else Flags &= ~TermbitImageFlags.ContainsMetadata;
    }

    public void RemoveMetadata(string key)
    {
        _metadata.Remove(key);
        
        if (_metadata.Any()) Flags |= TermbitImageFlags.ContainsMetadata;
        else Flags &= ~TermbitImageFlags.ContainsMetadata;
    }
    
    public void WriteToStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        Serialize(writer);
    }

    private void Serialize(BinaryWriter writer)
    {
        writer.Write(new[] { (byte)'T', (byte)'B', (byte)'T' });
        writer.Write(FormatVersion);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write((int)Flags);
        writer.Write(_metadata.Count);

        using var ms = new MemoryStream();
        foreach (var cell in Cells)
            ms.Write(cell.ToBytes());

        using var metaWriter = new BinaryWriter(ms, Encoding.UTF8, true);
        foreach (var (k, v) in _metadata)
        {
            metaWriter.Write(k);
            metaWriter.Write(v);
        }
        
        ms.Position = 0;
        var decompressedSize = (int)ms.Length;
        var compressedSize = 0;

        Stream outStream = ms;

        if (IsCompressed)
        {
            using var zlibStream = new ZLibStream(
                writer.BaseStream, 
                CompressionLevel.SmallestSize,
                true
            );
            
            ms.CopyTo(zlibStream);
            compressedSize = (int)zlibStream.Length;
            outStream = zlibStream;
        }
        
        writer.Write(decompressedSize);
        writer.Write(compressedSize);
        
        outStream.Position = 0;
        outStream.CopyTo(writer.BaseStream);        
    }

    public static TermbitImage FromStream(Stream stream)
    {
        var reader = new BinaryReader(stream, Encoding.UTF8, true);
        var magic = reader.ReadBytes(3);

        if (!magic.SequenceEqual(new[] { (byte)'T', (byte)'B', (byte)'T' }))
            throw new FormatException("Invalid TBT magic.");
        
        var version = reader.ReadByte();
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var flags = (TermbitImageFlags)reader.ReadInt32();

        int metaCount = 0;
        if (version == 2)
        {
            metaCount = reader.ReadInt32();
        }
        
        var isCompressed = flags.HasFlag(TermbitImageFlags.Compressed);
        
        var decompressedSize = reader.ReadInt32();
        var compressedSize = reader.ReadInt32();
        var outImage = new TermbitImage(width, height);
        outImage.Version = version;
        outImage.Flags = flags;

        Stream imageDataStream;
        
        if (isCompressed)
        {
            var cellData = reader.ReadBytes(compressedSize);
            using var inStream = new MemoryStream(cellData);
            using var zlib = new ZLibStream(inStream, CompressionMode.Decompress);
            imageDataStream = new MemoryStream();
            zlib.CopyTo(imageDataStream);
        }
        else
        {
            var cellData = reader.ReadBytes(decompressedSize);
            imageDataStream = new MemoryStream(cellData);
        }

        imageDataStream.Position = 0;
        
        var cellCount = width * height;
        for (var i = 0; i < cellCount; i++)
        {
            outImage.Cells[i] = TermbitImageCell.FromStream(imageDataStream);
        }

        for (var i = 0; i < metaCount; i++) 
            outImage._metadata[reader.ReadString()] = reader.ReadString();
        
        imageDataStream.Dispose();
        return outImage;
    }

    public static TermbitImage FromFile(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        return FromStream(fs);
    }
}