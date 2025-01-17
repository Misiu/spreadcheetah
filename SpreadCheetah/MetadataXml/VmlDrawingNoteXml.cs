using SpreadCheetah.CellReferences;
using SpreadCheetah.Helpers;
using System.Buffers;
using System.Runtime.InteropServices;

namespace SpreadCheetah.MetadataXml;

[StructLayout(LayoutKind.Auto)]
internal struct VmlDrawingNoteXml
{
    private static ReadOnlySpan<byte> ShapeStart =>
        """<v:shapetype id="_x0000_t202" coordsize="21600,21600" o:spt="202" path="m,l,21600r21600,l21600,xe">"""u8
        + """<v:stroke joinstyle="miter"/>"""u8
        + """<v:path gradientshapeok="t" o:connecttype="rect"/>"""u8
        + """</v:shapetype>"""u8
        + """<v:shape id="shape_0" type="#_x0000_t202" style="position:absolute;margin-left:57pt;margin-top:"""u8;

    private static ReadOnlySpan<byte> ShapeAfterMarginTop =>
        """pt;width:100.8pt;height:60.6pt;z-index:1;visibility:hidden" fillcolor="infoBackground [80]" strokecolor="none [81]" o:insetmode="auto">"""u8
        + """<v:fill color2="infoBackground [80]"/>"""u8
        + """<v:shadow color="none [81]" obscured="t"/>"""u8
        + """<v:textbox/>"""u8
        + """<x:ClientData ObjectType="Note">"""u8
        + """<x:MoveWithCells/>"""u8
        + """<x:SizeWithCells/>"""u8
        + """<x:AutoFill>False</x:AutoFill>"""u8
        + """<x:Anchor>"""u8;

    private static ReadOnlySpan<byte> ShapeAfterAnchor => "</x:Anchor><x:Row>"u8;
    private static ReadOnlySpan<byte> ShapeAfterRow => "</x:Row><x:Column>"u8;
    private static ReadOnlySpan<byte> ShapeEnd => "</x:Column></x:ClientData></v:shape>"u8;

    private readonly SingleCellRelativeReference _reference;
    private Element _next;

    public VmlDrawingNoteXml(SingleCellRelativeReference reference)
    {
        _reference = reference;
    }

    public bool TryWrite(Span<byte> bytes, out int bytesWritten)
    {
        bytesWritten = 0;

        if (_next == Element.ShapeStart && !Advance(ShapeStart.TryCopyTo(bytes, ref bytesWritten))) return false;
        if (_next == Element.MarginTop && !Advance(TryWriteMarginTop(bytes, ref bytesWritten))) return false;
        if (_next == Element.ShapeAfterMarginTop && !Advance(ShapeAfterMarginTop.TryCopyTo(bytes, ref bytesWritten))) return false;
        if (_next == Element.Anchor && !Advance(TryWriteAnchor(bytes, ref bytesWritten))) return false;
        if (_next == Element.ShapeEnd && !Advance(TryWriteShapeEnd(bytes, ref bytesWritten))) return false;

        return true;
    }

    private readonly bool TryWriteMarginTop(Span<byte> bytes, ref int bytesWritten)
    {
        var row = _reference.Row;
        var marginTop = row == 1 ? 1.2 : row * 14.4 - 20.4;
        return SpanHelper.TryWrite(marginTop, bytes, ref bytesWritten, new StandardFormat('F', 1));
    }

    /// <summary>
    /// From
    /// [0] Left column
    /// [1] Left column offset
    /// [2] Left row
    /// [3] Left row offset
    /// To
    /// [4] Right column
    /// [5] Right column offset
    /// [6] Right row
    /// [7] Right row offset
    /// </summary>
    private readonly bool TryWriteAnchor(Span<byte> bytes, ref int bytesWritten)
    {
        var span = bytes.Slice(bytesWritten);
        var written = 0;
        var col = _reference.Column;
        var row = _reference.Row;

        if (!SpanHelper.TryWrite(col, span, ref written)) return false;

        if (row <= 1)
        {
            if (!",12,0,1,"u8.TryCopyTo(span, ref written)) return false;
            if (!SpanHelper.TryWrite(col + 2, span, ref written)) return false;
            if (!",18,4,5"u8.TryCopyTo(span, ref written)) return false;
        }
        else
        {
            if (!",12,"u8.TryCopyTo(span, ref written)) return false;
            if (!SpanHelper.TryWrite(row - 2, span, ref written)) return false;
            if (!",11,"u8.TryCopyTo(span, ref written)) return false;
            if (!SpanHelper.TryWrite(col + 2, span, ref written)) return false;
            if (!",18,"u8.TryCopyTo(span, ref written)) return false;
            if (!SpanHelper.TryWrite(row + 2, span, ref written)) return false;
            if (!",15"u8.TryCopyTo(span, ref written)) return false;
        }

        bytesWritten += written;
        return true;
    }

    private readonly bool TryWriteShapeEnd(Span<byte> bytes, ref int bytesWritten)
    {
        var span = bytes.Slice(bytesWritten);
        var written = 0;
        var col = _reference.Column;
        var row = _reference.Row;

        if (!ShapeAfterAnchor.TryCopyTo(span, ref written)) return false;
        if (!SpanHelper.TryWrite(row - 1, span, ref written)) return false;
        if (!ShapeAfterRow.TryCopyTo(span, ref written)) return false;
        if (!SpanHelper.TryWrite(col - 1, span, ref written)) return false;
        if (!ShapeEnd.TryCopyTo(span, ref written)) return false;

        bytesWritten += written;
        return true;
    }

    private bool Advance(bool success)
    {
        if (success)
            ++_next;

        return success;
    }

    private enum Element
    {
        ShapeStart,
        MarginTop,
        ShapeAfterMarginTop,
        Anchor,
        ShapeEnd,
        Done
    }
}
