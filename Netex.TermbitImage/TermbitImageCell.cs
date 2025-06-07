namespace Netex.TermbitImage;

using System.Drawing;
using System.IO;
using System.Text;

public record TermbitImageCell
{
    public static readonly TermbitImageCell Empty = new()
    {
        Background = Color.Black,
        Foreground = Color.White,
        Glyph = ' ',
        Blink = false,
    };
    
    public Color Background { get; set; }
    public Color Foreground { get; set; }
    public char Glyph { get; set; }
    public bool Blink { get; set; }

    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8);
        
        bw.Write(Background.R);
        bw.Write(Background.G);
        bw.Write(Background.B);
        bw.Write(Background.A);
        bw.Write(Foreground.R);
        bw.Write(Foreground.G);
        bw.Write(Foreground.B);
        bw.Write(Foreground.A);
        bw.Write(Glyph);
        bw.Write(Blink);
        
        return ms.ToArray();
    }

    public static TermbitImageCell FromStream(Stream stream)
    {
        using var br = new BinaryReader(stream, Encoding.UTF8, true);
        
        var cell = new TermbitImageCell();
        
        var r = br.ReadByte();
        var g = br.ReadByte();
        var b = br.ReadByte();
        var a = br.ReadByte();
        cell.Background = Color.FromArgb(a, r, g, b);
        
        r = br.ReadByte();
        g = br.ReadByte();
        b = br.ReadByte();
        a = br.ReadByte();
        cell.Foreground = Color.FromArgb(a, r, g, b);
        
        cell.Glyph = br.ReadChar();
        cell.Blink = br.ReadBoolean();

        return cell;
    }
}